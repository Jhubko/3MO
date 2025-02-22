using Discord_Bot.other;
using DSharpPlus.CommandsNext.Attributes;
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

        [SlashCommand("cards", "Play simple card game with bot.")]
        public async Task CardCommand(InteractionContext ctx, [Option("amount", "Amount of points to gamble (number, %, or 'all')")] string amountInput)
        {
            await ctx.DeferAsync();
            var userCard = new CardSystem();
            bool isPlayerWinner = false;
            ulong userId = ctx.User.Id;
            int currentPoints = await Program.voicePointsManager.GetUserPoints(userId);
            int amountToGamble = Program.voicePointsManager.ParseGambleAmount(amountInput, currentPoints);
            var checkAmout = GambleUtils.CheckGambleAmout(amountToGamble, currentPoints);

            if (!checkAmout.isProperValue)
            {
                await ctx.EditResponseAsync(new DiscordWebhookBuilder().WithContent(checkAmout.errorMessage));
                return;
            }

            var startEmbed = new DiscordEmbedBuilder
            {
                Title = $"{GambleUtils.CapitalizeUserFirstLetter(ctx.User.Username)} Rozpoczynasz gre w karty za {amountToGamble}!",
                Color = DiscordColor.Gold
            };

            await ctx.EditResponseAsync(new DiscordWebhookBuilder().AddEmbed(startEmbed));

            var userCardEmbed = new DiscordEmbedBuilder
            {
                Title = $"Twoja karta to {userCard.SelectedCard}",
                Color = userCard.suitIndex == 0 || userCard.suitIndex == 1 ? DiscordColor.Black : DiscordColor.Red
            };

            await ctx.EditResponseAsync(new DiscordWebhookBuilder().AddEmbeds(new List<DiscordEmbed> { startEmbed, userCardEmbed }));

            var botCard = new CardSystem();

            var botCardEmbed = new DiscordEmbedBuilder
            {
                Title = $"Karta bota to {botCard.SelectedCard}",
                Color = botCard.suitIndex == 0 || botCard.suitIndex == 1 ? DiscordColor.Black : DiscordColor.Red
            };

            await ctx.EditResponseAsync(new DiscordWebhookBuilder().AddEmbeds(new List<DiscordEmbed> { startEmbed, userCardEmbed, botCardEmbed }));

            if (userCard.numberIndex > botCard.numberIndex)
            {
                isPlayerWinner = true;
                currentPoints += amountToGamble;
            }
            else
                currentPoints -= amountToGamble;

            var resultEmbed = new DiscordEmbedBuilder
            {
                Title = $"{(isPlayerWinner ? $"{GambleUtils.CapitalizeUserFirstLetter(ctx.User.Username)}" : "Bot")} Wygrał!"
                + $"\n{(isPlayerWinner ? $"Wygrałeś {2 * amountToGamble}" : $"Przegrałes {amountToGamble}")}. Masz teraz: {currentPoints}",
                Color = isPlayerWinner ? DiscordColor.Green : DiscordColor.Red
            };

            Program.voicePointsManager.SaveUserPoints(userId, currentPoints);
            await ctx.EditResponseAsync(new DiscordWebhookBuilder().AddEmbeds(new List<DiscordEmbed> { startEmbed, userCardEmbed, botCardEmbed, resultEmbed }));
        }

        [SlashCommand("freepoints", "???")]
        [Cooldown(1, 60, CooldownBucketType.User)]
        public async Task DebilCommand(InteractionContext ctx)
        {
            await ctx.CreateResponseAsync("[Darmowe punkty tylko tutaj!!!](<https://www.youtube.com/watch?v=dQw4w9WgXcQ>)");
        }
    }
}
