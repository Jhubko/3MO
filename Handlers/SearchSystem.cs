using DSharpPlus.Entities;
using DSharpPlus.SlashCommands;
using Newtonsoft.Json;

namespace Discord_Bot.other
{
    internal class SearchSystem
    {
        private static readonly HttpClient _httpClient = new HttpClient();
        private static List<string> WeatherTextList = new();
        private static List<string> WeatherRainChanceList = new();
        private static List<string> WeatherSnowChanceList = new();
        private static List<string> WeatherTempList = new();
        private static List<string> WeatherDateList = new();
        private static List<string> WeatherWindList = new();
        public static async Task<DiscordEmbedBuilder?> GetRandomMemeAsync(InteractionContext ctx)
        {
            try
            {
                var request = new HttpRequestMessage(HttpMethod.Get, "https://meme-api.com/gimme/memes");
                request.Headers.Add("User-Agent", "DiscordBot");

                var response = await _httpClient.SendAsync(request);

                response.EnsureSuccessStatusCode();

                var json = await response.Content.ReadAsStringAsync();
                var memeData = JsonConvert.DeserializeObject<dynamic>(json);

                if (memeData == null)
                {
                    await ctx.EditResponseAsync(new DiscordWebhookBuilder().WithContent("Brak danych w memie."));
                    return null;
                }

                string memeUrl = memeData.url;
                string memeTitle = memeData.title;
                string memeAuthor = memeData.author;

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

        public static async Task<DiscordEmbedBuilder?> GetRandomWikiAsync(InteractionContext ctx)
        {
            try
            {
                var request = new HttpRequestMessage(HttpMethod.Get, "https://pl.wikipedia.org/api/rest_v1/page/random/summary");
                request.Headers.Add("User-Agent", "DiscordBot");

                var response = await _httpClient.SendAsync(request);

                response.EnsureSuccessStatusCode();

                var json = await response.Content.ReadAsStringAsync();
                var wikiData = JsonConvert.DeserializeObject<dynamic>(json);

                if (wikiData == null)
                {
                    await ctx.EditResponseAsync(new DiscordWebhookBuilder().WithContent("Brak danych wiki."));
                    return null;
                }

                string wikiTitle = wikiData.title;
                string wikiUrl = wikiData.content_urls.desktop.page;
                string wikiImage = wikiData.thumbnail.source;
                string wikiText = wikiData.extract;

                var wikiEmbed = new DiscordEmbedBuilder()
                {
                    Color = DiscordColor.White,
                    Title = wikiTitle,
                    Url = wikiUrl,
                    Description = wikiText,
                    ImageUrl = wikiImage,

                };

                return wikiEmbed;
            }
            catch (HttpRequestException ex)
            {
                await ctx.EditResponseAsync(new DiscordWebhookBuilder().WithContent($"Błąd podczas pobierania strony: {ex.Message}"));
                return null;
            }
            catch (JsonException ex)
            {
                await ctx.EditResponseAsync(new DiscordWebhookBuilder().WithContent($"Błąd podczas parsowania danych JSON: {ex.Message}"));
                return null;
            }
        }

        public static async Task<DiscordEmbedBuilder?> GetWeather(InteractionContext ctx, string city)
        {
            try
            {
                string apiKey = Program.globalConfig.WeatherApi;
                string apiUrl = $"http://api.weatherapi.com/v1/current.json?key={apiKey}&q={city}&aqi=no";
                var request = new HttpRequestMessage(HttpMethod.Get, apiUrl);
                var response = await _httpClient.SendAsync(request);

                response.EnsureSuccessStatusCode();

                var json = await response.Content.ReadAsStringAsync();
                var WeatherData = JsonConvert.DeserializeObject<dynamic>(json);

                if (WeatherData == null)
                {
                    await ctx.EditResponseAsync(new DiscordWebhookBuilder().WithContent("Brak danych pogodowych."));
                    return null;
                }

                string WeatherTitle = WeatherData.location.name;
                string WeatherCountry = WeatherData.location.country;
                string WeatherText = WeatherData.current.condition.text;
                string WeatherImage = WeatherData.current.condition.icon;
                string WeatherTemp = WeatherData.current.temp_c;
                string WeatherTempFeels = WeatherData.current.feelslike_c;
                string WeatherWind = WeatherData.current.wind_kph;
                string WeatherCloud = WeatherData.current.cloud;
                string WeatherPreasure = WeatherData.current.pressure_mb;
                string WeatherTime = WeatherData.current.last_updated;

                var weatherEmbed = new DiscordEmbedBuilder()
                {
                    Color = DiscordColor.CornflowerBlue,
                    Title = $"{WeatherTitle}, {WeatherCountry}",
                    ImageUrl = $"https:{WeatherImage}",
                    Description =
                        $"Weather: {WeatherText} \n" +
                        $"Temperature: {WeatherTemp} °C \n" +
                        $"Feels Like: {WeatherTempFeels} °C \n" +
                        $"Wind: {WeatherWind} Km/H \n" +
                        $"Preasure: {WeatherPreasure} hPa \n" +
                        $"Clouds: {WeatherCloud}% \n" +
                        $"Time: {WeatherTime}",
                };

                return weatherEmbed;
            }
            catch (HttpRequestException ex)
            {
                await ctx.EditResponseAsync(new DiscordWebhookBuilder().WithContent($"Błąd podczas pobierania strony: {ex.Message}"));
                return null;
            }
            catch (JsonException ex)
            {
                await ctx.EditResponseAsync(new DiscordWebhookBuilder().WithContent($"Błąd podczas parsowania danych JSON: {ex.Message}"));
                return null;
            }
        }

        public static async Task<DiscordEmbedBuilder?> GetForecast(InteractionContext ctx, string city)
        {
            string desc = string.Empty;
            string frame = "════════════════════════════════\n";

            try
            {
                string apiKey = Program.globalConfig.WeatherApi;
                string apiUrl = $"http://api.weatherapi.com/v1/forecast.json?key={apiKey}&q={city}&days=4";
                var request = new HttpRequestMessage(HttpMethod.Get, apiUrl);
                var response = await _httpClient.SendAsync(request);

                response.EnsureSuccessStatusCode();

                var json = await response.Content.ReadAsStringAsync();
                var WeatherData = JsonConvert.DeserializeObject<dynamic>(json);

                if (WeatherData == null)
                {
                    await ctx.EditResponseAsync(new DiscordWebhookBuilder().WithContent("Brak danych pogodowych."));
                    return null;
                }

                string WeatherTitle = WeatherData.location.name;
                string WeatherCountry = WeatherData.location.country;

                for (int i = 0; i < 3; i++)
                {
                    WeatherWindList.Add($"{WeatherData.forecast.forecastday[i].day.maxwind_kph}");
                    WeatherTextList.Add($"{WeatherData.forecast.forecastday[i].day.condition.text}");
                    WeatherRainChanceList.Add($"{WeatherData.forecast.forecastday[i].day.daily_chance_of_rain}");
                    WeatherSnowChanceList.Add($"{WeatherData.forecast.forecastday[i].day.daily_chance_of_snow}");
                    WeatherTempList.Add($"{WeatherData.forecast.forecastday[i].day.maxtemp_c}");
                    WeatherDateList.Add($"{WeatherData.forecast.forecastday[i].date}");
                }

                desc += $"{frame}";

                for (int i = 0; i < WeatherTextList.Count(); i++)
                {
                    desc += $"**Date:{WeatherDateList[i]}**\n **Weather:** {WeatherTextList[i]}, **Temperature:** {WeatherTempList[i]}°C\n **Wind**: {WeatherWindList[i]} k/h, **Chance of rain/snow:** {WeatherRainChanceList[i]}% / {WeatherSnowChanceList[i]}%\n";
                    desc += $"{frame}";
                }

                var weatherEmbed = new DiscordEmbedBuilder()
                {
                    Color = DiscordColor.CornflowerBlue,
                    Title = $"{WeatherTitle}, {WeatherCountry}",
                    Description = "**Forecast:**\n" + desc
                };

                return weatherEmbed;
            }
            catch (HttpRequestException ex)
            {
                await ctx.EditResponseAsync(new DiscordWebhookBuilder().WithContent($"Błąd podczas pobierania strony: {ex.Message}"));
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
