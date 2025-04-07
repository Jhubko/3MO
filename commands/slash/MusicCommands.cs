using DSharpPlus.CommandsNext.Attributes;
using DSharpPlus.Entities;
using DSharpPlus.SlashCommands;
using Lavalink4NET;
using Lavalink4NET.Players;
using Lavalink4NET.Players.Queued;
using Lavalink4NET.Rest.Entities.Tracks;
using Microsoft.Extensions.Options;
using System.Text.RegularExpressions;

namespace Discord_Bot.commands.slash
{
    internal class MusicCommands : ApplicationCommandModule
    {
        private readonly IAudioService? _audioService = Program.AudioService;
        private async ValueTask<QueuedLavalinkPlayer?> GetPlayerAsync(InteractionContext ctx, bool connectToVoiceChannel = true)
        {
            var playerOptions = new QueuedLavalinkPlayerOptions { DisconnectOnStop = true, SelfDeaf = true };
            var channelBehavior = connectToVoiceChannel
                ? PlayerChannelBehavior.Join
                : PlayerChannelBehavior.None;

            var retrieveOptions = new PlayerRetrieveOptions(ChannelBehavior: channelBehavior);

            var result = await _audioService.Players
                        .RetrieveAsync(guildId: ctx.Guild.Id, memberVoiceChannel: ctx.Member.VoiceState.Channel.Id, playerFactory: PlayerFactory.Queued, options: Options.Create(options: playerOptions), retrieveOptions)
                        .ConfigureAwait(false);

            if (!result.IsSuccess)
            {
                var errorMessage = result.Status switch
                {
                    PlayerRetrieveStatus.UserNotInVoiceChannel => "You are not connected to a voice channel.",
                    PlayerRetrieveStatus.BotNotConnected => "The bot is currently not connected.",
                    _ => "Unknown error.",
                };

                await ctx.CreateResponseAsync(errorMessage).ConfigureAwait(false);
                return null;
            }

            return result.Player;
        }

        [SlashCommand("play", "Play music")]
        public async Task PlayMusic(InteractionContext ctx, [Option("songname", "Song or playlist You want to play")][RemainingText] string songname)
        {
            var player = await GetPlayerAsync(ctx, connectToVoiceChannel: true);
            await ctx.DeferAsync();

            if (player is null)
                return;

            if (Regex.IsMatch(songname, "list="))
            {
                await AddPlaylist(ctx, player, songname);
                return;
            }

            var track = await _audioService.Tracks
                        .LoadTrackAsync(songname, TrackSearchMode.YouTube)
                        .ConfigureAwait(false);

            if (track is null)
            {
                await ctx.EditResponseAsync(new DiscordWebhookBuilder().WithContent("😖 No results."));
                return;
            }

            await player.PlayAsync(track).ConfigureAwait(false);

            string musicDesc = $"Title: {track.Title} \n" +
                   $"Author: {track.Author} \n" +
                   $"URL: {track.Uri}";

            var nowPlayingEmbed = new DiscordEmbedBuilder()
            {
                Color = DiscordColor.DarkRed,
                Title = "Teraz leci:",
                Description = musicDesc
            };

            if (player.Queue.Count != 0)
                await ctx.EditResponseAsync(new DiscordWebhookBuilder().WithContent($"**{track.Author} - {track.Title}** added to queue")).ConfigureAwait(false);
            else
                await ctx.EditResponseAsync(new DiscordWebhookBuilder().AddEmbed(nowPlayingEmbed)).ConfigureAwait(false);
        }
        public async Task AddPlaylist(InteractionContext ctx, QueuedLavalinkPlayer player, [Option("playlist", "Playlist You want to play")][RemainingText] string playlist)
        {
            var result = await _audioService.Tracks
               .LoadTracksAsync(playlist, TrackSearchMode.YouTube)
               .ConfigureAwait(false);

            if (result.Track is null)
            {
                await ctx.EditResponseAsync(new DiscordWebhookBuilder().WithContent("😖 No results."));
                return;
            }

            await player.Queue
                        .AddRangeAsync(result.Tracks.Select(x => new TrackQueueItem(new TrackReference(x))).ToArray())
                        .ConfigureAwait(false);

            if (player.CurrentItem is null)
            {
                await player
                        .SkipAsync()
                        .ConfigureAwait(false);

                string musicDesc = $"Title: {result.Track.Title} \n" +
                   $"Author: {result.Track.Author} \n" +
                   $"URL: {result.Track.Uri}";

                var nowPlayingEmbed = new DiscordEmbedBuilder()
                {
                    Color = DiscordColor.DarkRed,
                    Title = "Teraz leci:",
                    Description = musicDesc
                };
                await ctx.EditResponseAsync(new DiscordWebhookBuilder().AddEmbed(nowPlayingEmbed)).ConfigureAwait(false);
            }
            else
                await ctx.EditResponseAsync(new DiscordWebhookBuilder().WithContent($"**{result.Track.Author} - {result.Track.Title}** added to queue")).ConfigureAwait(false);
        }

