using DSharpPlus;
using DSharpPlus.Entities;
using DSharpPlus.EventArgs;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace Discord_Bot.other
{
    internal class MessagesHandler
    {
        public static async Task DeleteUnwantedMessage(MessageReactionAddEventArgs args, DiscordEmoji emoji)
        {
            var deleteMessageReactions = await args.Message.GetReactionsAsync(emoji);
            var message = await args.Channel.GetMessageAsync(args.Message.Id);
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
                await Program.ReadJson(guild);

                if (Program.jsonReader.BotMessages == null)
                    continue;

                foreach (var channelId in Program.jsonReader.BotMessages.Keys.ToList())
                {
                    foreach (var messageId in Program.jsonReader.BotMessages[channelId])
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

                    Program.jsonReader.BotMessages[channelId].Clear();
                }

                string filePath = Path.Combine(Program.configPath, $"{guild}.json");

                JObject existingJson;
                if (File.Exists(filePath))
                {
                    string existingJsonString = File.ReadAllText(filePath);
                    existingJson = JObject.Parse(existingJsonString);
                }
                else
                {
                    existingJson = new JObject();
                }

                existingJson["BotMessages"] = JObject.FromObject(Program.jsonReader.BotMessages);

                File.WriteAllText(filePath, existingJson.ToString(Formatting.Indented));
            }
        }
    }
}
