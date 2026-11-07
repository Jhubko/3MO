using Discord_Bot.Handlers;
using Discord_Bot.other;
using DSharpPlus.Entities;
using DSharpPlus.SlashCommands;

namespace Discord_Bot.Commands.Slash
{
    public class GiveItemsCommand : ApplicationCommandModule
    {
        private static readonly InventoryManager inventoryManager = new();

        [SlashCommand("giveitem", "Przekaż przedmiot lub Pokémona innemu użytkownikowi.")]
        public async Task GiveItem(InteractionContext ctx,
            [Option("recipient", "Osoba, której chcesz przekazać przedmiot.")] DiscordUser recipient,
            [Autocomplete(typeof(UserItemAutocomplete))]
            [Option("item", "Wybierz przedmiot ze swojego ekwipunku.")] string itemName,
            [Option("amount", "Ile sztuk przedmiotu przekazać.")] long amount)
        {
            if (recipient.Id == ctx.User.Id)
            {
                await ctx.CreateResponseAsync("Nie możesz przekazać przedmiotu samemu sobie.", true);
                return;
            }

            if (recipient.IsBot)
            {
                await ctx.CreateResponseAsync("Nie można przekazywać przedmiotów botom.", true);
                return;
            }

            if (amount <= 0 || amount > uint.MaxValue)
            {
                await ctx.CreateResponseAsync("Liczba przekazywanych przedmiotów musi być większa od zera.", true);
                return;
            }

            await InventoryManager.InventoryTransactionSemaphore.WaitAsync();
            try
            {
                var senderInventory = await inventoryManager.GetUserItems(ctx.User.Id);
                var recipientInventory = await inventoryManager.GetUserItems(recipient.Id);
                if (itemName.StartsWith("pokemon:", StringComparison.Ordinal))
                {
                    if (amount != 1)
                    {
                        await ctx.CreateResponseAsync("Pokémony przekazuje się pojedynczo — ustaw liczbę na 1.", true);
                        return;
                    }

                    string pokemonId = itemName["pokemon:".Length..];
                    var pokemon = senderInventory.Pokemons.FirstOrDefault(entry => entry.Id == pokemonId);
                    if (pokemon == null)
                    {
                        await ctx.CreateResponseAsync("Nie masz już tego Pokémona w kolekcji. Odśwież podpowiedzi i spróbuj ponownie.", true);
                        return;
                    }

                    senderInventory.Pokemons.Remove(pokemon);
                    recipientInventory.Pokemons.Add(pokemon);
                    await inventoryManager.UpdateUserItems(recipient.Id, recipientInventory);
                    await inventoryManager.UpdateUserItems(ctx.User.Id, senderInventory);
                    await ctx.CreateResponseAsync(embed: new DiscordEmbedBuilder
                    {
                        Title = "🎁 Przekazano Pokémona",
                        Description = $"{ctx.User.Mention} przekazał(a) **#{pokemon.PokedexId} {pokemon.Name}** użytkownikowi {recipient.Mention}.",
                        Color = DiscordColor.Gold
                    });
                    return;
                }

                if (!itemName.StartsWith("item:", StringComparison.Ordinal))
                {
                    await ctx.CreateResponseAsync("Nieprawidłowy wybór przedmiotu. Wybierz pozycję z podpowiedzi.", true);
                    return;
                }

                string inventoryItemName = itemName["item:".Length..];
                if (!senderInventory.Items.TryGetValue(inventoryItemName, out uint senderAmount) || senderAmount < (uint)amount)
                {
                    await ctx.CreateResponseAsync($"Nie masz wystarczającej liczby przedmiotów **{inventoryItemName}**.", true);
                    return;
                }

                senderAmount -= (uint)amount;
                if (senderAmount == 0)
                    senderInventory.Items.Remove(inventoryItemName);
                else
                    senderInventory.Items[inventoryItemName] = senderAmount;

                if (recipientInventory.Items.TryGetValue(inventoryItemName, out uint recipientAmount))
                {
                    if (uint.MaxValue - recipientAmount < (uint)amount)
                    {
                        await ctx.CreateResponseAsync("Odbiorca nie może pomieścić takiej liczby przedmiotów.", true);
                        return;
                    }
                    recipientInventory.Items[inventoryItemName] = recipientAmount + (uint)amount;
                }
                else
                {
                    recipientInventory.Items[inventoryItemName] = (uint)amount;
                }

                await inventoryManager.UpdateUserItems(recipient.Id, recipientInventory);
                await inventoryManager.UpdateUserItems(ctx.User.Id, senderInventory);

                var embed = new DiscordEmbedBuilder
                {
                    Title = "🎁 Przekazano przedmiot",
                    Description = $"{ctx.User.Mention} przekazał(a) **{amount} × {inventoryItemName}** użytkownikowi {recipient.Mention}.",
                    Color = DiscordColor.Gold
                };
                await ctx.CreateResponseAsync(embed: embed);
            }
            finally
            {
                InventoryManager.InventoryTransactionSemaphore.Release();
            }
        }
    }
}
