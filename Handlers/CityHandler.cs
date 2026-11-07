using Discord_Bot;
using Discord_Bot.Config;
using Discord_Bot.Handlers;
using DSharpPlus.Entities;

class CityHandler
{
    private readonly VoicePointsManager _pointsManager = Program.voicePointsManager;
    public const int CitySize = 8;

    public readonly List<Building> Buildings =
    [
        new Building { Emote = "🌲", Name = "Tree", Cost = 1000, Income = 50 },
        new Building { Emote = "🅿️", Name = "Parking", Cost = 5000, Income = 250 },
        new Building { Emote = "🛣️", Name = "Road", Cost = 7000, Income = 350 },
        new Building { Emote = "🏠", Name = "House", Cost = 10000, Income = 500 },
        new Building { Emote = "🏢", Name = "Office", Cost = 25000, Income = 1250 },
        new Building { Emote = "🏤", Name = "Post", Cost = 30000, Income = 1500 },
        new Building { Emote = "🏫", Name = "School", Cost = 35000, Income = 1750 },
        new Building { Emote = "🏬", Name = "Mall", Cost = 50000, Income = 2500 },
        new Building { Emote = "🏦", Name = "Bank", Cost = 60000, Income = 3000 },
        new Building { Emote = "🏥", Name = "Hospital", Cost = 70000, Income = 3500 },
        new Building { Emote = "🏭", Name = "Factory", Cost = 80000, Income = 4000 },
        new Building { Emote = "🏕️", Name = "Camping", Cost = 90000, Income = 4500 },
        new Building { Emote = "⛽", Name = "Gas station", Cost = 120000, Income = 6000 },
        new Building { Emote = "🏙️", Name = "Skyscraper", Cost = 120000, Income = 6000 },
        new Building { Emote = "🏪", Name = "Shop", Cost = 140000, Income = 7000 },
        new Building { Emote = "🏛️", Name = "City Hall", Cost = 180000, Income = 9000 },
        new Building { Emote = "🏰", Name = "Castle", Cost = 220000, Income = 11000 },
        new Building { Emote = "🏨", Name = "Hotel", Cost = 260000, Income = 13000 },
        new Building { Emote = "🛬", Name = "Airport", Cost = 320000, Income = 16000 },
        new Building { Emote = "🎢", Name = "Amusement park", Cost = 450000, Income = 22500 },
    ];

    private static readonly JSONReader jsonReader = new();
    private static readonly JSONWriter jsonWriter = new(jsonReader, "config.json", Program.serverConfigPath);


    public CityHandler()
    {
    }
    public async Task<uint> GetCityPoints(ulong guildId, ulong userId)
    {
        var city = await LoadCity(guildId, userId);
        uint points = city.StoredPoints;
        city.StoredPoints = 0;
        await jsonWriter.UpdateCityConfig(guildId, userId, "StoredPoints", city.StoredPoints);
        return points;
    }

    public async Task<DiscordEmbed> ViewCity(ulong guildId, ulong userId)
    {
        var city = await LoadCity(guildId, userId);
        string cityView = RenderCity(city.Grid);
        return new DiscordEmbedBuilder()
        {
            Title = $"{city.Name}\n{cityView} ",
            Color = DiscordColor.Blurple
        }.Build();
    }

    public async Task<bool> BuyBuilding(ulong guildId, ulong userId, string buildingInput, int x, int y)
    {
        var city = await LoadCity(guildId, userId);

        x -= 1;
        y -= 1;

        var building = Buildings.FirstOrDefault(b =>
            string.Equals(b.Emote, buildingInput, StringComparison.OrdinalIgnoreCase) ||
            string.Equals(b.Name.Trim(), buildingInput.Trim(), StringComparison.OrdinalIgnoreCase)
        );

        if (building == null)
            return false;

        if (x < 0 || x >= CitySize || y < 0 || y >= CitySize || city.Grid[x][y] != "⬜")
            return false;

        uint buildingCost = building.Cost;
        uint userPoints = await _pointsManager.GetUserPoints(guildId, userId);

        if (userPoints < buildingCost)
            return false;

        city.Grid[x][y] = building.Emote;

        userPoints -= buildingCost;
        await _pointsManager.SaveUserPoints(guildId, userId, userPoints);

        await jsonWriter.UpdateCityConfig(guildId, userId, "Grid", city.Grid);

        return true;
    }



