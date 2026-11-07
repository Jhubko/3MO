using Discord_Bot.Config;
using Discord_Bot.Handlers;
using Discord_Bot.other;
using DSharpPlus;
using DSharpPlus.Entities;
using DSharpPlus.Interactivity.Extensions;
using DSharpPlus.SlashCommands;
using System.Collections.Concurrent;
using System.Globalization;
using System.Text.Json;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.Formats.Png;
using SixLabors.ImageSharp.PixelFormats;
using SixLabors.ImageSharp.Processing;

namespace Discord_Bot.Commands.Slash
{
    public class PokemonCommand : ApplicationCommandModule
    {
        private static readonly HttpClient httpClient = new()
        {
            BaseAddress = new Uri("https://pokeapi.co/api/v2/"),
            Timeout = TimeSpan.FromSeconds(15)
        };
        private static readonly ConcurrentDictionary<(ulong GuildId, ulong UserId), byte> activeEncounters = new();
        private readonly InventoryManager inventoryManager = new();

        [SlashCommand("pokemonencounter", "Spotkaj losowego Pokémona i wybierz Poké Balla podczas spotkania.")]
        public async Task PokemonEncounter(InteractionContext ctx)
        {
            var encounterKey = (GuildId: ctx.Guild.Id, UserId: ctx.User.Id);
            if (!activeEncounters.TryAdd(encounterKey, 0))
            {
                await ctx.CreateResponseAsync("Masz już aktywne spotkanie z Pokémonem.", true);
                return;
            }

            try
            {
                await ctx.DeferAsync();
                var inventory = await inventoryManager.GetUserItems(ctx.Guild.Id, ctx.User.Id);
                var ballsRemaining = PokemonService.Pokeballs.ToDictionary(
                    ball => ball,
                    ball => GetPokeballCount(inventory, ball.Name));

                if (ballsRemaining.Values.All(count => count == 0))
                {
                    await ctx.FollowUpAsync(new DiscordFollowupMessageBuilder().WithContent("Nie masz Poké Balli. Kup jeden w sklepie komendą `/shop`."));
                    return;
                }

                PokemonEncounterData pokemon;
                try
                {
                    pokemon = await GetRandomPokemonAsync();
                }
                catch (HttpRequestException ex)
                {
                    Console.WriteLine($"PokéAPI request failed: {ex.Message}");
                    await ctx.FollowUpAsync(new DiscordFollowupMessageBuilder().WithContent("Nie udało się połączyć z PokéAPI. Żaden Poké Ball nie został zużyty — spróbuj ponownie później."));
                    return;
                }
                catch (TaskCanceledException ex)
                {
                    Console.WriteLine($"PokéAPI request timed out: {ex.Message}");
                    await ctx.FollowUpAsync(new DiscordFollowupMessageBuilder().WithContent("PokéAPI nie odpowiedziało na czas. Żaden Poké Ball nie został zużyty — spróbuj ponownie później."));
                    return;
                }
                catch (JsonException ex)
                {
                    Console.WriteLine($"PokéAPI returned invalid JSON: {ex.Message}");
                    await ctx.FollowUpAsync(new DiscordFollowupMessageBuilder().WithContent("PokéAPI zwróciło nieprawidłową odpowiedź. Żaden Poké Ball nie został zużyty — spróbuj ponownie później."));
                    return;
                }

                double baseCatchChance = Math.Clamp(pokemon.CaptureRate / 255.0, 0.05, 0.8);
                var fleeEmoji = DiscordEmoji.FromUnicode("🏃");
                var interactivity = ctx.Client.GetInteractivity();
                var encounterEmbed = new DiscordEmbedBuilder
                {
                    Title = $"Dziki Pokémon: {pokemon.Name}",
                    Description = BuildEncounterDescription(pokemon, baseCatchChance, ballsRemaining),
                    Color = DiscordColor.Blurple
                };
                if (!string.IsNullOrWhiteSpace(pokemon.SpriteUrl))
                    encounterEmbed.WithImageUrl(pokemon.SpriteUrl);

                var message = await ctx.FollowUpAsync(new DiscordFollowupMessageBuilder()
                    .WithContent($"{ctx.User.Mention} spotyka Pokémona!")
                    .AddEmbed(encounterEmbed));
                await AddEncounterReactionsAsync(message, ballsRemaining, fleeEmoji);

                while (ballsRemaining.Values.Any(count => count > 0))
                {
                    var availableBalls = PokemonService.Pokeballs
                        .Where(ball => ballsRemaining[ball] > 0)
                        .ToArray();
                    var availableEmojis = availableBalls
                        .Select(ball => DiscordEmoji.FromUnicode(ball.Emoji))
                        .ToArray();
                    var reaction = await interactivity.WaitForReactionAsync(
                        x => x.Message.Id == message.Id
                            && (x.Emoji == fleeEmoji || availableEmojis.Contains(x.Emoji))
                            && x.User.Id == ctx.User.Id,
                        TimeSpan.FromSeconds(30));

                    if (reaction.TimedOut)
                    {
                        await message.DeleteAsync();
                        await ctx.FollowUpAsync(new DiscordFollowupMessageBuilder().WithContent($"⏰ {pokemon.Name} uciekł. Niezużyte Poké Balle: {GetRemainingBallsDescription(ballsRemaining)}."));
                        return;
                    }

                    if (reaction.Result.Emoji == fleeEmoji)
                    {
                        await message.DeleteAsync();
                        await ctx.FollowUpAsync(new DiscordFollowupMessageBuilder().WithContent($"🏃 Uciekasz przed {pokemon.Name}. Nie zużyto żadnego Poké Balla podczas tej ucieczki."));
                        return;
                    }

                    var selectedBall = availableBalls.First(ball =>
                        DiscordEmoji.FromUnicode(ball.Emoji) == reaction.Result.Emoji);
                    uint remaining = --ballsRemaining[selectedBall];
                    if (remaining == 0)
                        inventory.Items.Remove(selectedBall.Name);
                    else
                        inventory.Items[selectedBall.Name] = remaining;

                    double catchChance = Math.Min(1.0, baseCatchChance * selectedBall.CatchMultiplier);
                    if (Random.Shared.NextDouble() < catchChance)
                    {
                        inventory.Pokemons.Add(new PokemonInventoryItem
                        {
                            Name = pokemon.Name,
                            PokedexId = pokemon.PokedexId,
                            SpriteUrl = pokemon.SpriteUrl,
                            CaughtAt = DateTime.UtcNow
                        });
                        await inventoryManager.UpdateUserItems(ctx.Guild.Id, ctx.User.Id, inventory);
                        await message.DeleteAsync();
                        await ctx.FollowUpAsync(new DiscordFollowupMessageBuilder().WithContent($"✅ {ctx.User.Mention} złapał(a) **{pokemon.Name}** używając **{selectedBall.Name}**! Sprawdź kolekcję komendą `/pokedex`."));
                        return;
                    }

                    await inventoryManager.UpdateUserItems(ctx.Guild.Id, ctx.User.Id, inventory);
                    await message.DeleteAsync();
                    if (ballsRemaining.Values.All(count => count == 0))
                    {
                        await ctx.FollowUpAsync(new DiscordFollowupMessageBuilder().WithContent($"❌ {pokemon.Name} uwolnił się i uciekł. Nie masz już Poké Balli."));
                        return;
                    }

                    encounterEmbed.Description = $"Pokémon uwolnił się z **{selectedBall.Name}**!\n{BuildEncounterDescription(pokemon, baseCatchChance, ballsRemaining)}";
                    message = await ctx.FollowUpAsync(new DiscordFollowupMessageBuilder()
                        .WithContent($"{ctx.User.Mention}, wybierz kolejny Poké Ball albo uciekaj!")
                        .AddEmbed(encounterEmbed));
                    await AddEncounterReactionsAsync(message, ballsRemaining, fleeEmoji);
                }
            }
            finally
            {
                activeEncounters.TryRemove(encounterKey, out _);
            }
        }

