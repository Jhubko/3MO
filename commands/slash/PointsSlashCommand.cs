using DSharpPlus;
using DSharpPlus.Entities;
using DSharpPlus.SlashCommands;

namespace Discord_Bot.commands.slash
{
    public class PointsSlashCommands : ApplicationCommandModule
    {
        [SlashCommand("points", "Display your voice points.")]
        public async Task PointsCommand(InteractionContext ctx)
        {
            ulong userId = ctx.User.Id;
            int points = await Program.voicePointsManager.GetUserPoints(userId);

            var embed = new DiscordEmbedBuilder
            {
                Title = $"{GambleUtils.CapitalizeUserFirstLetter(ctx.User.Username)} masz **{points}** punktów!",
                Color = DiscordColor.Green
            };
            await ctx.CreateResponseAsync(InteractionResponseType.ChannelMessageWithSource, new DiscordInteractionResponseBuilder().AddEmbed(embed));
        }
    }
}
