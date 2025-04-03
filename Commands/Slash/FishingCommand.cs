using Discord_Bot;
using Discord_Bot.Config;
using Discord_Bot.other;
using DSharpPlus;
using DSharpPlus.Entities;
using DSharpPlus.Interactivity.Extensions;
using DSharpPlus.SlashCommands;
using System.ComponentModel;
using System.Text;

public class FishingCommand : ApplicationCommandModule
{
    private static readonly Random random = new();
    private static readonly Dictionary<ulong, bool> fishingUsers = new();
    private static IJsonHandler jsonReader = new JSONReader();

    private readonly int fishingPrice = 10;
    private readonly string folderPath = $"{Program.globalConfig.ConfigPath}\\user_points";

    private JSONWriter jsonWriter = new JSONWriter(jsonReader, "config.json", Program.serverConfigPath);
    private readonly VoicePointsManager pointsManager = Program.voicePointsManager;
    private readonly InventoryManager inventoryManager = new InventoryManager();

    [SlashCommand("fishing", "Cast your rod and try to catch a fish!")]
    public async Task Fish(InteractionContext ctx)
    {
        var fishList = await inventoryManager.LoadFishDataAsync(ctx.Guild.Id);
        DiscordMessage message;
        if (fishingUsers.ContainsKey(ctx.User.Id))
        {
            await ctx.CreateResponseAsync("🎣 Już łowisz rybę! Poczekaj na branie.", true);
            return;
        }
        if (fishList.Count == 0)
        {
            await ctx.CreateResponseAsync($"W wodzie nie ma ryb!", true);
            return;
        }

        fishingUsers[ctx.User.Id] = true;
        int userPoints = await pointsManager.GetUserPoints(ctx.User.Id);

        if (userPoints < fishingPrice)
        {
            await ctx.CreateResponseAsync($"Nie masz wystarczającej liczby punktów, aby łowić ryby!", true);
            return;
        }

        userPoints -= fishingPrice;
        pointsManager.SaveUserPoints(ctx.User.Id, userPoints);

        await ctx.CreateResponseAsync($"🎣{ctx.User.Mention} Zarzuciłeś wędkę... czekaj na branie!");

        if (random.NextDouble() < 0.2)
        {
            await Task.Delay(600000);
            await ctx.FollowUpAsync(new DiscordFollowupMessageBuilder().WithContent($"❌ {ctx.User.Mention} Nic nie złapało się na haczyk. Spróbuj ponownie!"));
            fishingUsers.Remove(ctx.User.Id);
            return;
        }

        await Task.Delay(random.Next(30000, 600000));
        var fish = fishList[random.Next(fishList.Count)];
        double weight = Math.Round(random.NextDouble() * (fish.MaxWeight - fish.MinWeight) + fish.MinWeight, 2);
        int difficulty = Math.Min(20, Math.Max(1, (int)Math.Ceiling(Math.Log10(weight) * 10)));
        int reactionTime = Math.Max(1, 15 - difficulty);

        message = await ctx.FollowUpAsync(new DiscordFollowupMessageBuilder().WithContent($"🐟 {ctx.User.Mention} 🐟 Branie! Kliknij 🎣 by ją złowić!"));
        await message.CreateReactionAsync(DiscordEmoji.FromName(ctx.Client, ":fishing_pole_and_fish:"));

        int catchAttempts = 0;
        var interactivity = ctx.Client.GetInteractivity();

        while (difficulty - catchAttempts > 0)
        {
            var reaction = await interactivity.WaitForReactionAsync(
                x => x.Emoji == DiscordEmoji.FromName(ctx.Client, ":fishing_pole_and_fish:") && x.User.Id == ctx.User.Id,
                TimeSpan.FromSeconds(reactionTime));

            if (reaction.TimedOut)
            {
                await StatsHandler.IncreaseStats(ctx.User.Id, "FishBreakoffs");
                await ctx.FollowUpAsync(new DiscordFollowupMessageBuilder().WithContent($"❌ {ctx.User.Mention} Ryba uciekła!"));
                fishingUsers.Remove(ctx.User.Id);
                return;
            }

            catchAttempts++;
            if (difficulty - catchAttempts != 0)
            {
                var oldMessage = message;
                message = await ctx.FollowUpAsync(new DiscordFollowupMessageBuilder().WithContent($"🐟 {ctx.User.Mention} 🐟 Ryba się wyrywa! Kliknij 🎣 jeszcze raz!"));
                if(message != null)
                    await message.CreateReactionAsync(DiscordEmoji.FromName(ctx.Client, ":fishing_pole_and_fish:"));               
                await oldMessage.DeleteAsync();
            }
        }
        await message.DeleteAsync();
        await ctx.FollowUpAsync(new DiscordFollowupMessageBuilder().WithContent($"✅ Gratulacje {ctx.User.Mention}! Złowiłeś {fish.Name} o wadze {weight}kg!"));
        await inventoryManager.SaveFishToInventory(ctx.User.Id, fish.Name, weight, fish.BasePrice);
        await StatsHandler.IncreaseStats(ctx.User.Id, "FishCaught");
        fishingUsers.Remove(ctx.User.Id);
    }


