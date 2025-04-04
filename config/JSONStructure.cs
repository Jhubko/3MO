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
        public string SlotsPool { get; set; } = "2000";
    }

    public class ServerConfigShop
    {
        public List<ShopItem> ShopItems { get; set; } = new List<ShopItem>();
    }

    public class UserInventory
    {
        public List<FishItem> Fish { get; set; } = new List<FishItem>();
        public Dictionary<string, int> Items { get; set; } = new Dictionary<string, int>();
    }

    public class UserConfig
    {
        public string Messages { get; set; } = "0";
        public string Points { get; set; } = "500";
        public string Tickets { get; set; } = "0";
        public string Wins { get; set; } = "0";
        public string Losses { get; set; } = "0";
        public string WonPoints { get; set; } = "0";
        public string LostPoints { get; set; } = "0";
        public string GivedPoints { get; set; } = "0";
        public string ReceivedPoints { get; set; } = "0";
        public string GambleWins { get; set; } = "0";
        public string GambleLosses { get; set; } = "0";
        public string SlotsWins { get; set; } = "0";
        public string SlotsLosses { get; set; } = "0";
        public string CardsWins { get; set; } = "0";
        public string CardsLosses { get; set; } = "0";
        public string DuelWins { get; set; } = "0";
        public string DuelLosses { get; set; } = "0";
        public string RaffleTicketsBought { get; set; } = "0";
        public string RaffleWins { get; set; } = "0";
        public string RaffleSpent { get; set; } = "0";
        public string RaffleWinnings { get; set; } = "0";
        public string TotalCityIncome { get; set; } = "0";
        public string BoildingsSpent { get; set; } = "0";
        public string BuildingsBought { get; set; } = "0";
        public string BuildingsSold { get; set; } = "0";
        public string WordleWins { get; set; } = "0";
        public string WordleLosses { get; set; } = "0";
        public string HangmanWins { get; set; } = "0";
        public string HangmanLosses { get; set; } = "0";
        public string FishCaught { get; set; } = "0";
        public string FishBreakoffs { get; set; } = "0";
        public FishItem? HeaviestFish { get; set; } = new FishItem();
    }
}

public class ShopItem
{
    public string? Name { get; set; }
    public string? Description { get; set; }
    public int BaseCost { get; set; }
    public int PassivePointsIncrease { get; set; }
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