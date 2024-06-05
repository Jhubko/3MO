using DSharpPlus;
using DSharpPlus.Entities;
using DSharpPlus.EventArgs;

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

    }
}
