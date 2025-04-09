using Discord_Bot.Config;
using Discord_Bot.Handlers;
using DSharpPlus.SlashCommands;
namespace Discord_Bot.Commands.Slash
{
    public class RaffleCommand : ApplicationCommandModule
    {
        private static uint rafflePool;
        private static readonly uint raffleTicketStartCost = 100;
        private static readonly uint raffleTicketCostIncrease = 50;
        private static bool raffleActive = false;
        private static readonly JSONReader jsonReader = new();
        private readonly JSONWriter jsonWriter = new(jsonReader, "config.json", Program.serverConfigPath);
        private readonly string folderPath = $"{Program.globalConfig.ConfigPath}\\user_points";

        public async Task StartRaffle(CustomInteractionContext ctx)
        {
            if (raffleActive)
            {
                await ctx.CreateResponseAsync("Loteria jest już aktywna!", true);
                return;
            }

            rafflePool = (uint)new Random().Next(100, 5001);
            await ResetAllRaffleTickets();
            raffleActive = true;
            await jsonWriter.UpdateServerConfig(ctx.Guild.Id, "RafflePool", rafflePool.ToString());
            await ctx.CreateResponseAsync($"Loteria została rozpoczęta! W puli {rafflePool} punktów. Wpisz /buyticket, aby kupić los.", false);
        }

        [SlashCommand("buyticket", "But a ticket for the raffle!")]
        public async Task BuyTicket(InteractionContext ctx, [Option("amount", "Amount to buy (number lub 'max')")] string amountInput)
        {
            if (!raffleActive)
            {
                await ctx.CreateResponseAsync("Brak aktywnych loterii.", true);
                return;
            }

            ulong userId = ctx.User.Id;
            var userData = await jsonReader.ReadJson<UserConfig>($"{folderPath}\\{userId}.json") ?? throw new InvalidOperationException("UserConfig cannot be null");
            uint currentPoints = userData.Points;
            uint currentTickets = userData.Tickets;
            uint ticketsToBuy;
            if (amountInput.Equals("max", StringComparison.CurrentCultureIgnoreCase))
            {
                ticketsToBuy = CalculateMaxTickets(currentPoints, currentTickets);
            }
            else if (!uint.TryParse(amountInput, out ticketsToBuy) || ticketsToBuy <= 0)
            {
                await ctx.CreateResponseAsync("Nieprawidłowa liczba losów.", true);
                return;
            }

            uint totalCost = CalculateTotalCost(currentTickets, ticketsToBuy);

            if (currentPoints < totalCost)
            {
                await ctx.CreateResponseAsync($"Nie masz tyle punktów bambiku, nie stać cię na {ticketsToBuy} losów. Wróć z {totalCost} punktami.", true);
                return;
            }

            currentTickets += ticketsToBuy;
            rafflePool += totalCost;
            currentPoints -= totalCost;
            await jsonWriter.UpdateUserConfig(userId, "Points", currentPoints);
            await jsonWriter.UpdateUserConfig(userId, "Tickets", currentTickets);
            await jsonWriter.UpdateServerConfig(ctx.Guild.Id, "RafflePool", rafflePool);
            await ctx.CreateResponseAsync($"{ctx.User.Mention} kupił {ticketsToBuy} losów za {totalCost} punktów. Łączna liczba losów: {currentTickets}. Aktualna pula: {rafflePool} punktów.");
        }

        [SlashCommand("checktickets", "Sprawdź liczbę swoich losów!")]
        public async Task CheckTickets(InteractionContext ctx)
        {
            ulong userId = ctx.User.Id;
            var userData = await jsonReader.ReadJson<UserConfig>($"{folderPath}\\{userId}.json") ?? throw new InvalidOperationException("UserConfig cannot be null");
            int currentTickets = (int)userData.Tickets;
            await ctx.CreateResponseAsync($"Masz {currentTickets} losów.", true);
        }

        [SlashCommand("checkraffle", "Check raffle pool.")]
        public async Task CheckRaffle(InteractionContext ctx)
        {
            if (!raffleActive)
            {
                await ctx.CreateResponseAsync("Brak aktywnych loterii.", true);
                return;
            }

            await ctx.CreateResponseAsync($"Aktualna pula: {rafflePool} punktów. Loteria kończy się codziennie o 18:00", true);
        }

