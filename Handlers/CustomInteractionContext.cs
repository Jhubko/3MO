using DSharpPlus;
using DSharpPlus.Entities;

namespace Discord_Bot.Handlers
{
    public class CustomInteractionContext(DiscordClient client, DiscordGuild guild, DiscordChannel channel, DiscordUser user)
    {
        public DiscordClient Client { get; set; } = client;
        public DiscordGuild Guild { get; set; } = guild;
        public DiscordChannel Channel { get; set; } = channel;
        public DiscordUser User { get; set; } = user;

        public async Task CreateResponseAsync(string content, bool isEphemeral)
        {
            await Channel.SendMessageAsync(content);
        }
    }
}