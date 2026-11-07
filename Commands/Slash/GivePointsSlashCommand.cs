using DSharpPlus.Entities;
using DSharpPlus.SlashCommands;
namespace Discord_Bot.Commands.Slash
{
    public class GivePointsCommand : ApplicationCommandModule
    {
        [SlashCommand("givepoints", "Give points to another player.")]
        public async Task GivePoints(InteractionContext ctx,
                                               [Option("recipient", "The player you want to give points to")] DiscordUser recipient,
                                               [Option("amount", "Number of points to transfer")] string amount)
        {
            ulong senderId = ctx.User.Id;
            ulong recipientId = recipient.Id;

            if (senderId == recipientId)
            {
                var errorEmbed = new DiscordEmbedBuilder
                {
                    Title = "❌ Transfer Error ❌",
                    Description = $"Cannot transfer points to yourself!",
                    Color = DiscordColor.Red
                };

                await ctx.CreateResponseAsync(embed: errorEmbed);
                return;
            }

            uint senderPoints = await Program.voicePointsManager.GetUserPoints(ctx.Guild.Id, senderId);
            uint recipientPoints = await Program.voicePointsManager.GetUserPoints(ctx.Guild.Id, recipientId);
            uint amountToGive = GambleUtils.ParseGambleAmount(amount, senderPoints);
            var (isProperValue, errorMessage) = GambleUtils.CheckGambleAmout(amountToGive, senderPoints);

            if (!isProperValue)
            {
                var errorEmbed = new DiscordEmbedBuilder
                {
                    Title = "❌ Transfer Error ❌",
                    Description = $"{errorMessage}",
                    Color = DiscordColor.Red
                };

                await ctx.CreateResponseAsync(embed: errorEmbed, true);
                return;
            }

            senderPoints -= amountToGive;
            recipientPoints += amountToGive;
            await Program.voicePointsManager.SaveUserPoints(ctx.Guild.Id, senderId, senderPoints);
            await Program.voicePointsManager.SaveUserPoints(ctx.Guild.Id, recipientId, recipientPoints);
            await StatsHandler.IncreaseStats(ctx.Guild.Id, senderId, "GivedPoints", amountToGive);
            await StatsHandler.IncreaseStats(ctx.Guild.Id, recipientId, "ReceivedPoints", amountToGive);

            var embedTransfer = new DiscordEmbedBuilder
            {
                Title = "💸 Points transfer 💸",
                Description = $"{ctx.User.Mention} gave **{amount}** points to {recipient.Mention}!",
                Color = DiscordColor.Gold
            };

            await ctx.CreateResponseAsync(embed: embedTransfer);
        }
    }
}