using Discord_Bot.Config;
using Discord_Bot.Handlers;
using Discord_Bot.other;
using DSharpPlus.Entities;
using DSharpPlus.SlashCommands;
namespace Discord_Bot.Commands.Slash
{
    public class ShopCommand : ApplicationCommandModule
    {
        private static readonly JSONReader jsonReader = new();
        private readonly JSONWriter jsonWriter = new(jsonReader, "config.json", Program.serverConfigPath);
        private readonly InventoryManager inventoryManager = new();

        [SlashCommand("buy", "Buy an item from the shop.")]
        public async Task BuyItem(InteractionContext ctx,
            [Autocomplete(typeof(ShopItemAutocomplete))]
            [Option("item", "Wybierz przedmiot ze sklepu do kupienia.")] string itemName)
        {
            ulong userId = ctx.User.Id;
            var userData = await jsonReader.ReadJson<UserConfig>(UserDataPaths.GetUserConfigPath(Program.serverConfigPath, ctx.Guild.Id, userId)) ?? new UserConfig();
            var serverConfig = await jsonReader.ReadJson<ServerConfigShop>($"{Program.serverConfigPath}\\{ctx.Guild.Id}_shop.json") ?? throw new InvalidOperationException("ServerConfigShop cannot be null");

            var pokeball = PokemonService.FindPokeball(itemName);
            var item = pokeball != null
                ? new ShopItem { Name = pokeball.Name, Description = $"Mnożnik szansy złapania: {pokeball.CatchMultiplier:0.#}x.", BaseCost = pokeball.Price, PassivePointsIncrease = 0 }
                : serverConfig.ShopItems.FirstOrDefault(i => i.Name.Equals(itemName, StringComparison.OrdinalIgnoreCase));
            if (item == null)
            {
                await ctx.CreateResponseAsync($"Item '{itemName}' not found in the shop.", true);
                return;
            }

            itemName = item.Name;
            uint currentPoints = userData.Points;
            var inventory = await inventoryManager.GetUserItems(ctx.Guild.Id, userId);
            uint itemCount = inventory.Items.TryGetValue(itemName, out uint value) ? value : 0;
            uint itemCost = pokeball != null ? pokeball.Price : item.BaseCost * (itemCount + 1);

            if (currentPoints < itemCost)
            {
                await ctx.CreateResponseAsync($"Nie masz wystarczającej ilości punktów, aby kupić {itemName}. Potrzebujesz {itemCost} punktów.", true);
                return;
            }

            currentPoints -= itemCost;
            userData.Points = currentPoints;

            if (inventory.Items.TryGetValue(itemName, out uint itemValue))
                inventory.Items[itemName] = ++itemValue;
            else
                inventory.Items[itemName] = 1;

            await jsonWriter.UpdateUserConfig(ctx.Guild.Id, userId, "Points", userData.Points);
            await inventoryManager.UpdateUserItems(ctx.Guild.Id, userId, inventory);

            var embedBuy = new DiscordEmbedBuilder
            {
                Title = "💰 Nowy zakup 💰",
                Description = $"{ctx.User.Mention} kupił **{itemName}**! Ma teraz {inventory.Items[itemName]} sztuk {itemName}.",
                Color = DiscordColor.Blurple
            };
            await ctx.CreateResponseAsync(embed: embedBuy);
        }


        [SlashCommand("shop", "List all items in the shop.")]
        public async Task ListShopItems(InteractionContext ctx)
        {
            var serverConfig = await jsonReader.ReadJson<ServerConfigShop>($"{Program.serverConfigPath}\\{ctx.Guild.Id}_shop.json") ?? throw new InvalidOperationException("ServerConfigShop cannot be null");
            var inventory = await inventoryManager.GetUserItems(ctx.Guild.Id, ctx.User.Id);

            var shopItems = serverConfig.ShopItems
                .Where(i => PokemonService.FindPokeball(i.Name) == null)
                .Select(i => (i.Name, i.Description, Cost: i.BaseCost * (inventory.Items.TryGetValue(i.Name, out uint count) ? count + 1 : 1)))
                .Concat(PokemonService.Pokeballs.Select(ball =>
                    (ball.Name, Description: $"Mnożnik szansy złapania: {ball.CatchMultiplier:0.#}x.", Cost: ball.Price)));

            var itemsList = string.Join("\n", shopItems.Select(i =>
                $"**{i.Name}** - {i.Cost} punktów - {i.Description}"));

            var embedShopList = new DiscordEmbedBuilder
            {
                Title = "🛒 Shop Items 🛒",
                Description = itemsList,
                Color = DiscordColor.Blurple
            };

            await ctx.CreateResponseAsync(embed: embedShopList);
        }


        [SlashCommand("items", "List your items.")]
        public async Task ListUserItems(InteractionContext ctx, [Option("user", "The user to check items for")] DiscordUser? user = null)
        {
            ulong userId = user?.Id ?? ctx.User.Id;
            var inventory = await inventoryManager.GetUserItems(ctx.Guild.Id, userId);

            if (inventory.Items.Count == 0 && inventory.Fish.Count == 0)
            {
                await ctx.CreateResponseAsync($"{user?.Username ?? ctx.User.Username} don't have any items.", true);
                return;
            }

            var itemList = string.Join("\n", inventory.Items.Select(i => $"{i.Key} - {i.Value}"));
            var fishList = string.Join("\n", inventory.Fish.Select(f => $"{f.Name} - {f.Weight}kg ({f.Price} punktów)"));

            var embedUserItems = new DiscordEmbedBuilder
            {
                Title = $"📦 Ekwipunek {user?.Username ?? ctx.User.Username} 📦",
                Description = $"**🎣 Ryby:**\n{(fishList != "" ? fishList : "Brak")}\n\n**🔹 Przedmioty:**\n{(itemList != "" ? itemList : "Brak")}",
                Color = DiscordColor.Blurple
            };

            await ctx.CreateResponseAsync(embed: embedUserItems);
        }
    }
}