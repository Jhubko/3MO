using Discord_Bot.Config;
using DSharpPlus.Entities;
using DSharpPlus.SlashCommands;
namespace Discord_Bot.Commands.Slash
{
    public class ShopCommand : ApplicationCommandModule
    {
        private static readonly JSONReader jsonReader = new();
        private readonly JSONWriter jsonWriter = new(jsonReader, "config.json", Program.serverConfigPath);
        private readonly string folderPath = $"{Program.globalConfig.ConfigPath}\\user_points";
        private readonly InventoryManager inventoryManager = new();

        [SlashCommand("buy", "Buy an item from the shop.")]
        public async Task BuyItem(InteractionContext ctx, [Option("item", "The name of the item to buy")] string itemName)
        {
            ulong userId = ctx.User.Id;
            var userData = await jsonReader.ReadJson<UserConfig>($"{folderPath}\\{userId}.json") ?? new UserConfig();
            var serverConfig = await jsonReader.ReadJson<ServerConfigShop>($"{Program.serverConfigPath}\\{ctx.Guild.Id}_shop.json") ?? throw new InvalidOperationException("ServerConfigShop cannot be null");

            var item = serverConfig.ShopItems.FirstOrDefault(i => i.Name.Equals(itemName, StringComparison.OrdinalIgnoreCase));
            if (item == null)
            {
                await ctx.CreateResponseAsync($"Item '{itemName}' not found in the shop.", true);
                return;
            }

            itemName = item.Name;
            uint currentPoints = userData.Points;
            var inventory = await inventoryManager.GetUserItems(userId);
            uint itemCount = inventory.Items.TryGetValue(itemName, out uint value) ? value : 0;
            uint itemCost = item.BaseCost * (itemCount + 1);

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

            await jsonWriter.UpdateUserConfig(userId, "Points", userData.Points);
            await inventoryManager.UpdateUserItems(userId, inventory);

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
            var inventory = await inventoryManager.GetUserItems(ctx.User.Id);

            if (serverConfig.ShopItems == null || serverConfig.ShopItems.Count == 0)
            {
                await ctx.CreateResponseAsync("The shop is currently empty.", true);
                return;
            }

            var itemsList = string.Join("\n", serverConfig.ShopItems.Select(i =>
            {
                uint itemCount = inventory.Items.TryGetValue(i.Name, out uint value) ? value : 0;
                uint nextItemCost = i.BaseCost * (itemCount + 1);
                return $"**{i.Name}** - {nextItemCost} punktów" + (i.Description != null ? $" - {i.Description}" : "");
            }));

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
            var inventory = await inventoryManager.GetUserItems(userId);

            if (inventory.Items.Count == 0 && inventory.Fish.Count == 0)
            {
                await ctx.CreateResponseAsync("You don't have any items.", true);
                return;
            }

            var itemList = string.Join("\n", inventory.Items.Select(i => $"{i.Key} - {i.Value}"));
            var fishList = string.Join("\n", inventory.Fish.Select(f => $"{f.Name} - {f.Weight}kg ({f.Price} punktów)"));

            var embedUserItems = new DiscordEmbedBuilder
            {
                Title = $"📦 Ekwipunek {ctx.User.Username} 📦",
                Description = $"**🎣 Ryby:**\n{(fishList != "" ? fishList : "Brak")}\n\n**🔹 Przedmioty:**\n{(itemList != "" ? itemList : "Brak")}",
                Color = DiscordColor.Blurple
            };

            await ctx.CreateResponseAsync(embed: embedUserItems);
        }
    }
}