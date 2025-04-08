namespace Discord_Bot.Config
{
    public class GlobalConfig
    {
        public required string Token { get; set; }
        public required string Prefix { get; set; }
        public required string ApiGoogle { get; set; }
        public required string CseId { get; set; }
        public required string ApiGPT { get; set; }
        public required string WeatherApi { get; set; }
        public required string LlHostname { get; set; }
        public required int LlPort { get; set; }
        public required string LlPass { get; set; }
        public required bool Secured { get; set; }
        public required string ConfigPath { get; set; }
    }

    public class ServerConfig
    {
        public string? DefaultRole { get; set; }
        public string? DeleteMessageEmoji { get; set; }
        public List<string>? ImageChannels { get; set; }
        public Dictionary<string, List<string>>? BotMessages { get; set; }
        public string? GamblingChannelId { get; set; }
        public uint RafflePool { get; set; }
        public uint SlotsPool { get; set; } = 2000;
    }

    public class ServerConfigShop
    {
        public List<ShopItem> ShopItems { get; set; } = new List<ShopItem>();
    }

    public class UserInventory
    {
        public List<FishItem> Fish { get; set; } = new List<FishItem>();
        public Dictionary<string, uint> Items { get; set; } = new Dictionary<string, uint>();
    }

    public class UserConfig
    {
        public uint Messages { get; set; } = 0;
        public uint Points { get; set; } = 500;
        public uint Tickets { get; set; } = 0;
        public uint Wins { get; set; } = 0;
        public uint Losses { get; set; } = 0;
        public uint WonPoints { get; set; } = 0;
        public uint LostPoints { get; set; } = 0;
        public uint GivedPoints { get; set; } = 0;
        public uint ReceivedPoints { get; set; } = 0;
        public uint GambleWins { get; set; } = 0;
        public uint GambleLosses { get; set; } = 0;
        public uint SlotsWins { get; set; } = 0;
        public uint SlotsLosses { get; set; } = 0;
        public uint CardsWins { get; set; } = 0;
        public uint CardsLosses { get; set; } = 0;
        public uint DuelWins { get; set; } = 0;
        public uint DuelLosses { get; set; } = 0;
        public uint RaffleTicketsBought { get; set; } = 0;
        public uint RaffleWins { get; set; } = 0;
        public uint RaffleSpent { get; set; } = 0;
        public uint RaffleWinnings { get; set; } = 0;
        public uint TotalCityIncome { get; set; } = 0;
        public uint BoildingsSpent { get; set; } = 0;
        public uint BuildingsBought { get; set; } = 0;
        public uint BuildingsSold { get; set; } = 0;
        public uint WordleWins { get; set; } = 0;
        public uint WordleLosses { get; set; } = 0;
        public uint HangmanWins { get; set; } = 0;
        public uint HangmanLosses { get; set; } = 0;
        public uint FishCaught { get; set; } = 0;
        public uint FishBreakoffs { get; set; } = 0;
        public FishItem? HeaviestFish { get; set; } = new FishItem();
    }

    public class ShopItem
    {
        public required string Name { get; set; }
        public required string Description { get; set; }
        public required uint BaseCost { get; set; }
        public required uint PassivePointsIncrease { get; set; }
    }
    public class Fish
    {
        public required string Name { get; set; }
        public required double MinWeight { get; set; }
        public required double MaxWeight { get; set; }
        public required int BasePrice { get; set; }
    }

    public class FishItem
    {
        public string? Name { get; set; }
        public double Weight { get; set; }
        public int Price { get; set; }
    }
}