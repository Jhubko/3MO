using Discord_Bot;
using Discord_Bot.Config;
using System.Reflection;
using System.Text.RegularExpressions;

class StatsHandler
{
    private readonly static string folderPath = $"{Program.globalConfig.ConfigPath}\\user_points";
    private static IJsonHandler jsonReader = new JSONReader();
    private static JSONWriter jsonWriter = new JSONWriter(jsonReader, "config.json", Program.serverConfigPath);
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

}
