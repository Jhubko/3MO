using Discord_Bot.config;
using Discord_Bot.other;
using DSharpPlus.CommandsNext;
using DSharpPlus.CommandsNext.Attributes;
using DSharpPlus.Entities;

namespace Discord_Bot.commands
{
    internal class ManagementCommands : BaseCommandModule
    {
        public static JSONReader jsonReader = new JSONReader();

        [Command("help")]
        public async Task HelpCommand(CommandContext ctx)
        {
            var message = new DiscordMessageBuilder()
                .WithContent("Your message content here")
                .AddEmbed(Buttons.helpCommandEmbed)
                .AddComponents(Buttons.gamesButton, Buttons.searchButton, Buttons.mngmtButton, Buttons.musicButton);

            await ctx.Channel.SendMessageAsync(message);
        }

        [Command("defaultRole")]
        public async Task DefaultRoleCommand(CommandContext ctx, [RemainingText] string newDefaultRole)
        {
            foreach (var role in ctx.Guild.Roles)
            {
                if (role.Value.Name == newDefaultRole)
                {
                    await jsonReader.UpdateJSON(ctx.Guild.Id, "DefaultRole", role.Key.ToString());
                    await ctx.RespondAsync($"New default role set to: {newDefaultRole}");
                    return;
                }               
            }
            await ctx.RespondAsync($"Role '{newDefaultRole}' not found on this server.");
        }

        [Command("deleteMessageEmoji")]
        public async Task DeleteMessageCommand(CommandContext ctx, [RemainingText] DiscordEmoji emoji )
        {
            if (DiscordEmoji.IsValidUnicode(emoji))
            {
                await jsonReader.UpdateJSON(ctx.Guild.Id, "DeleteMessageEmoji", emoji.GetDiscordName());
                await ctx.RespondAsync($"Delete emoji was set to: {emoji}");
                return;
            }
            else
            {
                foreach (var e in ctx.Guild.Emojis.ToList())
                {
                    if (e.Value.Name == emoji.Name)
                    {
                        await jsonReader.UpdateJSON(ctx.Guild.Id, "DeleteMessageEmoji", emoji.GetDiscordName());
                        await ctx.RespondAsync($"Delete emoji was set to: {emoji}");
                        return;
                    }
                }
            }

            await ctx.RespondAsync($"Emoji '{emoji}' was not found.");
        }

        [Command("imageOnly")]
        public async Task ImageOnlyChannelCommand(CommandContext ctx, [RemainingText] string channelToChange)
        {
            foreach (var channel in ctx.Guild.Channels)
            {
                if (channel.Value.Name == channelToChange)
                {
                    await jsonReader.UpdateJSON(ctx.Guild.Id, "ImageOnlyChannels", channel.Key.ToString());
                    await ctx.RespondAsync($"{channelToChange} was changed.");
                    return;
                }
            }
            await ctx.RespondAsync($"Role '{channelToChange}' not found on this server.");
        }
    }
}
