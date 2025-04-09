using Discord_Bot.Commands.Slash;
using Discord_Bot.Config;
using Discord_Bot.Handlers;
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
using System.Text.RegularExpressions;


namespace Discord_Bot
{
    internal class Program
    {
        public static IAudioService AudioService { get; set; } = null!;
        public static DiscordClient Client { get; set; } = null!;

        public static IJsonHandler jsonHandler = new JSONReader();

        public static string serverConfigPath = string.Empty;

        private static JSONWriter globalJsonWriter = null!;

        public static string configPath = string.Empty;

        public static GlobalConfig globalConfig = null!;

        public static VoicePointsManager voicePointsManager = new();

        private static readonly InventoryManager inventoryManager = new();


        static async Task Main(string[] args)
        {
            globalConfig = await jsonHandler.ReadJson<GlobalConfig>("config.json") ?? throw new InvalidOperationException("GlobalConfig cannot be null");
            serverConfigPath = globalConfig.ConfigPath ?? throw new InvalidOperationException("ConfigPath cannot be null");
            globalJsonWriter = new JSONWriter(jsonHandler, "config.json", serverConfigPath);
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
            await Task.Run(() => voicePointsManager.AddPointsLoop());

            UriBuilder builder = new()
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
                        x.Passphrase = globalConfig.LlPass ?? throw new InvalidOperationException("LlPass cannot be null");
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
            SlashCommandConfig.RegisterCommands<HangmanCommands>();
            SlashCommandConfig.RegisterCommands<WordleCommands>();
            SlashCommandConfig.RegisterCommands<FishingCommand>();

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
            taskManager.ScheduleDailyTask("DailyIncome", new TimeSpan(11, 00, 0), async () =>
            {
                var cityHandler = new CityHandler();
                foreach (var guild in GetGuilds())
                {
                    var serverConfig = await jsonHandler.ReadJson<ServerConfig>($"{configPath}\\{guild}.json") ?? throw new InvalidOperationException("ServerConfig cannot be null");
                    if (serverConfig.GamblingChannelId == null)
                        continue;
                    var channel = await Client.GetChannelAsync(Convert.ToUInt64(serverConfig.GamblingChannelId));
                    CustomInteractionContext? ctx = CreateInteractionContext(Client, channel);
                    if (ctx != null)
                        await ctx.CreateResponseAsync($"💸 City revenues have been generated! 💸", false);
                }
                await cityHandler.GenerateDailyIncome();
            });

            await Task.Delay(-1);
        }

        public static CustomInteractionContext? CreateInteractionContext(DiscordClient client, DiscordChannel channel)
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
            await voicePointsManager.OnVoiceStateUpdated(sender, args);
        }

        private static async Task Client_MessageReactionAdded(DiscordClient sender, MessageReactionAddEventArgs args)
        {
            var serverConfig = await jsonHandler.ReadJson<ServerConfig>($"{configPath}\\{args.Guild.Id}.json");

            if (serverConfig?.DeleteMessageEmoji == null)
                return;

            await MessagesHandler.DeleteUnwantedMessage(args, DiscordEmoji.FromName(sender, serverConfig.DeleteMessageEmoji));
        }

        private static async Task Client_MessageCreated(DiscordClient sender, MessageCreateEventArgs args)
        {
            var channel = args.Channel;
            var serverConfig = await jsonHandler.ReadJson<ServerConfig>($"{configPath}\\{args.Guild.Id}.json") ?? throw new InvalidOperationException("ServerConfig cannot be null");

            if (serverConfig.ImageChannels != null && serverConfig.ImageChannels.ToList().Contains(channel.Id.ToString()))
            {
                if (Regex.IsMatch(args.Message.Content, "."))
                    await args.Message.DeleteAsync();
            }

            if (args.Message.Author.IsBot)
            {
                await globalJsonWriter.UpdateServerConfig(args.Guild.Id, "BotMessages", args.Message.ChannelId.ToString(), args.Message.Id.ToString());
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
            var fishList = await inventoryManager.LoadFishDataAsync(args.Guild.Id);

            if (args.Interaction.Data.ComponentType == ComponentType.StringSelect && args.Interaction.Data.CustomId == "pageSelect")
            {
                int selectedPage = int.Parse(args.Interaction.Data.Values.First());
                var embed = new DiscordEmbedBuilder
                {
                    Title = "🐟 Lista Ryb 🐟",
                    Description = $"```\n{FishingCommand.GetFishPage(fishList, selectedPage)}\n```",
                    Color = DiscordColor.Blurple
                };

                var selectComponent = new DiscordSelectComponent("pageSelect", "Wybierz stronę",
                    Enumerable.Range(1, (int)Math.Ceiling((double)fishList.Count / 10))
                    .Select(i => new DiscordSelectComponentOption($"Strona {i}", i.ToString())).ToArray(), false);

                await args.Interaction.CreateResponseAsync(InteractionResponseType.UpdateMessage,
                    new DiscordInteractionResponseBuilder()
                        .AddEmbed(embed)
                        .AddComponents(selectComponent));
            }

            if (args.Interaction.Data.ComponentType == ComponentType.StringSelect && args.Interaction.Data.CustomId == "help_menu")
            {
                var selectedValue = args.Interaction.Data.Values[0];

                DiscordEmbedBuilder selectedEmbed = selectedValue switch
                {
                    "casino" => HelpContent.casinoCommandEmbed,
                    "shop" => HelpContent.shopCommandEmbed,
                    "city" => HelpContent.cityCommandEmbed,
                    "stats" => HelpContent.statsCommandEmbed,
                    "games" => HelpContent.gamesCommandEmbed,
                    "music" => HelpContent.musicCommandEmbed,
                    "search" => HelpContent.searchCommandEmbed,
                    "mngmt" => HelpContent.managementCommandsEmbed,
                    "fish" => HelpContent.fishingCommandEmbed,
                    _ => HelpContent.helpCommandEmbed
                };

                await args.Interaction.CreateResponseAsync(InteractionResponseType.UpdateMessage,
                    new DiscordInteractionResponseBuilder()
                        .AddEmbed(selectedEmbed)
                        .AddComponents(backButton));

                return;
            }

            if (args.Interaction.Data.CustomId == "backButton")
            {
                var selectMenu = new DiscordSelectComponent("help_menu", "Wybierz kategorię",
                [
                    new("Casino", "casino"),
                    new("Shop", "shop"),
                    new("City", "city"),
                    new("Stats", "stats"),
                    new("Games", "games"),
                    new("Fishing", "fish"),
                    new("Music", "music"),
                    new("Search", "search"),
                    new("Management", "mngmt"),
                ]);

                await args.Interaction.CreateResponseAsync(InteractionResponseType.UpdateMessage,
                    new DiscordInteractionResponseBuilder()
                        .AddEmbed(HelpContent.helpCommandEmbed)
                        .AddComponents(selectMenu));
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
            var guilds = Client.Guilds;
            return guilds.Keys.ToList();
        }
    }
}
