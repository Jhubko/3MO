using Discord_Bot;
using DSharpPlus.SlashCommands;
using Newtonsoft.Json;

public class HangmanCommands : ApplicationCommandModule
{
    private static readonly string[] HANGMANPICS =
    {
        "```\n      \n      \n      \n      \n      \n      \n```",
        "```\n      \n      \n      \n      \n      \n=========" + "\n```",
        "```\n      |\n      |\n      |\n      |\n      |\n=========" + "\n```",
        "```\n  +---+\n      |\n      |\n      |\n      |\n      |\n=========" + "\n```",
        "```\n  +---+\n  |   |\n      |\n      |\n      |\n      |\n=========" + "\n```",
        "```\n  +---+\n  |   |\n  O   |\n      |\n      |\n      |\n=========" + "\n```",
        "```\n  +---+\n  |   |\n  O   |\n  |   |\n      |\n      |\n=========" + "\n```",
        "```\n  +---+\n  |   |\n  O   |\n /|   |\n      |\n      |\n=========" + "\n```",
        "```\n  +---+\n  |   |\n  O   |\n /|\\  |\n      |\n      |\n=========" + "\n```",
        "```\n  +---+\n  |   |\n  O   |\n /|\\  |\n /    |\n      |\n=========" + "\n```",
        "```\n  +---+\n  |   |\n  O   |\n /|\\  |\n / \\  |\n      |\n=========" + "\n```"
    };

    private static readonly HttpClient httpClient = new();
    private static Dictionary<ulong, HangmanGameState> activeGames = new();
    private static Dictionary<ulong, Task> activeTimers = new();

    [SlashCommand("hangman", "Start a game of hangman.")]
    public async Task StartGame(InteractionContext ctx)
    {
        if (activeGames.ContainsKey(ctx.Channel.Id))
        {
            await ctx.CreateResponseAsync("🎮 The game is already in progress on this channel! Use `/guess <letter>`.");
            return;
        }

        string word = await GetRandomWord();
        var game = new HangmanGameState(word);
        activeGames[ctx.Channel.Id] = game;

        var timerTask = Task.Delay(300000).ContinueWith(async _ =>
        {
            if (activeGames.ContainsKey(ctx.Channel.Id))
            {
                activeGames.Remove(ctx.Channel.Id);
                await ctx.Channel.SendMessageAsync($"⏳ Time's up! The game has ended. The word was **{game.WordToGuess}**.");
            }
        });

        activeTimers[ctx.Channel.Id] = timerTask;

        await ctx.CreateResponseAsync($"🕹 **New Hangman Game!** You have **5 minutes** to guess the word and win **{CalculatePoints(game.WordToGuess)} points**! Guess the letters or word with the command /guess <letter/word>\n\n{GetGameState(ctx.Channel.Id)}");
    }

    [SlashCommand("guess", "Guess a letter or the whole word in Hangman.")]
    public async Task Guess(InteractionContext ctx, [Option("input", "Give a letter or a word to guess.")] string input)
    {
        if (!activeGames.ContainsKey(ctx.Channel.Id))
        {
            await ctx.CreateResponseAsync("⚠ There is no active game in this channel. Use `/hangman` to start.", true);
            return;
        }

        var game = activeGames[ctx.Channel.Id];
        input = input.ToLower();

        if (input.Length == 1 && char.IsLetter(input[0]))
        {
            char letter = input[0];

            if (game.GuessedWord.Contains(letter) || game.WrongGuesses.Contains(letter))
            {
                await ctx.CreateResponseAsync($"🔄 The letter **{letter}** has already been guessed. Try another one!", true);
                return;
            }

            if (game.WordToGuess.Contains(letter))
            {
                for (int i = 0; i < game.WordToGuess.Length; i++)
                {
                    if (game.WordToGuess[i] == letter)
                        game.GuessedWord[i] = letter;
                }
            }
            else
            {
                game.WrongGuesses.Add(letter);
                game.WrongAttempts++;
            }
        }
        else if (input.Length > 1)
        {
            if (input == game.WordToGuess)
            {
                ulong userId = ctx.User.Id;
                int currentPoints = await Program.voicePointsManager.GetUserPoints(userId);
                await ctx.CreateResponseAsync($"🎉 {ctx.User.Mention} guessed the word **{game.WordToGuess}**!");
                Program.voicePointsManager.SaveUserPoints(userId, currentPoints + CalculatePoints(game.WordToGuess));
                activeGames.Remove(ctx.Channel.Id);
                return;
            }
            else
            {
                game.WrongAttempts++;
                game.WrongWords.Add(input);
            }
        }
        else
        {
            await ctx.CreateResponseAsync("⚠ Invalid input. Enter a single letter or a full word.", true);
            return;
        }

        if (new string(game.GuessedWord) == game.WordToGuess)
        {
            ulong userId = ctx.User.Id;
            int currentPoints = await Program.voicePointsManager.GetUserPoints(userId);
            await ctx.CreateResponseAsync($"🎉 {ctx.User.Mention} guessed the word **{game.WordToGuess}**!");
            Program.voicePointsManager.SaveUserPoints(userId, currentPoints + CalculatePoints(game.WordToGuess));
            activeGames.Remove(ctx.Channel.Id);
            return;
        }
        else if (game.WrongAttempts >= HANGMANPICS.Length - 1)
        {
            await ctx.CreateResponseAsync($"💀 Hanged man hanged! The word is: **{game.WordToGuess}**");
            activeGames.Remove(ctx.Channel.Id);
        }
        else
        {
            await ctx.CreateResponseAsync(GetGameState(ctx.Channel.Id));
        }
    }

    private async Task<string> GetRandomWord()
    {
        string response = await httpClient.GetStringAsync("https://random-word-api.herokuapp.com/word");
        var words = JsonConvert.DeserializeObject<List<string>>(response);
        return words[0].ToLower();
    }

    private string GetGameState(ulong channelId)
    {
        if (!activeGames.ContainsKey(channelId))
            return "❌ No active game in this channel.";

        var game = activeGames[channelId];
        return $"{HANGMANPICS[game.WrongAttempts]}\n" +
               $"Word: `{new string(game.GuessedWord)}`\n" +
               $"Wrong letters: `{(game.WrongGuesses.Count > 0 ? string.Join(", ", game.WrongGuesses) : "None")}`\n" +
               $"Wrong words: `{(game.WrongWords.Count > 0 ? string.Join(", ", game.WrongWords) : "None")}`\n" +
               $"Attempts: {game.WrongAttempts}/{HANGMANPICS.Length - 1}";
    }

    public int CalculatePoints(string word)
    {
        int length = word.Length;

        if (length <= 4)
            return 300;
        else if (length == 5)
            return 400; 
        else if (length <= 7)
            return 500; 
        else if (length <= 9)
            return 600;
        else
            return 800;
    }
}

public class HangmanGameState
{
    public string WordToGuess { get; set; }
    public char[] GuessedWord { get; set; }
    public HashSet<char> WrongGuesses { get; set; } = new();
    public HashSet<string> WrongWords { get; set; } = new();
    public int WrongAttempts { get; set; } = 0;

    public HangmanGameState(string word)
    {
        WordToGuess = word;
        GuessedWord = new string('_', word.Length).ToCharArray();
    }
}
