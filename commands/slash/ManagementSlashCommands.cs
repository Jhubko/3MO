﻿using Discord_Bot.Config;
using Discord_Bot.other;
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
        private static string? configPath = Program.globalConfig.ConfigPath;
        private readonly InventoryManager inventoryManager = new InventoryManager();

        [SlashCommand("help", "Show information about all commands.")]
        public async Task HelpCommand(InteractionContext ctx)
        {
            await ctx.DeferAsync(true);

            var helpEmbed = HelpContent.helpCommandEmbed;

            var selectMenu = new DiscordSelectComponent("help_menu", "Wybierz kategorię", new List<DiscordSelectComponentOption>
            {
                new DiscordSelectComponentOption("Casino", "casino"),
                new DiscordSelectComponentOption("Shop", "shop"),
                new DiscordSelectComponentOption("City", "city"),
                new DiscordSelectComponentOption("Stats", "stats"),
                new DiscordSelectComponentOption("Games", "games"),
                new DiscordSelectComponentOption("Music", "music"),
                new DiscordSelectComponentOption("Search", "search"),
                new DiscordSelectComponentOption("Management", "mngmt")
            });

            var message = new DiscordWebhookBuilder()
                .AddEmbed(helpEmbed)
                .AddComponents(selectMenu);

            await ctx.EditResponseAsync(message);
        }

        [SlashCommand("editfish", "Edit fish data in the fish database.")]
        [RequirePermissions(DSharpPlus.Permissions.Administrator)]
        public async Task EditFish(InteractionContext ctx,
            [Option("action", "The action to perform (add, edit, remove)")] string action,
            [Option("name", "The name of the fish")] string name,
            [Option("minweight", "The minimum weight of the fish", true)] string minWeightStr = null,
            [Option("maxweight", "The maximum weight of the fish", true)] string maxWeightStr = null,
            [Option("baseprice", "The base price of the fish", true)] string basePriceStr = null)
        {

            var fishList = await inventoryManager.LoadFishDataAsync(ctx.Guild.Id);

            double? minWeight = null, maxWeight = null;
            int? basePrice = null;

            if (!string.IsNullOrEmpty(minWeightStr) && double.TryParse(minWeightStr, out double parsedMinWeight))
            {
                minWeight = parsedMinWeight;
            }
            else if (!string.IsNullOrEmpty(minWeightStr))
            {
                await ctx.CreateResponseAsync("❌ Parametr minweight musi być liczbą.", true);
                return;
            }

            if (!string.IsNullOrEmpty(maxWeightStr) && double.TryParse(maxWeightStr, out double parsedMaxWeight))
            {
                maxWeight = parsedMaxWeight;
            }
            else if (!string.IsNullOrEmpty(maxWeightStr))
            {
                await ctx.CreateResponseAsync("❌ Parametr maxweight musi być liczbą.", true);
                return;
            }

            if (!string.IsNullOrEmpty(basePriceStr) && int.TryParse(basePriceStr, out int parsedBasePrice))
            {
                basePrice = parsedBasePrice;
            }
            else if (!string.IsNullOrEmpty(basePriceStr))
            {
                await ctx.CreateResponseAsync("❌ Parametr baseprice musi być liczbą całkowitą.", true);
                return;
            }

            if (action.ToLower() == "add")
            {
                if (string.IsNullOrEmpty(name) || minWeight == null || maxWeight == null || basePrice == null)
                {
                    await ctx.CreateResponseAsync("❌ Proszę podać wszystkie wymagane parametry (name, minweight, maxweight, baseprice).", true);
                    return;
                }

                var existingFish = fishList.FirstOrDefault(f => f.Name.Equals(name, StringComparison.OrdinalIgnoreCase));
                if (existingFish != null)
                {
                    await ctx.CreateResponseAsync($"❌ Ryba o nazwie {name} już istnieje w bazie danych.", true);
                    return;
                }

                fishList.Add(new Fish
                {
                    Name = name,
                    MinWeight = minWeight.Value,
                    MaxWeight = maxWeight.Value,
                    BasePrice = basePrice.Value
                });

                File.WriteAllText($"{configPath}\\{ctx.Guild.Id}_fish_data.json", JsonConvert.SerializeObject(fishList, Formatting.Indented));
                await ctx.CreateResponseAsync($"✅ Ryba {name} została dodana do bazy danych.");
            }
            else if (action.ToLower() == "edit")
            {
                var fishToEdit = fishList.FirstOrDefault(f => f.Name.Equals(name, StringComparison.OrdinalIgnoreCase));
                if (fishToEdit == null)
                {
                    await ctx.CreateResponseAsync($"❌ Nie znaleziono ryby o nazwie {name}.", true);
                    return;
                }

                if (minWeight.HasValue)
                    fishToEdit.MinWeight = minWeight.Value;

                if (maxWeight.HasValue)
                    fishToEdit.MaxWeight = maxWeight.Value;

                if (basePrice.HasValue)
                    fishToEdit.BasePrice = basePrice.Value;

                File.WriteAllText($"{configPath}\\{ctx.Guild.Id}_fish_data.json", JsonConvert.SerializeObject(fishList, Formatting.Indented));
                await ctx.CreateResponseAsync($"✅ Ryba {name} została zaktualizowana.");
            }
            else if (action.ToLower() == "remove")
            {
                var fishToRemove = fishList.FirstOrDefault(f => f.Name.Equals(name, StringComparison.OrdinalIgnoreCase));
                if (fishToRemove == null)
                {
                    await ctx.CreateResponseAsync($"❌ Nie znaleziono ryby o nazwie {name}.", true);
                    return;
                }

                fishList.Remove(fishToRemove);

                File.WriteAllText($"{configPath}\\{ctx.Guild.Id}_fish_data.json", JsonConvert.SerializeObject(fishList, Formatting.Indented));
                await ctx.CreateResponseAsync($"✅ Ryba {name} została usunięta z bazy danych.");
            }
            else
            {
                await ctx.CreateResponseAsync("❌ Nieznana akcja. Wybierz jedną z akcji: add, edit, remove.", true);
            }
        }


        [SlashCommand("defaultRole", "Set default role for server.")]
        [RequirePermissions(DSharpPlus.Permissions.Administrator)]
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

        [SlashCommand("imageOnly", "Set image only channels for your channel.")]
        [RequirePermissions(DSharpPlus.Permissions.Administrator)]
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

        [SlashCommand("raffleChannel", "Set raffle channel for your server.")]
        [RequirePermissions(DSharpPlus.Permissions.Administrator)]
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

        [SlashCommand("deleteMessageEmoji", "Set emoji to delete messages.")]
        [RequirePermissions(DSharpPlus.Permissions.Administrator)]
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
            await GlobalJsonWriter.UpdateShopConfig(shopFilePath, serverConfigDict);

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
            await GlobalJsonWriter.UpdateShopConfig(shopFilePath, serverConfigDict);

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
                        await GlobalJsonWriter.UpdateShopConfig(file, userItems);
                    }
                }
            }
        }
    }
}