        [SlashCommand("playnow", "Force play song")]
        public async Task PlayNow(InteractionContext ctx, [Option("songname", "Song or playlist You want to play")][RemainingText] string songname)
        {
            var player = await GetPlayerAsync(ctx, connectToVoiceChannel: true);
            await ctx.DeferAsync();

            if (player is null)
                return;

            if (Regex.IsMatch(songname, "&list="))
            {
                await ctx.EditResponseAsync(new DiscordWebhookBuilder().WithContent($"😖 You can not force play a playlist!")).ConfigureAwait(false);
                return;
            }

            var track = await _audioService.Tracks
            .LoadTrackAsync(songname, TrackSearchMode.YouTube)
            .ConfigureAwait(false);

            if (track is null)
            {
                await ctx.EditResponseAsync(new DiscordWebhookBuilder().WithContent("😖 No results."));
                return;
            }

            await player.Queue.InsertAsync(0, new TrackQueueItem(track));
            await player.SkipAsync().ConfigureAwait(false);

            string musicDesc = $"Title: {track.Title} \n" +
                   $"Author: {track.Author} \n" +
                   $"URL: {track.Uri}";

            var nowPlayingEmbed = new DiscordEmbedBuilder()
            {
                Color = DiscordColor.DarkRed,
                Title = "Teraz leci:",
                Description = musicDesc
            };

            await ctx.EditResponseAsync(new DiscordWebhookBuilder().AddEmbed(nowPlayingEmbed)).ConfigureAwait(false);
        }

        [SlashCommand("next", "Add song as next")]
        public async Task AddNext(InteractionContext ctx, [Option("songname", "Song or playlist You want to play")][RemainingText] string songname)
        {
            var player = await GetPlayerAsync(ctx, connectToVoiceChannel: true);
            await ctx.DeferAsync();

            if (player is null)
                return;

            if (Regex.IsMatch(songname, "&list="))
            {
                await ctx.EditResponseAsync(new DiscordWebhookBuilder().WithContent($"😖 You can not add playlists")).ConfigureAwait(false);
                return;
            }

            var track = await _audioService.Tracks
            .LoadTrackAsync(songname, TrackSearchMode.YouTube)
            .ConfigureAwait(false);

            if (track is null)
            {
                await ctx.EditResponseAsync(new DiscordWebhookBuilder().WithContent("😖 No results."));
                return;
            }

            var nextTrack = new TrackQueueItem(track);
            await player.Queue.InsertAsync(0, nextTrack);
            await ctx.EditResponseAsync(new DiscordWebhookBuilder().WithContent($"**{track.Author} - {track.Title}** added to the queue next")).ConfigureAwait(false);
        }

        [SlashCommand("pause", description: "Pauses the player.")]
        public async Task PauseMusic(InteractionContext ctx)
        {
            var player = await GetPlayerAsync(ctx, connectToVoiceChannel: false);
            await ctx.DeferAsync();

            if (player is null)
                return;

            if (player.State is PlayerState.Paused)
            {
                await ctx.EditResponseAsync(new DiscordWebhookBuilder().WithContent("Player is already paused.")).ConfigureAwait(false);
                return;
            }

            var pauseEmbed = new DiscordEmbedBuilder()
            {
                Color = DiscordColor.Yellow,
                Title = "Track paused",
            };

            await player.PauseAsync().ConfigureAwait(false);
            await ctx.EditResponseAsync(new DiscordWebhookBuilder().AddEmbed(pauseEmbed)).ConfigureAwait(false);
        }

        [SlashCommand("resume", description: "Resumes the player.")]
        public async Task ResumeMusic(InteractionContext ctx)
        {
            var player = await GetPlayerAsync(ctx, connectToVoiceChannel: false);
            await ctx.DeferAsync();

            if (player is null)
                return;

            if (player.State is not PlayerState.Paused)
            {
                await ctx.EditResponseAsync(new DiscordWebhookBuilder().WithContent("Player is not paused.")).ConfigureAwait(false);
                return;
            }
            var resumeEmbed = new DiscordEmbedBuilder()
            {
                Color = DiscordColor.SpringGreen,
                Title = "Track resumed",
            };

            await player.ResumeAsync().ConfigureAwait(false);
            await ctx.EditResponseAsync(new DiscordWebhookBuilder().AddEmbed(resumeEmbed)).ConfigureAwait(false);
        }

        [SlashCommand("stop", description: "Stops the current track")]
        public async Task StopMusic(InteractionContext ctx)
        {
            var player = await GetPlayerAsync(ctx, connectToVoiceChannel: false);
            await ctx.DeferAsync().ConfigureAwait(false);

            if (player is null)
                return;

            if (player.CurrentTrack is null)
            {
                await ctx.EditResponseAsync(new DiscordWebhookBuilder().WithContent("Nothing playing!")).ConfigureAwait(false);
                await player.DisconnectAsync().ConfigureAwait(false);
                return;
            }

            var stopEmbed = new DiscordEmbedBuilder()
            {
                Color = DiscordColor.Red,
                Title = "Music stopped",
            };

            await player.StopAsync().ConfigureAwait(false);
            await player.DisconnectAsync().ConfigureAwait(false);
            await ctx.EditResponseAsync(new DiscordWebhookBuilder().AddEmbed(stopEmbed)).ConfigureAwait(false);
        }