        [SlashCommand("pokedex", "Wyświetl kolekcję Pokémonów w siatce 3×3.")]
        public async Task Pokedex(InteractionContext ctx,
            [Option("user", "Kolekcja użytkownika do wyświetlenia.")] DiscordUser? user = null,
            [Option("page", "Numer strony Pokédexu (9 Pokémonów na stronę).")]
            long page = 1)
        {
            ulong userId = user?.Id ?? ctx.User.Id;
            var inventory = await inventoryManager.GetUserItems(ctx.Guild.Id, userId);
            if (inventory.Pokemons.Count == 0)
            {
                await ctx.CreateResponseAsync($"{user?.Username ?? ctx.User.Username} nie ma jeszcze żadnego Pokémona.", true);
                return;
            }

            var pokemonGroups = inventory.Pokemons
                .GroupBy(pokemon => pokemon.PokedexId)
                .OrderBy(group => group.Key)
                .ToArray();
            int totalPages = (int)Math.Ceiling(pokemonGroups.Length / 9.0);
            if (page < 1 || page > totalPages)
            {
                await ctx.CreateResponseAsync($"Nieprawidłowy numer strony. Dostępne strony: 1–{totalPages}.", true);
                return;
            }

            await ctx.DeferAsync();
            var (image, message) = await BuildPokedexPageAsync(inventory, user?.Username ?? ctx.User.Username, ctx.User.Id, userId, (int)page);
            using (image)
                await ctx.EditResponseAsync(message);
        }

