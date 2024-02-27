using DSharpPlus.Entities;
using DSharpPlus.Interactivity.Extensions;
using DSharpPlus.SlashCommands;

namespace Discord_Bot.commands.slash
{
    internal class PollCommands : ApplicationCommandModule
    {
        [SlashCommand("poll", "Create a pool with up to four options")]
        public async Task Pool(InteractionContext ctx, [Option("poolTitle", "Title for your poll")] string poolTitle,
                                                       [Option("option1", "First poll option")] string option1,
                                                       [Option("option2", "Second poll option")] string option2,
                                                       [Option("option3", "Third poll option")] string option3 = null,
                                                       [Option("option4", "Fourth poll option")] string option4 = null)
        {
            if (option3 == null && option4 != null)
            {
                option3 = option4;
                option4 = null;
            }

            await ctx.DeferAsync();

            var interactivity = Program.Client.GetInteractivity();
            var pollTime = TimeSpan.FromSeconds(20);

            List<DiscordEmoji> emojiOptions = new List<DiscordEmoji> {
                                            DiscordEmoji.FromName(Program.Client, ":one:"),
                                            DiscordEmoji.FromName(Program.Client, ":two:")};

            if (option3 != null)
                emojiOptions.Add(DiscordEmoji.FromName(Program.Client, ":three:"));

            if (option4 != null)
                emojiOptions.Add(DiscordEmoji.FromName(Program.Client, ":four:"));


            string optionsDiscription = $"{emojiOptions[0]} | {option1} \n" +
                                        $"{emojiOptions[1]} | {option2} \n";

            if (option3 != null)
                optionsDiscription += $"{emojiOptions[2]} | {option3} \n";

            if (option4 != null)
                optionsDiscription += $"{emojiOptions[3]} | {option4} \n";

            var poolMasage = new DiscordEmbedBuilder()
            {
                Color = DiscordColor.HotPink,
                Title = poolTitle,
                Description = optionsDiscription,
            };

            var sendPool = await ctx.EditResponseAsync(new DiscordWebhookBuilder().AddEmbed(poolMasage));

            foreach (var emoji in emojiOptions)
            {
                await sendPool.CreateReactionAsync(emoji);
            }

            var totalReactions = await interactivity.CollectReactionsAsync(sendPool, pollTime);

            Dictionary<DiscordEmoji, int> count = new Dictionary<DiscordEmoji, int>() {
                {emojiOptions[0], 0 },
                {emojiOptions[1], 0 },
            };

            if (option3 != null)
                count.Add(emojiOptions[2], 0);

            if (option4 != null)
                count.Add(emojiOptions[3], 0);


            foreach (var vote in totalReactions)
            {
                for (int i = 0; i < emojiOptions.Count; i++)
                {
                    if (vote.Emoji == emojiOptions[i])
                        count[emojiOptions[i]] += vote.Total;
                }
            }

            var sortedDict = count.OrderByDescending(pair => pair.Value).ToDictionary(pair => pair.Key, pair => pair.Value);

            string resultsDiscription = string.Empty;

            foreach (var entry in sortedDict)
            {
                switch (entry.Key)
                {
                    case string emoji when emoji == emojiOptions[0]:
                        resultsDiscription += $"{entry.Value} Głosów | **{option1}** \n";
                        break;
                    case string emoji when emoji == emojiOptions[1]:
                        resultsDiscription += $"{entry.Value} Głosów | **{option2}** \n";
                        break;
                    case string emoji when emoji == emojiOptions[2]:
                        resultsDiscription += $"{entry.Value} Głosów |  **{option3}** \n";
                        break;
                    case string emoji when emoji == emojiOptions[3]:
                        resultsDiscription += $"{entry.Value} Głosów |  **{option4}** \n";
                        break;

                }
            }

            resultsDiscription += $"\n Liczba głosów {count.Sum(x => x.Value)}";

            var resultsMasage = new DiscordEmbedBuilder()
            {
                Color = DiscordColor.Green,
                Title = $"Wyniki {poolTitle}",
                Description = resultsDiscription,
            };

            await ctx.EditResponseAsync(new DiscordWebhookBuilder().AddEmbed(resultsMasage));

        }

    }
}