    public async Task<bool> SellBuilding(ulong guildId, ulong userId, int x, int y)
    {
        x -= 1;
        y -= 1;
        uint userPoints = await _pointsManager.GetUserPoints(guildId, userId);
        var city = await LoadCity(guildId, userId);
        if (x < 0 || x >= CitySize || y < 0 || y >= CitySize || city.Grid[x][y] == "⬜")
            return false;

        var buildingEmote = city.Grid[x][y];
        var building = Buildings.FirstOrDefault(b => b.Emote == buildingEmote);

        if (building == null)
            return false;

        uint refund = building.Cost / 2;

        city.Grid[x][y] = "⬜";
        userPoints += refund;
        await _pointsManager.SaveUserPoints(guildId, userId, userPoints);

        await jsonWriter.UpdateCityConfig(guildId, userId, "Grid", city.Grid);
        return true;
    }

    public async Task<bool> SetCityName(ulong guildId, ulong userId, string newCityName)
    {
        if (string.IsNullOrWhiteSpace(newCityName))
            return false;

        var city = await LoadCity(guildId, userId);
        city.Name = newCityName;

        await jsonWriter.UpdateCityConfig(guildId, userId, "Name", city.Name);

        return true;
    }

    public async Task<bool> MoveBuilding(ulong guildId, ulong userId, int x1, int y1, int x2, int y2)
    {
        x1 -= 1;
        y1 -= 1;
        x2 -= 1;
        y2 -= 1;

        var city = await LoadCity(guildId, userId);
        if (x1 < 0 || x1 >= CitySize || y1 < 0 || y1 >= CitySize ||
            x2 < 0 || x2 >= CitySize || y2 < 0 || y2 >= CitySize)
            return false;

        (city.Grid[x1][y1], city.Grid[x2][y2]) = (city.Grid[x2][y2], city.Grid[x1][y1]);
        await jsonWriter.UpdateCityConfig(guildId, userId, "Grid", city.Grid);
        return true;
    }

    public async Task GenerateDailyIncome()
    {
        string citiesPath = Path.Combine(Program.serverConfigPath, "cities");
        if (!Directory.Exists(citiesPath))
            return;

        foreach (var guildDirectory in Directory.GetDirectories(citiesPath))
        {
            if (!ulong.TryParse(Path.GetFileName(guildDirectory), out ulong guildId))
                continue;

            foreach (var file in Directory.GetFiles(guildDirectory, "*_city.json"))
            {
                if (!ulong.TryParse(Path.GetFileNameWithoutExtension(file).Split('_')[0], out ulong userId))
                {
                    Console.WriteLine($"Skipping city file with an invalid user ID: {file}");
                    continue;
                }

                var city = await LoadCity(guildId, userId);
                foreach (var row in city.Grid)
                {
                    foreach (var cell in row)
                    {
                        var building = Buildings.FirstOrDefault(b => b.Emote == cell);
                        if (building != null)
                            city.StoredPoints += building.Income;
                    }
                }

                await jsonWriter.UpdateCityConfig(guildId, userId, "StoredPoints", city.StoredPoints);
            }
        }
    }

    private async Task<City> LoadCity(ulong guildId, ulong userId)
    {
        return await jsonReader.ReadJson<City>(UserDataPaths.GetCityPath(Program.serverConfigPath, guildId, userId)) ?? throw new InvalidOperationException("City cannot be null at this point");
    }

    public string RenderCity(string[][] grid)
    {
        string result = "";
        foreach (var row in grid)
        {
            result += string.Join(" ", row) + "\n";
        }
        return result;
    }
}