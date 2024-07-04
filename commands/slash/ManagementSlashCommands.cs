using Discord_Bot.config;
using Discord_Bot.other;
using DSharpPlus.CommandsNext;
using DSharpPlus.CommandsNext.Attributes;
using DSharpPlus.Entities;
using DSharpPlus.SlashCommands;

namespace Discord_Bot.commands.slash
{
    internal class ManagementSlashCommands : ApplicationCommandModule
    {
        private static JSONReader jsonReader = new JSONReader();

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
                    await jsonReader.UpdateJSON(ctx.Guild.Id, "DefaultRole", role.Key.ToString());
                    await ctx.EditResponseAsync(new DiscordWebhookBuilder().WithContent($"New default role set to: {newDefaultRole}")).ConfigureAwait(false);
                    return;
                }
            }
            await ctx.EditResponseAsync(new DiscordWebhookBuilder().WithContent($"Role '{newDefaultRole}' not found on this server.")).ConfigureAwait(false);
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
                    await jsonReader.UpdateJSON(ctx.Guild.Id, "ImageOnlyChannels", channel.Key.ToString());
                    await ctx.EditResponseAsync(new DiscordWebhookBuilder().WithContent($"{channelToChange} was changed to image only.")).ConfigureAwait(false);
                    return;
                }
            }
            await ctx.EditResponseAsync(new DiscordWebhookBuilder().WithContent($"Channel: '{channelToChange}' not found on this server.")).ConfigureAwait(false);
        }
    }
}
