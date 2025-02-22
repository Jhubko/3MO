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

    public static (bool isProperValue, string errorMessage) CheckGambleAmout(int amountToGamble, int currentPoints)
    {
        if (amountToGamble <= 0)
            return (false, "Niewłaściwa kwota. Podaj numer, wartosć procentową lub 'all'.");

        if (currentPoints < amountToGamble)
            return (false, $"Nie masz wystarczającej kwoty żeby zagrać za {amountToGamble} punktów!");

        return (true, "");
    }
}
