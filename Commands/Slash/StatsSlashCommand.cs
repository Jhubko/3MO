using Discord_Bot.Config;
using Discord_Bot.other;
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
        [SlashCommand("highscore", "Display the top 10 scores for a specific category.")]
        public async Task HighscoreCommand(InteractionContext ctx, [Option("category", "The category of statistics to display")][Autocomplete(typeof(HighscoreAutocomplete))] string category = "Points")
        {
            {
                string[] validCategories = typeof(UserConfig).GetProperties()
                    .Select(p => p.Name)
                    .ToArray();

                if (!validCategories.Contains(category))
                {
                    await ctx.CreateResponseAsync($"Nieznana kategoria. Dostępne: {string.Join(", ", validCategories)}");
                    return;
                }

                var topUsers = await StatsHandler.GetTopUsersByCategory(10, category);

                var embed = new DiscordEmbedBuilder
                {
                    Title = $"Top 10 - {StatsHandler.AddSpacesBeforeCapitalLetters(category)}",
                    Color = DiscordColor.Gold
                };

                var highscoreList = new StringBuilder();
                foreach (var user in topUsers)
                {
                    try
                    {
                        var discordMember = await ctx.Guild.GetMemberAsync(user.UserId);

                        string valueToDisplay = category == "HeaviestFish"
                            ? user.ExtraInfo ?? $"{user.Points} kg"
                            : user.Points.ToString();

                        highscoreList.AppendLine($"{GambleUtils.CapitalizeUserFirstLetter(discordMember.DisplayName)}: {valueToDisplay}");
                    }
                    catch (DSharpPlus.Exceptions.NotFoundException)
                    {
                        string valueToDisplay = category == "HeaviestFish"
                            ? user.ExtraInfo ?? $"{user.Points} kg"
                            : user.Points.ToString();

                        highscoreList.AppendLine($"Unknown User: {valueToDisplay}");
                    }
                }

                embed.AddField("Highscores", highscoreList.ToString());

                await ctx.CreateResponseAsync(embed: embed);
            }

        }
        [SlashCommand("stats", "Display your Statistics.")]
        public async Task StatsCommand(InteractionContext ctx, [Option("user", "The user to check points for")] DiscordUser user = null)
        {
            ulong userId = user?.Id ?? ctx.User.Id;
            UserConfig userStats = await StatsHandler.LoadUserStats(userId);
            var member = await ctx.Guild.GetMemberAsync(userId);
            string desc = string.Empty;

            PropertyInfo[] properties = typeof(UserConfig).GetProperties();

            foreach (PropertyInfo stats in properties)
            {
                desc += "------------------------\n";

                if (stats.Name == "HeaviestFish" && stats.GetValue(userStats) is FishItem fish)
                {
                    if (fish.Weight > 0)
                        desc += $"**{StatsHandler.AddSpacesBeforeCapitalLetters(stats.Name)}**: {fish.Name} - {fish.Weight} kg\n";
                    else
                        desc += $"**{StatsHandler.AddSpacesBeforeCapitalLetters(stats.Name)}**: brak danych\n";
                }
                else
                {
                    desc += $"**{StatsHandler.AddSpacesBeforeCapitalLetters(stats.Name)}**: {stats.GetValue(userStats)}\n";
                }
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