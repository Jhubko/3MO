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
}

public class ShopItem
{
    public string? Name { get; set; }
    public string? Description { get; set; }
    public uint BaseCost { get; set; }
    public uint PassivePointsIncrease { get; set; }
}
public class Fish
{
    public string? Name { get; set; }
    public double MinWeight { get; set; }
    public double MaxWeight { get; set; }
    public int BasePrice { get; set; }
}

public class FishItem
{
    public string? Name { get; set; }
    public double Weight { get; set; }
    public int Price { get; set; }
}