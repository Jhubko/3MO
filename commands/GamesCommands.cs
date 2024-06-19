using Discord_Bot.other;
using DSharpPlus.CommandsNext;
using DSharpPlus.CommandsNext.Attributes;
using DSharpPlus.Entities;

namespace Discord_Bot.commands
{
    public class GamesCommands : BaseCommandModule
    {

        [Command("debil")]
        [Cooldown(1, 60, CooldownBucketType.User)]
        public async Task DebilCommand(CommandContext ctx)
        {
            await ctx.Channel.SendMessageAsync(ctx.User.Username + " To debil");
        }

        [Command("motor")]
        public async Task MotorCommand(CommandContext ctx)
        {
            await Program.ReadJson(ctx.Guild.Id);

            int numbersOfMotorsMentioned = Program.jsonReader.Motor == null ? 0 : int.Parse(Program.jsonReader.Motor);

            numbersOfMotorsMentioned++;

            await ctx.Channel.SendMessageAsync($"🏍️ Adam wspomniał o Motorze już {numbersOfMotorsMentioned} razy 🏍️");            

            await Program.jsonReader.UpdateJSON(ctx.Guild.Id, "Motor", numbersOfMotorsMentioned.ToString());
            
        }

        [Command("karty")]
        public async Task CardGame(CommandContext ctx)
        {
            var userCard = new CardSystem();

            var userCardEmbed = new DiscordEmbedBuilder
            {
                Title = $"Twoja karta to {userCard.SelectedCard}",
                Color = userCard.suitIndex == 0 || userCard.suitIndex == 1 ? DiscordColor.Black : DiscordColor.Red
            };

            await ctx.Channel.SendMessageAsync(embed: userCardEmbed);

            var botCard = new CardSystem();

            var botCardEmbed = new DiscordEmbedBuilder
            {
                Title = $"Karta bota to {botCard.SelectedCard}",
                Color = botCard.suitIndex == 0 || botCard.suitIndex == 1 ? DiscordColor.Black : DiscordColor.Red
            };

            await ctx.Channel.SendMessageAsync(embed: botCardEmbed);

            var resultEmbed = new DiscordEmbedBuilder
            {
                Title = $"Wygrał {(userCard.numberIndex > botCard.numberIndex ? $"{ctx.User.Username}" : "Bot")}",
                Color = DiscordColor.Gold
            };

            await ctx.Channel.SendMessageAsync(embed: resultEmbed);
        }
    }
}