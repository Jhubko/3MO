using Discord_Bot;
using Discord_Bot.Config;
using Discord_Bot.Handlers;
using DSharpPlus;
using DSharpPlus.EventArgs;

class VoicePointsManager
{
    private readonly string folderPath = $"{Program.globalConfig.ConfigPath}\\user_points";
    private readonly HashSet<ulong> activeUsers;
    private static readonly JSONReader jsonReader = new();
    private readonly InventoryManager inventoryManager = new();
    private readonly JSONWriter jsonWriter = new(jsonReader, "config.json", Program.serverConfigPath);
    public VoicePointsManager()
    {

        activeUsers = [];
        Directory.CreateDirectory(folderPath);
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
                        if (!activeUsers.Contains(user.Id))
                        {
                            activeUsers.Add(user.Id);

                            uint currentPoints = await LoadUserPoints(user.Id);
                            uint passivePoints = await CalculatePassivePoints(user.Id);
                            currentPoints += passivePoints;
                            SaveUserPoints(user.Id, currentPoints);
                        }
                    }
                }
            }
        }

        Console.WriteLine("Active users have been collected and their points updated.");
    }

    private async Task<uint> LoadUserPoints(ulong userId)
    {
        var userConfig = await jsonReader.ReadJson<UserConfig>($"{folderPath}\\{userId}.json") ?? throw new InvalidOperationException("UserConfig cannot be null");
        return userConfig.Points;
    }

    public async void SaveUserPoints(ulong userId, uint points)
    {
        await jsonWriter.UpdateUserConfig(userId, "Points", points);
    }

    public async Task OnVoiceStateUpdated(DiscordClient client, VoiceStateUpdateEventArgs e)
    {
        ulong userId = e.User.Id;

        if (e.After.Channel != null)
        {
            activeUsers.Add(userId);
        }
        else if (e.Before.Channel != null && e.After.Channel == null)
        {
            activeUsers.Remove(userId);
        }

        await Task.CompletedTask;
    }

    public async Task AddPointsLoop()
    {
        while (true)
        {
            await Task.Delay(TimeSpan.FromMinutes(1));
            foreach (ulong userId in activeUsers)
            {
                uint currentPoints = await LoadUserPoints(userId);
                uint passivePoints = await CalculatePassivePoints(userId);
                currentPoints += passivePoints;
                SaveUserPoints(userId, currentPoints);
            }
        }
    }

    public async Task<uint> GetUserPoints(ulong userId)
    {
        return await LoadUserPoints(userId);
    }

    private async Task<uint> CalculatePassivePoints(ulong userId)
    {
        var guild = Program.Client.Guilds.Values
            .FirstOrDefault(g => g.VoiceStates.TryGetValue(userId, out var voiceState) && voiceState.Channel != null);

        if (guild == null)
            return 0;

        var userConfig = await jsonReader.ReadJson<UserConfig>($"{folderPath}\\{userId}.json");
        var serverConfig = await jsonReader.ReadJson<ServerConfigShop>($"{Program.serverConfigPath}\\{guild.Id}_shop.json") ?? throw new InvalidOperationException("ServerConfigShop cannot be null");

        uint passivePoints = 10;
        var userItems = await inventoryManager.GetUserItems(userId);
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

