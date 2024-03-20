using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace Discord_Bot.config
{
    internal class JSONReader
    {
        public string? token { get; set; }
        public string? prefix { get; set; }
        public string? apikey { get; set; }
        public string? cseId { get; set; }
        public string? apiGPT { get; set; }
        public string? llHostname { get; set; }
        public int llPort { get; set; }
        public string? llPass { get; set; }
        public bool secured { get; set; }
        public string? defaultRole { get; set; }
        public List<string>? imageChannels { get; set; }
        public string? configPath { get; set; }

        private List<string> arrayDataTypes = new List<string>() { "ImageOnlyChannels" };

        public async Task ReadJSON(string jsonName = "config.json")
        {
            using (StreamReader sr = new StreamReader(jsonName))
            {
                string json = await sr.ReadToEndAsync();
                JSONStructure? data = JsonConvert.DeserializeObject<JSONStructure>(json);
                this.token = this.token ?? data.Token;
                this.prefix = this.prefix ?? data.Prefix;
                this.apikey = this.apikey ?? data.Apikey;
                this.cseId = this.cseId ?? data.CseId;
                this.apiGPT = this.apiGPT ?? data.ApiGPT;
                this.llHostname = this.llHostname ?? data.LlHostname;
                this.llPort = this.llPort == 0 ? data.LlPort : this.llPort; 
                this.llPass = this.llPass ?? data.LlPass;
                this.secured = this.secured || data.Secured;
                this.defaultRole = this.defaultRole ?? data.DefaultRole;
                this.configPath = this.configPath ?? data.ConfigPath;
                this.imageChannels = this.imageChannels ?? data.ImageOnlyChannels;
            }
        }

        public async void UpdateJSON(ulong index, string dataType, string Content)
        {
            await ReadJSON();

            string filePath = Path.Combine(configPath, $"{index}.json");

            if (!File.Exists(filePath))
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

        public void CreateJSON(ulong index, string dataType, string Content)
        {
            string filePath = Path.Combine(configPath, $"{index}.json");
            var data = new Dictionary<string, object>();

            if (arrayDataTypes.Contains(dataType))
                data[dataType] = new List<string>() { Content };
            else
                data[dataType] = Content;

            string json = JsonConvert.SerializeObject(data);
            File.WriteAllText(filePath, json);
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
        public string? Apikey { get; set; }
        public string? CseId { get; set; }
        public string? ApiGPT { get; set; }
        public string? LlHostname { get; set; }
        public int LlPort { get; set; }
        public string? LlPass { get; set; }
        public bool Secured { get; set; }
        public string? DefaultRole { get; set; }
        public List<string>? ImageOnlyChannels { get; set; }
        public string? ConfigPath { get; set; }
    }
}