        public static async Task<(MemoryStream Image, DiscordWebhookBuilder Message)> BuildPokedexPageAsync(
            UserInventory inventory,
            string username,
            ulong requesterId,
            ulong collectionUserId,
            int page)
        {
            const int pageSize = 9;
            const int tileSize = 160;
            const int spriteSize = 128;
            var pokemonGroups = inventory.Pokemons
                .GroupBy(pokemon => pokemon.PokedexId)
                .OrderBy(group => group.Key)
                .ToArray();
            int totalPages = (int)Math.Ceiling(pokemonGroups.Length / (double)pageSize);
            if (page < 1 || page > totalPages)
                throw new ArgumentOutOfRangeException(nameof(page), "Pokédex page is outside the available range.");

            var pageGroups = pokemonGroups.Skip((page - 1) * pageSize).Take(pageSize).ToArray();
            var spriteDownloads = await Task.WhenAll(pageGroups.Select(async group =>
            {
                var pokemon = group.First();
                string spriteUrl = pokemon.SpriteUrl
                    ?? $"https://raw.githubusercontent.com/PokeAPI/sprites/master/sprites/pokemon/{pokemon.PokedexId}.png";
                try
                {
                    return await httpClient.GetByteArrayAsync(spriteUrl);
                }
                catch (Exception exception) when (exception is HttpRequestException or TaskCanceledException)
                {
                    Console.WriteLine($"Unable to download Pokédex sprite for #{pokemon.PokedexId}: {exception.Message}");
                    return null;
                }
            }));

            using var grid = new Image<Rgba32>(tileSize * 3, tileSize * 3, new Rgba32(255, 255, 255, 0));
            for (int index = 0; index < pageGroups.Length; index++)
            {
                if (spriteDownloads[index] is not byte[] spriteBytes)
                    continue;

                try
                {
                    using var sprite = Image.Load<Rgba32>(spriteBytes);
                    sprite.Mutate(operation => operation.Resize(new ResizeOptions
                    {
                        Size = new Size(spriteSize, spriteSize),
                        Mode = ResizeMode.Max,
                        Sampler = KnownResamplers.NearestNeighbor
                    }));

                    int x = (index % 3) * tileSize + (tileSize - sprite.Width) / 2;
                    int y = (index / 3) * tileSize + (tileSize - sprite.Height) / 2;
                    grid.Mutate(operation => operation.DrawImage(sprite, new Point(x, y), 1f));
                }
                catch (InvalidImageContentException exception)
                {
                    Console.WriteLine($"Invalid Pokédex sprite for #{pageGroups[index].Key}: {exception.Message}");
                }
            }

            var imageStream = new MemoryStream();
            await grid.SaveAsync(imageStream, new PngEncoder());
            imageStream.Position = 0;

            string pokemonList = string.Join("\n", pageGroups.Select((group, index) =>
                $"{index + 1}. #{group.Key} {group.First().Name} ×{group.Count()}"));
            var embed = new DiscordEmbedBuilder
            {
                Title = $"📖 Pokédex — {username} — strona {page}/{totalPages}",
                Description = $"```\n{pokemonList}\n```",
                ImageUrl = "attachment://pokedex.png",
                Color = DiscordColor.Blurple
            };
            var builder = new DiscordWebhookBuilder()
                .AddFile("pokedex.png", imageStream)
                .AddEmbed(embed);
            if (totalPages > 1)
            {
                builder.AddComponents(
                    new DiscordButtonComponent(DSharpPlus.ButtonStyle.Primary, $"pokedex:{requesterId}:{collectionUserId}:{page - 1}", "⬅️ Poprzednia", page == 1),
                    new DiscordButtonComponent(DSharpPlus.ButtonStyle.Primary, $"pokedex:{requesterId}:{collectionUserId}:{page + 1}", "Następna ➡️", page == totalPages));
            }

            return (imageStream, builder);
        }

