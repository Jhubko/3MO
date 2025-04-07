using Discord_Bot;
using Discord_Bot.Config;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

public class InventoryManager
{
    private static IJsonHandler jsonReader = new JSONReader();
    private readonly string folderPath = $"{Program.globalConfig.ConfigPath}\\user_points";

    public async Task<UserInventory> GetUserItems(ulong userId)
    {
        string userFilePath = $"{folderPath}\\{userId}_Items.json";
        if (!File.Exists(userFilePath))
        {
            var defaultInventory = new UserInventory
            {
                Fish = new List<FishItem>(),
                Items = new Dictionary<string, uint>()
            };
            var defaultJson = JObject.FromObject(new
            {
                Fish = defaultInventory.Fish,
                Items = defaultInventory.Items
            });

            await File.WriteAllTextAsync(userFilePath, defaultJson.ToString());
        }
        var userData = await jsonReader.ReadJson<JObject>(userFilePath) ?? new JObject();

        return new UserInventory
        {
            Fish = userData["Fish"]?.ToObject<List<FishItem>>() ?? new List<FishItem>(),
            Items = userData["Items"]?.ToObject<Dictionary<string, uint>>() ?? new Dictionary<string, uint>()
        };
    }
    public async Task<List<Fish>> LoadFishDataAsync(ulong serverId)
    {
        string serverFishFilePath = $"{Program.globalConfig.ConfigPath}\\{serverId}_fish_data.json";

        if (File.Exists(serverFishFilePath))
        {
            string json = File.ReadAllText(serverFishFilePath);
            return JsonConvert.DeserializeObject<List<Fish>>(json) ?? new List<Fish>();
        }
        else
        {
            var fishData = await jsonReader.ReadJson<List<Fish>>("Config\\fish_data.json");
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
        var fishList = userInventory.Fish ?? new List<FishItem>();
        fishList.Add(newFish);
        userInventory.Fish = fishList;
        await UpdateUserItems(userId, userInventory);
    }

}