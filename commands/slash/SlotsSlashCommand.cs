using Discord_Bot;
using Discord_Bot.Config;
using DSharpPlus;
using DSharpPlus.Entities;
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

    [SlashCommand("checkSlotsChances", "Check the chances of winning the slots game!")]
    public async Task CheckSlotsChances(InteractionContext ctx)
    {
        var results = new int[2];
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
        ulong userId = ctx.User.Id;
        var userData = await jsonReader.ReadJson<UserConfig>($"{folderPath}\\{userId}.json");
        uint currentPoints = userData.Points;

        if (currentPoints < BetAmount)
        {
            await ctx.CreateResponseAsync($"Nie masz wystarczającej ilości punktów, aby zagrać. Potrzebujesz {BetAmount} punktów.", true);
            return;
        }

        if (CaptchaHandler.MustCompleteCaptcha(userId))
        {
            await ctx.CreateResponseAsync("❌ Musisz ukończyć CAPTCHA przed dalszą grą.", true);
            return;
        }

        if (CaptchaHandler.CheckCaptchaCooldown(userId))
        {
            TimeSpan remaining = CaptchaHandler.GetRemainingCooldownTime(userId);
            await ctx.CreateResponseAsync($"❌ Musisz poczekać {remaining.Seconds} sekund przed kolejną próbą.", true);
            return;
        }

        Random random = new Random();
        bool requiresCaptcha = random.Next(1, 101) <= 5;

        if (requiresCaptcha)
        {
            int num1 = random.Next(1, 10);
            int num2 = random.Next(1, 10);
            int correctAnswer = num1 + num2;
            string filePath = CaptchaHandler.GenerateCaptchaImage(num1, num2);

            using (var fileStream = new FileStream(filePath, FileMode.Open))
            {
                await ctx.CreateResponseAsync(new DiscordInteractionResponseBuilder()
                        .WithContent("🛑 Przed grą rozwiąż CAPTCHA 🛑")
                        .AddFile("captcha.png", fileStream).AsEphemeral());
                CaptchaHandler.SetCaptchaCooldown(userId, 60, true);
            }

            File.Delete(filePath);

            if (!await CaptchaHandler.VerifyCaptchaAnswer(ctx, correctAnswer))
            {
                await ctx.DeleteResponseAsync();
                CaptchaHandler.SetCaptchaCooldown(userId, 60, false);
                return;
            }
        }

        currentPoints -= BetAmount;
        await jsonWriter.UpdateUserConfig(userId, "Points", currentPoints);

        var serverConfig = await jsonReader.ReadJson<ServerConfig>($"{Program.serverConfigPath}\\{ctx.Guild.Id}.json");
        var slotsPool = serverConfig.SlotsPool;
        slotsPool += BetAmount - 2;
        await jsonWriter.UpdateServerConfig(ctx.Guild.Id, "SlotsPool", slotsPool);

        var reels = SpinReels();
        bool isWin = CheckWin(reels);

        if (isWin)
        {
            uint winAmount = serverConfig.SlotsPool;
            currentPoints += winAmount;
            await StatsHandler.IncreaseStats(userId, "SlotsWins");
            await StatsHandler.IncreaseStats(userId, "WonPoints", winAmount);
            await jsonWriter.UpdateServerConfig(ctx.Guild.Id, "SlotsPool", DefaultPool);
            await jsonWriter.UpdateUserConfig(userId, "Points", currentPoints);
        }
        else
        {
            await StatsHandler.IncreaseStats(userId, "SlotsLosses");
            await StatsHandler.IncreaseStats(userId, "LostPoints", BetAmount);
        }

        var embed = new DiscordEmbedBuilder
        {
            Title = "🎰 Slots 🎰",
            Description =
                  $"{reels[0, 0]} | {reels[0, 1]} | {reels[0, 2]}\n" +
                  $"{reels[1, 0]} | {reels[1, 1]} | {reels[1, 2]}\n" +
                  $"{reels[2, 0]} | {reels[2, 1]} | {reels[2, 2]}\n\n" +
                  (isWin ? $"🎉 Wygrałeś! Masz teraz {currentPoints} punktów. Pula zresetowana do {DefaultPool}" : $"Przegrałeś. Masz teraz {currentPoints} punktów. W puli {slotsPool}"),
            Color = isWin ? DiscordColor.Green : DiscordColor.Red
        };

        try
        {
            await ctx.CreateResponseAsync(new DiscordInteractionResponseBuilder().AddEmbed(embed));
        }
        catch
        {
            await ctx.DeleteResponseAsync();
            await ctx.FollowUpAsync(new DiscordFollowupMessageBuilder().AddEmbed(embed));
        }
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
        for (int i = 0; i < 3; i++)
        {
            if (reels[i, 0] == reels[i, 1] && reels[i, 1] == reels[i, 2]) return true;
            if (reels[0, i] == reels[1, i] && reels[1, i] == reels[2, i]) return true;
        }
        return (reels[0, 0] == reels[1, 1] && reels[1, 1] == reels[2, 2]) ||
               (reels[0, 2] == reels[1, 1] && reels[1, 1] == reels[2, 0]);
    }
}
