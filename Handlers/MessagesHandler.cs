using Discord_Bot.Config;
using DSharpPlus;
using DSharpPlus.Entities;
using DSharpPlus.EventArgs;
using DSharpPlus.Exceptions;
using System.Threading.Channels;

namespace Discord_Bot.other
{
    internal class MessagesHandler
    {
        private static readonly IJsonHandler jsonReader = new JSONReader();
        private static readonly JSONWriter jsonWriter = new(jsonReader, "config.json", Program.serverConfigPath);

        public static async Task DeleteUnwantedMessage(MessageReactionAddEventArgs args, DiscordEmoji emoji)
        {
            DiscordMessage? message;
            try
            {
                message = await args.Channel.GetMessageAsync(args.Message.Id);
            }
            catch (NotFoundException)
            {
                return;
            }

            if (message == null)
                return;

            IReadOnlyList<DiscordUser> deleteMessageReactions;
            try
            {
                deleteMessageReactions = await message.GetReactionsAsync(emoji);
            }
            catch (NotFoundException)
            {
                return;
            }

            bool adminReaction = false;
            foreach (var user in deleteMessageReactions)
            {
                var member = await args.Guild.GetMemberAsync(user.Id);

                if (member.Permissions == Permissions.All || member.Permissions == Permissions.Administrator)
                {
                    adminReaction = true;
                    break;
                }
            }

            if (deleteMessageReactions.Count >= 2 || (deleteMessageReactions.Count == 1 && adminReaction))
            {
                try
                {
                    await message.DeleteAsync();
                }
                catch (NotFoundException)
                {
                    return;
                }

                await args.Channel.SendMessageAsync($"{message.Author.Mention} Your message was deleted in vote");
            }
        }
        public static async Task DeleteBotMessages()
        {
            var guilds = Program.GetGuilds();

            foreach (var guild in guilds)
            {
                var serverConfig = await Program.jsonHandler.ReadJson<ServerConfig>($"{Program.globalConfig.ConfigPath}\\{guild}.json");

                if (serverConfig?.BotMessages == null)
                    continue;

                foreach (var channelId in serverConfig.BotMessages.Keys.ToList())
                {
                    foreach (var messageId in serverConfig.BotMessages[channelId].ToList())
                    {
                        try
                        {
                            if (ulong.TryParse(channelId, out ulong parsedChannelId) &&
                                ulong.TryParse(messageId, out ulong parsedMessageId))
                            {
                                var channel = await Program.Client.GetChannelAsync(parsedChannelId);
                                if (channel != null)
                                {
                                    var message = await channel.GetMessageAsync(parsedMessageId);
                                    if (message != null)
                                    {
                                        await message.DeleteAsync();
                                    }
                                }
                            }
                        }
                        catch (Exception ex)
                        {
                            Console.WriteLine($"Błąd przy usuwaniu wiadomości {messageId} w kanale {channelId}: {ex.Message}");
                        }
                    }
                    await jsonWriter.UpdateServerConfig(guild, "BotMessages", null, channelId.ToString());
                }
            }
        }

    }
}
