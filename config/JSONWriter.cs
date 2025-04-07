using Newtonsoft.Json.Linq;

namespace Discord_Bot.Config
{
    public class JSONWriter
    {
        private readonly IJsonHandler _jsonHandler;
        private readonly string _configPath;
        private readonly string _serverConfigDir;

        private static readonly SemaphoreSlim FileSemaphore = new SemaphoreSlim(1, 1);
        public JSONWriter(IJsonHandler jsonHandler, string configPath, string serverConfigDir)
        {
            _jsonHandler = jsonHandler;
            _configPath = configPath;
            _serverConfigDir = serverConfigDir;
        }

        public async Task UpdateGlobalConfig(string key, string value)
        {
            string filePath = Path.Combine(_configPath, "config.json");

            var jsonData = await _jsonHandler.ReadJson<JObject>(filePath) ?? new JObject();
            jsonData[key] = value;

            await _jsonHandler.WriteJson(filePath, jsonData);
        }

        public async Task UpdateServerConfig(ulong serverId, string key, object value, object? value2 = null)
        {
            string filePath = Path.Combine(_serverConfigDir, $"{serverId}.json");
            await UpdateConfig(filePath, key, value, value2);
        }

        public async Task UpdateUserConfig(ulong userID, string key, object value, object? value2 = null)
        {
            string filePath = Path.Combine($"{_serverConfigDir}\\user_points", $"{userID}.json");
            await UpdateConfig(filePath, key, value, value2);
        }

        public async Task UpdateCityConfig(ulong userID, string key, object value)
        {
            string filePath = Path.Combine($"{_serverConfigDir}\\cities", $"{userID}_city.json");
            await UpdateConfig(filePath, key, value);
        }
        public async Task UpdateShopConfig<T>(string filePath, Dictionary<string, T> newConfig)
        {
            if (!File.Exists(filePath) || new FileInfo(filePath).Length == 0)
            {
                _jsonHandler.CreateJson(filePath);
            }

            var jsonData = await _jsonHandler.ReadJson<JObject>(filePath) ?? new JObject();

            foreach (var kvp in newConfig)
            {
                jsonData[kvp.Key] = JToken.FromObject(kvp.Value);
                if (kvp.Value is int intValue && intValue == 0)
                {
                    jsonData.Remove(kvp.Key);
                }
            }
            await _jsonHandler.WriteJson(filePath, jsonData);
        }

        public async Task UpdateConfig(string filePath, string key, object value, object? value2 = null)
        {
            await FileSemaphore.WaitAsync();

            try
            {
                if (!File.Exists(filePath) || new FileInfo(filePath).Length == 0)
                {
                    _jsonHandler.CreateJson(filePath);
                }

                var jsonData = await _jsonHandler.ReadJson<JObject>(filePath) ?? new JObject();

                if (key == "Grid")
                {
                    UpdateGrid(jsonData, key, value);
                }
                else if (IsArrayDataType(key))
                {
                    UpdateArray(jsonData, key, value.ToString());
                }
                else if (IsDictionaryDataType(key))
                {
                    UpdateDictionary(jsonData, key, value.ToString(), value2.ToString());
                }
                else
                {
                    jsonData[key] = JToken.FromObject(value);
                }

                await _jsonHandler.WriteJson(filePath, jsonData);
            }
            catch (IOException ex)
            {
                Console.WriteLine($"Błąd zapisu pliku: {ex.Message}");
            }
            finally
            {
                FileSemaphore.Release();
            }
        }

        private static void UpdateGrid(JObject jsonData, string key, object value)
        {
            if (value is string[][] grid)
            {
                JArray gridArray = new JArray();

                foreach (var row in grid)
                {
                    JArray rowArray = new JArray(row);
                    gridArray.Add(rowArray);
                }

                jsonData[key] = gridArray;
            }
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
