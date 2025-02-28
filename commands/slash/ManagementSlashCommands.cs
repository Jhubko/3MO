using Discord_Bot.Config;
using Discord_Bot.other;
using DSharpPlus.CommandsNext;
using DSharpPlus.CommandsNext.Attributes;
using DSharpPlus.Entities;
using DSharpPlus.SlashCommands;
using Newtonsoft.Json;
using System.Text.RegularExpressions;

namespace Discord_Bot.commands.slash
{
    internal class ManagementSlashCommands : ApplicationCommandModule
    {
        private static IJsonHandler jsonReader = new JSONReader();
        private JSONWriter GlobalJsonWriter = new JSONWriter(jsonReader, "config.json", Program.serverConfigPath);

        [SlashCommand("help", "Show information about all commands.")]
        public async Task HelpCommand(InteractionContext ctx)
        {
            await ctx.DeferAsync();

            var helpEmbed = Buttons.helpCommandEmbed;

            var message = new DiscordWebhookBuilder()
                .AddEmbed(helpEmbed)
                .AddComponents(Buttons.gamesButton, Buttons.searchButton, Buttons.mngmtButton, Buttons.musicButton);

            await ctx.EditResponseAsync(message);
        }

        [SlashCommand("defaultRole", "Set default role for server.")]
        public async Task DefaultRoleCommand(InteractionContext ctx, [Option("newDefaultRole", "Role you want to be default for this server.")][RemainingText] string newDefaultRole)
        {
            await ctx.DeferAsync();

            foreach (var role in ctx.Guild.Roles)
            {
                if (role.Value.Name == newDefaultRole)
                {
                    await GlobalJsonWriter.UpdateServerConfig(ctx.Guild.Id, "DefaultRole", role.Key.ToString());
                    await ctx.EditResponseAsync(new DiscordWebhookBuilder().WithContent($"New default role set to: {newDefaultRole}")).ConfigureAwait(false);
                    return;
                }
            }
            await ctx.EditResponseAsync(new DiscordWebhookBuilder().WithContent($"Role '{newDefaultRole}' not found on this server.")).ConfigureAwait(false);
        }

        [Command("imageOnly")]
        [SlashCommand("imageOnly", "Set image only channels for your channel.")]
        public async Task ImageOnlyChannelCommand(InteractionContext ctx, [Option("channelToChange", "Channel name you want to be image only channels for your channel")][RemainingText] string channelToChange)
        {
            await ctx.DeferAsync();

            foreach (var channel in ctx.Guild.Channels)
            {
                if (channel.Value.Name == channelToChange)
                {
                    await GlobalJsonWriter.UpdateServerConfig(ctx.Guild.Id, "ImageOnlyChannels", channel.Key.ToString());
                    await ctx.EditResponseAsync(new DiscordWebhookBuilder().WithContent($"{channelToChange} was changed to image only.")).ConfigureAwait(false);
                    return;
                }
            }
            await ctx.EditResponseAsync(new DiscordWebhookBuilder().WithContent($"Channel: '{channelToChange}' not found on this server.")).ConfigureAwait(false);
        }

        [Command("raffleChannel")]
        [SlashCommand("raffleChannel", "Set raffle channel for your server.")]
        public async Task RaffleChannelCommand(InteractionContext ctx, [Option("channelToChange", "Channel name you want to be raffle channel for your server")][RemainingText] string channelToChange)
        {
            await ctx.DeferAsync();

            foreach (var channel in ctx.Guild.Channels)
            {
                if (channel.Value.Name == channelToChange)
                {
                    await GlobalJsonWriter.UpdateServerConfig(ctx.Guild.Id, "GamblingChannelId", channel.Key.ToString());
                    await ctx.EditResponseAsync(new DiscordWebhookBuilder().WithContent($"{channelToChange} was changed to raffle channel.")).ConfigureAwait(false);
                    return;
                }
            }
            await ctx.EditResponseAsync(new DiscordWebhookBuilder().WithContent($"Channel: '{channelToChange}' not found on this server.")).ConfigureAwait(false);
        }

        [Command("deleteMessageEmoji")]
        [SlashCommand("deleteMessageEmoji", "Set emoji to delete messages.")]
        public async Task DeleteMessageEmojiCommand(InteractionContext ctx, [Option("emoji", "Emoji, that will start vote to delete message.")][RemainingText] string emoji)
        {
            await ctx.DeferAsync();

            if (DiscordEmoji.IsValidUnicode(emoji))
            {
                await GlobalJsonWriter.UpdateServerConfig(ctx.Guild.Id, "DeleteMessageEmoji", emoji);
                await ctx.EditResponseAsync(new DiscordWebhookBuilder().WithContent($"Delete emoji was set to: {emoji}")).ConfigureAwait(false);
                return;
            }
            else
            {
                string pattern = "<:([^:]+):\\d+>";
                Match match = Regex.Match(emoji, pattern);
                string emojiToSet = match.Groups[1].Value;
                foreach (var e in ctx.Guild.Emojis.ToList())
                {
                    if (e.Value.Name == emojiToSet)
                    {
                        await GlobalJsonWriter.UpdateServerConfig(ctx.Guild.Id, "DeleteMessageEmoji", $":{emojiToSet}:");
                        await ctx.EditResponseAsync(new DiscordWebhookBuilder().WithContent($"Delete emoji was set to: {emoji}")).ConfigureAwait(false);
                        return;
                    }
                }
            }
            await ctx.EditResponseAsync(new DiscordWebhookBuilder().WithContent($"Emoji '{emoji}' was not found.")).ConfigureAwait(false);
        }

