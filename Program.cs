using Discord_Bot.commands.slash;
using Discord_Bot.Config;
using Discord_Bot.other;
using DSharpPlus;
using DSharpPlus.CommandsNext;
using DSharpPlus.CommandsNext.Attributes;
using DSharpPlus.CommandsNext.Exceptions;
using DSharpPlus.Entities;
using DSharpPlus.EventArgs;
using DSharpPlus.Interactivity;
using DSharpPlus.Interactivity.Extensions;
using DSharpPlus.SlashCommands;
using Lavalink4NET;
using Lavalink4NET.Extensions;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using System;
using System.Text.RegularExpressions;


namespace Discord_Bot
{
    internal class Program
    {
        public static IAudioService? AudioService { get; set; }
        public static DiscordClient? Client { get; set; }
        private static CommandsNextExtension? Commands { get; set; }

        public static IJsonHandler jsonHandler = new JSONReader();

        public static string serverConfigPath = string.Empty;

        private static JSONWriter GlobalJsonWriter;

        public static string configPath = string.Empty;

        public static VoicePointsManager voicePointsManager;

        public static GlobalConfig globalConfig;


        static async Task Main(string[] args)
        {
            globalConfig = await jsonHandler.ReadJson<GlobalConfig>("config.json");
            serverConfigPath = globalConfig?.ConfigPath;
            GlobalJsonWriter = new JSONWriter(jsonHandler, "config.json", serverConfigPath);
            configPath = globalConfig.ConfigPath;
            var taskManager = new ScheduledTaskManager();

            var discordConfig = new DiscordConfiguration()
            {
                Intents = DiscordIntents.All,
                Token = globalConfig.Token,
                TokenType = TokenType.Bot,
                AutoReconnect = true
            };

            Client = new DiscordClient(discordConfig);

            Client.UseInteractivity(new InteractivityConfiguration()
            {
                Timeout = TimeSpan.FromMinutes(2)
            });

            Client.Ready += Client_Ready;
            Client.ComponentInteractionCreated += Client_ComponentInteractionCreated;
            Client.MessageReactionAdded += Client_MessageReactionAdded;
            Client.GuildMemberAdded += Client_GuildMemberAdded;
            Client.MessageCreated += Client_MessageCreated;
            Client.VoiceStateUpdated += Client_VoiceStateUpdated;


            voicePointsManager = new VoicePointsManager();
            Task.Run(() => voicePointsManager.AddPointsLoop());

            UriBuilder builder = new UriBuilder
            {
                Scheme = globalConfig.Secured ? Uri.UriSchemeHttps : Uri.UriSchemeHttp,
                Host = globalConfig.LlHostname,
                Port = globalConfig.LlPort
            };

            using var serviceProvider = new ServiceCollection()
                    .AddSingleton(Client)
                    .AddLavalink()
                    .ConfigureLavalink(x =>  
                    {
                        x.Label = "Lavalink";
                        x.BaseAddress = builder.Uri;
                        x.Passphrase = globalConfig.LlPass;
                        x.ResumptionOptions = new LavalinkSessionResumptionOptions(TimeSpan.FromSeconds(60));
                        x.ReadyTimeout = TimeSpan.FromSeconds(15);
                    })
                    .BuildServiceProvider();
            
            AudioService = serviceProvider.GetRequiredService<IAudioService>();

            var SlashCommandConfig = Client.UseSlashCommands(new SlashCommandsConfiguration
            {
                Services = serviceProvider,
            });


            SlashCommandConfig.RegisterCommands<ManagementSlashCommands>();
            SlashCommandConfig.RegisterCommands<GamesSlashCommands>();
            SlashCommandConfig.RegisterCommands<SearchCommands>();
            SlashCommandConfig.RegisterCommands<MusicCommands>();
            SlashCommandConfig.RegisterCommands<StatsSlashCommands>();
            SlashCommandConfig.RegisterCommands<GambleCommand>();
            SlashCommandConfig.RegisterCommands<DuelCommand>();
            SlashCommandConfig.RegisterCommands<RaffleCommand>();
            SlashCommandConfig.RegisterCommands<SlotsCommand>();
            SlashCommandConfig.RegisterCommands<CitySlashCommands>();
            SlashCommandConfig.RegisterCommands<GivePointsCommand>();
            SlashCommandConfig.RegisterCommands<ShopCommand>();

            await Client.ConnectAsync();

            foreach (var hostedService in serviceProvider.GetServices<IHostedService>())
            {
                await hostedService
                .StartAsync(CancellationToken.None)
                .ConfigureAwait(false);
            }

            taskManager.ScheduleDailyTask("DeleteMessagesTask", new TimeSpan(22, 00, 0), async () => await MessagesHandler.DeleteBotMessages());
            taskManager.ScheduleDailyTask("ScheduleRaffle", new TimeSpan(18, 00, 0), async () =>
            {
                foreach (var guild in GetGuilds())
                {
                   await RaffleHandlers.HandleRaffle(Client, guild);
                }
            });
            taskManager.ScheduleDailyTask("DailyIncome", new TimeSpan(10, 00, 0), async () =>
            {
                var cityHandler = new CityHandler();
                foreach (var guild in GetGuilds())
                {
                    var serverConfig = await jsonHandler.ReadJson<ServerConfig>($"{configPath}\\{guild}.json");
                    var channel = await Client.GetChannelAsync(Convert.ToUInt64(serverConfig.GamblingChannelId));
                    CustomInteractionContext ctx = CreateInteractionContext(Client, channel);
                    await ctx.CreateResponseAsync($"💸 @everyone City revenues have been generated! 💸", false);
                }
                await cityHandler.GenerateDailyIncome();
            });

            await Task.Delay(-1);
        }