        [SlashCommand("pokemontrade", "Wymień jednego duplikata Pokémona na zwykły Poké Ball.")]
        public async Task TradeDuplicatePokemon(InteractionContext ctx,
            [Autocomplete(typeof(DuplicatePokemonAutocomplete))]
            [Option("pokemon", "Wybierz gatunek, którego duplikat chcesz wymienić.")] string pokedexIdValue)
        {
            if (!int.TryParse(pokedexIdValue, out int pokedexId))
            {
                await ctx.CreateResponseAsync("Wybierz Pokémona z podpowiedzi.", true);
                return;
            }

            await InventoryManager.InventoryTransactionSemaphore.WaitAsync();
            try
            {
                var inventory = await inventoryManager.GetUserItems(ctx.Guild.Id, ctx.User.Id);
                var duplicates = inventory.Pokemons.Where(pokemon => pokemon.PokedexId == pokedexId).ToList();
                if (duplicates.Count < 2)
                {
                    await ctx.CreateResponseAsync("Do wymiany potrzebujesz co najmniej dwóch Pokémonów tego samego gatunku.", true);
                    return;
                }

                if (inventory.Items.TryGetValue(PokemonService.Pokeballs[0].Name, out uint pokeballCount)
                    && pokeballCount == uint.MaxValue)
                {
                    await ctx.CreateResponseAsync("Nie możesz pomieścić kolejnego Poké Balla.", true);
                    return;
                }

                inventory.Pokemons.Remove(duplicates[^1]);
                inventory.Items[PokemonService.Pokeballs[0].Name] = pokeballCount + 1;
                await inventoryManager.UpdateUserItems(ctx.Guild.Id, ctx.User.Id, inventory);
                await ctx.CreateResponseAsync($"Wymieniono jednego duplikata **#{pokedexId} {duplicates[0].Name}** na **1 Poké Ball**.");
            }
            finally
            {
                InventoryManager.InventoryTransactionSemaphore.Release();
            }
        }

