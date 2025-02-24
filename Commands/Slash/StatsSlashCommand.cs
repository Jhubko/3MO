using Discord_Bot.Config;
using DSharpPlus;
using DSharpPlus.Entities;
using DSharpPlus.SlashCommands;
using System.Reflection;
using System.Text;

namespace Discord_Bot.commands.slash
{
    public class StatsSlashCommands : ApplicationCommandModule
    {
        [SlashCommand("points", "Display your voice points or the points of a specified user.")]
        public async Task PointsCommand(InteractionContext ctx, [Option("user", "The user to check points for")] DiscordUser user = null)
        {
            ulong userId = user?.Id ?? ctx.User.Id;
            int points = await Program.voicePointsManager.GetUserPoints(userId);
            var member = await ctx.Guild.GetMemberAsync(userId);

            var embed = new DiscordEmbedBuilder
            {
                Title = $"{GambleUtils.CapitalizeUserFirstLetter(member.DisplayName)} ma **{points}** punktów!",
                Color = DiscordColor.Green
            };
            await ctx.CreateResponseAsync(InteractionResponseType.ChannelMessageWithSource, new DiscordInteractionResponseBuilder().AddEmbed(embed));
        }

        [SlashCommand("highscore", "Display the top 10 scores.")]
        public async Task HighscoreCommand(InteractionContext ctx)
        {
            var topUsers = await Program.voicePointsManager.GetTopUsers(10);

            var embed = new DiscordEmbedBuilder
            {
                Title = "Top 10 Scores",
                Color = DiscordColor.Gold
            };

            var highscoreList = new StringBuilder();
            foreach (var user in topUsers)
            {
                var discordMember = await ctx.Guild.GetMemberAsync(user.UserId);
                highscoreList.AppendLine($"{GambleUtils.CapitalizeUserFirstLetter(discordMember.DisplayName)}: {user.Points}");
            }

            embed.AddField("Highscores", highscoreList.ToString());

            await ctx.CreateResponseAsync(InteractionResponseType.ChannelMessageWithSource, new DiscordInteractionResponseBuilder().AddEmbed(embed));
        }

        [SlashCommand("stats", "Show Statistics.")]
        public async Task StatsCommand(InteractionContext ctx, [Option("user", "The user to check points for")] DiscordUser user = null)
        {
            ulong userId = user?.Id ?? ctx.User.Id;
            UserConfig userStast = await StatsHandler.LoadUserStats(userId);
            var member = await ctx.Guild.GetMemberAsync(userId);
            string desc = string.Empty;

            PropertyInfo[] properties = typeof(UserConfig).GetProperties();

            foreach (PropertyInfo stats in properties)
            {
                desc += "------------------------\n";
                desc += $"**{StatsHandler.AddSpacesBeforeCapitalLetters(stats.Name)}**: {stats.GetValue(userStast)}\n";
            }

            var embed = new DiscordEmbedBuilder
            {
                Title = $"{GambleUtils.CapitalizeUserFirstLetter(member.DisplayName)} Stats!",
                Description = desc,
                Color = DiscordColor.Blurple
            };
            await ctx.CreateResponseAsync(InteractionResponseType.ChannelMessageWithSource, new DiscordInteractionResponseBuilder().AddEmbed(embed));
        }
    }
}