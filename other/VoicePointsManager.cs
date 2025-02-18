﻿using Discord_Bot;
using Discord_Bot.Config;
using DSharpPlus;
using DSharpPlus.EventArgs;
using Newtonsoft.Json;
using System;
using System.Text.Json;

class VoicePointsManager
{
    private readonly string folderPath = $"{Program.globalConfig.ConfigPath}\\user_points";
    private HashSet<ulong> activeUsers;
    private static IJsonHandler jsonReader = new JSONReader();
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
                            currentPoints += 10;
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
        return int.Parse(userConfig.Points); ;
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
                currentPoints += 10;
                SaveUserPoints(userId, currentPoints);
            }
        }
    }

    public async Task<int> GetUserPoints(ulong userId)
    {
        return await LoadUserPoints(userId);
    }
}
