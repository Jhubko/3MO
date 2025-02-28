using Newtonsoft.Json.Linq;

namespace Discord_Bot.Config
{
    public class JSONWriter
    {
        private readonly IJsonHandler _jsonHandler;
        private readonly string _configPath;
        private readonly string _serverConfigDir;

        public JSONWriter(IJsonHandler jsonHandler, string configPath, string serverConfigDir)
        {
            _jsonHandler = jsonHandler;
            _configPath = configPath;
            _serverConfigDir = serverConfigDir;
        }
        public async Task UpdateConfig(string filePath, Dictionary<string, int> newConfig)
        {
            await _jsonHandler.WriteJson(filePath, newConfig);
        }
        
        public async Task UpdateGlobalConfig(string key, string value)
        {
            string filePath = Path.Combine(_configPath, "config.json");

            var jsonData = await _jsonHandler.ReadJson<JObject>(filePath) ?? new JObject();
            jsonData[key] = value;

            await _jsonHandler.WriteJson(filePath, jsonData);
        }

        public async Task UpdateServerConfig(ulong serverId, string key, string value, string? value2 = null)
        {
            string filePath = Path.Combine(_serverConfigDir, $"{serverId}.json");
            await _UpdateConfig(filePath, key, value, value2);
        }

        public async Task UpdateUserConfig(ulong userID, string key, string value, string? value2 = null)
        {
            string filePath = Path.Combine($"{_serverConfigDir}\\user_points", $"{userID}.json");
            await _UpdateConfig(filePath, key, value, value2);
        }

        private async Task _UpdateConfig(string filePath, string key, string value, string? value2 = null)
        {
            if (!File.Exists(filePath) || new FileInfo(filePath).Length == 0)
            {
                _jsonHandler.CreateJson(filePath);
            }

            var jsonData = await _jsonHandler.ReadJson<JObject>(filePath) ?? new JObject();

            if (IsArrayDataType(key))
            {
                UpdateArray(jsonData, key, value);
            }
            else if (IsDictionaryDataType(key))
            {
                UpdateDictionary(jsonData, key, value, value2);
            }
            else
            {
                jsonData[key] = value;
            }

            await _jsonHandler.WriteJson(filePath, jsonData);
        }

        private static void UpdateArray(JObject jsonData, string key, string value)
        {
            var array = jsonData[key] as JArray ?? new JArray();
            if (!array.Contains(value)) array.Add(value);
            jsonData[key] = array;
        }

        private static void UpdateDictionary(JObject jsonData, string key, string subKey, string? value)
        {
            if (string.IsNullOrEmpty(value)) return;

            if (jsonData[key] is not JObject obj) obj = new JObject();
            if (obj[subKey] is not JArray array) array = new JArray();

            array.Add(value);
            obj[subKey] = array;
            jsonData[key] = obj;
        }

        private static bool IsArrayDataType(string dataType) => dataType == "ImageOnlyChannels";
        private static bool IsDictionaryDataType(string dataType) => dataType == "BotMessages";
    }
}
