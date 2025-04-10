using Newtonsoft.Json.Linq;

namespace Discord_Bot.Config
{
    public class JSONWriter(IJsonHandler jsonHandler, string configPath, string serverConfigDir)
    {
        private readonly IJsonHandler _jsonHandler = jsonHandler;
        private readonly string _configPath = configPath;
        private readonly string _serverConfigDir = serverConfigDir;

        private static readonly SemaphoreSlim FileSemaphore = new(1, 1);

        public async Task UpdateGlobalConfig(string key, string value)
        {
            string filePath = Path.Combine(_configPath, "config.json");

            var jsonData = await _jsonHandler.ReadJson<JObject>(filePath) ?? [];
            jsonData[key] = value;

            await _jsonHandler.WriteJson(filePath, jsonData);
        }

        public async Task UpdateServerConfig(ulong serverId, string key, object? value, object? value2 = null)
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

            var jsonData = await _jsonHandler.ReadJson<JObject>(filePath) ?? [];

            foreach (var kvp in newConfig)
            {
                if (kvp.Value != null)
                {
                    jsonData[kvp.Key] = JToken.FromObject(kvp.Value);
                    if (kvp.Value is int intValue && intValue == 0)
                    {
                        jsonData.Remove(kvp.Key);
                    }
                }
            }
            await _jsonHandler.WriteJson(filePath, jsonData);
        }

        public async Task UpdateConfig(string filePath, string key, object? value, object? value2 = null)
        {
            await FileSemaphore.WaitAsync();

            try
            {
                if (!File.Exists(filePath) || new FileInfo(filePath).Length == 0)
                {
                    _jsonHandler.CreateJson(filePath);
                }

                var jsonData = await _jsonHandler.ReadJson<JObject>(filePath) ?? [];

                if (key == "Grid")
                {
                    if (value == null) return;
                    UpdateGrid(jsonData, key, value);
                }
                else if (IsArrayDataType(key))
                {
                    UpdateArray(jsonData, key, value?.ToString() ?? string.Empty);
                }
                else if (IsDictionaryDataType(key))
                {
                    if (value == null && value2 != null)
                    {
                        ClearDictionarySubKey(jsonData, key, value2.ToString()!);
                    }
                    else
                    {
                        UpdateDictionary(jsonData, key, value?.ToString() ?? string.Empty, value2?.ToString() ?? string.Empty);
                    }
                }
                else
                {
                    if (value == null) return;
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
                JArray gridArray = [];

                foreach (var row in grid)
                {
                    JArray rowArray = new(row);
                    gridArray.Add(rowArray);
                }

                jsonData[key] = gridArray;
            }
        }

        private static void UpdateArray(JObject jsonData, string key, string value)
        {
            var array = jsonData[key] as JArray ?? [];
            if (!array.Contains(value)) array.Add(value);
            jsonData[key] = array;
        }

        private static void UpdateDictionary(JObject jsonData, string key, string subKey, string? value)
        {
            if (string.IsNullOrEmpty(value)) return;

            if (jsonData[key] is not JObject obj) obj = [];
            if (obj[subKey] is not JArray array) array = [];

            array.Add(value);
            obj[subKey] = array;
            jsonData[key] = obj;
        }
        private static void ClearDictionarySubKey(JObject jsonData, string key, string subKey)
        {
            if (jsonData[key] is not JObject obj)
            {
                obj = [];
            }

            obj[subKey] = new JArray();
            jsonData[key] = obj;
        }
        private static bool IsArrayDataType(string dataType) => dataType == "ImageOnlyChannels";
        private static bool IsDictionaryDataType(string dataType) => dataType == "BotMessages";
    }
}
