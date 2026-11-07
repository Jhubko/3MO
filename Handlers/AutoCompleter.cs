using Discord_Bot.Config;
using Discord_Bot.Handlers;
using DSharpPlus.Entities;
using DSharpPlus.SlashCommands;

namespace Discord_Bot.other
{
    public class BuildingAutocomplete : IAutocompleteProvider
    {
        readonly CityHandler _cityHandler = new();
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

    public class ShopItemAutocomplete : IAutocompleteProvider
    {
        private static readonly JSONReader _jsonReader = new();
        private readonly InventoryManager _inventoryManager = new();

        public async Task<IEnumerable<DiscordAutoCompleteChoice>> Provider(AutocompleteContext ctx)
        {
            var shop = await _jsonReader.ReadJson<ServerConfigShop>(
                $"{Program.serverConfigPath}\\{ctx.Guild.Id}_shop.json") ?? new ServerConfigShop();
            var inventory = await _inventoryManager.GetUserItems(ctx.User.Id);
            string userInput = ctx.OptionValue?.ToString() ?? "";

            var regularItems = shop.ShopItems
                .Where(item => PokemonService.FindPokeball(item.Name) == null)
                .Select(item => new
                {
                    item.Name,
                    Cost = (ulong)item.BaseCost * (inventory.Items.TryGetValue(item.Name, out uint count) ? (ulong)count + 1 : 1UL)
                });
            var pokeballs = PokemonService.Pokeballs
                .Select(ball => new { ball.Name, Cost = (ulong)ball.Price });

            return regularItems.Concat(pokeballs)
                .Where(item => item.Name.Contains(userInput, StringComparison.OrdinalIgnoreCase))
                .OrderBy(item => item.Name, StringComparer.OrdinalIgnoreCase)
                .Take(25)
                .Select(item => new DiscordAutoCompleteChoice($"{item.Name} — {item.Cost} punktów", item.Name));
        }
    }

    public class PokemonInventoryAutocomplete : IAutocompleteProvider
    {
        private readonly InventoryManager _inventoryManager = new();

        public async Task<IEnumerable<DiscordAutoCompleteChoice>> Provider(AutocompleteContext ctx)
        {
            var inventory = await _inventoryManager.GetUserItems(ctx.User.Id);
            string userInput = ctx.OptionValue?.ToString() ?? "";

            return inventory.Pokemons
                .GroupBy(pokemon => pokemon.PokedexId)
                .Select(group => group.First())
                .Where(pokemon => pokemon.Name.Contains(userInput, StringComparison.OrdinalIgnoreCase)
                    || pokemon.PokedexId.ToString().Contains(userInput, StringComparison.OrdinalIgnoreCase))
                .OrderBy(pokemon => pokemon.PokedexId)
                .Take(25)
                .Select(pokemon => new DiscordAutoCompleteChoice(
                    $"#{pokemon.PokedexId} {pokemon.Name}",
                    pokemon.PokedexId.ToString()));
        }
    }

    public class DuplicatePokemonAutocomplete : IAutocompleteProvider
    {
        private readonly InventoryManager _inventoryManager = new();

        public async Task<IEnumerable<DiscordAutoCompleteChoice>> Provider(AutocompleteContext ctx)
        {
            var inventory = await _inventoryManager.GetUserItems(ctx.User.Id);
            string userInput = ctx.OptionValue?.ToString() ?? "";

            return inventory.Pokemons
                .GroupBy(pokemon => pokemon.PokedexId)
                .Where(group => group.Count() > 1)
                .Select(group => group.First())
                .Where(pokemon => pokemon.Name.Contains(userInput, StringComparison.OrdinalIgnoreCase)
                    || pokemon.PokedexId.ToString().Contains(userInput, StringComparison.OrdinalIgnoreCase))
                .OrderBy(pokemon => pokemon.PokedexId)
                .Take(25)
                .Select(pokemon => new DiscordAutoCompleteChoice(
                    $"#{pokemon.PokedexId} {pokemon.Name} (duplikaty)",
                    pokemon.PokedexId.ToString()));
        }
    }

    public class UserItemAutocomplete : IAutocompleteProvider
    {
        private readonly InventoryManager _inventoryManager = new();

        public async Task<IEnumerable<DiscordAutoCompleteChoice>> Provider(AutocompleteContext ctx)
        {
            var inventory = await _inventoryManager.GetUserItems(ctx.User.Id);
            string userInput = ctx.OptionValue?.ToString() ?? "";

            var itemChoices = inventory.Items
                .Where(item => item.Value > 0 && item.Key.Contains(userInput, StringComparison.OrdinalIgnoreCase))
                .OrderBy(item => item.Key)
                .Select(item => new DiscordAutoCompleteChoice($"{item.Key} (x{item.Value})", $"item:{item.Key}"));
            var pokemonChoices = inventory.Pokemons
                .Where(pokemon => pokemon.Name.Contains(userInput, StringComparison.OrdinalIgnoreCase)
                    || pokemon.PokedexId.ToString().Contains(userInput, StringComparison.OrdinalIgnoreCase))
                .OrderBy(pokemon => pokemon.PokedexId)
                .Select(pokemon => new DiscordAutoCompleteChoice($"Pokémon #{pokemon.PokedexId} {pokemon.Name}", $"pokemon:{pokemon.Id}"));

            return itemChoices.Concat(pokemonChoices)
                .OrderBy(choice => choice.Name, StringComparer.OrdinalIgnoreCase)
                .Take(25);
        }
    }

    public class FishAutocomplete : IAutocompleteProvider
    {
        private readonly InventoryManager _inventoryManager = new();

        public async Task<IEnumerable<DiscordAutoCompleteChoice>> Provider(AutocompleteContext ctx)
        {
            var userInventory = await _inventoryManager.GetUserItems(ctx.User.Id);

            if (userInventory?.Fish == null || userInventory.Fish.Count == 0)
            {
                return [];
            }

            var fishNames = userInventory.Fish
                .Select(f => f.Name)
                .Distinct()
                .OrderBy(name => name)
                .ToList();

            string userInput = ctx.OptionValue?.ToString() ?? "";
            var filteredFish = fishNames
                .Where(name => name?.StartsWith(userInput, StringComparison.OrdinalIgnoreCase) == true)
                .Take(25)
                .Select(name => new DiscordAutoCompleteChoice(name, name));

            return await Task.FromResult(filteredFish);
        }
    }
}
