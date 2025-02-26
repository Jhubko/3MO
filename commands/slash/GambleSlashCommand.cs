using Discord_Bot;
using DSharpPlus.Entities;
using DSharpPlus.SlashCommands;

public class GambleCommand : ApplicationCommandModule
{
    [SlashCommand("gamble", "Let's go gambling!")]
    public async Task Gamble(InteractionContext ctx, [Option("amount", "Amount of points to gamble (number, %, or 'all')")] string amountInput)
    {
        ulong userId = ctx.User.Id;
        int currentPoints = await Program.voicePointsManager.GetUserPoints(userId);
        int amountToGamble = GambleUtils.ParseGambleAmount(amountInput, currentPoints);
        var checkAmout = GambleUtils.CheckGambleAmout(amountToGamble, currentPoints);

        if (!checkAmout.isProperValue)
        {
            await ctx.CreateResponseAsync(checkAmout.errorMessage);
            return;
        }

        Random random = new Random();
        bool win = random.Next(2) == 0;

        if (win)
        {
            currentPoints += amountToGamble;
            await ctx.CreateResponseAsync(new DiscordEmbedBuilder
            {
                Title = $"🎉  Pogchamp!  🎉",
                Description = $"{ctx.User.Mention} postawiłeś: {amountInput}  i  Wygrałeś: **{2 * amountToGamble}** punktów.\nMasz teraz: **{currentPoints}** punktów.",
                Color = DiscordColor.Green
            });
            await StatsHandler.IncreaseStats(userId, "GambleWins");
            await StatsHandler.IncreaseStats(userId, "WonPoints", amountToGamble);
        }
        else
        {
            currentPoints -= amountToGamble;
            await ctx.CreateResponseAsync(new DiscordEmbedBuilder
            {
                Title = $":joy:  Yikes  :joy: ",
                Description = $"{ctx.User.Mention} przegrałeś: **{amountToGamble}** punktów XD.\nZostało Ci: **{currentPoints}** punktów.",
                Color = DiscordColor.Red
            });
            await StatsHandler.IncreaseStats(userId, "GambleLosses");
            await StatsHandler.IncreaseStats(userId, "LostPoints", amountToGamble);
        }

        Program.voicePointsManager.SaveUserPoints(userId, currentPoints);
    }
}
