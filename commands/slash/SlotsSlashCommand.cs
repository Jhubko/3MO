using Discord_Bot;
using Discord_Bot.Config;
using DSharpPlus;
using DSharpPlus.Entities;
using DSharpPlus.SlashCommands;

public class SlotsCommand : ApplicationCommandModule
{
    private static readonly string[] Symbols = { "🍒", "🍋", "🫐", "🍇", "🍓", "🍍", "🍋‍🟩", "🍑", "🥥", "🥝"};
    private const int BetAmount = 10;
    private const int DefaultPool = 2000; 
    private static IJsonHandler jsonReader = new JSONReader();
    private JSONWriter jsonWriter = new JSONWriter(jsonReader, "config.json", Program.serverConfigPath);
    private readonly string folderPath = $"{Program.globalConfig.ConfigPath}\\user_points";

    [SlashCommand("slots", "Play the slots game!")]
    public async Task Slots(InteractionContext ctx)
    {
        ulong userId = ctx.User.Id;
        var userData = await jsonReader.ReadJson<UserConfig>($"{folderPath}\\{userId}.json");
        int currentPoints = int.Parse(userData.Points);

        if (currentPoints < BetAmount)
        {
            await ctx.CreateResponseAsync($"Nie masz wystarczającej ilości punktów, aby zagrać. Potrzebujesz {BetAmount} punktów.", true);
            return;
        }

        // Deduct the bet amount
        currentPoints -= BetAmount;
        await jsonWriter.UpdateUserConfig(userId, "Points", currentPoints.ToString());

        // Update the slots pool
        var serverConfig = await jsonReader.ReadJson<ServerConfig>($"{Program.serverConfigPath}\\{ctx.Guild.Id}.json");
        var slotsPool = int.Parse(serverConfig.SlotsPool);
        slotsPool += BetAmount;
        await jsonWriter.UpdateServerConfig(ctx.Guild.Id, "SlotsPool", slotsPool.ToString());

        // Spin the reels
        var reels = new string[3, 3];
        var random = new Random();
        for (int i = 0; i < 3; i++)
        {
            for (int j = 0; j < 3; j++)
            {
                reels[i, j] = Symbols[random.Next(Symbols.Length)];
            }
        }

        // Check for wins
        bool isWin = CheckWin(reels);
        if (isWin)
        {
            int winAmount = int.Parse(serverConfig.SlotsPool); // Win the entire pool
            currentPoints += winAmount;
            await jsonWriter.UpdateServerConfig(ctx.Guild.Id, "SlotsPool", DefaultPool.ToString()); // Reset the pool
            await jsonWriter.UpdateUserConfig(userId, "Points", currentPoints.ToString());
        }

        // Create the response message
        var embed = new DiscordEmbedBuilder
        {
            Title = "🎰 Slots 🎰",
            Description = $"{reels[0, 0]} | {reels[0, 1]} | {reels[0, 2]}\n" +
                          $"{reels[1, 0]} | {reels[1, 1]} | {reels[1, 2]}\n" +
                          $"{reels[2, 0]} | {reels[2, 1]} | {reels[2, 2]}\n\n" +
                          (isWin ? $"🎉 Wygrałeś! Masz teraz {currentPoints} punktów. Pula zresetowana do {DefaultPool}" : $"Przegrałeś. Masz teraz {currentPoints} punktów. W puli {slotsPool}"),
            Color = isWin ? DiscordColor.Green : DiscordColor.Red
        };

        await ctx.CreateResponseAsync(InteractionResponseType.ChannelMessageWithSource, new DiscordInteractionResponseBuilder().AddEmbed(embed));
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