        [SlashCommand("pokemoninfo", "Wyświetl szczegółowe informacje o swoim Pokémonie.")]
        public async Task PokemonInfo(InteractionContext ctx,
            [Autocomplete(typeof(PokemonInventoryAutocomplete))]
            [Option("pokemon", "Wybierz Pokémona ze swojej kolekcji po nazwie lub numerze.")] string pokemonQuery)
        {
            var inventory = await inventoryManager.GetUserItems(ctx.Guild.Id, ctx.User.Id);
            PokemonInventoryItem? ownedPokemon = int.TryParse(pokemonQuery, out int pokedexId)
                ? inventory.Pokemons.FirstOrDefault(pokemon => pokemon.PokedexId == pokedexId)
                : inventory.Pokemons.FirstOrDefault(pokemon => pokemon.Name.Equals(pokemonQuery, StringComparison.OrdinalIgnoreCase));

            if (ownedPokemon == null)
            {
                await ctx.CreateResponseAsync("Nie masz takiego Pokémona w kolekcji. Sprawdź posiadane Pokémony komendą `/pokedex`.", true);
                return;
            }

            await ctx.DeferAsync();
            try
            {
                var details = await GetPokemonDetailsAsync(ownedPokemon.PokedexId);
                var embed = new DiscordEmbedBuilder
                {
                    Title = $"#{ownedPokemon.PokedexId} {ownedPokemon.Name}",
                    Description = details.Description,
                    Color = DiscordColor.Blurple
                };

                if (!string.IsNullOrWhiteSpace(ownedPokemon.SpriteUrl))
                    embed.WithImageUrl(ownedPokemon.SpriteUrl);
                else if (!string.IsNullOrWhiteSpace(details.SpriteUrl))
                    embed.WithImageUrl(details.SpriteUrl);

                embed.AddField("Typy", details.Types, true);
                embed.AddField("Wzrost", $"{details.HeightMeters:0.0} m", true);
                embed.AddField("Waga", $"{details.WeightKilograms:0.0} kg", true);
                embed.AddField("Umiejętności", details.Abilities);
                embed.AddField("Statystyki bazowe", details.Stats);
                embed.AddField("Złapany", ownedPokemon.CaughtAt.ToString("dd.MM.yyyy HH:mm UTC"), true);

                await ctx.FollowUpAsync(new DiscordFollowupMessageBuilder().AddEmbed(embed));
            }
            catch (HttpRequestException ex)
            {
                Console.WriteLine($"PokéAPI request failed while loading Pokémon info: {ex.Message}");
                await ctx.FollowUpAsync(new DiscordFollowupMessageBuilder().WithContent("Nie udało się pobrać informacji z PokéAPI. Spróbuj ponownie później."));
            }
            catch (TaskCanceledException ex)
            {
                Console.WriteLine($"PokéAPI request timed out while loading Pokémon info: {ex.Message}");
                await ctx.FollowUpAsync(new DiscordFollowupMessageBuilder().WithContent("PokéAPI nie odpowiedziało na czas. Spróbuj ponownie później."));
            }
            catch (JsonException ex)
            {
                Console.WriteLine($"PokéAPI returned invalid JSON while loading Pokémon info: {ex.Message}");
                await ctx.FollowUpAsync(new DiscordFollowupMessageBuilder().WithContent("PokéAPI zwróciło nieprawidłowe dane. Spróbuj ponownie później."));
            }
        }

        private static async Task<PokemonDetails> GetPokemonDetailsAsync(int pokedexId)
        {
            using var pokemonResponse = await httpClient.GetAsync($"pokemon/{pokedexId}");
            pokemonResponse.EnsureSuccessStatusCode();
            using var pokemonDocument = JsonDocument.Parse(await pokemonResponse.Content.ReadAsStringAsync());
            var pokemon = pokemonDocument.RootElement;

            string types = string.Join(", ", pokemon.GetProperty("types").EnumerateArray()
                .Select(type => type.GetProperty("type").GetProperty("name").GetString()));
            string abilities = string.Join(", ", pokemon.GetProperty("abilities").EnumerateArray()
                .Select(ability => ability.GetProperty("ability").GetProperty("name").GetString()));
            string stats = string.Join(" | ", pokemon.GetProperty("stats").EnumerateArray()
                .Select(stat => $"{stat.GetProperty("stat").GetProperty("name").GetString()}: {stat.GetProperty("base_stat").GetInt32()}"));
            double heightMeters = pokemon.GetProperty("height").GetInt32() / 10.0;
            double weightKilograms = pokemon.GetProperty("weight").GetInt32() / 10.0;
            string? spriteUrl = pokemon.GetProperty("sprites").GetProperty("front_default").GetString();

            using var speciesResponse = await httpClient.GetAsync($"pokemon-species/{pokedexId}");
            speciesResponse.EnsureSuccessStatusCode();
            using var speciesDocument = JsonDocument.Parse(await speciesResponse.Content.ReadAsStringAsync());
            string? description = speciesDocument.RootElement.GetProperty("flavor_text_entries")
                .EnumerateArray()
                .FirstOrDefault(entry => entry.GetProperty("language").GetProperty("name").GetString() == "en")
                .GetProperty("flavor_text")
                .GetString();

            return new PokemonDetails(
                string.IsNullOrWhiteSpace(description) ? "Brak opisu w PokéAPI." : string.Join(" ", description.Split((char[]?)null, StringSplitOptions.RemoveEmptyEntries)),
                string.IsNullOrWhiteSpace(types) ? "Nieznany" : types,
                string.IsNullOrWhiteSpace(abilities) ? "Nieznane" : abilities,
                string.IsNullOrWhiteSpace(stats) ? "Brak danych" : stats,
                heightMeters,
                weightKilograms,
                spriteUrl);
        }

