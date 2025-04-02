using DSharpPlus.Entities;

namespace Discord_Bot.other
{
    internal class HelpContent
    {
        public static DiscordEmbedBuilder casinoCommandEmbed = new DiscordEmbedBuilder()
        {
            Color = DiscordColor.DarkRed,
            Title = "Casino Commands",
            Description = "**/points** - Pokazuje ilość punktów twoją lub innego użytkownika\n" +
                "**/gamble** - Postaw określoną ilość punktów, aby je podwoić lub stracić\n" +
                "**/slots** - Zagraj za małą stawkę i wygraj wielkie nagrody\n" +
                "**/cards** - Wyzwanie o punkty w grze karcianej przeciwko botowi\n" +
                "**/duel** - Zmierz się z innymi graczami\n" +
                "**/checkraffle** - Sprawdź obecną loterię\n" +
                "**/buyticket** - Kup bilet na loterię\n" +
                "**/givepoints** - Podziel się punktami z przyjaciółmi\n" +
                "**/freepoints** - 1000 darmowych punktów"
        };

        public static DiscordEmbedBuilder gamesCommandEmbed = new DiscordEmbedBuilder()
        {
            Color = DiscordColor.Green,
            Title = "Games Commands",
            Description = "**/hangman** - Zaczyne gre w wisielca \n" +
                "**/wordle** - Zaczyna gre w wordle"
        };

        public static DiscordEmbedBuilder fishingCommandEmbed = new DiscordEmbedBuilder()
        {
            Color = DiscordColor.Aquamarine,
            Title = "Fishing Commands",
            Description = "**/fish** - Lista dostępnych ryb \n" +
                "**/fishing** - Zaczyna łowienie\n" +
                "**/sellfish** - Sprzedaj wybraną rybe\n"
        };

        public static DiscordEmbedBuilder shopCommandEmbed = new DiscordEmbedBuilder()
        {
            Color = DiscordColor.Gold,
            Title = "Shop Commands",
            Description = "**/buy** - Kup przedmiot ze sklepu\n" +
                  "**/shop** - Wyświetl listę dostępnych przedmiotów w sklepie\n" +
                  "**/items** - Sprawdź swoje przedmioty"
        };

        public static DiscordEmbedBuilder cityCommandEmbed = new DiscordEmbedBuilder()
        {
            Color = DiscordColor.Cyan,
            Title = "City Commands",
            Description = "**/buildings** - Lista dostępnych budynków z ceną i dochodem\n" +
                "**/setcityname** - Ustaw nazwę swojego miasta\n" +
                "**/city** - Wyświetl swoje miasto lub miasto innego gracza\n" +
                "**/buybuilding** - Kup budynek na określonej lokalizacji\n" +
                "**/sellbuilding** - Sprzedaj budynek z określonej lokalizacji\n" +
                "**/movebuilding** - Przenieś budynek w inne miejsce\n" +
                "**/collectpoints** - Zbierz dochód z budynków w swoim mieście"
        };

        public static DiscordEmbedBuilder managementCommandsEmbed = new DiscordEmbedBuilder()
        {
            Color = DiscordColor.Blurple,
            Title = "Management Commands",
            Description = "**/help**  -> Pokazuję komendy z opisami\n" +
                "**/defaultRole**  -> Ustawia domyślną role dla nowych urzytkowników\n" +
                "**/imageOnly**  -> Ustawia który kanał będzie usuwał wiadomości tekstowe\n" +
                "**/raffleChannel**  -> Ustawia na którym beda informacje o loteriach\n" +
                "**/createShopItem**  -> Dodaje nowy przedmiot do sklepu\n" +
                "**/removeShopItem**  -> Usuwa podany item ze sklepu\n" +
                "**/editfish**  -> Dodaj/Edytuj/Usuń ryby z listy\n" +
                "**/deleteMessageEmoji**   -> Ustawia emoji, które będzie usuwało wiadomości"
        };

        public static DiscordEmbedBuilder helpCommandEmbed = new DiscordEmbedBuilder()
        {
            Color = DiscordColor.Gray,
            Title = "Help Section",
            Description = "Wybierz kategorię, aby zobaczyć dostępne komendy:\n\n" +
                "Kliknij na rozwijane menu, aby uzyskać szczegóły.\n\n" +
                "═════════════════════════════════════\n" +
                "**1. Casino** - Komendy związane z grami hazardowymi.\n" +
                "**2. Games** - Komendy do gier, takich jak wisielec i wordle.\n" +
                "**3. Shop** - Komendy do zarządzania sklepem i przedmiotami.\n" +
                "**4. City** - Komendy związane z budowaniem i zarządzaniem miastem.\n" +
                "**5. Fishing** - Komendy związane z wędkowaniem.\n" +
                "**6. Stats** - Komendy do przeglądania statystyk graczy i punktów.\n" +
                "**7. Music** - Komendy do odtwarzania muzyki na kanale głosowym.\n" +
                "**8. Search** - Komendy do wyszukiwania memów, artykułów i obrazków.\n" +
                "**9. Management** - Komendy administracyjne i zarządzanie botem.\n\n" +
                "═════════════════════════════════════\n" +
                "Wybierz kategorię z rozwijanego menu, aby uzyskać szczegóły."
        };

        public static DiscordEmbedBuilder statsCommandEmbed = new DiscordEmbedBuilder()
        {
            Color = DiscordColor.Purple,
            Title = "Stats Commands",
            Description = "**/points** - Sprawdź swoje punkty lub punkty innego użytkownika\n" +
                "**/highscore** - Sprawdź top 10 graczy w danej kategorii\n" +
                "**/stats** - Zobacz swoje statystyki"
        };

        public static DiscordEmbedBuilder searchCommandEmbed = new DiscordEmbedBuilder()
        {
            Color = DiscordColor.Teal,
            Title = "Search Commands",
            Description = "**/image**  -> Wyszukuje randomowy obrazek o danej tematyce \n" +
                "**/chatgpt**  -> ChatGPT odpowie na twoje pytanie\n" +
                "**/meme**  -> Wysyła randomowy mem z reddit\n" +
                "**/wiki**  -> Wysyła randomowy artykuł z wikipedii\n" +
                "**/weather**  -> Wysyła informacje o aktualnej pogodzie w danym mieście\n" +
                "**/forecast**  -> Wysyła 3 dniową prognoze pogody w danym mieście"
        };

        public static DiscordEmbedBuilder musicCommandEmbed = new DiscordEmbedBuilder()
        {
            Color = DiscordColor.Blue,
            Title = "Music Commands",
            Description = "**/play**  -> Puszcza muzyke na kanale głosowym \n" +
                "**/playnow **  -> Puszcza od razu utwór na kanale głosowym \n" +
                "**/next **  -> Dodaje piosenke jako pierwsza w kolejce \n" +
                "**/pause**  -> Pauzuje muzyke\n" +
                "**/resume**  -> Wznawia muzyke\n" +
                "**/stop**  -> Całkowicie zatrzymuje muzyke\n" +
                "**/skip**  -> Pomija aktualnie grany utwor\n" +
                "**/queue**  -> Wyświetla kolejke odtwarzania\n" +
                "**/position**  -> Wyświetla aktualny czas utworu\n" +
                "**/volume**  -> Ustawia głośność 0 - 1000% \n" +
                "**/shuffle**  -> Zmienia kolejność playlisty"
        };
    }
}
