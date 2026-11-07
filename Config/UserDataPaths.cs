namespace Discord_Bot.Config
{
    public static class UserDataPaths
    {
        public static string GetUserConfigPath(string configPath, ulong guildId, ulong userId) =>
            Path.Combine(configPath, "user_points", guildId.ToString(), $"{userId}.json");

        public static string GetInventoryPath(string configPath, ulong guildId, ulong userId) =>
            Path.Combine(configPath, "user_points", guildId.ToString(), $"{userId}_Items.json");

        public static string GetCityPath(string configPath, ulong guildId, ulong userId) =>
            Path.Combine(configPath, "cities", guildId.ToString(), $"{userId}_city.json");
    }
}
