using DSharpPlus;
using DSharpPlus.Entities;

namespace Discord_Bot.other
{
    internal class Buttons
    {
        public static DiscordButtonComponent gamesButton = new DiscordButtonComponent(ButtonStyle.Success, "gamesButton", "Games");
        public static DiscordButtonComponent mngmtButton = new DiscordButtonComponent(ButtonStyle.Success, "mngmtButton", "Management");
        public static DiscordButtonComponent searchButton = new DiscordButtonComponent(ButtonStyle.Danger, "searchButton", "Search");
        public static DiscordButtonComponent musicButton = new DiscordButtonComponent(ButtonStyle.Danger, "musicButton", "Music");

        public static DiscordEmbedBuilder gamesCommandEmbed = new DiscordEmbedBuilder()
        {
            Color = DiscordColor.Black,
            Title = "Games Commands",
            Description = "**!debil**  -> Pisze, że jesteś debil \n" +
                          "**!karty**  -> Gra w karty z botem \n" +
                          "**!random** -> Losuje liczbe od 1 do podanej liczby \n" +
                          "**/poll**  -> Tworzy ankiete \n"
        };

        public static DiscordEmbedBuilder managementCommandsEmbed = new DiscordEmbedBuilder()
        {
            Color = DiscordColor.Black,
            Title = "Management Commands",
            Description = "**!help**  -> Pokazuję komendy z opisami \n" +
                          "**!defaultRole**  -> Ustawia domyślną role dla nowych urzytkowników \n" +
                          "**!imageOnly**  -> Ustawia który kanał będzie usuwał wiadomości tekstowe"
        };

        public static DiscordEmbedBuilder helpCommandEmbed = new DiscordEmbedBuilder()
        {
            Color = DiscordColor.Black,
            Title = "Help Section",
            Description = "Press a button to view command"
        };

        public static DiscordEmbedBuilder searchCommandEmbed = new DiscordEmbedBuilder()
        {
            Color = DiscordColor.Black,
            Title = "Search Commands",
            Description = "**/image**  -> Wyszukuje randomowy obrazek o danej tematyce \n" +
                          "**/chatgpt**  -> ChatGPT odpowie na twoje pytanie\n" +
                          "**/meme**  -> Wysyła randomowy mem z reddit\n"
        };

        public static DiscordEmbedBuilder musicCommandEmbed = new DiscordEmbedBuilder()
        {
            Color = DiscordColor.Black,
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
