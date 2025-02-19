namespace Discord_Bot.Config
{
    public class GlobalConfig
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
        public string? ConfigPath { get; set; }
        public string? GamblingChannelId { get; set; }
    }

    public class ServerConfig
    {
        public string? DefaultRole { get; set; }
        public string? DeleteMessageEmoji { get; set; }
        public List<string>? ImageChannels { get; set; }
        public Dictionary<string, List<string>>? BotMessages { get; set; }
    }
    public class UserConfig
    {
        public string? Points { get; set; } = "500";
        public string? Tickets { get; set; } = "0";
    }
}
