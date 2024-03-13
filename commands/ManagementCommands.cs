using Discord_Bot.config;
using Discord_Bot.other;
using DSharpPlus.CommandsNext;
using DSharpPlus.CommandsNext.Attributes;
using DSharpPlus.Entities;

namespace Discord_Bot.commands
{
    internal class ManagementCommands : BaseCommandModule
    {
        public static config.JSONReader jsonReader = new JSONReader();

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
                    jsonReader.UpdateJSON(ctx.Guild.Id, "DefaultRole", role.Key.ToString());
                    await ctx.RespondAsync($"New default role set to: {newDefaultRole}");
                    return;
                }               
            }
            await ctx.RespondAsync($"Role '{newDefaultRole}' not found on this server.");

        }
    }
}
