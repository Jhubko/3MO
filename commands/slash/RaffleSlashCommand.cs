using Discord_Bot;
using System.IO;
using System.Text.Json;
using DSharpPlus.SlashCommands;
using DSharpPlus.Entities;

public class RaffleCommand : ApplicationCommandModule
{
    private static int rafflePool;
    private static int raffleTicketStartCost = 100;
    private static int raffleTicketCostIncrease = 50;
    private static bool raffleActive = false;

    [SlashCommand("startraffle", "Zaczymany loterie!")]
    public async Task StartRaffle(InteractionContext ctx)
    {
        if (raffleActive)
        {
            await ctx.CreateResponseAsync("Loteria jest już aktywna!", true);
            return;
        }

        rafflePool = new Random().Next(100, 5001);
        ResetAllRaffleTickets();
        raffleActive = true;
        await ctx.CreateResponseAsync($"Loteria została rozpoczęta! W puli {rafflePool} punktów. Wpisz /buyticket, aby kupić los.", true);
    }

    [SlashCommand("buyticket", "Kup los na loterie!")]
    public async Task BuyTicket(InteractionContext ctx, [Option("amount", "Liczba losów do kupienia (liczba lub 'max')")] string amountInput)
    {
        if (!raffleActive)
        {
            await ctx.CreateResponseAsync("Brak aktywnych loterii.", true);
            return;
        }

        ulong userId = ctx.User.Id;
        int currentPoints = await Program.voicePointsManager.GetUserPoints(userId);
        int currentTickets = await Program.voicePointsManager.GetUserRaffleTickets(userId);
        int ticketsToBuy = 1;

        if (amountInput.ToLower() == "max")
        {
            ticketsToBuy = CalculateMaxTickets(currentPoints, currentTickets);
        }
        else if (!int.TryParse(amountInput, out ticketsToBuy) || ticketsToBuy <= 0)
        {
            await ctx.CreateResponseAsync("Nieprawidłowa liczba losów.", true);
            return;
        }

        int totalCost = CalculateTotalCost(currentTickets, ticketsToBuy);

        if (currentPoints < totalCost)
        {
            await ctx.CreateResponseAsync($"Nie masz tyle punktów bambiku. Wróć z {totalCost} punktami.", true);
            return;
        }

        currentTickets += ticketsToBuy;
        rafflePool += totalCost;
        currentPoints -= totalCost;

        await Program.voicePointsManager.SaveUserPoints(userId, currentPoints);
        await Program.voicePointsManager.SaveUserRaffleTickets(userId, currentTickets);

        await ctx.CreateResponseAsync($"{ctx.User.Mention} kupił {ticketsToBuy} losów za {totalCost} punktów. Łączna liczba losów: {currentTickets}. Aktualna pula: {rafflePool} punktów.", true);
    }

    [SlashCommand("endraffle", "Zakończ loterie!")]
    public async Task EndRaffle(InteractionContext ctx)
    {
        if (!raffleActive)
        {
            await ctx.CreateResponseAsync("Loteria nie jest aktywna!", true);
            return;
        }

        raffleActive = false;
        var winner = DrawRaffleWinner();
        if (winner == 0)
        {
            await ctx.CreateResponseAsync("Loteria została zakończona! Brak zwycięzcy, ponieważ nie było biletów.", true);
        }
        else
        {
            SaveWinnerData(winner, rafflePool);
            await ctx.CreateResponseAsync($"Loteria została zakończona! <@{winner}> wygrał {rafflePool} punktów", true);
        }
    }

    private ulong DrawRaffleWinner()
    {
        List<ulong> ticketEntries = new List<ulong>();
        string folderPath = $"{Program.jsonReader.ConfigPath}\\user_data";

        foreach (var file in Directory.GetFiles(folderPath, "*.json"))
        {
            string json = File.ReadAllText(file);
            var userData = JsonSerializer.Deserialize<UserData>(json);
            if (userData != null)
            {
                for (int i = 0; i < userData.RaffleTickets; i++)
                {
                    ticketEntries.Add(userData.UserId);
                }
            }
        }

        if (ticketEntries.Count == 0)
        {
            return 0; // No tickets, return 0 to indicate no winner
        }

        // Shuffle the tickets
        Random rng = new Random();
        int n = ticketEntries.Count;
        while (n > 1)
        {
            n--;
            int k = rng.Next(n + 1);
            ulong value = ticketEntries[k];
            ticketEntries[k] = ticketEntries[n];
            ticketEntries[n] = value;
        }

        // Pick a random winner
        ulong winner = ticketEntries[rng.Next(ticketEntries.Count)];
        return winner;
    }

    private void ResetAllRaffleTickets()
    {
        string folderPath = $"{Program.jsonReader.ConfigPath}\\user_data";
        foreach (var file in Directory.GetFiles(folderPath, "*.json"))
        {
            string json = File.ReadAllText(file);
            var userData = JsonSerializer.Deserialize<UserData>(json);
            if (userData != null)
            {
                userData.RaffleTickets = 0;
                string updatedJson = JsonSerializer.Serialize(userData, new JsonSerializerOptions { WriteIndented = true });
                File.WriteAllText(file, updatedJson);
            }
        }
    }

    private void SaveWinnerData(ulong winnerId, int points)
    {
        string filePath = Path.Combine($"{Program.jsonReader.ConfigPath}\\user_data", $"{winnerId}.json");
        if (File.Exists(filePath))
        {
            string json = File.ReadAllText(filePath);
            var userData = JsonSerializer.Deserialize<UserData>(json);
            if (userData != null)
            {
                userData.Points += points;
                string updatedJson = JsonSerializer.Serialize(userData, new JsonSerializerOptions { WriteIndented = true });
                File.WriteAllText(filePath, updatedJson);
            }
        }
    }

    private int CalculateTotalCost(int currentTickets, int ticketsToBuy)
    {
        int totalCost = 0;
        for (int i = 0; i < ticketsToBuy; i++)
        {
            totalCost += raffleTicketStartCost + (currentTickets + i) * raffleTicketCostIncrease;
        }
        return totalCost;
    }

        private int CalculateMaxTickets(int currentPoints, int currentTickets)
    {
        int ticketsToBuy = 0;
        int totalCost = 0;
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
}
