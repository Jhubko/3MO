using System.Text.Json;
using Discord_Bot;
using Discord_Bot.config;
using DSharpPlus;
using DSharpPlus.EventArgs;

class VoicePointsManager
{
    private readonly string folderPath = $"{Program.jsonReader.ConfigPath}\\user_points";
    private HashSet<ulong> activeUsers;

    public VoicePointsManager()
    {
        
        activeUsers = new HashSet<ulong>();
        Directory.CreateDirectory(folderPath);
    }

    private string GetUserFilePath(ulong userId)
    {
        return Path.Combine(folderPath, $"{userId}.json"); 
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

                            int currentPoints = LoadUserPoints(user.Id);
                            currentPoints += 10;
                            SaveUserPoints(user.Id, currentPoints);
                        }
                    }
                }
            }
        }

        Console.WriteLine("Active users have been collected and their points updated.");
    }


    private int LoadUserPoints(ulong userId)
    {
        string filePath = GetUserFilePath(userId);
        if (File.Exists(filePath))
        {
            string json = File.ReadAllText(filePath);

            try
            {
                return JsonSerializer.Deserialize<int>(json);
            }
            catch (JsonException)
            {
                return 500;
            }
        }
        return 500;
    }

    public void SaveUserPoints(ulong userId, int points)
    {
        string filePath = GetUserFilePath(userId);
        string json = JsonSerializer.Serialize(points, new JsonSerializerOptions { WriteIndented = true });
        File.WriteAllText(filePath, json); // Zapis punktów użytkownika do pliku
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
                int currentPoints = LoadUserPoints(userId);
                currentPoints += 10;
                SaveUserPoints(userId, currentPoints);
            }
        }
    }

    public async Task<int> GetUserPoints(ulong userId)
    {
        string filePath = GetUserFilePath(userId);
        if (File.Exists(filePath))
        {
            string json = await File.ReadAllTextAsync(filePath);

            try
            {
                return JsonSerializer.Deserialize<int>(json);
            }
            catch (JsonException)
            {
                return 500;
            }
        }
        return 500;
    }
}
