using DSharpPlus.Entities;
using DSharpPlus.SlashCommands;

namespace Discord_Bot.commands.slash
{
    internal class GamesSlashCommands : ApplicationCommandModule
    {
        [SlashCommand("random", "Get a random number in a given range.")]
        public async Task RandomCommand(InteractionContext ctx, [Option("range", "Sets the range from 1 to this number.")] string range)
        {
            await ctx.DeferAsync();

            int parsedRange = int.Parse(range);

            if (parsedRange <= 1)
            {
                await ctx.EditResponseAsync(new DiscordWebhookBuilder().WithContent("Range must be greater than 1."));
                return;
            }

            var random = new Random();
            var randomNumber = random.Next(1, parsedRange + 1);

            await ctx.EditResponseAsync(new DiscordWebhookBuilder().WithContent($"Your random number is {randomNumber}"));
        }
    }
}
