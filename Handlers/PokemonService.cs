namespace Discord_Bot.Handlers
{
    public static class PokemonService
    {
        public static IReadOnlyList<PokeballType> Pokeballs { get; } =
        [
            new("Poké Ball", 1000, 1.0, "🔴"),
            new("Great Ball", 5000, 1.5, "🔵"),
            new("Ultra Ball", 10000, 2.0, "🟡"),
            new("Master Ball", 20000, 5.0, "⚫")
        ];

        public static PokeballType? FindPokeball(string name) =>
            Pokeballs.FirstOrDefault(ball => ball.Name.Equals(name, StringComparison.OrdinalIgnoreCase));
    }

    public sealed record PokeballType(string Name, uint Price, double CatchMultiplier, string Emoji);
}
