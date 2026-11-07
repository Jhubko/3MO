using DSharpPlus.Entities;
using DSharpPlus.SlashCommands;
namespace Discord_Bot.Commands.Slash
{
    public class GambleCommand : ApplicationCommandModule
    {
        [SlashCommand("gamble", "Let's go gambling!")]
        public async Task Gamble(InteractionContext ctx, [Option("amount", "Amount of points to gamble (number, %, or 'all')")] string amountInput)
        {
            ulong userId = ctx.User.Id;
            uint currentPoints = await Program.voicePointsManager.GetUserPoints(ctx.Guild.Id, userId);
            uint amountToGamble = GambleUtils.ParseGambleAmount(amountInput, currentPoints);
            var (isProperValue, errorMessage) = GambleUtils.CheckGambleAmout(amountToGamble, currentPoints);

            if (!isProperValue)
            {
                await ctx.CreateResponseAsync(errorMessage, true);
                return;
            }

            Random random = new();
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
                await StatsHandler.IncreaseStats(ctx.Guild.Id, userId, "GambleWins");
                await StatsHandler.IncreaseStats(ctx.Guild.Id, userId, "WonPoints", amountToGamble);
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
                await StatsHandler.IncreaseStats(ctx.Guild.Id, userId, "GambleLosses");
                await StatsHandler.IncreaseStats(ctx.Guild.Id, userId, "LostPoints", amountToGamble);
            }

            await Program.voicePointsManager.SaveUserPoints(ctx.Guild.Id, userId, currentPoints);
        }
    }
}