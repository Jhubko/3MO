using Newtonsoft.Json;

namespace Discord_Bot.Config
{
    public class JSONReader : IJsonHandler
    {
        public async Task<T?> ReadJson<T>(string filePath) where T : class
        {
            if (!File.Exists(filePath)) return null;

            using StreamReader sr = new(filePath);
            string json = await sr.ReadToEndAsync();
            return JsonConvert.DeserializeObject<T>(json);
        }

        public async Task WriteJson<T>(string filePath, T data) where T : class
        {
            string json = JsonConvert.SerializeObject(data, Formatting.Indented);
            await File.WriteAllTextAsync(filePath, json);
        }

        public void CreateJson(string filePath)
        {
            File.WriteAllText(filePath, "{}");
        }
    }
}