        private sealed record PokemonDetails(
            string Description,
            string Types,
            string Abilities,
            string Stats,
            double HeightMeters,
            double WeightKilograms,
            string? SpriteUrl);

        private static uint GetPokeballCount(UserInventory inventory, string pokeballName)
        {
            return inventory.Items.TryGetValue(pokeballName, out uint count) ? count : 0;
        }

        private static string BuildEncounterDescription(
            PokemonEncounterData pokemon,
            double baseCatchChance,
            IReadOnlyDictionary<PokeballType, uint> ballsRemaining)
        {
            string ballChoices = string.Join("\n", PokemonService.Pokeballs
                .Where(ball => ballsRemaining[ball] > 0)
                .Select(ball => $"{ball.Emoji} **{ball.Name}** — {ballsRemaining[ball]} szt., szansa: **{Math.Min(1.0, baseCatchChance * ball.CatchMultiplier):P0}** (×{ball.CatchMultiplier:0.#})"));

            return $"Pokédex **#{pokemon.PokedexId}**\nWybierz reakcją typ Balla, którego chcesz użyć:\n{ballChoices}\n🏃 Ucieczka — nie zużywa Balla.";
        }

        private static async Task AddEncounterReactionsAsync(
            DSharpPlus.Entities.DiscordMessage message,
            IReadOnlyDictionary<PokeballType, uint> ballsRemaining,
            DiscordEmoji fleeEmoji)
        {
            foreach (var ball in PokemonService.Pokeballs.Where(ball => ballsRemaining[ball] > 0))
                await message.CreateReactionAsync(DiscordEmoji.FromUnicode(ball.Emoji));

            await message.CreateReactionAsync(fleeEmoji);
        }

        private static string GetRemainingBallsDescription(IReadOnlyDictionary<PokeballType, uint> ballsRemaining) =>
            string.Join(", ", PokemonService.Pokeballs
                .Where(ball => ballsRemaining[ball] > 0)
                .Select(ball => $"{ball.Name}: {ballsRemaining[ball]}"));

        private static async Task<PokemonEncounterData> GetRandomPokemonAsync()
        {
            int pokedexId = Random.Shared.Next(1, 1026);
            using var speciesResponse = await httpClient.GetAsync($"pokemon-species/{pokedexId}");
            speciesResponse.EnsureSuccessStatusCode();
            using var speciesDocument = JsonDocument.Parse(await speciesResponse.Content.ReadAsStringAsync());
            var species = speciesDocument.RootElement;
            int captureRate = species.GetProperty("capture_rate").GetInt32();
            string name = species.GetProperty("name").GetString() ?? $"#{pokedexId}";

            foreach (var localizedName in species.GetProperty("names").EnumerateArray())
            {
                if (localizedName.GetProperty("language").GetProperty("name").GetString() == "pl")
                {
                    name = localizedName.GetProperty("name").GetString() ?? name;
                    break;
                }
            }

            using var pokemonResponse = await httpClient.GetAsync($"pokemon/{pokedexId}");
            pokemonResponse.EnsureSuccessStatusCode();
            using var pokemonDocument = JsonDocument.Parse(await pokemonResponse.Content.ReadAsStringAsync());
            string? spriteUrl = pokemonDocument.RootElement
                .GetProperty("sprites")
                .GetProperty("front_default")
                .GetString();

            return new PokemonEncounterData(pokedexId, name, captureRate, spriteUrl);
        }

        private sealed record PokemonEncounterData(int PokedexId, string Name, int CaptureRate, string? SpriteUrl);
    }
}