        public static CustomInteractionContext CreateInteractionContext(DiscordClient client, DiscordChannel channel)
        {
            if (channel.GuildId.HasValue)
            {
            var guild = client.GetGuildAsync(channel.GuildId.Value).Result;
            return new CustomInteractionContext(client, guild, channel, client.CurrentUser);
            }
            return null;
        }

        private static async Task Client_VoiceStateUpdated(DiscordClient sender, VoiceStateUpdateEventArgs args)
        {
            await voicePointsManager.OnVoiceStateUpdated(sender, args); // Obsługuje zmiany stanu głosowego
        }

        private static async Task Client_MessageReactionAdded(DiscordClient sender, MessageReactionAddEventArgs args)
        {
            var reaction = args.Emoji;
            var serverConfig = await jsonHandler.ReadJson<ServerConfig>($"{configPath}\\{args.Guild.Id}.json");

            if (serverConfig?.DeleteMessageEmoji == null)
                return;

            await MessagesHandler.DeleteUnwantedMessage(args, DiscordEmoji.FromName(sender, serverConfig.DeleteMessageEmoji));
        }

        private static async Task Client_MessageCreated(DiscordClient sender, MessageCreateEventArgs args)
        {
            var channel = args.Channel;
            var serverConfig = await jsonHandler.ReadJson<ServerConfig>($"{configPath}\\{args.Guild.Id}.json");

            if (serverConfig.ImageChannels != null && serverConfig.ImageChannels.ToList().Contains(channel.Id.ToString()))
            {
                if (Regex.IsMatch(args.Message.Content, "."))
                    await args.Message.DeleteAsync();
            }

            if (args.Message.Author.IsBot)
            {
                await GlobalJsonWriter.UpdateServerConfig(args.Guild.Id, "BotMessages", args.Message.ChannelId.ToString(), args.Message.Id.ToString());
            }
            else
            {
                await StatsHandler.IncreaseStats(args.Author.Id, "Messages");
            }


        }

        private static async Task Client_GuildMemberAdded(DiscordClient sender, GuildMemberAddEventArgs args)
        {
            var member = args.Member;
            var serverConfig = await jsonHandler.ReadJson<ServerConfig>($"{configPath}\\{args.Guild.Id}.json");

            var roleid = Convert.ToUInt64(serverConfig?.DefaultRole);

            var role = args.Guild.GetRole(roleid);

            if (role != null)
            {
                await member.GrantRoleAsync(role);
            }
        }

        private static async Task Client_ComponentInteractionCreated(DiscordClient sender, ComponentInteractionCreateEventArgs args)
        {
            var backButton = new DiscordButtonComponent(ButtonStyle.Primary, "backButton", "Back");

            switch (args.Interaction.Data.CustomId)
            {
                case "gamesButton":
                    await args.Interaction.CreateResponseAsync(InteractionResponseType.UpdateMessage, new DiscordInteractionResponseBuilder().AddEmbed(Buttons.gamesCommandEmbed).AddComponents(backButton));
                    break;
                case "mngmtButton":
                    await args.Interaction.CreateResponseAsync(InteractionResponseType.UpdateMessage, new DiscordInteractionResponseBuilder().AddEmbed(Buttons.managementCommandsEmbed).AddComponents(backButton));
                    break;
                case "searchButton":
                    await args.Interaction.CreateResponseAsync(InteractionResponseType.UpdateMessage, new DiscordInteractionResponseBuilder().AddEmbed(Buttons.searchCommandEmbed).AddComponents(backButton));
                    break;
                case "musicButton":
                    await args.Interaction.CreateResponseAsync(InteractionResponseType.UpdateMessage, new DiscordInteractionResponseBuilder().AddEmbed(Buttons.musicCommandEmbed).AddComponents(backButton));
                    break;
                case "backButton":
                    await args.Interaction.CreateResponseAsync(InteractionResponseType.UpdateMessage, new DiscordInteractionResponseBuilder().AddEmbed(Buttons.helpCommandEmbed).AddComponents(Buttons.gamesButton, Buttons.searchButton, Buttons.mngmtButton, Buttons.musicButton));
                    break;


            }
        }

        private static async Task CommandEventHandler(CommandsNextExtension sender, CommandErrorEventArgs e)
        {
            if (e.Exception is ChecksFailedException exception)
            {
                string timeLeft = string.Empty;

                foreach (var check in exception.FailedChecks)
                {
                    var coolDown = (CooldownAttribute)check;
                    timeLeft = coolDown.GetRemainingCooldown(e.Context).ToString(@"hh\:mm\:ss");
                }

                var coolDownMessage = new DiscordEmbedBuilder()
                {
                    Color = DiscordColor.Red,
                    Title = "Please wait for the cooldown to end",
                    Description = $"Time: {timeLeft}"
                };

                await e.Context.Channel.SendMessageAsync(embed: coolDownMessage);

            }
        }

        private static async Task Client_Ready(DiscordClient sender, ReadyEventArgs args)
        {
            await voicePointsManager.CollectActiveUsers(sender);
            foreach (var guild in GetGuilds())
            {
                await RaffleHandlers.ResumeRaffle(Client, guild);
            }
        }

        public static List<ulong> GetGuilds()
        {
            var guilds = Client?.Guilds;
            return guilds.Keys.ToList();
        }
    }
}
