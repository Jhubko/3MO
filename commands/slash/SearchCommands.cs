using DSharpPlus.Entities;
using DSharpPlus.SlashCommands;
using Google.Apis.CustomSearchAPI.v1;
using Google.Apis.Services;
using OpenAI_API;
using Discord_Bot.other;

namespace Discord_Bot.commands.slash
{
    internal class SearchCommands : ApplicationCommandModule
    {

        [SlashCommand("image", "Google images search")]
        public async Task GoogleImageSearch(InteractionContext ctx, [Option("search", "Search image in google")] string search)
        {
            await ctx.DeferAsync();

            string apiKey = Program.jsonReader.Apikey;
            string cseId = Program.jsonReader.CseId;

            var customSearchService = new CustomSearchAPIService(new BaseClientService.Initializer()
            {
                ApiKey = apiKey,
                ApplicationName = "3MO",
            });

            var listRequest = customSearchService.Cse.List();
            listRequest.Cx = cseId;
            listRequest.SearchType = CseResource.ListRequest.SearchTypeEnum.Image;
            listRequest.Q = search;

            var searchRequest = await listRequest.ExecuteAsync();
            var results = searchRequest.Items;

            if (results == null || !results.Any())
            {
                await ctx.EditResponseAsync(new DiscordWebhookBuilder().WithContent("No results found"));
                return;
            }
            else
            {
                var random = new Random();
                int index = random.Next(results.Count);

                var embed = new DiscordEmbedBuilder()
                {
                    Title = "Results for search: " + search,
                    ImageUrl = results[index].Link,
                    Color = DiscordColor.CornflowerBlue
                };

                await ctx.EditResponseAsync(new DiscordWebhookBuilder().AddEmbed(embed));
            }


        }

        [SlashCommand("chatgpt", "Ask question to ChatGPT")]
        public async Task ChatGPTCommand(InteractionContext ctx, [Option("question", "Question You want to ask")] string question)
        {
            await ctx.DeferAsync();

            var api = new OpenAIAPI(Program.jsonReader.ApiGPT);
            var chat = api.Chat.CreateConversation();

            chat.AppendSystemMessage("Type a question");

            chat.AppendUserInput(question);

            var response = await chat.GetResponseFromChatbotAsync();

            var outputembed = new DiscordEmbedBuilder()
            {
                Title = $"Results of  {question}",
                Description = response,
                Color = DiscordColor.PhthaloGreen
            };

            await ctx.EditResponseAsync(new DiscordWebhookBuilder().AddEmbed(outputembed));
        }

        [SlashCommand("meme", "Send random meme")]
        public async Task SendMemeAsync(InteractionContext ctx)
        {
            await ctx.DeferAsync();
            var meme = await SearchSystem.GetRandomMemeAsync(ctx);
            await ctx.EditResponseAsync(new DiscordWebhookBuilder().AddEmbed(meme));
        }
    }
}
