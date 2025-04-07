using Discord_Bot.Config;
using DSharpPlus;

namespace Discord_Bot.other
{
    internal class RaffleHandlers
    {
        public static async Task ResumeRaffle(DiscordClient client, ulong guild)
        {
            var serverConfig = await Program.jsonHandler.ReadJson<ServerConfig>($"{Program.configPath}\\{guild}.json");
            if (serverConfig.GamblingChannelId == null)
                return;
            var channel = await client.GetChannelAsync(Convert.ToUInt64(serverConfig.GamblingChannelId));
            CustomInteractionContext ctx = Program.CreateInteractionContext(client, channel);
            var raffleCommand = new RaffleCommand();
            var pool = serverConfig.RafflePool;
            if (pool == null)
            {
                if (raffleCommand.IsRaffleActive())
                {
                    await raffleCommand.EndRaffle(ctx);
                }
                await raffleCommand.StartRaffle(ctx);
                return;
            }

            await raffleCommand.ResumeRaffle(ctx, pool);
        }

        public static async Task HandleRaffle(DiscordClient client, ulong guild)
        {
            var raffleCommand = new RaffleCommand();
            var serverConfig = await Program.jsonHandler.ReadJson<ServerConfig>($"{Program.configPath}\\{guild}.json");
            if (serverConfig.GamblingChannelId == null)
                return;
            var channel = await client.GetChannelAsync(Convert.ToUInt64(serverConfig.GamblingChannelId));
            CustomInteractionContext ctx = Program.CreateInteractionContext(client, channel);

            if (raffleCommand.IsRaffleActive())
            {
                await raffleCommand.EndRaffle(ctx);
            }
            await raffleCommand.StartRaffle(ctx);
        }
    }
}
