using Discord_Bot;
using DSharpPlus.SlashCommands;
using Discord_Bot.Config;
using Newtonsoft.Json;

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
        int itemCount = userData.PurchasedItems.ContainsKey(itemName) ? userData.PurchasedItems[itemName] : 0;
        int itemCost = item.BaseCost * (itemCount + 1);

        if (currentPoints < itemCost)
        {
            await ctx.CreateResponseAsync($"Nie masz wystarczającej ilości punktów, aby kupić {itemName}. Potrzebujesz {itemCost} punktów.", true);
            return;
        }

        currentPoints -= itemCost;
        userData.Points = currentPoints.ToString();
        if (userData.PurchasedItems.ContainsKey(itemName))
        {
            userData.PurchasedItems[itemName]++;
        }
        else
        {
            userData.PurchasedItems[itemName] = 1;
        }

        await jsonWriter.UpdateUserConfig(userId, "Points", userData.Points);
        await jsonWriter.UpdateUserConfig(userId, "PurchasedItems", userData.PurchasedItems);

        await ctx.CreateResponseAsync($"Kupiłeś {itemName}! Masz teraz {userData.PurchasedItems[itemName]} {itemName}(s). Koszt następnego {itemName}: {item.BaseCost * (userData.PurchasedItems[itemName] + 1)} punktów.", false);
    }

    [SlashCommand("shoplist", "List all items in the shop.")]
    public async Task ListItems(InteractionContext ctx)
    {
        var serverConfig = await jsonReader.ReadJson<ServerConfig>($"{Program.serverConfigPath}\\{ctx.Guild.Id}.json");

        if (serverConfig.ShopItems == null || !serverConfig.ShopItems.Any())
        {
            await ctx.CreateResponseAsync("The shop is currently empty.", true);
            return;
        }

        var itemsList = string.Join("\n", serverConfig.ShopItems.Select(i => $"{i.Name} - {i.BaseCost} points" + (i.Description != null ? $" - {i.Description}" : "")));
        await ctx.CreateResponseAsync($"Items available in the shop:\n{itemsList}", false);
    }
}