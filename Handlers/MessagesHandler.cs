using Discord_Bot.Config;
using DSharpPlus;
using DSharpPlus.Entities;
using DSharpPlus.EventArgs;

namespace Discord_Bot.other
{
    internal class MessagesHandler
    {
        private static readonly IJsonHandler jsonReader = new JSONReader();
        private static readonly JSONWriter jsonWriter = new(jsonReader, "config.json", Program.serverConfigPath);

        public static async Task DeleteUnwantedMessage(MessageReactionAddEventArgs args, DiscordEmoji emoji)
        {
            var message = await args.Channel.GetMessageAsync(args.Message.Id);
            if (message == null)
                return;
            var deleteMessageReactions = await args.Message.GetReactionsAsync(emoji);
            var mentionAuthor = message.Author.Mention;
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
                await args.Channel.SendMessageAsync($"{mentionAuthor} Your message was deleted in vote");
                await args.Message.DeleteAsync();
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
                    foreach (var messageId in serverConfig.BotMessages[channelId])
                    {
                        ulong parsedChannelId;
                        ulong parsedMessageId;

                        if (ulong.TryParse(channelId, out parsedChannelId) && ulong.TryParse(messageId, out parsedMessageId))
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
                }
                await jsonWriter.UpdateServerConfig(guild, "BotMessages", "{}");
            }
        }
    }
}
