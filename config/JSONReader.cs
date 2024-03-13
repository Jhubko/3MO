using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace Discord_Bot.config
{
    internal class JSONReader
    {
        public string token { get; set; }
        public string prefix { get; set; }
        public string apikey { get; set; }
        public string cseId { get; set; }
        public string apiGPT { get; set; }
        public string llHostname { get; set; }
        public int llPort { get; set; }
        public string llPass { get; set; }
        public bool secured { get; set; }
        public string defaultRole { get; set; }
        public string configPath { get; set; }

        public async Task ReadJSON(string jsonName = "config.json")
        {
            using (StreamReader sr = new StreamReader(jsonName))
            {
                string json = await sr.ReadToEndAsync();
                JSONStructure data = JsonConvert.DeserializeObject<JSONStructure>(json);
                this.token = data.Token;
                this.prefix = data.Prefix;
                this.apikey = data.Apikey;
                this.cseId = data.CseId;
                this.apiGPT = data.ApiGPT;
                this.llHostname = data.LlHostname;
                this.llPort = data.LlPort;
                this.llPass = data.LlPass;
                this.secured = data.Secured;
                this.defaultRole = data.DefaultRole;
                this.configPath = data.ConfigPath;
            }
        }

        public async void UpdateJSON(ulong index, string dataType, string Content)
        {
            await ReadJSON();

            string filePath = Path.Combine(configPath, $"{index}.json");

            if (!File.Exists(index.ToString()))
            {
                CreateJSON(index, dataType, Content);
                return;
            }

            string existingJson = File.ReadAllText(filePath);
            JObject jsonData = JObject.Parse(existingJson);

            jsonData[dataType] = Content;

            string updatedJson = JsonConvert.SerializeObject(jsonData, Formatting.Indented);
            File.WriteAllText(filePath, updatedJson);
        }

        public void CreateJSON(ulong index, string dataType, string Content)
        {
            string filePath = Path.Combine(configPath, $"{index}.json");
            var data = new Dictionary<string, string>();
            data[dataType] = Content;

            string json = JsonConvert.SerializeObject(data);
            File.WriteAllText(filePath, json);
        }

    }
    internal sealed class JSONStructure
    {
        public string Token { get; set; }
        public string Prefix { get; set; }
        public string Apikey { get; set; }
        public string CseId { get; set; }
        public string ApiGPT { get; set; }
        public string LlHostname { get; set; }
        public int LlPort { get; set; }
        public string LlPass { get; set; }
        public bool Secured { get; set; }
        public string DefaultRole { get; set; }
        public string ConfigPath { get; set; }
    }
}
