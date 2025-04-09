namespace Discord_Bot.Handlers
{
    public class City
    {
        public string Name { get; set; } = "Unnamed City";
        public string[][] Grid { get; set; } = new string[CityHandler.CitySize][];
        public uint StoredPoints { get; set; } = 0;

        public City()
        {
            for (int i = 0; i < CityHandler.CitySize; i++)
            {
                Grid[i] = new string[CityHandler.CitySize];
                for (int j = 0; j < CityHandler.CitySize; j++)
                {
                    Grid[i][j] = "⬜";
                }
            }
        }
    }

    public class Building
    {
        public required string Emote { get; set; }
        public required string Name { get; set; }
        public required uint Cost { get; set; }
        public required uint Income { get; set; }
    }
}