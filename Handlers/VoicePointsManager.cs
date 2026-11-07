using Discord_Bot;
using Discord_Bot.Config;
using Discord_Bot.Handlers;
using System.Collections.Concurrent;
using DSharpPlus;
using DSharpPlus.EventArgs;

class VoicePointsManager
{
    private readonly ConcurrentDictionary<(ulong GuildId, ulong UserId), byte> activeUsers;
    private static readonly JSONReader jsonReader = new();
    private readonly InventoryManager inventoryManager = new();
    private readonly JSONWriter jsonWriter = new(jsonReader, "config.json", Program.serverConfigPath);
    public VoicePointsManager()
    {
        activeUsers = new();
        Directory.CreateDirectory(Path.Combine(Program.serverConfigPath, "user_points"));
    }

    public async Task CollectActiveUsers(DiscordClient client)
    {
        foreach (var guild in client.Guilds.Values)
        {
            foreach (var channel in await guild.GetChannelsAsync())
            {
                if (channel.Type == ChannelType.Voice)
                {
                    var users = channel.Users;
                    foreach (var user in users)
                    {
                        var activeUser = (GuildId: guild.Id, UserId: user.Id);
                        if (activeUsers.TryAdd(activeUser, 0))
                        {
                            uint currentPoints = await LoadUserPoints(guild.Id, user.Id);
                            uint passivePoints = await CalculatePassivePoints(guild.Id, user.Id);
                            currentPoints += passivePoints;
                            await SaveUserPoints(guild.Id, user.Id, currentPoints);
                        }
                    }
                }
            }
        }

        Console.WriteLine("Active users have been collected and their points updated.");
    }

    private async Task<uint> LoadUserPoints(ulong guildId, ulong userId)
    {
        var userConfig = await jsonReader.ReadJson<UserConfig>(UserDataPaths.GetUserConfigPath(Program.serverConfigPath, guildId, userId)) ?? throw new InvalidOperationException("UserConfig cannot be null");
        return userConfig.Points;
    }

    public async Task SaveUserPoints(ulong guildId, ulong userId, uint points)
    {
        await jsonWriter.UpdateUserConfig(guildId, userId, "Points", points);
    }

    public async Task OnVoiceStateUpdated(DiscordClient client, VoiceStateUpdateEventArgs e)
    {
        ulong userId = e.User.Id;
        ulong guildId = e.Before.Channel?.Guild.Id ?? e.After.Channel?.Guild.Id
            ?? throw new InvalidOperationException("Voice state update has no guild channel.");
        var activeUser = (GuildId: guildId, UserId: userId);

        if (e.After.Channel != null)
        {
            activeUsers.TryAdd(activeUser, 0);
        }
        else if (e.Before.Channel != null)
        {
            activeUsers.TryRemove(activeUser, out _);
        }

        await Task.CompletedTask;
    }

    public async Task AddPointsLoop()
    {
        while (true)
        {
            await Task.Delay(TimeSpan.FromMinutes(1));
            foreach (var (guildId, userId) in activeUsers.Keys)
            {
                uint currentPoints = await LoadUserPoints(guildId, userId);
                uint passivePoints = await CalculatePassivePoints(guildId, userId);
                currentPoints += passivePoints;
                await SaveUserPoints(guildId, userId, currentPoints);
            }
        }
    }

    public async Task<uint> GetUserPoints(ulong guildId, ulong userId)
    {
        return await LoadUserPoints(guildId, userId);
    }

    private async Task<uint> CalculatePassivePoints(ulong guildId, ulong userId)
    {
        var serverConfig = await jsonReader.ReadJson<ServerConfigShop>($"{Program.serverConfigPath}\\{guildId}_shop.json") ?? throw new InvalidOperationException("ServerConfigShop cannot be null");

        uint passivePoints = 10;
        var userItems = await inventoryManager.GetUserItems(guildId, userId);
        foreach (var item in userItems.Items)
        {
            var shopItem = serverConfig.ShopItems.FirstOrDefault(i => i.Name.Equals(item.Key, StringComparison.OrdinalIgnoreCase));
            if (shopItem != null)
            {
                passivePoints += shopItem.PassivePointsIncrease * item.Value;
            }
        }

        return passivePoints;
    }
}

