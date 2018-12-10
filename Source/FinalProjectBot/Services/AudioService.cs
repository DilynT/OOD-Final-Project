using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Discord;
using Discord.Audio;
using FinalProjectBot.Helpers;
using Microsoft.VisualBasic;

namespace FinalProjectBot.Services
{
    public class AudioService : CustomService
    {
        // Concurrent dictionary for multithreaded environments.
        private readonly ConcurrentDictionary<ulong, IAudioClient> m_ConnectedChannels = new ConcurrentDictionary<ulong, IAudioClient>();

        // Playlist.
        private readonly ConcurrentQueue<AudioFile> m_Playlist = new ConcurrentQueue<AudioFile>();

        // Downloader.
        private readonly AudioDownloader m_AudioDownloader = new AudioDownloader(); // Only downloaded on playlist add.

        // Player.
        private readonly AudioPlayer m_AudioPlayer = new AudioPlayer();

        // Private variables.
        private int m_NumPlaysCalled = 0;           // This is to check for the last 'ForcePlay' call.
        private int m_DelayActionLength = 10000;    // To prevent connection issues, we set it to a fairly 'large' value.
        private bool m_DelayAction = false;         // Temporary Semaphore to control leaving and joining too quickly.
        private bool m_AutoPlay = false;            // Flag to check if autoplay is currently on or not.
        private bool m_AutoPlayRunning = false;     // Flag to check if autoplay is currently running or not. More of a 'sanity' check really.
        private bool m_AutoDownload = true;         // Flag to auto download network items in the playlist.
        private bool m_AutoStop = false;            // Flag to stop the autoplay service when we're done playing all songs in the playlist.
        private Timer m_VoiceChannelTimer = null;   // Timer to check for active users in the voice channel.
        private bool m_LeaveWhenEmpty = true;       // Flag to set up leaving the channel when there are no active users.

        // Any async function that's called after this, if required can check for m_DelayAction before continuing.
        private async Task DelayAction(Action f)
        {
            m_DelayAction = true; // Lock.
            f();
            await Task.Delay(m_DelayActionLength); // Delay to prevent error condition. TEMPORARY.
            m_DelayAction = false; // Unlock.
        }

        // Gets m_DelayAction, this is a temporary semaphore to prevent joining too quickly after leaving a channel.
        public bool GetDelayAction()
        {
            if (m_DelayAction) Log("This action is delayed. Please try again later.");
            return m_DelayAction;
        }

        // Joins the voice channel of the target.
        // Adds a new client to the ConcurrentDictionary.
        public async Task JoinAudioAsync(IGuild guild, IVoiceChannel target)
        {
            // We can't connect to an empty guilds or targets.
            if (guild == null || target == null) return;

            // Delayed join if the client recently left a voice channel. This is to prevent reconnection issues
            if (m_DelayAction)
            {
                Log("The client is currently disconnecting from a voice channel. Please try again later.");
                return;
            }

            // Try to get the current audio client. If it's already there, we've already joined
            if (m_ConnectedChannels.TryGetValue(guild.Id, out var connectedAudioClient))
            {
                Log("The client is already connected to the current voice channel.");
                return;
            }

            // If the target guild id doesn't match the guild id we want, return
            // This will likely never happen, but the source message could refer to the incorrect server
            if (target.Guild.Id != guild.Id)
            {
                Log("Are you sure the current voice channel is correct?");
                return;
            }

            // Attempt to connect to this audio channel.
            var audioClient = await target.ConnectAsync();

            try
            {
                // Once connected, add it to the dictionary of connected channels.
                if (m_ConnectedChannels.TryAdd(guild.Id, audioClient))
                {
                    Log("The client is now connected to the current voice channel.");

                    // Start check to see if anyone is even in the channel.
                    if (m_LeaveWhenEmpty)
                        m_VoiceChannelTimer = new Timer(CheckVoiceChannelState, target, TimeSpan.FromMinutes(10), TimeSpan.FromMinutes(10));

                    return;
                }
            }
            catch
            {
                Log("The client failed to connect to the target voice channel.");
            }
            Log("Unable to join the current voice channel.");
        }

        // Leaves the current voice channel.
        public async Task LeaveAudioAsync(IGuild guild)
        {
            // We can't disconnect from an empty guild.
            if (guild == null) return;

            if (m_AudioPlayer.IsRunning()) StopAudio();
            while (m_AudioPlayer.IsRunning()) await Task.Delay(1000);

            // Attempt to remove from the current dictionary, and if removed, stop it.
            if (m_ConnectedChannels.TryRemove(guild.Id, out var audioClient))
            {
                Log("The client is now disconnected from the current voice channel.");
                await DelayAction(() => audioClient.StopAsync()); 
                return;
            }

            Log("Unable to disconnect from the current voice channel. Are you sure that it is currently connected?");
        }

