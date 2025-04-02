using Discord_Bot;
using Discord_Bot.Config;
using DSharpPlus.Entities;
using DSharpPlus.SlashCommands;

public class ShopCommand : ApplicationCommandModule
{
    private static IJsonHandler jsonReader = new JSONReader();
    private JSONWriter jsonWriter = new JSONWriter(jsonReader, "config.json", Program.serverConfigPath);
    private readonly string folderPath = $"{Program.globalConfig.ConfigPath}\\user_points";
    private readonly InventoryManager inventoryManager = new InventoryManager();

    [SlashCommand("buy", "Buy an item from the shop.")]
    public async Task BuyItem(InteractionContext ctx, [Option("item", "The name of the item to buy")] string itemName)
    {
        ulong userId = ctx.User.Id;
        var userData = await jsonReader.ReadJson<UserConfig>($"{folderPath}\\{userId}.json") ?? new UserConfig();
        var serverConfig = await jsonReader.ReadJson<ServerConfigShop>($"{Program.serverConfigPath}\\{ctx.Guild.Id}_shop.json");

        var item = serverConfig.ShopItems.FirstOrDefault(i => i.Name.Equals(itemName, StringComparison.OrdinalIgnoreCase));
        if (item == null)
        {
            await ctx.CreateResponseAsync($"Item '{itemName}' not found in the shop.", true);
            return;
        }

        itemName = item.Name;
        int currentPoints = int.Parse(userData.Points);
        var inventory = await inventoryManager.GetUserItems(userId);
        int itemCount = inventory.Items.ContainsKey(itemName) ? inventory.Items[itemName] : 0;
        int itemCost = item.BaseCost * (itemCount + 1);

        if (currentPoints < itemCost)
        {
            await ctx.CreateResponseAsync($"Nie masz wystarczającej ilości punktów, aby kupić {itemName}. Potrzebujesz {itemCost} punktów.", true);
            return;
        }

        currentPoints -= itemCost;
        userData.Points = currentPoints.ToString();

        if (inventory.Items.ContainsKey(itemName))
            inventory.Items[itemName]++;
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
        var serverConfig = await jsonReader.ReadJson<ServerConfigShop>($"{Program.serverConfigPath}\\{ctx.Guild.Id}_shop.json");
        var inventory = await inventoryManager.GetUserItems(ctx.User.Id);

        if (serverConfig.ShopItems == null || !serverConfig.ShopItems.Any())
        {
            await ctx.CreateResponseAsync("The shop is currently empty.", true);
            return;
        }

        var itemsList = string.Join("\n", serverConfig.ShopItems.Select(i =>
        {
            int itemCount = inventory.Items.ContainsKey(i.Name) ? inventory.Items[i.Name] : 0;
            int nextItemCost = i.BaseCost * (itemCount + 1);
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
    public async Task ListUserItems(InteractionContext ctx, [Option("user", "The user to check items for")] DiscordUser user = null)
    {
        ulong userId = user?.Id ?? ctx.User.Id;
        var inventory = await inventoryManager.GetUserItems(userId);

        if (!inventory.Items.Any() && !inventory.Fish.Any())
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