        public async Task ResumeRaffle(CustomInteractionContext ctx, uint pool)
        {
            rafflePool = pool;
            raffleActive = true;
            await ctx.CreateResponseAsync($"Loteria została reaktywowana! W puli {rafflePool} punktów. Wpisz /buyticket, aby kupić los.", false);
        }

        public async Task EndRaffle(CustomInteractionContext ctx)
        {
            if (!raffleActive)
            {
                await ctx.CreateResponseAsync("Loteria nie jest aktywna!", true);
                return;
            }

            raffleActive = false;
            var winner = await DrawRaffleWinner();
            if (winner == 0)
            {
                await ctx.CreateResponseAsync("Loteria została zakończona! Brak zwycięzcy, ponieważ nie było biletów.", false);
            }
            else
            {
                await SaveWinnerData(winner, rafflePool);
                await ctx.CreateResponseAsync($"Loteria została zakończona! <@{winner}> wygrał {rafflePool} punktów", false);
            }
        }

        private async Task<ulong> DrawRaffleWinner()
        {
            List<ulong> ticketEntries = [];

            foreach (var file in Directory.GetFiles(folderPath, "*.json"))
            {
                var filename = Path.GetFileNameWithoutExtension(file);
                if (filename.Contains('_')) continue;
                var userData = await jsonReader.ReadJson<UserConfig>(file);
                ulong userId = ulong.Parse(filename);
                if (userData != null)
                {
                    for (int i = 0; i < userData.Tickets; i++)
                    {
                        ticketEntries.Add(userId);
                    }
                }
            }

            if (ticketEntries.Count == 0)
            {
                return 0;
            }

            Random rng = new();
            int n = ticketEntries.Count;
            while (n > 1)
            {
                n--;
                int k = rng.Next(n + 1);
                (ticketEntries[n], ticketEntries[k]) = (ticketEntries[k], ticketEntries[n]);
            }

            ulong winner = ticketEntries[rng.Next(ticketEntries.Count)];
            return winner;
        }

        private async Task ResetAllRaffleTickets()
        {
            foreach (var file in Directory.GetFiles(folderPath, "*.json"))
            {
                var filename = Path.GetFileNameWithoutExtension(file);
                if (filename.Contains('_')) continue;
                var userData = await jsonReader.ReadJson<UserConfig>(file);

                if (userData != null)
                {
                    await StatsHandler.IncreaseStats(ulong.Parse(filename), "RaffleTicketsBought", userData.Tickets);
                    await StatsHandler.IncreaseStats(ulong.Parse(filename), "RaffleSpent", CalculateTotalCost(0, userData.Tickets));
                    ulong userId = ulong.Parse(filename);
                    await jsonWriter.UpdateUserConfig(userId, "Tickets", "0");
                }
            }
        }

        private async Task SaveWinnerData(ulong winnerId, uint points)
        {
            string filePath = $"{folderPath}\\{winnerId}.json";
            if (File.Exists(filePath))
            {
                var userData = await jsonReader.ReadJson<UserConfig>(filePath) ?? throw new InvalidOperationException("UserConfig cannot be null");
                uint user_points = userData.Points;
                if (userData != null)
                {
                    user_points += points;
                    await jsonWriter.UpdateUserConfig(winnerId, "Points", user_points.ToString());
                    await StatsHandler.IncreaseStats(winnerId, "RaffleWins");
                    await StatsHandler.IncreaseStats(winnerId, "RaffleWinnings", points);
                    await StatsHandler.IncreaseStats(winnerId, "WonPoints", points);
                }
            }
        }

        private uint CalculateTotalCost(uint currentTickets, uint ticketsToBuy)
        {
            uint totalCost = 0;
            for (uint i = 0; i < ticketsToBuy; i++)
            {
                totalCost += raffleTicketStartCost + (currentTickets + i) * raffleTicketCostIncrease;
            }
            return totalCost;
        }

        private uint CalculateMaxTickets(uint currentPoints, uint currentTickets)
        {
            uint ticketsToBuy = 0;
            uint totalCost = 0;
            while (totalCost <= currentPoints)
            {
                totalCost += raffleTicketStartCost + (currentTickets + ticketsToBuy) * raffleTicketCostIncrease;
                if (totalCost <= currentPoints)
                {
                    ticketsToBuy++;
                }
            }
            return ticketsToBuy;
        }
        public bool IsRaffleActive()
        {
            return raffleActive;
        }
    }
}