        // Checks the current status of the voice channel and leaves when empty
        private async void CheckVoiceChannelState(object state)
        {
            if (!(state is IVoiceChannel channel)) return;

            int count = (await channel.GetUsersAsync().Flatten()).Count();
            if (count < 2)
            {
                await LeaveAudioAsync(channel.Guild);
                if (m_VoiceChannelTimer != null)
                {
                    m_VoiceChannelTimer.Dispose();
                    m_VoiceChannelTimer = null;
                }
            }
        }

        // Returns the number of async calls to ForcePlayAudioSync.
        public int GetNumPlaysCalled() { return m_NumPlaysCalled; }

        public async Task ForcePlayAudioAsync(IGuild guild, IMessageChannel channel, string path)
        {
            if (guild == null) return;

            AudioFile song = await GetAudioFileAsync(path);

            if (song == null) return;

            Interlocked.Increment(ref m_NumPlaysCalled);

            if (m_AudioPlayer.IsRunning()) StopAudio();
            while (m_AudioPlayer.IsRunning()) await Task.Delay(1000);

            if (m_ConnectedChannels.TryGetValue(guild.Id, out var audioClient))
            {
                Log($"Now Playing: {song.Title}", (int)E_LogOutput.Reply); 
                Log(song.Title, (int)E_LogOutput.Playing); 
                await m_AudioPlayer.Play(audioClient, song); 
                //Log(Strings.NotPlaying, (int)E_LogOutput.Playing);
            }
            else
            {
                Log("Unable to play in the proper channel. Make sure the audio client is connected.");
            }

            Interlocked.Decrement(ref m_NumPlaysCalled);
        }

        // This is for the autoplay function which waits after each playback and pulls from the playlist.
        public async Task AutoPlayAudioAsync(IGuild guild, IMessageChannel channel)
        {
            if (guild == null) return;

            if (m_AutoPlayRunning) return; 
            while (m_AutoPlayRunning = m_AutoPlay)
            {
                if (m_AudioPlayer.IsRunning()) await Task.Delay(1000);

                if (m_Playlist.IsEmpty || !m_AutoPlayRunning || !m_AutoPlay) break;

                if (m_ConnectedChannels.TryGetValue(guild.Id, out var audioClient))
                {
                    AudioFile song = PlaylistNext(); 
                    if (song != null)
                    {
                        Log($"Now Playing: {song.Title}", (int)E_LogOutput.Reply); 
                        Log(song.Title, (int)E_LogOutput.Playing); 
                        await m_AudioPlayer.Play(audioClient, song); 
                        //Log(Strings.NotPlaying, (int)E_LogOutput.Playing);
                    }
                    else
                        Log($"Cannot play the audio source specified : {song}");

                    if (m_Playlist.IsEmpty || !m_AutoPlayRunning || !m_AutoPlay) break;

                    continue;
                }

                Log("Unable to play in the proper channel. Make sure the audio client is connected.");
                break;
            }

            if (m_AutoStop) m_AutoPlay = false;
            m_AutoPlayRunning = false;
        }

        public bool IsAudioPlaying() { return m_AudioPlayer.IsPlaying(); }

        // AudioPlayback Functions. Pause, Resume, Stop, AdjustVolume
        public void PauseAudio() { m_AudioPlayer.Pause(); }
        public void ResumeAudio() { m_AudioPlayer.Resume(); }
        public void StopAudio() { m_AutoPlay = false; m_AutoPlayRunning = false; m_AudioPlayer.Stop(); }
        public void AdjustVolume(float volume = 100) { m_AudioPlayer.AdjustVolume(volume); } 

        public void SetAutoPlay(bool enable) { m_AutoPlay = enable; }

        public bool GetAutoPlay() { return m_AutoPlay; }

        // Checks if autoplay is true, but not started yet
        public async Task CheckAutoPlayAsync(IGuild guild, IMessageChannel channel)
        {
            if (m_AutoPlay && !m_AutoPlayRunning && !m_AudioPlayer.IsRunning()) 
                await AutoPlayAudioAsync(guild, channel);
        }

