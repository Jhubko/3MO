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

    public class FishAutocomplete : IAutocompleteProvider
    {
        private readonly InventoryManager _inventoryManager = new InventoryManager();

        public async Task<IEnumerable<DiscordAutoCompleteChoice>> Provider(AutocompleteContext ctx)
        {
            var userInventory = await _inventoryManager.GetUserItems(ctx.User.Id);

            if (userInventory?.Fish == null || userInventory.Fish.Count == 0)
            {
                return new List<DiscordAutoCompleteChoice>(); // Brak podpowiedzi, jeśli nie ma ryb
            }

            var fishNames = userInventory.Fish
                .Select(f => f.Name)
                .Distinct()
                .OrderBy(name => name)
                .ToList();

            string userInput = ctx.OptionValue?.ToString() ?? "";
            var filteredFish = fishNames
                .Where(name => name.StartsWith(userInput, StringComparison.OrdinalIgnoreCase))
                .Take(25)
                .Select(name => new DiscordAutoCompleteChoice(name, name));

            return await Task.FromResult(filteredFish);
        }
    }
}
