using Discord_Bot.Config;
using Discord_Bot.other;
using DSharpPlus.CommandsNext;
using DSharpPlus.CommandsNext.Attributes;
using DSharpPlus.Entities;
using DSharpPlus.SlashCommands;

namespace Discord_Bot.commands.slash
{
    internal class ManagementCommands : ApplicationCommandModule
    {
        private static IJsonHandler jsonReader = new JSONReader();
        private JSONWriter GlobalJsonWriter = new JSONWriter(jsonReader, "config.json", Program.serverConfigPath);

        [SlashCommand("help", "Show information about all commands.")]
        public async Task HelpCommand(InteractionContext ctx)
        {
            await ctx.DeferAsync();

            var helpEmbed = Buttons.helpCommandEmbed;

            var message = new DiscordWebhookBuilder()
                .AddEmbed(helpEmbed)
                .AddComponents(Buttons.gamesButton, Buttons.searchButton, Buttons.mngmtButton, Buttons.musicButton);

            await ctx.EditResponseAsync(message);
        }

        [SlashCommand("defaultRole", "Set default role for server.")]
        public async Task DefaultRoleCommand(InteractionContext ctx, [Option("newDefaultRole", "Role you want to be default for this server.")][RemainingText] string newDefaultRole)
        {
            await ctx.DeferAsync();

            foreach (var role in ctx.Guild.Roles)
            {
                if (role.Value.Name == newDefaultRole)
                {
                    await GlobalJsonWriter.UpdateServerConfig(ctx.Guild.Id, "DefaultRole", role.Key.ToString());
                    await ctx.EditResponseAsync(new DiscordWebhookBuilder().WithContent($"New default role set to: {newDefaultRole}")).ConfigureAwait(false);
                    return;
                }
            }
            await ctx.EditResponseAsync(new DiscordWebhookBuilder().WithContent($"Role '{newDefaultRole}' not found on this server.")).ConfigureAwait(false);
        }

        [SlashCommand("deleteMessageEmoji", "Set emoji for delete messages pools.")]
        public async Task DeleteMessageCommand(InteractionContext ctx, [Option("emoji", "Emoji you for delete messages pools")][RemainingText] DiscordEmoji emoji)
        {
            await ctx.DeferAsync();

            if (DiscordEmoji.IsValidUnicode(emoji))
            {
                await GlobalJsonWriter.UpdateServerConfig(ctx.Guild.Id, "DeleteMessageEmoji", emoji.GetDiscordName());
                await ctx.EditResponseAsync(new DiscordWebhookBuilder().WithContent($"Delete emoji was set to: {emoji}")).ConfigureAwait(false);
                return;
            }
            else
            {
                foreach (var e in ctx.Guild.Emojis.ToList())
                {
                    if (e.Value.Name == emoji.Name)
                    {
                        await GlobalJsonWriter.UpdateServerConfig(ctx.Guild.Id, "DeleteMessageEmoji", emoji.GetDiscordName());
                        await ctx.EditResponseAsync(new DiscordWebhookBuilder().WithContent($"Delete emoji was set to: {emoji}")).ConfigureAwait(false);
                        return;
                    }
                }
            }

            await ctx.EditResponseAsync(new DiscordWebhookBuilder().WithContent($"Emoji '{emoji}' was not found.")).ConfigureAwait(false);
        }

        [Command("imageOnly")]
        [SlashCommand("imageOnly", "Set image only channels for your channel.")]
        public async Task ImageOnlyChannelCommand(InteractionContext ctx, [Option("channelToChange", "Channel name you want to be image only channels for your channel")][RemainingText] string channelToChange)
        {
            await ctx.DeferAsync();

            foreach (var channel in ctx.Guild.Channels)
            {
                if (channel.Value.Name == channelToChange)
                {
                    await GlobalJsonWriter.UpdateServerConfig(ctx.Guild.Id, "ImageOnlyChannels", channel.Key.ToString());
                    await ctx.EditResponseAsync(new DiscordWebhookBuilder().WithContent($"{channelToChange} was changed to image only.")).ConfigureAwait(false);
                    return;
                }
            }
            await ctx.EditResponseAsync(new DiscordWebhookBuilder().WithContent($"Channel: '{channelToChange}' not found on this server.")).ConfigureAwait(false);
        }
    }
}
