using Discord_Bot.Config;
using Discord_Bot.Handlers;
using DSharpPlus.SlashCommands;
using System.Collections.Concurrent;
namespace Discord_Bot.Commands.Slash
{
    public class RaffleCommand : ApplicationCommandModule
    {
        private static readonly ConcurrentDictionary<ulong, uint> rafflePools = new();
        private static readonly ConcurrentDictionary<ulong, byte> activeRaffles = new();
        private static readonly uint raffleTicketStartCost = 100;
        private static readonly uint raffleTicketCostIncrease = 50;
        private static readonly JSONReader jsonReader = new();
        private readonly JSONWriter jsonWriter = new(jsonReader, "config.json", Program.serverConfigPath);

        public async Task StartRaffle(CustomInteractionContext ctx)
        {
            ulong guildId = ctx.Guild.Id;
            if (activeRaffles.ContainsKey(guildId))
            {
                await ctx.CreateResponseAsync("Loteria jest już aktywna!", true);
                return;
            }

            uint rafflePool = (uint)new Random().Next(100, 5001);
            await ResetAllRaffleTickets(guildId);
            rafflePools[guildId] = rafflePool;
            activeRaffles[guildId] = 0;
            await jsonWriter.UpdateServerConfig(guildId, "RafflePool", rafflePool.ToString());
            await ctx.CreateResponseAsync($"Loteria została rozpoczęta! W puli {rafflePool} punktów. Wpisz /buyticket, aby kupić los.", false);
        }

        [SlashCommand("buyticket", "But a ticket for the raffle!")]
        public async Task BuyTicket(InteractionContext ctx, [Option("amount", "Amount to buy (number lub 'max')")] string amountInput)
        {
            ulong guildId = ctx.Guild.Id;
            if (!activeRaffles.ContainsKey(guildId))
            {
                await ctx.CreateResponseAsync("Brak aktywnych loterii.", true);
                return;
            }

            ulong userId = ctx.User.Id;
            uint rafflePool = rafflePools[guildId];
            var userData = await jsonReader.ReadJson<UserConfig>(UserDataPaths.GetUserConfigPath(Program.serverConfigPath, guildId, userId)) ?? throw new InvalidOperationException("UserConfig cannot be null");
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
            rafflePools[guildId] = rafflePool;
            await jsonWriter.UpdateUserConfig(guildId, userId, "Points", currentPoints);
            await jsonWriter.UpdateUserConfig(guildId, userId, "Tickets", currentTickets);
            await jsonWriter.UpdateServerConfig(guildId, "RafflePool", rafflePool);
            await ctx.CreateResponseAsync($"{ctx.User.Mention} kupił {ticketsToBuy} losów za {totalCost} punktów. Łączna liczba losów: {currentTickets}. Aktualna pula: {rafflePool} punktów.");
        }

        [SlashCommand("checktickets", "Sprawdź liczbę swoich losów!")]
        public async Task CheckTickets(InteractionContext ctx)
        {
            ulong userId = ctx.User.Id;
            var userData = await jsonReader.ReadJson<UserConfig>(UserDataPaths.GetUserConfigPath(Program.serverConfigPath, ctx.Guild.Id, userId)) ?? throw new InvalidOperationException("UserConfig cannot be null");
            int currentTickets = (int)userData.Tickets;
            await ctx.CreateResponseAsync($"Masz {currentTickets} losów.", true);
        }

        [SlashCommand("checkraffle", "Check raffle pool.")]
        public async Task CheckRaffle(InteractionContext ctx)
        {
            if (!activeRaffles.ContainsKey(ctx.Guild.Id))
            {
                await ctx.CreateResponseAsync("Brak aktywnych loterii.", true);
                return;
            }

            await ctx.CreateResponseAsync($"Aktualna pula: {rafflePools[ctx.Guild.Id]} punktów. Loteria kończy się codziennie o 18:00", true);
        }

        public async Task ResumeRaffle(CustomInteractionContext ctx, uint pool)
        {
            rafflePools[ctx.Guild.Id] = pool;
            activeRaffles[ctx.Guild.Id] = 0;
            await ctx.CreateResponseAsync($"Loteria została reaktywowana! W puli {pool} punktów. Wpisz /buyticket, aby kupić los.", false);
        }

        public async Task EndRaffle(CustomInteractionContext ctx)
        {
            ulong guildId = ctx.Guild.Id;
            if (!activeRaffles.ContainsKey(guildId))
            {
                await ctx.CreateResponseAsync("Loteria nie jest aktywna!", true);
                return;
            }

            activeRaffles.TryRemove(guildId, out _);
            uint rafflePool = rafflePools.TryRemove(guildId, out uint pool) ? pool : 0;
            var winner = await DrawRaffleWinner(guildId);
            if (winner == 0)
            {
                await ctx.CreateResponseAsync("Loteria została zakończona! Brak zwycięzcy, ponieważ nie było biletów.", false);
            }
            else
            {
                await SaveWinnerData(guildId, winner, rafflePool);
                await ctx.CreateResponseAsync($"Loteria została zakończona! <@{winner}> wygrał {rafflePool} punktów", false);
            }
        }

        private async Task<ulong> DrawRaffleWinner(ulong guildId)
        {
            List<ulong> ticketEntries = [];
            string guildFolder = Path.Combine(Program.serverConfigPath, "user_points", guildId.ToString());
            if (!Directory.Exists(guildFolder))
                return 0;

            foreach (var file in Directory.GetFiles(guildFolder, "*.json"))
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

        private async Task ResetAllRaffleTickets(ulong guildId)
        {
            string guildFolder = Path.Combine(Program.serverConfigPath, "user_points", guildId.ToString());
            if (!Directory.Exists(guildFolder))
                return;

            foreach (var file in Directory.GetFiles(guildFolder, "*.json"))
            {
                var filename = Path.GetFileNameWithoutExtension(file);
                if (filename.Contains('_')) continue;
                var userData = await jsonReader.ReadJson<UserConfig>(file);

                if (userData != null)
                {
                    ulong userId = ulong.Parse(filename);
                    await StatsHandler.IncreaseStats(guildId, userId, "RaffleTicketsBought", userData.Tickets);
                    await StatsHandler.IncreaseStats(guildId, userId, "RaffleSpent", CalculateTotalCost(0, userData.Tickets));
                    await jsonWriter.UpdateUserConfig(guildId, userId, "Tickets", "0");
                }
            }
        }

        private async Task SaveWinnerData(ulong guildId, ulong winnerId, uint points)
        {
            string filePath = UserDataPaths.GetUserConfigPath(Program.serverConfigPath, guildId, winnerId);
            var userData = await jsonReader.ReadJson<UserConfig>(filePath) ?? throw new InvalidOperationException("UserConfig cannot be null");
            uint userPoints = userData.Points + points;
            await jsonWriter.UpdateUserConfig(guildId, winnerId, "Points", userPoints);
            await StatsHandler.IncreaseStats(guildId, winnerId, "RaffleWins");
            await StatsHandler.IncreaseStats(guildId, winnerId, "RaffleWinnings", points);
            await StatsHandler.IncreaseStats(guildId, winnerId, "WonPoints", points);
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
        public bool IsRaffleActive(ulong guildId)
        {
            return activeRaffles.ContainsKey(guildId);
        }
    }
}