using DSharpPlus.Entities;
using DSharpPlus.SlashCommands;
using Newtonsoft.Json;

namespace Discord_Bot.other
{
    internal class SearchSystem
    {
        private static readonly HttpClient _httpClient = new HttpClient();
        public static async Task<DiscordEmbedBuilder> GetRandomMemeAsync(InteractionContext ctx)
        {
            try
            {
                var request = new HttpRequestMessage(HttpMethod.Get, "https://www.reddit.com/r/memes/random.json");
                request.Headers.Add("User-Agent", "DiscordBot");

                var response = await _httpClient.SendAsync(request);

                response.EnsureSuccessStatusCode();

                var json = await response.Content.ReadAsStringAsync();
                var memeData = JsonConvert.DeserializeObject<dynamic>(json);

                string memeUrl = memeData[0].data.children[0].data.url;
                string memeTitle = memeData[0].data.children[0].data.title;
                string memeAuthor = memeData[0].data.children[0].data.author;

                var memeEmbed = new DiscordEmbedBuilder()
                {
                    Color = DiscordColor.Orange,
                    Title = memeTitle,
                    ImageUrl = memeUrl,
                    Url = memeUrl,
                    Footer = new DiscordEmbedBuilder.EmbedFooter { Text = $"Author: {memeAuthor}" }
                };

                return memeEmbed;
            }
            catch (HttpRequestException ex)
            {
                await ctx.EditResponseAsync(new DiscordWebhookBuilder().WithContent($"Błąd podczas pobierania mema: {ex.Message}"));
                return null;
            }
            catch (JsonException ex)
            {
                await ctx.EditResponseAsync(new DiscordWebhookBuilder().WithContent($"Błąd podczas parsowania danych JSON: {ex.Message}"));
                return null;
            }
        }
    }
}