        [SlashCommand("volume", description: "Sets the player volume (0 - 1000%)")]
        public async Task Volume(InteractionContext ctx, [Option("volume", "What volume do you want?")] long volume = 100)
        {
            var player = await GetPlayerAsync(ctx, connectToVoiceChannel: false);
            await ctx.DeferAsync().ConfigureAwait(false);

            if (player is null)
                return;

            if (volume is > 1000 or < 0)
            {
                await ctx.EditResponseAsync(new DiscordWebhookBuilder().WithContent("Volume out of range: 0% - 1000%!")).ConfigureAwait(false);
                return;
            }


            await player.SetVolumeAsync(volume / 100f).ConfigureAwait(false);
            await ctx.EditResponseAsync(new DiscordWebhookBuilder().WithContent($"Volume updated: {volume}%")).ConfigureAwait(false);
        }

        [SlashCommand("skip", description: "Skips the current track")]
        public async Task SkipMusic(InteractionContext ctx)
        {
            var player = await GetPlayerAsync(ctx, connectToVoiceChannel: false);
            await ctx.DeferAsync().ConfigureAwait(false);

            if (player is null)
                return;

            if (player.CurrentTrack is null)
            {
                await ctx.EditResponseAsync(new DiscordWebhookBuilder().WithContent("Nothing playing!")).ConfigureAwait(false);
                return;
            }

            await player.SkipAsync().ConfigureAwait(false);
            var track = player.CurrentTrack;

            string musicDesc = $"Title: {track.Title} \n" +
                               $"Author: {track.Author} \n" +
                               $"URL: {track.Uri}";

            var nowPlayingEmbed = new DiscordEmbedBuilder()
            {
                Color = DiscordColor.DarkRed,
                Title = "Teraz leci:",
                Description = musicDesc
            };

            if (track is null)
            {
                await player.DisconnectAsync().ConfigureAwait(false);
                await ctx.EditResponseAsync(new DiscordWebhookBuilder().WithContent("Stopped playing because the queue is now empty.")).ConfigureAwait(false);
            }
            else
                await ctx.EditResponseAsync(new DiscordWebhookBuilder().AddEmbed(nowPlayingEmbed)).ConfigureAwait(false);

        }

        [SlashCommand("position", description: "Shows the track position")]
        public async Task Position(InteractionContext ctx)
        {
            var player = await GetPlayerAsync(ctx, connectToVoiceChannel: false);
            await ctx.DeferAsync().ConfigureAwait(false);

            if (player is null)
                return;

            if (player.CurrentTrack is null)
            {
                await ctx.EditResponseAsync(new DiscordWebhookBuilder().WithContent("Nothing playing!")).ConfigureAwait(false);
                return;
            }

            await ctx.EditResponseAsync(new DiscordWebhookBuilder().WithContent($"Position: {player.Position?.Position} / {player.CurrentTrack.Duration}.")).ConfigureAwait(false);
        }

        [SlashCommand("queue", description: "Shows the track queue")]
        public async Task ShowQueue(InteractionContext ctx)
        {
            var player = await GetPlayerAsync(ctx, connectToVoiceChannel: false);
            await ctx.DeferAsync().ConfigureAwait(false);

            if (player is null)
                return;

            var queue = player.Queue.Take(30);
            string songs = string.Empty;
            int num = 0;

            if (player.Queue.IsEmpty)
            {
                await ctx.EditResponseAsync(new DiscordWebhookBuilder().WithContent("Nothing in queue!")).ConfigureAwait(false);
                return;
            }

            foreach (var i in queue)
            {
                num++;
                songs += $"{num}.**{i.Track.Author} - {i.Track.Title}** \n";
            }

            var queueEmbed = new DiscordEmbedBuilder()
            {
                Color = DiscordColor.PhthaloBlue,
                Title = "Song queue",
                Description = songs,
            };

            await ctx.EditResponseAsync(new DiscordWebhookBuilder().AddEmbed(queueEmbed)).ConfigureAwait(false);
        }

        [SlashCommand("shuffle", description: "Turn shuffle on or off")]
        public async Task Shuffle(InteractionContext ctx, [Option("isShuffled", "Suffle player")] bool isShuffled)
        {
            var player = await GetPlayerAsync(ctx, connectToVoiceChannel: false);
            await ctx.DeferAsync().ConfigureAwait(false);

            if (player is null)
                return;

            player.Shuffle = isShuffled;

            if (player.Shuffle)
                await ctx.EditResponseAsync(new DiscordWebhookBuilder().WithContent("Player shuffled")).ConfigureAwait(false);
            else
                await ctx.EditResponseAsync(new DiscordWebhookBuilder().WithContent("Player is not shuffled")).ConfigureAwait(false);
        }
    }
}
