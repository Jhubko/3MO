using DSharpPlus;
using DSharpPlus.Entities;

public class CustomInteractionContext
{
    public DiscordClient Client { get; set; }
    public DiscordGuild Guild { get; set; }
    public DiscordChannel Channel { get; set; }
    public DiscordUser User { get; set; }

    public CustomInteractionContext(DiscordClient client, DiscordGuild guild, DiscordChannel channel, DiscordUser user)
    {
        Client = client;
        Guild = guild;
        Channel = channel;
        User = user;
    }

    public async Task CreateResponseAsync(string content, bool isEphemeral)
    {
        await Channel.SendMessageAsync(content);
    }
}