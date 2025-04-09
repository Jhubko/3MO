using Discord_Bot;
using Discord_Bot.Config;
using DSharpPlus.Entities;
using System.Reflection;
using System.Text.RegularExpressions;

class StatsHandler
{
    private readonly static string folderPath = $"{Program.globalConfig.ConfigPath}\\user_points";
    private static readonly JSONReader jsonReader = new();
    private static readonly JSONWriter jsonWriter = new(jsonReader, "config.json", Program.serverConfigPath);
    public async static Task<UserConfig> LoadUserStats(ulong userId)
    {
        return await jsonReader.ReadJson<UserConfig>($"{folderPath}\\{userId}.json") ?? throw new InvalidOperationException("UserConfig cannot be null");
    }

    public async static Task IncreaseStats(ulong userId, string statToChange, uint points = 0)
    {
        var userconfig = await LoadUserStats(userId);
        Type type = typeof(UserConfig);
        PropertyInfo? stat = type.GetProperty(statToChange);

        if (stat != null && stat.PropertyType == typeof(uint))
        {
            uint currentValue = (uint)(stat.GetValue(userconfig) ?? 0);
            if (statToChange.Contains("Wins") && statToChange != "Wins")
                await IncreaseStats(userId, "Wins");
            if (statToChange.Contains("Losses") && statToChange != "Losses")
                await IncreaseStats(userId, "Losses");

            uint newValue = currentValue + (points != 0 ? points : 1);
            await jsonWriter.UpdateUserConfig(userId, statToChange, newValue);
        }
    }
    public static string AddSpacesBeforeCapitalLetters(string input)
    {
        return Regex.Replace(input, "(?<!^)([A-Z])", " $1");
    }

    public static async Task<List<UserPoints>> GetTopUsersByCategory(DiscordGuild guild, int count, string category)
    {
        var allUsers = await GetAllUsersByCategory(category, guild);
        return allUsers.OrderByDescending(u => u.Points).Take(count).ToList();
    }
    private static async Task<List<UserPoints>> GetAllUsersByCategory(string category, DiscordGuild guild)
    {
        var users = new List<UserPoints>();
        var validUserIds = guild.Members.Keys;

        foreach (var file in Directory.GetFiles(folderPath, "*.json"))
        {
            var filename = Path.GetFileNameWithoutExtension(file);
            if (filename.Contains('_')) continue;

            if (!ulong.TryParse(filename, out var userId)) continue;
            if (!validUserIds.Contains(userId)) continue;

            var userData = await jsonReader.ReadJson<UserConfig>(file);
            if (userData == null) 
                continue;

            if (category == "HeaviestFish")
            {
                FishItem? heaviestFish = userData.HeaviestFish;
                if (heaviestFish != null && heaviestFish.Weight > 0)
                {
                    users.Add(new UserPoints
                    {
                        UserId = userId,
                        Points = heaviestFish.Weight,
                        ExtraInfo = $"{heaviestFish.Name} - {heaviestFish.Weight} kg"
                    });
                }
            }
            else
            {
                PropertyInfo? property = typeof(UserConfig).GetProperty(category);
                uint statValue = property != null ? (uint)(property.GetValue(userData) ?? 0) : 0;

                users.Add(new UserPoints { UserId = userId, Points = statValue });
            }
        }
        return users;
    }


    public async static Task CalculateHeaviestFish(ulong userId, string fishName, double weight, int basePrice)
    {
        var userConfig = await jsonReader.ReadJson<UserConfig>($"{folderPath}\\{userId}.json") ?? throw new InvalidOperationException("UserConfig cannot be null");
        double currentHeaviestFish = userConfig.HeaviestFish?.Weight ?? 0;
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
