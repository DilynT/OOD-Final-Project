using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;
using Discord.Commands;
using Discord;
using FinalProjectBot.Services;
using FinalProjectBot.Helpers;

namespace FinalProjectBot.Modules
{
    [Name("Audio")]
    [Summary("Audio module to interact with voice chat. Currently, used to playback audio in a stream.")]
    public class AudioModule : CustomModule
    {
        private readonly AudioService m_Service;

        //Dependencies are automatically injected via this constructor
        //Add an instance of the AudioService
        public AudioModule(AudioService service)
        {
            m_Service = service;
            m_Service.SetParentModule(this); // Reference to this from the service.
        }


        //A lot of these sections are very short, because as I was working I found this advice for working with discord bots
        // 'Avoid using long-running code in your modules wherever possible. 
        // You should not be implementing very much logic into your modules,
        // instead, outsource to a service for that.'

        //Gets bot to join the chat
        [Command("join", RunMode = RunMode.Async)]
        [Remarks("!join")]
        [Summary("Joins the user's voice channel.")]
        public async Task JoinVoiceChannel()
        {
            if (m_Service.GetDelayAction()) return; //Stop multiple attempts to join too quickly.
            await m_Service.JoinAudioAsync(Context.Guild, (Context.User as IVoiceState).VoiceChannel);

            //Start the autoplay service if enabled
            await m_Service.CheckAutoPlayAsync(Context.Guild, Context.Channel);
        }

        //gets bot to leave the chat
        [Command("leave", RunMode = RunMode.Async)]
        [Remarks("!leave")]
        [Summary("Leaves the current voice channel.")]
        public async Task LeaveVoiceChannel()
        {
            await m_Service.LeaveAudioAsync(Context.Guild);
        }

        //tells bot to play given Link
        [Command("play", RunMode = RunMode.Async)]
        [Remarks("!play [url/index]")]
        [Summary("Plays a song by url or local path.")]
        public async Task PlayVoiceChannel([Remainder] string song)
        {
            await m_Service.ForcePlayAudioAsync(Context.Guild, Context.Channel, song);

            //A counter to make sure this is the last play called, to avoid cascading auto plays
            if (m_Service.GetNumPlaysCalled() == 0) await m_Service.CheckAutoPlayAsync(Context.Guild, Context.Channel);
        }

        //play a song locally, Really buggy.
        [Command("play", RunMode = RunMode.Async)]
        public async Task PlayVoiceChannelByIndex(int index)
        {
            // Play a song by it's local index in the download folder.
            await PlayVoiceChannel(m_Service.GetLocalSong(index));
        }

        //Most of the commands from here down are self explanitory
        [Command("pause", RunMode = RunMode.Async)]
        [Remarks("!pause")]
        [Summary("Pauses the current song, if playing.")]
        public async Task PauseVoiceChannel()
        {
            m_Service.PauseAudio();
            await Task.Delay(0); // Suppress async warnings
        }

        [Command("resume", RunMode = RunMode.Async)]
        [Remarks("!resume")]
        [Summary("Pauses the current song, if paused.")]
        public async Task ResumeVoiceChannel()
        {
            m_Service.ResumeAudio();
            await Task.Delay(0); // Suppress async warnings.
        }

        [Command("stop", RunMode = RunMode.Async)]
        [Remarks("!stop")]
        [Summary("Stops the current song, if playing or paused.")]
        public async Task StopVoiceChannel()
        {
            m_Service.StopAudio();
            await Task.Delay(0); // Suppress async warnings.
        }

        [Command("volume")]
        [Remarks("!volume [num]")]
        [Summary("Changes the volume to [0 - 100].")]
        public async Task VolumeVoiceChannel(int volume)
        {
            m_Service.AdjustVolume((float)volume / 100.0f);
            await Task.Delay(0); // Suppress async warrnings.
        }

        [Command("add", RunMode = RunMode.Async)]
        [Remarks("!add [url/index]")]
        [Summary("Adds a song by url or local path to the playlist.")]
        public async Task AddVoiceChannel([Remainder] string song)
        {
            // Add it to the playlist.
            await m_Service.PlaylistAddAsync(song);

            // Start the autoplay service if enabled, but not yet started.
            await m_Service.CheckAutoPlayAsync(Context.Guild, Context.Channel);
        }

        [Command("add", RunMode = RunMode.Async)]
        public async Task AddVoiceChannelByIndex(int index)
        {
            // Add a song by it's local index in the download folder.
            await AddVoiceChannel(m_Service.GetLocalSong(index));
        }

        [Command("skip", RunMode = RunMode.Async)]
        [Alias("skip", "next")]
        [Remarks("!skip")]
        [Summary("Skips the current song, if playing from the playlist.")]
        public async Task SkipVoiceChannel()
        {
            m_Service.PlaylistSkip();
            await Task.Delay(0);
        }

        [Command("playlist", RunMode = RunMode.Async)]
        [Remarks("!playlist")]
        [Summary("Shows what's currently in the playlist.")]
        public async Task PrintPlaylistVoiceChannel()
        {
            m_Service.PrintPlaylist();
            await Task.Delay(0);
        }

        [Command("autoplay", RunMode = RunMode.Async)]
        [Remarks("!autoplay [enable]")]
        [Summary("Starts the autoplay service on the current playlist.")]
        public async Task AutoPlayVoiceChannel(bool enable)
        {
            m_Service.SetAutoPlay(enable);

            // Start the autoplay service if already on, but not started.
            await m_Service.CheckAutoPlayAsync(Context.Guild, Context.Channel);
        }

        [Command("download", RunMode = RunMode.Async)]
        [Remarks("!download [http]")]
        [Summary("Download songs into our local folder.")]
        public async Task DownloadSong([Remainder] string path)
        {
            await m_Service.DownloadSongAsync(path);
        }

        //List Songs in Queue.
        [Command("songs", RunMode = RunMode.Async)]
        [Remarks("!songs [page]")]
        [Summary("Shows songs in our local folder in pages.")]
        public async Task PrintSongDirectory(int page = 0)
        {
            m_Service.PrintLocalSongs(page);
            await Task.Delay(0);
        }

        //Remove Duplicate entries.
        [Command("cleanupsongs", RunMode = RunMode.Async)]
        [Remarks("!cleanupsongs")]
        [Summary("Cleans the local folder of duplicate files created by our downloader.")]
        public async Task CleanSongDirectory()
        {
            await m_Service.RemoveDuplicateSongsAsync();
        }

    }
}
