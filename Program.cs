﻿using Discord_Bot.commands;
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

namespace Discord_Bot
{
    internal class Program
    {
        public static IAudioService? AudioService { get; set; }
        public static DiscordClient? Client { get; set; }
        private static CommandsNextExtension? Commands { get; set; }
        public static JSONReader jsonReader = new JSONReader();

        static async Task Main(string[] args)
        {
            await jsonReader.ReadJSON();

            var discordConfig = new DiscordConfiguration()
            {
                Intents = DiscordIntents.All,
                Token = jsonReader.token,
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
            Client.GuildMemberAdded += Client_GuildMemberAdded;


            var commandsConfig = new CommandsNextConfiguration()
            {
                StringPrefixes = new string[] { jsonReader.prefix },
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
                Scheme = Uri.UriSchemeHttp, 
                Host = jsonReader.llHostname,
                Port = jsonReader.llPort
            };

            using var serviceProvider = new ServiceCollection()
                    .AddSingleton(Client)
                    .AddLavalink()
                    .ConfigureLavalink(x =>  
                    {
                        x.Label = "Lavalink";
                        x.BaseAddress = builder.Uri;
                        x.Passphrase = jsonReader.llPass;
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

        private static async Task Client_GuildMemberAdded(DiscordClient sender, GuildMemberAddEventArgs args)
        {
            var member = args.Member;
            var role = args.Guild.GetRole(ManagementCommands.defaultRole);

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

        private static Task Client_Ready(DiscordClient sender, ReadyEventArgs args)
        {

            return Task.CompletedTask;

            //throw new System.NotImplementedException();
        }
    }
}
