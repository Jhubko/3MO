using Discord_Bot.config;
using DSharpPlus.CommandsNext;
using DSharpPlus.CommandsNext.Attributes;
using DSharpPlus.Entities;


namespace Discord_Bot.commands
{
    public class MenagmentCommands : BaseCommandModule
    {
        private JSONReader jsonReader = new JSONReader();

        [Command("deleteMessageEmoji")]
        public async Task DeleteMessageCommand(CommandContext ctx, [RemainingText] DiscordEmoji emoji)
        {

            if (DiscordEmoji.IsValidUnicode(emoji))
            {
                await jsonReader.UpdateJSON(ctx.Guild.Id, "DeleteMessageEmoji", emoji.GetDiscordName());
                await ctx.Channel.SendMessageAsync($"Delete emoji was set to: {emoji}");
                return;
            }
            else
            {
                foreach (var e in ctx.Guild.Emojis.ToList())
                {
                    if (e.Value.Name == emoji.Name)
                    {
                        await jsonReader.UpdateJSON(ctx.Guild.Id, "DeleteMessageEmoji", emoji.GetDiscordName());
                        await ctx.Channel.SendMessageAsync($"Delete emoji was set to: {emoji}");
                        return;
                    }
                }
            }

            await ctx.Channel.SendMessageAsync($"Emoji '{emoji}' was not found.");
        }
    }
}