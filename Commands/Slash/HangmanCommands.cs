using DSharpPlus.SlashCommands;
namespace Discord_Bot.Commands.Slash
{
    public class HangmanCommands : ApplicationCommandModule
    {
        private static readonly string[] hangmanPics =
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

        private readonly WordGamesHandler _wordGamesHandler = new WordGamesHandler();
        private static Dictionary<ulong, HangmanGameState> activeGames = new();
        private static Dictionary<ulong, CancellationTokenSource> activeTimers = new();

        [SlashCommand("hangman", "Start a game of hangman.")]
        public async Task StartHangmanGame(InteractionContext ctx)
        {
            if (activeGames.ContainsKey(ctx.Channel.Id))
            {
                await ctx.CreateResponseAsync("🎮 The game is already in progress on this channel! Use `/hguess <letter>`.");
                return;
            }

            string word = await _wordGamesHandler.GetRandomWord();
            if (string.IsNullOrEmpty(word))
            {
                await ctx.CreateResponseAsync("❌ Failed to retrieve a word. Try again later.", true);
                return;
            }

            var game = new HangmanGameState(word);
            activeGames[ctx.Channel.Id] = game;

            if (activeTimers.ContainsKey(ctx.Channel.Id))
            {
                activeTimers[ctx.Channel.Id].Cancel();
                activeTimers[ctx.Channel.Id].Dispose();
                activeTimers.Remove(ctx.Channel.Id);
            }

            await ctx.CreateResponseAsync($"🕹 **New Hangman Game!** You have **5 minutes** to guess the word and win **{CalculatePoints(game.WordToGuess)} points**! Guess the letters or word with the command /hguess <letter/word>\n\n{GetGameState(ctx.Channel.Id)}");
            await _wordGamesHandler.StartTimer(ctx.Channel.Id, ctx, activeTimers, activeGames);
        }

        [SlashCommand("hguess", "Guess a letter or the whole word in Hangman.")]
        public async Task HangmanGuess(InteractionContext ctx, [Option("input", "Give a letter or a word to guess.")] string input)
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
                    uint currentPoints = await Program.voicePointsManager.GetUserPoints(userId);
                    await ctx.CreateResponseAsync($"🎉 {ctx.User.Mention} guessed the word **{game.WordToGuess}** and won **{CalculatePoints(game.WordToGuess)}** points!");
                    Program.voicePointsManager.SaveUserPoints(userId, currentPoints + CalculatePoints(game.WordToGuess));
                    activeTimers[ctx.Channel.Id].Cancel();
                    activeTimers[ctx.Channel.Id].Dispose();
                    activeTimers.Remove(ctx.Channel.Id);
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
                uint currentPoints = await Program.voicePointsManager.GetUserPoints(userId);
                await ctx.CreateResponseAsync($"🎉 {ctx.User.Mention} guessed the word **{game.WordToGuess}**  and won **{CalculatePoints(game.WordToGuess)}** points!");
                await StatsHandler.IncreaseStats(ctx.User.Id, "HangmanWins");
                Program.voicePointsManager.SaveUserPoints(userId, currentPoints + CalculatePoints(game.WordToGuess));
                activeTimers[ctx.Channel.Id].Cancel();
                activeTimers[ctx.Channel.Id].Dispose();
                activeTimers.Remove(ctx.Channel.Id);
                activeGames.Remove(ctx.Channel.Id);
                return;
            }
            else if (game.WrongAttempts >= hangmanPics.Length - 1)
            {
                await ctx.CreateResponseAsync($"💀 Hanged man hanged! The word is: **{game.WordToGuess}**");
                await StatsHandler.IncreaseStats(ctx.User.Id, "HangmanLosses");
                activeTimers[ctx.Channel.Id].Cancel();
                activeTimers[ctx.Channel.Id].Dispose();
                activeTimers.Remove(ctx.Channel.Id);
                activeGames.Remove(ctx.Channel.Id);
            }
            else
            {
                await ctx.CreateResponseAsync(GetGameState(ctx.Channel.Id));
            }
        }

        private string GetGameState(ulong channelId)
        {
            if (!activeGames.ContainsKey(channelId))
                return "❌ No active game in this channel.";

            var game = activeGames[channelId];
            return $"{hangmanPics[game.WrongAttempts]}\n" +
                   $"Word: `{new string(game.GuessedWord)}`\n" +
                   $"Wrong letters: `{(game.WrongGuesses.Count > 0 ? string.Join(", ", game.WrongGuesses) : "None")}`\n" +
                   $"Wrong words: `{(game.WrongWords.Count > 0 ? string.Join(", ", game.WrongWords) : "None")}`\n" +
                   $"Attempts: {game.WrongAttempts}/{hangmanPics.Length - 1}";
        }

        public uint CalculatePoints(string word)
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
}