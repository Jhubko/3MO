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
    }

    public class ServerConfig
    {
        public string? DefaultRole { get; set; }
        public string? DeleteMessageEmoji { get; set; }
        public List<string>? ImageChannels { get; set; }
        public Dictionary<string, List<string>>? BotMessages { get; set; }
        public string? GamblingChannelId { get; set; }
        public string? RafflePool { get; set; }
        public string? SlotsPool { get; set; } = "2000";
    }
    public class UserConfig
    {
        public string? Messages { get; set; } = "0";
        public string? Points { get; set; } = "500";
        public string? Tickets { get; set; } = "0";
        public string? Wins { get; set; } = "0";
        public string? Losses { get; set; } = "0";
        public string? WonPoints { get; set; } = "0";
        public string? LostPoints { get; set; } = "0";
        public string? GambleWins { get; set; } = "0";
        public string? GambleLosses { get; set; } = "0";
        public string? SlotsWins { get; set; } = "0";
        public string? SlotsLosses { get; set; } = "0";
        public string? CardsWins { get; set; } = "0";
        public string? CardsLosses { get; set; } = "0";
        public string? DuelWins { get; set; } = "0";
        public string? DuelLosses { get; set; } = "0";
    }
}
