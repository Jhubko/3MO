namespace Discord_Bot.Config
{
    public interface IJsonHandler
    {
        Task<T?> ReadJson<T>(string filePath) where T : class, new();
        Task WriteJson<T>(string filePath, T data) where T : class;
        void CreateJson(string filePath);
    }
}
