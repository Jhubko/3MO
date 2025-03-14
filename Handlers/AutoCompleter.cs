using Discord_Bot.Config;
using DSharpPlus.Entities;
using DSharpPlus.SlashCommands;

namespace Discord_Bot.other
{
    public class BuildingAutocomplete : IAutocompleteProvider
    {
        readonly CityHandler _cityHandler = new CityHandler();
        public async Task<IEnumerable<DiscordAutoCompleteChoice>> Provider(AutocompleteContext ctx)
        {
            var buildings = _cityHandler.Buildings
                .Select(b => new { Name = $"{b.Emote} {b.Name}", Value = b.Name })
                .ToList();

            string userInput = ctx.OptionValue?.ToString() ?? "";
            var filteredBuildings = buildings
                .Where(b => b.Value.StartsWith(userInput, StringComparison.OrdinalIgnoreCase))
                .Take(25)
                .Select(b => new DiscordAutoCompleteChoice(b.Name, b.Value));

            return await Task.FromResult(filteredBuildings);
        }
    }
    public class HighscoreAutocomplete : IAutocompleteProvider
    {
        public async Task<IEnumerable<DiscordAutoCompleteChoice>> Provider(AutocompleteContext ctx)
        {
            var categories = typeof(UserConfig).GetProperties()
                .Select(p => p.Name)
                .ToArray();

            string userInput = ctx.OptionValue?.ToString() ?? "";
            var filteredCategories = categories
                .Where(c => c.StartsWith(userInput, StringComparison.OrdinalIgnoreCase))
                .Take(25)
                .Select(c => new DiscordAutoCompleteChoice(c, c));

            return await Task.FromResult(filteredCategories);
        }
    }
}
