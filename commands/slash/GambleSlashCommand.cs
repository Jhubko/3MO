using System;
using System.Threading.Tasks;
using DSharpPlus;
using DSharpPlus.Entities;
using DSharpPlus.SlashCommands;
using System.Text.RegularExpressions;
using Discord_Bot;
using System.Text;
using System.Xml.Linq;

public class GambleCommand : ApplicationCommandModule
{
    [SlashCommand("gamble", "Let's go gambling!")]
    public async Task Gamble(InteractionContext ctx, [Option("amount", "Amount of points to gamble (number, %, or 'all')")] string amountInput)
    {
        ulong userId = ctx.User.Id;
        int currentPoints = await Program.voicePointsManager.GetUserPoints(userId);
        int amountToGamble = ParseGambleAmount(amountInput, currentPoints);

        if (amountToGamble <= 0)
        {
            await ctx.CreateResponseAsync("Niewłaściwa kwota. Podaj numer, wartosć procentową lub 'all'.", true);
            return;
        }

        if (currentPoints < amountToGamble)
        {
            await ctx.CreateResponseAsync($"Nie masz wystarczającej kwoty żeby zagrać za {amountToGamble} punktów!", true);
            return;
        }

        Random random = new Random();
        bool win = random.Next(2) == 0;

        var name = new StringBuilder(ctx.User.Username);
        name[0] = char.ToUpper(name[0]);

        if (win)
        {
            currentPoints += amountToGamble;
            await ctx.CreateResponseAsync(new DiscordEmbedBuilder
            {
                Title = $"🎉  Pogchamp!  🎉",
                Description = $"{ctx.User.Mention} postawiłeś: {amountInput}  i  Wygrałeś: **{2 * amountToGamble}** punktów.\nMasz teraz: **{currentPoints}** punktów.",
                Color = DiscordColor.Green
            });
        }
        else
        {
            currentPoints -= amountToGamble;
            await ctx.CreateResponseAsync(new DiscordEmbedBuilder
            {
                Title = $":joy:  Yikes  :joy: ",
                Description = $"{ctx.User.Mention} przegrałeś: **{amountToGamble}** punktów XD.\nZostało Ci: **{currentPoints}** punktów.",
                Color = DiscordColor.Red
            });
        }

        Program.voicePointsManager.SaveUserPoints(userId, currentPoints);
    }

    private int ParseGambleAmount(string input, int currentPoints)
    {
        input = input.Trim().ToLower();

        if (input == "all")
            return currentPoints;

        if (Regex.IsMatch(input, @"^\d+%$"))
        {
            int percentage = int.Parse(input.Replace("%", ""));
            return (currentPoints * percentage) / 100;
        }

        if (int.TryParse(input, out int amount))
            return amount;

        return -1;
    }
}
