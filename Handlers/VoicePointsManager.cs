using Discord_Bot;
using Discord_Bot.Config;
using DSharpPlus;
using DSharpPlus.EventArgs;
using System.Text.RegularExpressions;

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

    public async Task<List<UserPoints>> GetTopUsers(int count)
    {
        var allUsers = await GetAllUsers();
        return allUsers.OrderByDescending(u => u.Points).Take(count).ToList();
    }

    private async Task<List<UserPoints>> GetAllUsers()
    {
        var users = new List<UserPoints>();
        foreach (var file in Directory.GetFiles(folderPath, "*.json"))
        {
            var userData = await jsonReader.ReadJson<UserConfig>(file);

            if (userData != null)
            {
                ulong userId = ulong.Parse(Path.GetFileNameWithoutExtension(file));
                users.Add(new UserPoints { UserId = userId, Points = int.Parse(userData.Points) });
            }
        }
        return users;
    }
    public int ParseGambleAmount(string input, int currentPoints)
    {
        input = input.Trim().ToLower();

        if (input == "all")
            return currentPoints;

        if (Regex.IsMatch(input, @"^\d+%$"))
        {
            int percentage = int.Parse(input.Replace("%", ""));
            return (currentPoints * percentage) / 100;
        }

        if (int.TryParse(input, out int amount))
            return amount;

        return -1;
    }
}

public class UserPoints
{
    public ulong UserId { get; set; }
    public int Points { get; set; }
}