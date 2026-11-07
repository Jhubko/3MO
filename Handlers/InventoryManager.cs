using Discord_Bot.Config;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace Discord_Bot.Handlers
{
    public class InventoryManager
    {
        public static SemaphoreSlim InventoryTransactionSemaphore { get; } = new(1, 1);
        private static readonly JSONReader jsonReader = new();

        public async Task<UserInventory> GetUserItems(ulong guildId, ulong userId)
        {
            string userFilePath = UserDataPaths.GetInventoryPath(Program.serverConfigPath, guildId, userId);
            Directory.CreateDirectory(Path.GetDirectoryName(userFilePath)!);
            if (!File.Exists(userFilePath))
            {
                var defaultInventory = new UserInventory
                {
                    Fish = [],
                    Items = [],
                    Pokemons = []
                };
                var defaultJson = JObject.FromObject(new
                {
                    defaultInventory.Fish,
                    defaultInventory.Items,
                    defaultInventory.Pokemons
                });

                await File.WriteAllTextAsync(userFilePath, defaultJson.ToString());
            }
            var userData = await jsonReader.ReadJson<JObject>(userFilePath) ?? [];
            var inventory = new UserInventory
            {
                Fish = userData["Fish"]?.ToObject<List<FishItem>>() ?? [],
                Items = userData["Items"]?.ToObject<Dictionary<string, uint>>() ?? [],
                Pokemons = userData["Pokemons"]?.ToObject<List<PokemonInventoryItem>>() ?? []
            };

            bool hasPokemonWithoutId = userData["Pokemons"] is JArray pokemonData
                && pokemonData.Any(pokemon => string.IsNullOrWhiteSpace(pokemon["Id"]?.Value<string>()));
            if (hasPokemonWithoutId)
                await UpdateUserItems(guildId, userId, inventory);

            return inventory;
        }
        public async Task<List<Fish>> LoadFishDataAsync(ulong serverId)
        {
            string serverFishFilePath = $"{Program.globalConfig.ConfigPath}\\{serverId}_fish_data.json";

            if (File.Exists(serverFishFilePath))
            {
                string json = File.ReadAllText(serverFishFilePath);
                return JsonConvert.DeserializeObject<List<Fish>>(json) ?? [];
            }
            else
            {
                var fishData = await jsonReader.ReadJson<List<Fish>>("Config\\fish_data.json") ?? throw new InvalidOperationException("fish_data cannot be null");
                File.WriteAllText(serverFishFilePath, JsonConvert.SerializeObject(fishData, Formatting.Indented));
                return fishData;
            }
        }

        public async Task UpdateUserItems(ulong guildId, ulong userId, UserInventory inventory)
        {
            string userFilePath = UserDataPaths.GetInventoryPath(Program.serverConfigPath, guildId, userId);
            Directory.CreateDirectory(Path.GetDirectoryName(userFilePath)!);

            var newData = new JObject
            {
                ["Fish"] = JToken.FromObject(inventory.Fish),
                ["Items"] = JToken.FromObject(inventory.Items),
                ["Pokemons"] = JToken.FromObject(inventory.Pokemons)
            };

            string jsonString = newData.ToString();
            await File.WriteAllTextAsync(userFilePath, jsonString);
        }

        public async Task UpdateUserItem(ulong guildId, ulong userId, string item, uint amount)
        {
            var inventory = await GetUserItems(guildId, userId);
            if (inventory.Items.ContainsKey(item))
                inventory.Items[item] += amount;
            else
                inventory.Items[item] = amount;
            await UpdateUserItems(guildId, userId, inventory);
        }
        public async Task SaveFishToInventory(ulong guildId, ulong userId, string fishName, double weight, int basePrice)
        {
            int price = (int)(basePrice * (weight / 2));
            var newFish = new FishItem
            {
                Name = fishName,
                Weight = weight,
                Price = price
            };

            var userInventory = await GetUserItems(guildId, userId);
            var fishList = userInventory.Fish ?? [];
            fishList.Add(newFish);
            userInventory.Fish = fishList;
            await UpdateUserItems(guildId, userId, userInventory);
        }

    }
}