﻿using DSharpPlus.Entities;
using DSharpPlus.Interactivity.Extensions;
using DSharpPlus.SlashCommands;
namespace Discord_Bot.Commands.Slash
{
    public class DuelCommand : ApplicationCommandModule
    {
        [SlashCommand("duel", "Challenge another player to a duel.")]
        public async Task DuelCommandAsync(InteractionContext ctx,
                                           [Option("opponent", "The player you want to challenge")] DiscordUser opponent,
                                           [Option("amount", "Amount of points to bet (number, %, or 'all')")] string amountInput)
        {
            ulong userId = ctx.User.Id;
            ulong opponentId = opponent.Id;
            uint userPoints = await Program.voicePointsManager.GetUserPoints(userId);
            uint opponentPoints = await Program.voicePointsManager.GetUserPoints(opponentId);
            uint betAmount = GambleUtils.ParseGambleAmount(amountInput, userPoints);
            var (isProperValue, errorMessage) = GambleUtils.CheckGambleAmout(betAmount, userPoints);

            if (!isProperValue)
            {
                await ctx.CreateResponseAsync(errorMessage, true);
                return;
            }

            if (betAmount > opponentPoints)
            {
                await ctx.CreateResponseAsync($"{opponent.Username} nie ma wystarczających punktów, aby zagrać za {betAmount}!", true);
                return;
            }

            await ctx.DeferAsync();
            var embedRequest = new DiscordEmbedBuilder
            {
                Title = "⚔️ Pojedynek! ⚔️",
                Description = $"{ctx.User.Mention} wyzwał {opponent.Mention} na pojedynek za **{betAmount}** punktów. Akceptujesz?",
                Color = DiscordColor.Blurple
            };

            var message = await ctx.EditResponseAsync(new DiscordWebhookBuilder().AddEmbed(embedRequest)).ConfigureAwait(false);

            var checkEmoji = DiscordEmoji.FromUnicode("✅"); // Akceptacja
            var crossEmoji = DiscordEmoji.FromUnicode("❌"); // Odrzucenie
            await message.CreateReactionAsync(checkEmoji);
            await message.CreateReactionAsync(crossEmoji);

            var interactivity = ctx.Client.GetInteractivity();
            var reactionResult = await interactivity.WaitForReactionAsync(
                x => x.Message == message &&
                     x.User.Id == opponentId &&
                     (x.Emoji == checkEmoji || x.Emoji == crossEmoji),
                TimeSpan.FromSeconds(60)
            );

            if (reactionResult.TimedOut)
            {
                var embedTimeout = new DiscordEmbedBuilder
                {
                    Title = "⌛ Pojedynek Wygasł",
                    Description = "Propozycja pojedynku wygasła, brak odpowiedzi.",
                    Color = DiscordColor.Orange
                };
                await message.ModifyAsync(new DiscordMessageBuilder().WithEmbed(embedTimeout).WithContent(""));
                return;
            }

            if (reactionResult.Result.Emoji == crossEmoji)
            {
                var embedDeclined = new DiscordEmbedBuilder
                {
                    Title = "❌ Pojedynek Odrzucony",
                    Description = $"{opponent.Username} odmówił pojedynku!",
                    Color = DiscordColor.Red
                };
                await message.ModifyAsync(new DiscordMessageBuilder().WithEmbed(embedDeclined).WithContent(""));
                return;
            }

            if (reactionResult.Result.Emoji == checkEmoji)
            {
                // Losowanie zwycięzcy
                var random = new Random();
                bool userWins = random.Next(2) == 0; // 50/50 szansa

                if (userWins)
                {
                    userPoints += betAmount;
                    opponentPoints -= betAmount;
                    await StatsHandler.IncreaseStats(userId, "DuelWins");
                    await StatsHandler.IncreaseStats(opponentId, "DuelLosses");
                    await StatsHandler.IncreaseStats(userId, "WonPoints", betAmount);
                    await StatsHandler.IncreaseStats(opponentId, "LostPoints", betAmount);
                    Program.voicePointsManager.SaveUserPoints(userId, userPoints);
                    Program.voicePointsManager.SaveUserPoints(opponentId, opponentPoints);

                    var embedResult = new DiscordEmbedBuilder
                    {
                        Title = "🏆 Pojedynek Wygrany!",
                        Description = $"{ctx.User.Mention} wygrał pojedynek i zdobył **{betAmount}** punktów od {opponent.Mention}.",
                        Color = DiscordColor.Green
                    };
                    await message.ModifyAsync(new DiscordMessageBuilder().WithEmbed(embedResult).WithContent(""));
                }
                else
                {
                    userPoints -= betAmount;
                    opponentPoints += betAmount;
                    await StatsHandler.IncreaseStats(opponentId, "DuelWins");
                    await StatsHandler.IncreaseStats(userId, "DuelLosses");
                    await StatsHandler.IncreaseStats(userId, "LostPoints", betAmount);
                    await StatsHandler.IncreaseStats(opponentId, "WonPoints", betAmount);
                    Program.voicePointsManager.SaveUserPoints(userId, userPoints);
                    Program.voicePointsManager.SaveUserPoints(opponentId, opponentPoints);

                    var embedResult = new DiscordEmbedBuilder
                    {
                        Title = "💀 Pojedynek Przegrany!",
                        Description = $"{ctx.User.Mention} przegrał pojedynek i stracił **{betAmount}** punktów na rzecz {opponent.Mention}.",
                        Color = DiscordColor.Red
                    };
                    await message.ModifyAsync(new DiscordMessageBuilder().WithEmbed(embedResult).WithContent(""));
                }
            }
        }
    }
}