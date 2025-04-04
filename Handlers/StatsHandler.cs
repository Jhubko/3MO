using Discord_Bot;
using Discord_Bot.Config;
using System.Reflection;
using System.Text.RegularExpressions;

class StatsHandler
{
    private readonly static string folderPath = $"{Program.globalConfig.ConfigPath}\\user_points";
    private static IJsonHandler jsonReader = new JSONReader();
    private static JSONWriter jsonWriter = new JSONWriter(jsonReader, "config.json", Program.serverConfigPath);
    private static readonly InventoryManager inventoryManager = new InventoryManager();
    public async static Task<UserConfig> LoadUserStats(ulong userId)
    {
        return await jsonReader.ReadJson<UserConfig>($"{folderPath}\\{userId}.json");
    }

    public async static Task IncreaseStats(ulong userId, string statToChange, int points = -1)
    {
        var userconfig = await LoadUserStats(userId);
        Type type = typeof(UserConfig);
        PropertyInfo? stat = type.GetProperty(statToChange);

        if (stat != null && stat.PropertyType == typeof(string))
        {
            string? currentValue = (string?)stat.GetValue(userconfig);

            if (int.TryParse(currentValue, out int numericValue))
            {
                if (statToChange.Contains("Wins") && statToChange != "Wins")
                    await IncreaseStats(userId, "Wins");
                if (statToChange.Contains("Losses") && statToChange != "Losses")
                    await IncreaseStats(userId, "Losses");
                if (points != -1)
                {
                    await jsonWriter.UpdateUserConfig(userId, statToChange, (numericValue + points).ToString());
                    return;
                }

                await jsonWriter.UpdateUserConfig(userId, statToChange, (numericValue+1).ToString());
            }
        }
    }
    public static string AddSpacesBeforeCapitalLetters(string input)
    {
        return Regex.Replace(input, "(?<!^)([A-Z])", " $1");
    }

    public static async Task<List<UserPoints>> GetTopUsersByCategory(int count, string category)
    {
        var allUsers = await GetAllUsersByCategory(category);
        return allUsers.OrderByDescending(u => u.Points).Take(count).ToList();
    }


    private static async Task<List<UserPoints>> GetAllUsersByCategory(string category)
    {
        var users = new List<UserPoints>();

        foreach (var file in Directory.GetFiles(folderPath, "*.json"))
        {
            var filename = Path.GetFileNameWithoutExtension(file);
            if (filename.Contains('_')) continue;

            var userData = await jsonReader.ReadJson<UserConfig>(file);
            if (userData != null)
            {
                ulong userId = ulong.Parse(filename);

                if (category == "HeaviestFish")
                {
                    FishItem heaviestFish = userData.HeaviestFish;
                    if (heaviestFish != null && heaviestFish.Weight > 0)
                    {
                        users.Add(new UserPoints
                        {
                            UserId = userId,
                            Points = (int)heaviestFish.Weight,
                            ExtraInfo = $"{heaviestFish.Name} - {heaviestFish.Weight} kg"
                        });
                    }
                }
                else
                {
                    PropertyInfo property = typeof(UserConfig).GetProperty(category);
                    int statValue = property != null ? int.Parse(property.GetValue(userData)?.ToString() ?? "0") : 0;

                    users.Add(new UserPoints { UserId = userId, Points = statValue });
                }
            }
        }
        return users;
    }

    public async static Task calculateHeaviestFish(ulong userId, string fishName, double weight, int basePrice)
    {
        UserInventory userInventory = await inventoryManager.GetUserItems(userId);
        var userConfig = await jsonReader.ReadJson<UserConfig>($"{folderPath}\\{userId}.json");
        double currentHeaviestFish = userConfig.HeaviestFish.Weight;
        int price = (int)(basePrice * (weight / 2));
        
        var newFish = new FishItem
        {
            Name = fishName,
            Weight = weight,
            Price = price
        };

        if (currentHeaviestFish == 0 || newFish.Weight > currentHeaviestFish)
        {
            await jsonWriter.UpdateUserConfig(userId, "HeaviestFish", newFish);
            return;
        }
    }

}
