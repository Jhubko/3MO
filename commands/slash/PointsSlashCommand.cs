using DSharpPlus;
using DSharpPlus.Entities;
using DSharpPlus.SlashCommands;
using System.Text;

namespace Discord_Bot.commands.slash
{
    public class PointsSlashCommands : ApplicationCommandModule
    {
        [SlashCommand("points", "Display your voice points.")]
        public async Task PointsCommand(InteractionContext ctx)
        {
            ulong userId = ctx.User.Id;

            int points = await Program.voicePointsManager.GetUserPoints(userId);

            var name = new StringBuilder(ctx.User.Username);
            name[0] = char.ToUpper(name[0]);

            var embed = new DiscordEmbedBuilder
            {
                Title = $"{name} masz **{points}** punktów!",
                Color = DiscordColor.Green
            };
            await ctx.CreateResponseAsync(InteractionResponseType.ChannelMessageWithSource, new DiscordInteractionResponseBuilder().AddEmbed(embed));
        }
    }
}
