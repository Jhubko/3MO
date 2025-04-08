﻿using DSharpPlus.SlashCommands;
using Newtonsoft.Json;

class WordGamesHandler
{
    private static readonly HttpClient httpClient = new HttpClient
    {
        Timeout = TimeSpan.FromSeconds(1)
    };
    private static List<string> allWordsCache = new List<string>();
    private static List<string> wordleWordsCache = new List<string>();
    private static string baseLink = "https://random-word-api.herokuapp.com/word?number=";
    private static string wordleLink = "https://random-word-api.herokuapp.com/word?length=5&number=";
    private static int cacheSize = 100;

    public async Task<string> GetRandomWord(bool isWordle = false)
    {
        List<string> wordCache = isWordle ? wordleWordsCache : allWordsCache;
        string linkBase = isWordle ? wordleLink : baseLink;

        try
        {
            if (wordCache.Count == 0)
            {
                HttpResponseMessage response = await httpClient.GetAsync(linkBase + cacheSize);
                response.EnsureSuccessStatusCode();
                string jsonResponse = await response.Content.ReadAsStringAsync();
                var words = JsonConvert.DeserializeObject<List<string>>(jsonResponse);

                if (words != null && words.Count > 0)
                {
                    if (isWordle)
                        wordleWordsCache = words;
                    else
                        allWordsCache = words;
                }
                return await GetRandomWord(isWordle);
            }

            var random = new Random();
            string randomWord = wordCache[random.Next(wordCache.Count)].ToLower();
            wordCache.Remove(randomWord);
            return randomWord;
        }
        catch
        {
            return string.Empty;
        }
    }
    public async Task StartTimer(ulong channelId, InteractionContext ctx, Dictionary<ulong, CancellationTokenSource> activeTimers, dynamic activeGames)
    {
        if (activeTimers.ContainsKey(channelId))
        {
            activeTimers[channelId].Cancel();
            activeTimers[channelId].Dispose();
        }

        var cts = new CancellationTokenSource();
        activeTimers[channelId] = cts;

        try
        {
            await Task.Delay(300000, cts.Token);
            if (!cts.Token.IsCancellationRequested && activeGames.ContainsKey(channelId))
            {
                var currentGame = activeGames[channelId];
                await ctx.Channel.SendMessageAsync($"⏳ Time's up! The game has ended. The word was **{currentGame.WordToGuess}**.");
                activeGames.Remove(channelId);
                activeTimers.Remove(channelId);
            }
        }
        catch (TaskCanceledException)
        {
        }
    }
    public async Task<bool> CheckIfWordExist(string word)
    {
        HttpResponseMessage response = await httpClient.GetAsync($"https://api.dictionaryapi.dev/api/v2/entries/en/{word}");
        return response.IsSuccessStatusCode;
    }
}