    [SlashCommand("fish", "List all fish from the database.")]
    public async Task ListFishCommand(InteractionContext ctx)
    {
        await ctx.DeferAsync(true);
        var fishList = await inventoryManager.LoadFishDataAsync(ctx.Guild.Id);

        if (fishList.Count == 0)
        {
            await ctx.CreateResponseAsync("❌ Brak ryb w bazie danych.", true);
            return;
        }

        int pageSize = 10;
        int totalPages = (int)Math.Ceiling((double)fishList.Count / pageSize);

        var embed = new DiscordEmbedBuilder
        {
            Title = "🐟 Lista Ryb 🐟",
            Description = $"```\n{GetFishPage(fishList, 1)}\n```",
            Color = DiscordColor.Blurple
        };

        var selectComponent = new DiscordSelectComponent("pageSelect", "Wybierz stronę",
            Enumerable.Range(1, totalPages).Select(i => new DiscordSelectComponentOption($"Strona {i}", i.ToString())).ToArray(), false);

        var message = new DiscordWebhookBuilder()
            .AddEmbed(embed)
            .AddComponents(selectComponent);

        await ctx.EditResponseAsync(message);
    }

    [SlashCommand("sellfish", "Sell a fish from your inventory.")]
    public async Task SellFish(InteractionContext ctx,
     [Autocomplete(typeof(FishAutocomplete))]
    [Option("name", "Name of the fish to sell or 'all' to sell all fish.")] string fishName,
     [Option("amount", "Weight of the fish to sell ('heaviest', 'lightest', a specific weight, or 'all').")] string? amount = null)
    {
        var userInventory = await inventoryManager.GetUserItems(ctx.User.Id);
        var userData = await jsonReader.ReadJson<UserConfig>($"{folderPath}\\{ctx.User.Id}.json") ?? new UserConfig();

        if (userInventory.Fish == null || userInventory.Fish.Count == 0)
        {
            await ctx.CreateResponseAsync("❌ Nie masz żadnych ryb w ekwipunku.", true);
            return;
        }

        List<FishItem> fishList;

        if (fishName.ToLower() == "all")
        {
            fishList = new List<FishItem>(userInventory.Fish);
        }
        else
        {
            fishList = userInventory.Fish.Where(f => f.Name.Equals(fishName, StringComparison.OrdinalIgnoreCase)).ToList();
            if (!fishList.Any())
            {
                await ctx.CreateResponseAsync($"❌ Nie masz ryby o nazwie {fishName}.", true);
                return;
            }
        }

        int totalPrice = 0;
        int fishSoldCount = 0;

        if (fishName.ToLower() == "all" || amount == "all")
        {
            totalPrice = fishList.Sum(f => f.Price);
            fishSoldCount = fishList.Count;
            userInventory.Fish.RemoveAll(f => fishList.Contains(f));
        }
        else if (amount == "heaviest")
        {
            FishItem heaviestFish = fishList.OrderByDescending(f => f.Weight).First();
            totalPrice = heaviestFish.Price;
            fishSoldCount = 1;
            userInventory.Fish.Remove(heaviestFish);
        }
        else if (amount == "lightest")
        {
            FishItem lightestFish = fishList.OrderBy(f => f.Weight).First();
            totalPrice = lightestFish.Price;
            fishSoldCount = 1;
            userInventory.Fish.Remove(lightestFish);
        }
        else if (double.TryParse(amount, out double weight))
        {
            var fishToSell = fishList.FirstOrDefault(f => Math.Abs(f.Weight - weight) < 0.01);
            if (fishToSell == null)
            {
                await ctx.CreateResponseAsync($"❌ Nie masz {fishName} o wadze {weight} kg.", true);
                return;
            }
            totalPrice = fishToSell.Price;
            fishSoldCount = 1;
            userInventory.Fish.Remove(fishToSell);
        }
        else
        {
            FishItem fishToSell = fishList.First();
            totalPrice = fishToSell.Price;
            fishSoldCount = 1;
            userInventory.Fish.Remove(fishToSell);
        }

        int currentPoints = int.Parse(userData.Points);
        currentPoints += totalPrice;
        userData.Points = currentPoints.ToString();

        await jsonWriter.UpdateUserConfig(ctx.User.Id, "Points", userData.Points);
        await inventoryManager.UpdateUserItems(ctx.User.Id, userInventory);

        await ctx.CreateResponseAsync($"✅ Sprzedano {fishSoldCount} ryb(y) za {totalPrice} punktów!");
    }

    public static string GetFishPage(List<Fish> fishList, int pageNumber, int pageSize = 10)
    {
        var fishListBuilder = new StringBuilder();
        int startIndex = (pageNumber - 1) * pageSize;
        var fishOnPage = fishList.Skip(startIndex).Take(pageSize);

        int maxNameLength = fishList.Max(f => f.Name.Length);
        int weightColumnWidth = 20;

        fishListBuilder.AppendLine(new string('-', maxNameLength + weightColumnWidth + 10));

        foreach (var fish in fishOnPage)
        {
            string name = fish.Name.PadRight(maxNameLength);
            string minWeight = $"{fish.MinWeight:0.##}kg".PadLeft(6);
            string maxWeight = $"{fish.MaxWeight:0.##}kg".PadRight(6);
            string weight = $"⚖️ {minWeight} - {maxWeight}".PadRight(weightColumnWidth);
            string price = $"💰 {fish.BasePrice}";

            fishListBuilder.AppendLine($"{name} | {weight} | {price}");
        }

        return fishListBuilder.ToString();
    }

}
