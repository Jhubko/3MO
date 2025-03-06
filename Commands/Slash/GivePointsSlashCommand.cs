using Discord_Bot;
using DSharpPlus.Entities;
using DSharpPlus.SlashCommands;

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

        int senderPoints = await Program.voicePointsManager.GetUserPoints(senderId);
        int recipientPoints = await Program.voicePointsManager.GetUserPoints(recipientId);
        int amountToGive = GambleUtils.ParseGambleAmount(amount, senderPoints);
        var checkAmout = GambleUtils.CheckGambleAmout(amountToGive, senderPoints);

        if (!checkAmout.isProperValue)
        {
            var errorEmbed = new DiscordEmbedBuilder
            {
                Title = "❌ Transfer Error ❌",
                Description = $"{checkAmout.errorMessage}",
                Color = DiscordColor.Red
            };

            await ctx.CreateResponseAsync(embed: errorEmbed);
            return;
        }

        senderPoints -= amountToGive;
        recipientPoints += amountToGive;
        Program.voicePointsManager.SaveUserPoints(senderId, senderPoints);
        Program.voicePointsManager.SaveUserPoints(recipientId, recipientPoints);


        var embedTransfer = new DiscordEmbedBuilder
        {
            Title = "💸 Points transfer 💸",
            Description = $"{ctx.User.Mention} gave **{amount}** points to {recipient.Mention}!",
            Color = DiscordColor.Gold
        };

        await ctx.CreateResponseAsync(embed: embedTransfer);
    }
}
