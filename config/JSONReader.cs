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
        public List<string>? ImageChannels { get; set; }
        public string? ConfigPath { get; set; }

        private readonly List<string> arrayDataTypes = new List<string>() { "ImageOnlyChannels" };

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
                    this.DefaultRole = data.DefaultRole;
                    this.ImageChannels = data.ImageOnlyChannels;
                }
            }
        }

        public async Task UpdateJSON(ulong index, string dataType, string Content)
        {
            await ReadJSON();

            string filePath = Path.Combine(ConfigPath, $"{index}.json");
            FileInfo fileInfo = new FileInfo(filePath);

            if (!File.Exists(filePath) || fileInfo.Length == 0)
            {
                CreateJSON(index, dataType, Content);
                return;
            }

            string existingJson = File.ReadAllText(filePath);
            JObject jsonData = JObject.Parse(existingJson);          

            if (arrayDataTypes.Contains(dataType))
                UpdateArrayDataType(jsonData, dataType, Content);
            else
                jsonData[dataType] = Content;

            string updatedJson = JsonConvert.SerializeObject(jsonData, Formatting.Indented);
            File.WriteAllText(filePath, updatedJson);

        }

        public void CreateJSON(ulong index, string? dataType = null, string? Content = null)
        {
            string filePath = Path.Combine(ConfigPath, $"{index}.json");
            var data = new Dictionary<string, object>();

            if (dataType != null && Content != null)
            {
                if (arrayDataTypes.Contains(dataType))
                    data[dataType] = new List<string>() { Content };
                else
                    data[dataType] = Content;

                string json = JsonConvert.SerializeObject(data);
                File.WriteAllText(filePath, json);
            }
            else
            {
                File.WriteAllText(filePath, null);
            }

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
        public List<string>? ImageOnlyChannels { get; set; }
        public string? ConfigPath { get; set; }
    }
}