        [SlashCommand("createShopItem", "Create a new item in the shop.")]
        [RequirePermissions(DSharpPlus.Permissions.Administrator)]
        public async Task CreateShopItemCommand(InteractionContext ctx, 
            [Option("name", "The name of the item")] string name, 
            [Option("description", "The description of the item")] string description, 
            [Option("basePrice", "The base price of the item")] string baseCost, 
            [Option("passivePointsIncrease", "The passive points increase of the item")] string passivePointsIncrease)
        {
            await ctx.DeferAsync();

            var shopFilePath = $"{Program.serverConfigPath}\\{ctx.Guild.Id}_shop.json";
            var serverConfig = await jsonReader.ReadJson<ServerConfigShop>(shopFilePath) ?? new ServerConfigShop();

            // Check for duplicate item
            if (serverConfig.ShopItems.Any(i => i.Name.Equals(name, StringComparison.OrdinalIgnoreCase)))
            {
            await ctx.EditResponseAsync(new DiscordWebhookBuilder().WithContent($"Item '{name}' already exists in the shop."));
            return;
            }

            var shopItem = new ShopItem
            {
            Name = name,
            Description = description,
            BaseCost = GambleUtils.ParseInt(baseCost),
            PassivePointsIncrease = GambleUtils.ParseInt(passivePointsIncrease)
            };
            // If base cost or passive points less equal 0, return invalid value response
            if (shopItem.BaseCost < 0 || shopItem.PassivePointsIncrease < 0)
            {
            await ctx.EditResponseAsync(new DiscordWebhookBuilder().WithContent("Base price or/and passive points increase are invalid."));
            return;
            }

            serverConfig.ShopItems.Add(shopItem);
            var serverConfigDict = new Dictionary<string, object>
            {
            { "ShopItems", serverConfig.ShopItems }
            };
            await GlobalJsonWriter.UpdateConfig(shopFilePath, serverConfigDict);

            var embed = new DiscordEmbedBuilder
            {
            Title = "Shop Item Created",
            Description = $"Item '{name}' has been created and added to the shop.",
            Color = DiscordColor.Green
            };
            await ctx.EditResponseAsync(new DiscordWebhookBuilder().AddEmbed(embed));
        }

        [SlashCommand("removeShopItem", "Remove an item from the shop.")]
        [RequirePermissions(DSharpPlus.Permissions.Administrator)]
        public async Task RemoveShopItemCommand(InteractionContext ctx, [Option("name", "The name of the item to remove")] string name)
        {
            await ctx.DeferAsync();

            var shopFilePath = $"{Program.serverConfigPath}\\{ctx.Guild.Id}_shop.json";
            var serverConfig = await jsonReader.ReadJson<ServerConfigShop>(shopFilePath) ?? new ServerConfigShop();

            var itemToRemove = serverConfig.ShopItems.FirstOrDefault(i => i.Name.Equals(name, StringComparison.OrdinalIgnoreCase));
            if (itemToRemove == null)
            {
            await ctx.EditResponseAsync(new DiscordWebhookBuilder().WithContent($"Item '{name}' not found in the shop."));
            return;
            }

            serverConfig.ShopItems.Remove(itemToRemove);
            var serverConfigDict = new Dictionary<string, object>
            {
            { "ShopItems", serverConfig.ShopItems }
            };
            await GlobalJsonWriter.UpdateConfig(shopFilePath, serverConfigDict);

            // Remove the item from all users
            await RemoveItemFromAllUsers(name);

            var embed = new DiscordEmbedBuilder
            {
            Title = "Shop Item Removed",
            Description = $"Item '{name}' has been removed from the shop and from all users.",
            Color = DiscordColor.Red
            };
            await ctx.EditResponseAsync(new DiscordWebhookBuilder().AddEmbed(embed));
        }

        private async Task RemoveItemFromAllUsers(string itemName)
        {
            foreach (var file in Directory.GetFiles($"{Program.serverConfigPath}\\user_points", "*_Items.json"))
            {
            var userItems = await jsonReader.ReadJson<Dictionary<string, int>>(file);
            if (userItems != null)
            {
                var itemKey = userItems.Keys.FirstOrDefault(k => k.Equals(itemName, StringComparison.OrdinalIgnoreCase));
                if (itemKey != null)
                {
                userItems[itemKey] = 0;
                await GlobalJsonWriter.UpdateConfig(file, userItems);
                }
            }
            }
        }
    }
}
