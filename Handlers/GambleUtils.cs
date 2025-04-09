using System.Text;
using System.Text.RegularExpressions;

class GambleUtils
{
    public static StringBuilder CapitalizeUserFirstLetter(string username)
    {
        var name = new StringBuilder(Regex.Replace(username.Trim(), @"[^\w\s]+$", ""));
        name[0] = char.ToUpper(name[0]);
        return name;
    }

    public static (bool isProperValue, string errorMessage) CheckGambleAmout(uint amountToGamble, uint currentPoints)
    {
        if (amountToGamble <= 0)
            return (false, "Invalid amount. Enter a number, percentage, or 'all'.");

        if (currentPoints < amountToGamble)
            return (false, $"You don't have enough points: {amountToGamble}!");

        return (true, "");
    }
    public static uint ParseGambleAmount(string input, uint currentPoints)
    {
        input = input.Trim().ToLower();

        if (input == "all")
            return currentPoints;

        if (Regex.IsMatch(input, @"^\d+%$"))
        {
            int percentage = int.Parse(input.Replace("%", ""));
            return (uint)((currentPoints * percentage) / 100);
        }

        if (Regex.IsMatch(input, @"^\d+[kmb]?$"))
        {
            int multiplier = 1;
            if (input.EndsWith('k')) multiplier = 1000;
            else if (input.EndsWith('m')) multiplier = 1000000;
            else if (input.EndsWith('b')) multiplier = 1000000000;

            input = Regex.Replace(input, "[kmb]", "");
            if (int.TryParse(input, out int amount))
                return (uint)(amount * multiplier);
        }

        return 0;
    }

    public static int ParseInt(string input)
    {
        if (int.TryParse(input, out int result))
            return result;

        return -1;
    }
}
