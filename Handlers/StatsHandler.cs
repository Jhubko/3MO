using Discord_Bot;
using Discord_Bot.Config;
using DSharpPlus.Entities;
using System.Reflection;
using System.Text.RegularExpressions;

class StatsHandler
{
    private static readonly string folderPath = Path.Combine(Program.serverConfigPath, "user_points");
    private static readonly JSONReader jsonReader = new();
    private static readonly JSONWriter jsonWriter = new(jsonReader, "config.json", Program.serverConfigPath);
    public static async Task<UserConfig> LoadUserStats(ulong guildId, ulong userId)
    {
        return await jsonReader.ReadJson<UserConfig>(UserDataPaths.GetUserConfigPath(Program.serverConfigPath, guildId, userId)) ?? throw new InvalidOperationException("UserConfig cannot be null");
    }

    public async static Task IncreaseStats(ulong guildId, ulong userId, string statToChange, uint points = 0)
    {
        var userconfig = await LoadUserStats(guildId, userId);
        Type type = typeof(UserConfig);
        PropertyInfo? stat = type.GetProperty(statToChange);

        if (stat != null && stat.PropertyType == typeof(uint))
        {
            uint currentValue = (uint)(stat.GetValue(userconfig) ?? 0);
            if (statToChange.Contains("Wins") && statToChange != "Wins")
                await IncreaseStats(guildId, userId, "Wins");
            if (statToChange.Contains("Losses") && statToChange != "Losses")
                await IncreaseStats(guildId, userId, "Losses");

            uint newValue = currentValue + (points != 0 ? points : 1);
            await jsonWriter.UpdateUserConfig(guildId, userId, statToChange, newValue);
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
        string guildFolder = Path.Combine(folderPath, guild.Id.ToString());
        if (!Directory.Exists(guildFolder))
            return users;

        foreach (var file in Directory.GetFiles(guildFolder, "*.json"))
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


    public async static Task CalculateHeaviestFish(ulong guildId, ulong userId, string fishName, double weight, int basePrice)
    {
        var userConfig = await jsonReader.ReadJson<UserConfig>(UserDataPaths.GetUserConfigPath(Program.serverConfigPath, guildId, userId)) ?? throw new InvalidOperationException("UserConfig cannot be null");
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
            await jsonWriter.UpdateUserConfig(guildId, userId, "HeaviestFish", newFish);
            return;
        }
    }

}
public class UserPoints
{
    public ulong UserId { get; set; }
    public object? Points { get; set; }
    public string? ExtraInfo { get; internal set; }
}