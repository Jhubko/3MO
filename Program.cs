using Discord_Bot.commands;
using Discord_Bot.commands.slash;
using Discord_Bot.config;
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
        public static IAudioService? AudioService { get; set; }
        public static DiscordClient? Client { get; set; }
        private static CommandsNextExtension? Commands { get; set; }

        public static JSONReader jsonReader = new();
        public static string configPath = string.Empty;

        public static VoicePointsManager voicePointsManager;


        static async Task Main(string[] args)
        {
            await jsonReader.ReadJSON();

            configPath = jsonReader.ConfigPath;

            var discordConfig = new DiscordConfiguration()
            {
                Intents = DiscordIntents.All,
                Token = jsonReader.Token,
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


            var commandsConfig = new CommandsNextConfiguration()
            {
                StringPrefixes = new string[] { jsonReader.Prefix },
                EnableMentionPrefix = true,
                EnableDms = true,
                EnableDefaultHelp = false
            };

            Commands = Client.UseCommandsNext(commandsConfig);
            Commands.CommandErrored += CommandEventHandler;
            Commands.RegisterCommands<GamesCommands>();
            Commands.RegisterCommands<MenagmentCommands>();

            UriBuilder builder = new UriBuilder
            {
                Scheme = jsonReader.Secured ? Uri.UriSchemeHttps : Uri.UriSchemeHttp, // Możesz zmienić na "https" jeśli używasz HTTPS
                Host = jsonReader.LlHostname,
                Port = jsonReader.LlPort
            };

            using var serviceProvider = new ServiceCollection()
                    .AddSingleton(Client)
                    .AddLavalink()
                    .ConfigureLavalink(x =>  
                    {
                        x.Label = "Lavalink";
                        x.BaseAddress = builder.Uri;
                        x.Passphrase = jsonReader.LlPass;
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
            SlashCommandConfig.RegisterCommands<PointsSlashCommands>();
            SlashCommandConfig.RegisterCommands<GambleCommand>();
            SlashCommandConfig.RegisterCommands<DuelCommand>();

            await Client.ConnectAsync();
            foreach (var hostedService in serviceProvider.GetServices<IHostedService>())
            {
                await hostedService
                .StartAsync(CancellationToken.None)
                .ConfigureAwait(false);
            }

            StartDeleteBotMessagesTask();

            await Task.Delay(-1);
        }

        private static async Task Client_VoiceStateUpdated(DiscordClient sender, VoiceStateUpdateEventArgs args)
        {
            await voicePointsManager.OnVoiceStateUpdated(sender, args); // Obsługuje zmiany stanu głosowego
        }

        private static void StartDeleteBotMessagesTask()
        {
            var interval = TimeSpan.FromHours(12).TotalMilliseconds;

            var timer = new System.Timers.Timer(interval);
            timer.Elapsed += async (sender, e) =>
            {
                try
                {
                    await MessagesHandler.DeleteBotMessages();
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Exception in DeleteBotMessages: {ex.Message}");
                }
            };
            timer.Start();
        }

        private static async Task Client_MessageReactionAdded(DiscordClient sender, MessageReactionAddEventArgs args)
        {
            var reaction = args.Emoji;
            await ReadJson(args.Guild.Id);

            if (jsonReader.DeleteMessageEmoji == null)
                return;

            await MessagesHandler.DeleteUnwantedMessage(args, DiscordEmoji.FromName(sender, jsonReader.DeleteMessageEmoji));
        }

        private static async Task Client_MessageCreated(DiscordClient sender, MessageCreateEventArgs args)
        {
            var channel = args.Channel;
            await ReadJson(args.Guild.Id);

            if (jsonReader.ImageChannels != null && jsonReader.ImageChannels.ToList().Contains(channel.Id.ToString()))
            {
                if (Regex.IsMatch(args.Message.Content, "."))
                    await args.Message.DeleteAsync();
            }

            if (args.Message.Author.IsBot)
                await jsonReader.UpdateJSON(args.Guild.Id, "BotMessages", args.Message.ChannelId.ToString(), args.Message.Id.ToString());
        }

        private static async Task Client_GuildMemberAdded(DiscordClient sender, GuildMemberAddEventArgs args)
        {
            var member = args.Member;
            await ReadJson(args.Guild.Id);

            var roleid = Convert.ToUInt64(jsonReader.DefaultRole);

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
        }

        public static List<ulong> GetGuilds()
        {
            var guilds = Client.Guilds;
            return guilds.Keys.ToList();
        }

        public static async Task ReadJson(ulong serverId)
        {
            string filePath = Path.Combine(configPath, $"{serverId}.json");

            if (!File.Exists(filePath))
                jsonReader.CreateJSON(serverId);

            await jsonReader.ReadJSON(filePath);
        }
    }
}
