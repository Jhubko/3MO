using Discord_Bot;
using DSharpPlus.Entities;
using DSharpPlus.SlashCommands;
public class WordleCommands : ApplicationCommandModule
{
    private readonly int totalGuesses = 6;
    private readonly int wordLenght = 5;
    private static Dictionary<ulong, WordleGameState> activeGames = new();
    private static Dictionary<ulong, CancellationTokenSource> activeTimers = new();
    private readonly WordGamesHandler _wordGamesHandler = new WordGamesHandler();

    [SlashCommand("wordle", "Start wordle game.")]
    public async Task StartWordleGame(InteractionContext ctx)
    {
        if (activeGames.ContainsKey(ctx.Channel.Id))
        {
            await ctx.CreateResponseAsync("🎮 The game is already in progress on this channel! Use `/wguess <letter>`.");
            return;
        }

        string word = await _wordGamesHandler.GetRandomWord("https://random-word-api.herokuapp.com/word?length=5&number="); ;
        if (string.IsNullOrEmpty(word))
        {
            await ctx.CreateResponseAsync("❌ Failed to retrieve a word. Try again later.", true);
            return;
        }

        var game = new WordleGameState(word);
        activeGames[ctx.Channel.Id] = game;

        if (activeTimers.ContainsKey(ctx.Channel.Id))
        {
            activeTimers[ctx.Channel.Id].Cancel();
            activeTimers[ctx.Channel.Id].Dispose();
            activeTimers.Remove(ctx.Channel.Id);
        }

        await ctx.CreateResponseAsync($"🕹 **New Wordle Game!** You have **5 minutes** to guess the word and win points**! Guess tword with the command /wguess \n{GetGameState(ctx.Channel.Id)}");
        await _wordGamesHandler.StartTimer(ctx.Channel.Id, ctx, activeTimers, activeGames);
    }

    [SlashCommand("wguess", "Guess a word in Wordle.")]
    public async Task WordleGuess(InteractionContext ctx, [Option("input", "Give a word to guess.")] string input)
    {
        var game = activeGames[ctx.Channel.Id];
        input = input.ToLower();
        if (!activeGames.ContainsKey(ctx.Channel.Id))
        {
            await ctx.CreateResponseAsync("⚠ There is no active game in this channel. Use `/wordle` to start.", true);
            return;
        }

        if (await _wordGamesHandler.CheckIfWordExist(input) == false)
        {
            await ctx.CreateResponseAsync($"⚠ Word do not exist!", true);
            return;
        }

        if (input.Length != 5)
        {
            await ctx.CreateResponseAsync($"⚠ Invalid Word. Enter 5 character word", true);
            return;
        }
        if (input == game.WordToGuess)
        {
            game.GuessedWords.Add(input);
            game.WordleStrucutreToShow.Add(CheckWord(input, game));
            ulong userId = ctx.User.Id;
            int currentPoints = await Program.voicePointsManager.GetUserPoints(userId);
            await ctx.CreateResponseAsync($"🎉 {ctx.User.Mention} guessed the word **{game.WordToGuess}** and won **{CalculatePoints(game.GuessedWords)}** points! \n{GetGameState(ctx.Channel.Id)}");
            Program.voicePointsManager.SaveUserPoints(userId, currentPoints + CalculatePoints(game.GuessedWords));
            activeTimers[ctx.Channel.Id].Cancel();
            activeTimers[ctx.Channel.Id].Dispose();
            activeTimers.Remove(ctx.Channel.Id);
            activeGames.Remove(ctx.Channel.Id);
            return;
        }
        else if (game.GuessedWords.Count >= totalGuesses)
        {
            await ctx.CreateResponseAsync($"💀 You lost! The word is: **{game.WordToGuess}**");
            activeTimers[ctx.Channel.Id].Cancel();
            activeTimers[ctx.Channel.Id].Dispose();
            activeTimers.Remove(ctx.Channel.Id);
            activeGames.Remove(ctx.Channel.Id);
        }
        else
        {
            game.GuessedWords.Add(input);
            game.WordleStrucutreToShow.Add(CheckWord(input, game));
            await ctx.CreateResponseAsync(GetGameState(ctx.Channel.Id));
        }
    }

    private string GetGameState(ulong channelId)
    {
        var game = activeGames[channelId];
        string wordleStructure = "🔠 Wordle 🔠```";

        if (game.WordleStrucutreToShow.Count != 0)
        {
            foreach (var word in game.WordleStrucutreToShow)
            {
                wordleStructure += $"{word}\n";
            }

            for (var i = 0; i < totalGuesses - game.GuessedWords.Count; i++)
            {
                wordleStructure += $"{string.Join(" ", Enumerable.Repeat("⬜ _", wordLenght))}\n";
            }
        }
        else
        {
            for (var i = 0; i < totalGuesses; i++)
            {
                wordleStructure += $"{string.Join(" ", Enumerable.Repeat("⬜ _", wordLenght))}\n";
            }
        }
        wordleStructure += "```";
        return wordleStructure;
    }

    private string CheckWord(string word, WordleGameState game)
    {
        string checkedWord = string.Empty;

        for (int i = 0; i < word.Length; i++)
        {
            char upper = char.ToUpper(word[i]);

            if (word[i] == game.WordToGuess[i])
                checkedWord += $"🟩 {upper} ";
            else if (game.WordToGuess.Contains(word[i]))
                checkedWord += $"🟨 {upper} ";
            else
                checkedWord += $"⬜ {upper} ";
        }
        return checkedWord;
    }

    private int CalculatePoints(List<string> words)
    {
        int length = words.Count;

        if (length == 1)
            return 1000;
        else if (length == 2)
            return 800;
        else if (length == 3)
            return 600;
        else if (length == 4)
            return 400;
        else if (length == 5)
            return 200;
        else
            return 100;
    }
}

public class WordleGameState
{
    public string WordToGuess { get; set; }
    public List<string> GuessedWords { get; set; } = new List<string>();
    public List<string> WordleStrucutreToShow { get; set; } = new List<string>();

    public WordleGameState(string word)
    {
        WordToGuess = word;
    }
}