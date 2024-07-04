using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace Discord_Bot.config
{
    internal class JSONReader
    {
        public string? Token { get; set; }
        public string? Prefix { get; set; }
        public string? ApiGoogle { get; set; }
        public string? CseId { get; set; }
        public string? ApiGPT { get; set; }
        public string? WeatherApi { get; set; }
        public string? LlHostname { get; set; }
        public int LlPort { get; set; }
        public string? LlPass { get; set; }
        public bool Secured { get; set; }
        public string? DefaultRole { get; set; }
        public string? DeleteMessageEmoji { get; set; }
        public string? Motor { get; set; }
        public List<string>? ImageChannels { get; set; }
        public Dictionary<string, List<string>>? BotMessages { get; set; }
        public string? ConfigPath { get; set; }

        private readonly List<string> arrayDataTypes = new List<string>() { "ImageOnlyChannels" };

        private readonly List<string> dictionaryDataTypes = new List<string>() { "BotMessages" };

        public async Task ReadJSON(string jsonName = "config.json")
        {
            using (StreamReader sr = new StreamReader(jsonName))
            {
                string json = await sr.ReadToEndAsync();
                JSONStructure data = JsonConvert.DeserializeObject<JSONStructure>(json);
                if (data == null)
                    return;

                if (jsonName == "config.json")
                {
                    this.Token = data.Token;
                    this.Prefix = data.Prefix;
                    this.ApiGoogle = data.ApiGoogle;
                    this.CseId = data.CseId;
                    this.ApiGPT = data.ApiGPT;
                    this.WeatherApi = data.WeatherApi;
                    this.LlHostname = data.LlHostname;
                    this.LlPort = data.LlPort;
                    this.LlPass = data.LlPass;
                    this.Secured = data.Secured;
                    this.ConfigPath = data.ConfigPath;
                }
                else
                {
                    this.DeleteMessageEmoji = data.DeleteMessageEmoji;
                    this.DefaultRole = data.DefaultRole;
                    this.ImageChannels = data.ImageChannels;
                    this.BotMessages = data.BotMessages;
                    this.Motor = data.Motor;
                }
            }
        }
        public async Task UpdateJSON(ulong index, string dataType, string content, string? content2 = null)
        {
            await ReadJSON();

            string filePath = Path.Combine(ConfigPath, $"{index}.json");
            FileInfo fileInfo = new FileInfo(filePath);

            if (!File.Exists(filePath) || fileInfo.Length == 0)
            {
                CreateJSON(index);
            }

            string existingJson = File.ReadAllText(filePath);
            JObject jsonData = JObject.Parse(existingJson);          

            if (arrayDataTypes.Contains(dataType))
                UpdateArrayDataType(jsonData, dataType, content);
            if (dictionaryDataTypes.Contains(dataType))
                UpdateDictionary(jsonData, dataType, content, content2);
            else
                jsonData[dataType] = content;

            string updatedJson = JsonConvert.SerializeObject(jsonData, Formatting.Indented);
            File.WriteAllText(filePath, updatedJson);

        }
        public void CreateJSON(ulong index)
        {
            string filePath = Path.Combine(ConfigPath, $"{index}.json");
            File.WriteAllText(filePath, "{}");
        }
        private void UpdateArrayDataType(JObject jsonData, string dataType, string Content)
        {
            JArray? arrayDataType = jsonData[dataType] as JArray;
            
            if (arrayDataType == null)
            {
                jsonData[dataType] = new JArray();
                arrayDataType = jsonData[dataType] as JArray;
            }

            if (!arrayDataType.ToList().Contains(Content))
            {
                arrayDataType.Add(Content);
            }
            else
            {
                JToken? item = arrayDataType.FirstOrDefault(arr => arr.Type == JTokenType.String && arr.Value<string>() == Content);
                item?.Remove();
            }

            jsonData[dataType] = arrayDataType;
        }
        private void UpdateDictionary(JObject jsonData, string dataType, string key, string value)
        {
            if (jsonData == null)
            {
                throw new ArgumentNullException(nameof(jsonData));
            }

            JToken? token = jsonData[dataType];

            if (token == null)
            {
                jsonData[dataType] = new JObject();
                token = jsonData[dataType];
            }

            if (token is JObject obj)
            {
                // Sprawdź czy istnieje lista dla danego klucza
                if (!obj.ContainsKey(key))
                {
                    obj[key] = new JArray();
                }

                // Sprawdź czy wartość pod kluczem jest tablicą
                if (obj[key] is JArray array)
                {
                    // Dodaj nową wartość do istniejącej listy pod danym kluczem
                    array.Add(value);
                }
                else if (obj[key] is JValue)
                {
                    // Konwertuj istniejącą wartość na listę i dodaj nową wartość
                    JArray newArray = new JArray();
                    newArray.Add(obj[key]);
                    newArray.Add(value);
                    obj[key] = newArray;
                }
                else
                {
                    throw new InvalidOperationException($"Unexpected type found under key '{key}' in '{dataType}'.");
                }

                jsonData[dataType] = obj;
            }
            else
            {
                throw new InvalidOperationException($"Expected JSON object for data type '{dataType}', but found {token.Type}.");
            }
        }
    }
    internal sealed class JSONStructure
    {
        public string? Token { get; set; }
        public string? Prefix { get; set; }
        public string? ApiGoogle { get; set; }
        public string? CseId { get; set; }
        public string? ApiGPT { get; set; }
        public string? WeatherApi { get; set; }
        public string? LlHostname { get; set; }
        public int LlPort { get; set; }
        public string? LlPass { get; set; }
        public bool Secured { get; set; }
        public string? DefaultRole { get; set; }
        public string? DeleteMessageEmoji { get; set; }
        public string? Motor { get; set; }
        public List<string>? ImageChannels { get; set; }
        public Dictionary<string, List<string>>? BotMessages { get; set; }
        public string? ConfigPath { get; set; }
    }
}
