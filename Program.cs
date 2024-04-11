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

        public static JSONReader? jsonReader = new();

        static async Task Main(string[] args)
        {
            await jsonReader.ReadJSON();

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
            //Client.VoiceStateUpdated += VoiceChannelHandler;
            Client.GuildMemberAdded += Client_GuildMemberAdded;
            Client.MessageCreated += Client_MessageCreated;


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
            Commands.RegisterCommands<ManagementCommands>();

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

            SlashCommandConfig.RegisterCommands<PollCommands>();
            SlashCommandConfig.RegisterCommands<SearchCommands>();
            SlashCommandConfig.RegisterCommands<MusicCommands>();


            await Client.ConnectAsync();
            foreach (var hostedService in serviceProvider.GetServices<IHostedService>())
            {
                await hostedService
                .StartAsync(CancellationToken.None)
                .ConfigureAwait(false);
            }
            await Task.Delay(-1);
        }

        private static async Task Client_MessageCreated(DiscordClient sender, MessageCreateEventArgs args)
        {
            var channel = args.Channel;
            var serverId = args.Guild.Id.ToString();
            await jsonReader.ReadJSON(Path.Combine(jsonReader.ConfigPath, $"{serverId}.json"));

            if (jsonReader.ImageChannels == null)
                return;

            if (jsonReader.ImageChannels.ToList().Contains(channel.Id.ToString()))
            {
                if (Regex.IsMatch(args.Message.Content, "."))
                    await args.Message.DeleteAsync();
            }   
        }

        private static async Task Client_GuildMemberAdded(DiscordClient sender, GuildMemberAddEventArgs args)
        {
            var member = args.Member;
            var serverId = args.Guild.Id.ToString();
            await jsonReader.ReadJSON(Path.Combine(jsonReader.ConfigPath, $"{serverId}.json"));

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
        //private static async Task VoiceChannelHandler(DiscordClient sender, VoiceStateUpdateEventArgs e)
        //{
        //    if (e.Channel == null)
        //        return;

        //    var playerOptions = new QueuedLavalinkPlayerOptions { };
        //    var channelBehavior = PlayerChannelBehavior.Join;

        //    var retrieveOptions = new PlayerRetrieveOptions(ChannelBehavior: channelBehavior);

        //    var result = await AudioService.Players
        //                .RetrieveAsync(guildId: e.Guild.Id, memberVoiceChannel: e.Channel.Id, playerFactory: PlayerFactory.Queued, options: Options.Create(options: playerOptions), retrieveOptions)
        //                .ConfigureAwait(false);


        //    var track = await AudioService.Tracks
        //                .LoadTrackAsync("bandycka jazda", TrackSearchMode.YouTube)
        //                .ConfigureAwait(false);

        //    if (e.User.Id == 339126499510583298 && e.Before == null)
        //    {
        //        await result.Player.PlayAsync(track).ConfigureAwait(false);
        //    }
        //}

        private static Task Client_Ready(DiscordClient sender, ReadyEventArgs args)
        {

            return Task.CompletedTask;

            //throw new System.NotImplementedException();
        }
    }
}
