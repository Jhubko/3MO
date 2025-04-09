using Discord_Bot.Config;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace Discord_Bot.Handlers
{
    public class InventoryManager
    {
        private static readonly JSONReader jsonReader = new();
        private readonly string folderPath = $"{Program.globalConfig.ConfigPath}\\user_points";

        public async Task<UserInventory> GetUserItems(ulong userId)
        {
            string userFilePath = $"{folderPath}\\{userId}_Items.json";
            if (!File.Exists(userFilePath))
            {
                var defaultInventory = new UserInventory
                {
                    Fish = [],
                    Items = []
                };
                var defaultJson = JObject.FromObject(new
                {
                    defaultInventory.Fish,
                    defaultInventory.Items
                });

                await File.WriteAllTextAsync(userFilePath, defaultJson.ToString());
            }
            var userData = await jsonReader.ReadJson<JObject>(userFilePath) ?? [];

            return new UserInventory
            {
                Fish = userData["Fish"]?.ToObject<List<FishItem>>() ?? [],
                Items = userData["Items"]?.ToObject<Dictionary<string, uint>>() ?? []
            };
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

        public async Task UpdateUserItems(ulong userId, UserInventory inventory)
        {
            string userFilePath = $"{folderPath}\\{userId}_Items.json";

            var newData = new JObject
            {
                ["Fish"] = JToken.FromObject(inventory.Fish),
                ["Items"] = JToken.FromObject(inventory.Items)
            };

            string jsonString = newData.ToString();
            await File.WriteAllTextAsync(userFilePath, jsonString);
        }

        public async Task UpdateUserItem(ulong userId, string item, uint amount)
        {
            var inventory = await GetUserItems(userId);
            if (inventory.Items.ContainsKey(item))
                inventory.Items[item] += amount;
            else
                inventory.Items[item] = amount;
            await UpdateUserItems(userId, inventory);
        }
        public async Task SaveFishToInventory(ulong userId, string fishName, double weight, int basePrice)
        {
            int price = (int)(basePrice * (weight / 2));
            var newFish = new FishItem
            {
                Name = fishName,
                Weight = weight,
                Price = price
            };

            var userInventory = await GetUserItems(userId);
            var fishList = userInventory.Fish ?? [];
            fishList.Add(newFish);
            userInventory.Fish = fishList;
            await UpdateUserItems(userId, userInventory);
        }

    }
}