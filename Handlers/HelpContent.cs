using DSharpPlus.Entities;

namespace Discord_Bot.other
{
    internal class HelpContent
    {
        public static DiscordEmbedBuilder casinoCommandEmbed = new()
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
                "**/checktickets** - Sprawdź liczbę swoich losów\n" +
                "**/givepoints** - Podziel się punktami z przyjaciółmi\n" +
                "**/giveitem** - Przekaż przedmiot lub Pokémona innemu użytkownikowi\n" +
                "**/freepoints** - 1000 darmowych punktów"
        };

        public static DiscordEmbedBuilder gamesCommandEmbed = new()
        {
            Color = DiscordColor.Green,
            Title = "Games Commands",
            Description = "**/random** - Losuje liczbę z podanego zakresu\n" +
                "**/hangman** - Zaczyna grę w wisielca\n" +
                "**/hguess** - Zgaduje literę lub hasło w wisielcu\n" +
                "**/wordle** - Zaczyna grę w Wordle\n" +
                "**/wguess** - Zgaduje słowo w Wordle\n" +
                "**/pokemonencounter** - Spotkaj losowego Pokémona; wybierz rodzaj Balla lub uciekaj\n" +
                "**/pokemoninfo** - Zobacz szczegółowe informacje o swoim Pokémonie\n" +
                "**/pokedex** - Wyświetl kolekcję Pokémonów w siatce 3×3; strony zmieniaj przyciskami\n" +
                "**/pokemontrade** - Wymień duplikat Pokémona na Poké Ball"
        };

        public static DiscordEmbedBuilder fishingCommandEmbed = new()
        {
            Color = DiscordColor.Aquamarine,
            Title = "Fishing Commands",
            Description = "**/fish** - Lista dostępnych ryb \n" +
                "**/fishing** - Zaczyna łowienie\n" +
                "**/sellfish** - Sprzedaj wybraną rybe\n"
        };

        public static DiscordEmbedBuilder shopCommandEmbed = new()
        {
            Color = DiscordColor.Gold,
            Title = "Shop Commands",
            Description = "**/buy** - Kup przedmiot ze sklepu; opcjonalnie ustaw liczbę w parametrze `amount` (domyślnie 1). Cena zwykłych przedmiotów rośnie z każdą kupioną sztuką.\n" +
                  "**/shop** - Wyświetl listę dostępnych przedmiotów w sklepie\n" +
                  "**/items** - Sprawdź swoje przedmioty i ryby\n" +
                  "**/pokedex** - Wyświetl złapane Pokémony w siatce 3×3 z przyciskiem następnej strony\n" +
                  "**/pokemontrade** - Wymień duplikat Pokémona na Poké Ball\n" +
                  "**/giveitem** - Przekaż przedmiot lub Pokémona innemu użytkownikowi"
        };

        public static DiscordEmbedBuilder cityCommandEmbed = new()
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

        public static DiscordEmbedBuilder managementCommandsEmbed = new()
        {
            Color = DiscordColor.Blurple,
            Title = "Management Commands",
            Description = "**/help**  -> Pokazuję komendy z opisami\n" +
                "**/lavalink**  -> Zmienia hostname i hasło Lavalinka\n" +
                "**/restart**  -> Restartuje bota\n" +
                "**/defaultRole**  -> Ustawia domyślną role dla nowych urzytkowników\n" +
                "**/imageOnly**  -> Ustawia który kanał będzie usuwał wiadomości tekstowe\n" +
                "**/raffleChannel**  -> Ustawia na którym beda informacje o loteriach\n" +
                "**/createShopItem**  -> Dodaje nowy przedmiot do sklepu\n" +
                "**/removeShopItem**  -> Usuwa podany item ze sklepu\n" +
                "**/editfish**  -> Dodaj/Edytuj/Usuń ryby z listy\n" +
                "**/deleteMessageEmoji**   -> Ustawia emoji, które będzie usuwało wiadomości"
        };

        public static DiscordEmbedBuilder helpCommandEmbed = new()
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

        public static DiscordEmbedBuilder statsCommandEmbed = new()
        {
            Color = DiscordColor.Purple,
            Title = "Stats Commands",
            Description = "**/points** - Sprawdź swoje punkty lub punkty innego użytkownika\n" +
                "**/highscore** - Sprawdź top 10 graczy w danej kategorii\n" +
                "**/stats** - Zobacz swoje statystyki"
        };

        public static DiscordEmbedBuilder searchCommandEmbed = new()
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

        public static DiscordEmbedBuilder musicCommandEmbed = new()
        {
            Color = DiscordColor.Blue,
            Title = "Music Commands",
            Description = "**/play**  -> Puszcza muzykę na kanale głosowym\n" +
                "**/playnow**  -> Puszcza od razu utwór na kanale głosowym\n" +
                "**/next**  -> Dodaje piosenkę jako pierwszą w kolejce\n" +
                "**/pause**  -> Pauzuje muzykę\n" +
                "**/resume**  -> Wznawia muzykę\n" +
                "**/stop**  -> Całkowicie zatrzymuje muzykę\n" +
                "**/skip**  -> Pomija aktualnie grany utwór\n" +
                "**/nowplaying**  -> Wyświetla aktualnie odtwarzany utwór\n" +
                "**/queue**  -> Wyświetla kolejkę odtwarzania\n" +
                "**/position**  -> Wyświetla aktualny czas utworu\n" +
                "**/volume**  -> Ustawia głośność 0–1000%\n" +
                "**/shuffle**  -> Losowo miesza kolejkę; jej kolejność jest widoczna w /queue"
        };
    }
}
