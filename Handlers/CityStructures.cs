public class City
{
    public string Name { get; set; } = "Unnamed City";
    public string[][] Grid { get; set; } = new string[CityHandler.CitySize][];
    public int  StoredPoints { get; set; } = 0;

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
    public string Emote { get; set; }
    public string Name { get; set; }
    public int Cost { get; set; }
    public int Income { get; set; }
}