        // Prints the playlist information.
        public void PrintPlaylist()
        {
            int count = m_Playlist.Count;
            if (count == 0)
            {
                Log("There are currently no items in the playlist.", (int)E_LogOutput.Reply);
                return;
            }

            int countDigits = (int)(Math.Floor(Math.Log10(count) + 1));

            var emb = new EmbedBuilder();

            for (int i = 0; i < count; i++)
            {
                string zeros = "";
                int numDigits = (i == 0) ? 1 : (int)(Math.Floor(Math.Log10(i) + 1));
                while (numDigits < countDigits)
                {
                    zeros += "0";
                    ++numDigits;
                }

                // Filename
                AudioFile current = m_Playlist.ElementAt(i);
                emb.AddField(zeros + i, current);
            }

            DiscordReply("Playlist", emb);
        }

        // Adds a song to the playlist.
        public async Task PlaylistAddAsync(string path)
        {
            AudioFile audio = await GetAudioFileAsync(path);
            if (audio != null)
            {
                m_Playlist.Enqueue(audio); 
                Log($"Added to playlist : {audio.Title}", (int)E_LogOutput.Reply);

                if (m_AutoDownload)
                {
                    if (audio.IsNetwork) m_AudioDownloader.Push(audio);
                    await m_AudioDownloader.StartDownloadAsync(); 
                }
            }
        }

        // Gets the next song in the playlist queue.
        private AudioFile PlaylistNext()
        {
            if (m_Playlist.TryDequeue(out AudioFile nextSong))
                return nextSong;

            if (m_Playlist.Count <= 0) Log("We reached the end of the playlist.");
            else Log("The next song could not be opened.");
            return nextSong;
        }

        // Skips the current playlist song if autoplay is on.
        public void PlaylistSkip()
        {
            if (!m_AutoPlay)
            {
                Log("Autoplay service hasn't been started.");
                return;
            }
            if (!m_AudioPlayer.IsRunning())
            {
                Log("There's no audio currently playing.");
                return;
            }
            m_AudioPlayer.Stop();
        }

        // Extracts simple meta data from the path and fills a new AudioFile
        // information about the audio source. 
        private async Task<AudioFile> GetAudioFileAsync(string path)
        {
            try 
            {
                AudioFile song = await m_AudioDownloader.GetAudioFileInfo(path);
                if (song != null) 
                {
                    string filename = m_AudioDownloader.GetItem(song.Title);
                    if (filename != null) 
                    {
                        song.FileName = filename;
                        song.IsNetwork = false; 
                        song.IsDownloaded = true;
                    }
                }
                return song;
            }
            catch
            {
                return null;
            }
        }

        //Finds all the local songs and prints out a set at a time by page number
        public void PrintLocalSongs(int page)
        {
            string[] items = m_AudioDownloader.GetAllItems();
            int itemCount = items.Length;
            if (itemCount == 0)
            {
                Log("No local files found.", (int)E_LogOutput.Reply);
                return;
            }

            int countDigits = (int)(Math.Floor(Math.Log10(items.Length) + 1));

            int pageSize = 20;
            int pages = (itemCount / pageSize) + 1;
            if (page < 1 || page > pages)
            {
                Log($"There are {pages} pages. Select page 1 to {pages}.", (int)E_LogOutput.Reply);
                return;
            }

            //Start printing
            for (int p = page - 1; p < page; p++)
            {
                var emb = new EmbedBuilder();

                for (int i = 0; i < pageSize; i++)
                {
                    int index = (p * pageSize) + i;
                    if (index >= itemCount) break;

                    string zeros = "";
                    int numDigits = (index == 0) ? 1 : (int)(Math.Floor(Math.Log10(index) + 1));
                    while (numDigits < countDigits)
                    {
                        zeros += "0";
                        ++numDigits;
                    }

                    // Filename.
                    string file = items[index].Split(Path.DirectorySeparatorChar).Last();
                    emb.AddField(zeros + index, file);
                }

                DiscordReply($"Page {p + 1}", emb);
            }
        }

        // Returns the name with the specified song by index.
        public string GetLocalSong(int index) { return m_AudioDownloader.GetItem(index); }

        // Adds a song to the download queue.
        public async Task DownloadSongAsync(string path)
        {
            AudioFile audio = await GetAudioFileAsync(path);
            if (audio != null)
            {
                Log($"Added to the download queue : {audio.Title}", (int)E_LogOutput.Reply);

                // If the downloader is set to true, we start the autodownload helper.
                if (audio.IsNetwork) m_AudioDownloader.Push(audio); 
                await m_AudioDownloader.StartDownloadAsync(); 
            }
        }

        // Removes any duplicates in our download folder.
        public async Task RemoveDuplicateSongsAsync()
        {
            m_AudioDownloader.RemoveDuplicateItems();
            await Task.Delay(0);
        }

    }
}
