﻿using Discord_Bot;
using Discord_Bot.Config;
using DSharpPlus;
using DSharpPlus.EventArgs;
using System.Reflection;

class VoicePointsManager
{
    private readonly string folderPath = $"{Program.globalConfig.ConfigPath}\\user_points";
    private HashSet<ulong> activeUsers;
    private static IJsonHandler jsonReader = new JSONReader();
    private readonly InventoryManager inventoryManager = new InventoryManager();
    private JSONWriter jsonWriter = new JSONWriter(jsonReader, "config.json", Program.serverConfigPath);
    public VoicePointsManager()
    {
        
        activeUsers = new HashSet<ulong>();
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

                            int currentPoints = await LoadUserPoints(user.Id);
                            int passivePoints = await CalculatePassivePoints(user.Id);
                            currentPoints += passivePoints;
                            SaveUserPoints(user.Id, currentPoints);
                        }
                    }
                }
            }
        }

        Console.WriteLine("Active users have been collected and their points updated.");
    }


    private async Task<int> LoadUserPoints(ulong userId)
    {
        var userConfig = await jsonReader.ReadJson<UserConfig>($"{folderPath}\\{userId}.json");
        return int.Parse(userConfig.Points);
    }

    public async void SaveUserPoints(ulong userId, int points)
    {
       await jsonWriter.UpdateUserConfig(userId, "Points", points.ToString());
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
    }

    public async Task AddPointsLoop()
    {
        while (true)
        {
            await Task.Delay(TimeSpan.FromMinutes(1));
            foreach (ulong userId in activeUsers)
            {
                int currentPoints = await LoadUserPoints(userId);
                int passivePoints = await CalculatePassivePoints(userId);
                currentPoints += passivePoints;
                SaveUserPoints(userId, currentPoints);
            }
        }
    }

    public async Task<int> GetUserPoints(ulong userId)
    {
        return await LoadUserPoints(userId);
    }

    public async Task<List<UserPoints>> GetTopUsersByCategory(int count, string category)
    {
        var allUsers = await GetAllUsersByCategory(category);
        return allUsers.OrderByDescending(u => u.Points).Take(count).ToList();
    }

    private async Task<List<UserPoints>> GetAllUsersByCategory(string category)
    {
        var users = new List<UserPoints>();
        foreach (var file in Directory.GetFiles(folderPath, "*.json"))
        {
            var filename = Path.GetFileNameWithoutExtension(file);
            if (filename.Contains('_')) continue;

            var userData = await jsonReader.ReadJson<UserConfig>(file);
            if (userData != null)
            {
                ulong userId = ulong.Parse(filename);
                PropertyInfo property = typeof(UserConfig).GetProperty(category);
                int statValue = property != null ? int.Parse(property.GetValue(userData)?.ToString() ?? "0") : 0;

                users.Add(new UserPoints { UserId = userId, Points = statValue });
            }
        }
        return users;
    }

    private async Task<int> CalculatePassivePoints(ulong userId)
    {
        var guild = Program.Client.Guilds.Values
            .FirstOrDefault(g => g.VoiceStates.TryGetValue(userId, out var voiceState) && voiceState.Channel != null);

        var userConfig = await jsonReader.ReadJson<UserConfig>($"{folderPath}\\{userId}.json");
        var serverConfig = await jsonReader.ReadJson<ServerConfigShop>($"{Program.serverConfigPath}\\{guild.Id}_shop.json");

        int passivePoints = 10;
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

public class UserPoints
{
    public ulong UserId { get; set; }
    public int Points { get; set; }
}

