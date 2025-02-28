using Discord_Bot;
using DSharpPlus;
using DSharpPlus.Entities;
using DSharpPlus.SlashCommands;
using Discord_Bot.Config;

public class ShopCommand : ApplicationCommandModule
{
    private static IJsonHandler jsonReader = new JSONReader();
    private JSONWriter jsonWriter = new JSONWriter(jsonReader, "config.json", Program.serverConfigPath);
    private readonly string folderPath = $"{Program.globalConfig.ConfigPath}\\user_points";

    [SlashCommand("buy", "Buy an item from the shop.")]
    public async Task BuyItem(InteractionContext ctx, [Option("item", "The name of the item to buy")] string itemName)
    {
        ulong userId = ctx.User.Id;
        var userData = await jsonReader.ReadJson<UserConfig>($"{folderPath}\\{userId}.json");
        var serverConfig = await jsonReader.ReadJson<ServerConfig>($"{Program.serverConfigPath}\\{ctx.Guild.Id}.json");

        var item = serverConfig.ShopItems.FirstOrDefault(i => i.Name.Equals(itemName, StringComparison.OrdinalIgnoreCase));
        if (item == null)
        {
            await ctx.CreateResponseAsync($"Item '{itemName}' not found in the shop.", true);
            return;
        }

        int currentPoints = int.Parse(userData.Points);
        var userItems = await GetUserItems(userId);
        int itemCount = userItems.ContainsKey(itemName) ? userItems[itemName] : 0;
        int itemCost = item.BaseCost * (itemCount + 1);

        if (currentPoints < itemCost)
        {
            await ctx.CreateResponseAsync($"Nie masz wystarczającej ilości punktów, aby kupić {itemName}. Potrzebujesz {itemCost} punktów.", true);
            return;
        }

        currentPoints -= itemCost;
        userData.Points = currentPoints.ToString();
        if (userItems.ContainsKey(itemName))
        {
            userItems[itemName]++;
        }
        else
        {
            userItems[itemName] = 1;
        }

        await jsonWriter.UpdateUserConfig(userId, "Points", userData.Points);
        await UpdateUserItems(userId, userItems);

        await ctx.CreateResponseAsync($"Kupiłeś {itemName}! Masz teraz {userItems[itemName]} {itemName}(s). Koszt następnego {itemName}: {item.BaseCost * (userItems[itemName] + 1)} punktów.", false);
    }

    [SlashCommand("shoplist", "List all items in the shop.")]
    public async Task ListShopItems(InteractionContext ctx)
    {
        var serverConfig = await jsonReader.ReadJson<ServerConfig>($"{Program.serverConfigPath}\\{ctx.Guild.Id}.json");
        var userItems = await GetUserItems(ctx.User.Id);
    
        if (serverConfig.ShopItems == null || !serverConfig.ShopItems.Any())
        {
            await ctx.CreateResponseAsync("The shop is currently empty.", true);
            return;
        }
    
        var itemsList = string.Join("\n", serverConfig.ShopItems.Select(i =>
        {
            int itemCount = userItems.ContainsKey(i.Name) ? userItems[i.Name] : 0;
            int nextItemCost = i.BaseCost * (itemCount + 1);
            return $"{i.Name} - {nextItemCost} points" + (i.Description != null ? $" - {i.Description}" : "");
        }));
    
        await ctx.CreateResponseAsync($"Items available in the shop:\n{itemsList}", false);
    }


    [SlashCommand("items", "List your items.")]
    public async Task ListUserItems(InteractionContext ctx, [Option("user", "The user to check points for")] DiscordUser user = null)
    {
        ulong userId = user?.Id ?? ctx.User.Id;
        var userItems = await GetUserItems(userId);

        if (userItems == null || !userItems.Any())
        {
            await ctx.CreateResponseAsync("You don't have any items.", true);
            return;
        }

        var itemsList = string.Join("\n", userItems.Select(i => $"{i.Key} - {i.Value}"));
        await ctx.CreateResponseAsync($"Your items:\n{itemsList}", false);
    }


    public async Task<Dictionary<string, int>> GetUserItems(ulong userId)
    {
        var userItems = await jsonReader.ReadJson<Dictionary<string, int>>($"{folderPath}\\{userId}_Items.json");
        return userItems;
    }
    
    public async Task UpdateUserItems(ulong userId, Dictionary<string, int> items)
    {
        await jsonWriter.UpdateConfig($"{folderPath}\\{userId}_Items.json", items);
    }

    public async Task UpdateUserItem(ulong userId, string item, int amount)
    {
        var userItems = await GetUserItems(userId);
        if (userItems.ContainsKey(item))
        {
            userItems[item] += amount;
        }
        else
        {
            userItems.Add(item, amount);
        }
        await UpdateUserItems(userId, userItems);
    }
}