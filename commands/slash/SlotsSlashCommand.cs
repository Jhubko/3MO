using Discord_Bot;
using Discord_Bot.Config;
using DSharpPlus;
using DSharpPlus.Entities;
using DSharpPlus.Interactivity.Extensions;
using DSharpPlus.SlashCommands;

public class SlotsCommand : ApplicationCommandModule
{
    private static readonly string[] Symbols = {"🍎", "🍏", "🍐", "🍊", "🍋", "🍌", "🍉", "🍇", "🍓", "🫐",
    "🍒", "🍑", "🥭", "🍍", "🥝", "🍈", "🥥", "🌰", "🥑", "🍆",
    "🌽", "🥕", "🧄", "🧅", "🥔", "🥒", "🌶️", "🫑", "🍠", "🥜"};
    private const int BetAmount = 10;
    private const int DefaultPool = 2000; 
    private static IJsonHandler jsonReader = new JSONReader();
    private JSONWriter jsonWriter = new JSONWriter(jsonReader, "config.json", Program.serverConfigPath);
    private readonly string folderPath = $"{Program.globalConfig.ConfigPath}\\user_points";
    private static Dictionary<ulong, DateTime> captchaCooldowns = new();

    [SlashCommand("checkSlotsChances", "Check the chances of winning the slots game!")]
    public async Task CheckSlotsChances(InteractionContext ctx)
    {
        // Does 1 000 000 spins and calculates the win rate
        var results = new int[2]; // [wins, total]
        for (int i = 0; i < 1000000; i++)
        {
            var reels = SpinReels();
            if (CheckWin(reels))
            {
                results[0]++;
            }
            results[1]++;
        }
        var winRate = (double)results[0] / results[1] * 100;
        var embed = new DiscordEmbedBuilder
        {
            Title = "🎰 Slots Chances 🎰",
            Description = $"Szansa na wygraną w grze w automaty wynosi {winRate:F5}%",
            Color = DiscordColor.Gold
        };

        await ctx.CreateResponseAsync(InteractionResponseType.ChannelMessageWithSource, new DiscordInteractionResponseBuilder().AddEmbed(embed));
    }

    [SlashCommand("slots", "Play the slots game!")]
    public async Task Slots(InteractionContext ctx)
    {
        await ctx.DeferAsync();
        ulong userId = ctx.User.Id;
        var userData = await jsonReader.ReadJson<UserConfig>($"{folderPath}\\{userId}.json");
        int currentPoints = int.Parse(userData.Points);

        if (currentPoints < BetAmount)
        {
            await ctx.EditResponseAsync(new DiscordWebhookBuilder().WithContent($"Nie masz wystarczającej ilości punktów, aby zagrać. Potrzebujesz {BetAmount} punktów."));
            return;
        }

        if (captchaCooldowns.TryGetValue(ctx.User.Id, out DateTime cooldownEnd) && cooldownEnd > DateTime.UtcNow)
        {
            TimeSpan remaining = cooldownEnd - DateTime.UtcNow;
            await ctx.EditResponseAsync(new DiscordWebhookBuilder().WithContent($"❌ Musisz poczekać {remaining.Seconds} sekund przed kolejną próbą."));
            return;
        }

        currentPoints -= BetAmount;
        await jsonWriter.UpdateUserConfig(userId, "Points", currentPoints.ToString());

        var serverConfig = await jsonReader.ReadJson<ServerConfig>($"{Program.serverConfigPath}\\{ctx.Guild.Id}.json");
        var slotsPool = int.Parse(serverConfig.SlotsPool);
        slotsPool += BetAmount - 2;
        await jsonWriter.UpdateServerConfig(ctx.Guild.Id, "SlotsPool", slotsPool.ToString());

        Random random = new Random();
        int captchaChance = random.Next(1, 101);
        if (captchaChance <= 5)
        {
            Random rnd = new Random();
            int num1 = rnd.Next(1, 10);
            int num2 = rnd.Next(1, 10);
            int correctAnswer = num1 + num2;

            await ctx.EditResponseAsync(new DiscordWebhookBuilder().WithContent($"Zanim zagrasz, rozwiąż zagadkę: {num1} + {num2} = ?"));
            var interactivity = ctx.Client.GetInteractivity();
            var response = await interactivity.WaitForMessageAsync(m => m.Author.Id == ctx.User.Id, TimeSpan.FromSeconds(15));

            if (response.TimedOut || response.Result.Content != correctAnswer.ToString())
            {
                await ctx.EditResponseAsync(new DiscordWebhookBuilder().WithContent("❌ Zła odpowiedź! Musisz poczekać 60 sekund."));
                captchaCooldowns[ctx.User.Id] = DateTime.UtcNow.AddSeconds(60);
                return;
            }

            await ctx.EditResponseAsync(new DiscordWebhookBuilder().WithContent("✅ Poprawna odpowiedź! Możesz grać."));
        }

        var reels = SpinReels();

        bool isWin = CheckWin(reels);
        if (isWin)
        {
            int winAmount = int.Parse(serverConfig.SlotsPool); // Win the entire pool
            currentPoints += winAmount;
            await StatsHandler.IncreaseStats(userId, "SlotsWins");
            await StatsHandler.IncreaseStats(userId, "WonPoints", currentPoints);
            await jsonWriter.UpdateServerConfig(ctx.Guild.Id, "SlotsPool", DefaultPool.ToString()); // Reset the pool
            await jsonWriter.UpdateUserConfig(userId, "Points", currentPoints.ToString());
        }
        else
        {
            await StatsHandler.IncreaseStats(userId, "SlotsLosses");
            await StatsHandler.IncreaseStats(userId, "LostPoints", BetAmount);
        }

        var embed = new DiscordEmbedBuilder
        {
            Title = "🎰 Slots 🎰",
            Description = $"{reels[0, 0]} | {reels[0, 1]} | {reels[0, 2]}\n" +
                          $"{reels[1, 0]} | {reels[1, 1]} | {reels[1, 2]}\n" +
                          $"{reels[2, 0]} | {reels[2, 1]} | {reels[2, 2]}\n\n" +
                          (isWin ? $"🎉 Wygrałeś! Masz teraz {currentPoints} punktów. Pula zresetowana do {DefaultPool}" : $"Przegrałeś. Masz teraz {currentPoints} punktów. W puli {slotsPool}"),
            Color = isWin ? DiscordColor.Green : DiscordColor.Red
        };

        await ctx.EditResponseAsync(new DiscordWebhookBuilder().AddEmbed(embed));
    }



    private string[,] SpinReels()
    {
    var reels = new string[3, 3];
    var random = new Random();
    for (int i = 0; i < 3; i++)
    {
        for (int j = 0; j < 3; j++)
        {
            reels[i, j] = Symbols[random.Next(Symbols.Length)];
        }
    }
    return reels;
    }

    private bool CheckWin(string[,] reels)
    {
        // Check rows
        for (int i = 0; i < 3; i++)
        {
            if (reels[i, 0] == reels[i, 1] && reels[i, 1] == reels[i, 2])
            {
                return true;
            }
        }

        // Check columns
        for (int i = 0; i < 3; i++)
        {
            if (reels[0, i] == reels[1, i] && reels[1, i] == reels[2, i])
            {
                return true;
            }
        }

        // Check diagonals
        if (reels[0, 0] == reels[1, 1] && reels[1, 1] == reels[2, 2])
        {
            return true;
        }
        if (reels[0, 2] == reels[1, 1] && reels[1, 1] == reels[2, 0])
        {
            return true;
        }

        return false;
    }
}