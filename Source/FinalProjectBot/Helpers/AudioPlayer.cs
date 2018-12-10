using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Text;
using System.Threading.Tasks;
using Discord.Audio;

namespace FinalProjectBot.Helpers
{
    class AudioPlayer
    {
        //Important variables
        private bool m_IsRunning = false;           // Boolean to wrap the audio playback method
        private Process m_Process = null;           // Process that runs when playing
        private Stream m_Stream = null;             // Stream output when playing
        private bool m_IsPlaying = false;           // Flag to change to play or pause the audio
        private float m_Volume = 1.0f;              // Volume value that's checked during playback. Reference: PlayAudioAsync
        private int m_BLOCK_SIZE = 3840;            // Custom block size for playback, in bytes

        // Creates a local stream using the file path specified and ffmpeg to stream it directly
        // The format Discord takes is 16-bit 48000Hz PCM
        private Process CreateLocalStream(string path)
        {
            try
            {
                return Process.Start(new ProcessStartInfo
                {
                    FileName = "ffmpeg.exe",
                    Arguments = $"-hide_banner -loglevel panic -i \"{path}\" -ac 2 -f s16le -ar 48000 pipe:1",
                    UseShellExecute = false,
                    RedirectStandardOutput = true,
                    CreateNoWindow = true
                });
            }
            catch
            {
                Console.WriteLine($"Error while opening local stream : {path}");
                return null;
            }
        }

        // Creates a network stream using youtube-dl.exe, then piping it to ffmpeg to stream it directly.
        // The format Discord takes is 16-bit 48000Hz PCM
        private Process CreateNetworkStream(string path)
        {
            try
            {
                return Process.Start(new ProcessStartInfo
                {
                    FileName = "cmd.exe",
                    Arguments = $"/C youtube-dl.exe -o - {path} | ffmpeg -i pipe:0 -ac 2 -f s16le -ar 48000 pipe:1",
                    UseShellExecute = false,
                    RedirectStandardOutput = true,
                    CreateNoWindow = true
                });
            }
            catch
            {
                Console.WriteLine($"Error while opening network stream : {path}");
                return null;
            }
        }

        // Async function that handles the playback of the audio. This function is technically blocking in it's for loop
        // It can be broken by cancelling m_Process or when it reads to the end of the file
        // At the start, m_Process, m_Stream, amd m_IsPlaying is flushed
        // While it is playing, these will hold values of the current playback audio. It will depend on m_Volume for the volume
        // In the end, the three are flushed again
        private async Task AudioPlaybackAsync(IAudioClient client, AudioFile song)
        {
            // Set running to true
            m_IsRunning = true;

            // Starts a new process and create an output stream. Decide between network or local
            m_Process = (bool)song.IsNetwork ? CreateNetworkStream(song.FileName) : CreateLocalStream(song.FileName);
            m_Stream = client.CreatePCMStream(AudioApplication.Music); 
            m_IsPlaying = true;

            await Task.Delay(5000); //Waits for audio to load slightly so avoid buffering

            //This section details the breaks for when the audio stops.
            while (true)
            {
                if (m_Process == null || m_Process.HasExited) break;

                if (m_Stream == null) break;

                if (!m_IsPlaying) continue;

                int blockSize = m_BLOCK_SIZE; 
                byte[] buffer = new byte[blockSize];
                int byteCount;
                byteCount = await m_Process.StandardOutput.BaseStream.ReadAsync(buffer, 0, blockSize);

                if (byteCount <= 0) break;

                try
                {
                    await m_Stream.WriteAsync(ScaleVolumeSafeAllocateBuffers(buffer, m_Volume), 0, byteCount);
                }
                catch (Exception exception)
                {
                    Console.WriteLine(exception);
                    break;
                }
            }

            if (m_Process != null && !m_Process.HasExited) m_Process.Kill();

            if (m_Stream != null) m_Stream.FlushAsync().Wait();

            // Reset values
            m_Process = null;
            m_Stream = null;
            m_IsPlaying = false;
            m_IsRunning = false;
        }

        private byte[] ScaleVolumeSafeAllocateBuffers(byte[] audioSamples, float volume)
        {
            if (audioSamples == null) return null;
            if (audioSamples.Length % 2 != 0) return null;
            if (volume < 0.0f || volume > 1.0f) return null;

            // Adjust the output for the volume.
            var output = new byte[audioSamples.Length];
            try
            {
                if (Math.Abs(volume - 1f) < 0.0001f)
                {
                    Buffer.BlockCopy(audioSamples, 0, output, 0, audioSamples.Length);
                    return output;
                }

                int volumeFixed = (int)Math.Round(volume * 65536d);
                for (var i = 0; i < output.Length; i += 2)
                {
                    int sample = (short)((audioSamples[i + 1] << 8) | audioSamples[i]);
                    int processed = (sample * volumeFixed) >> 16;

                    output[i] = (byte)processed;
                    output[i + 1] = (byte)(processed >> 8);
                }
                return output;
            }
            catch (Exception exception)
            {
                Console.WriteLine(exception);
                return null;
            }
        }

        //Adjusts the current volume to the value passed
        public void AdjustVolume(float volume)
        {
            if (volume < 0.0f)
                volume = 0.0f;
            else if (volume > 1.0f)
                volume = 1.0f;

            m_Volume = volume; // Update the volume
        }

        public bool IsRunning() { return m_IsRunning; }

        //Returns if the process is in the middle of AudioPlaybackAsync
        public bool IsPlaying() { return ((m_Process != null) && m_IsPlaying); }

        // Starts the audioplayer playback for the specific song.
        // If something else is already playing, we stop it before putting this into the loop.
        public async Task Play(IAudioClient client, AudioFile song)
        {
            // Stop the current song. We wait until it's done to play the next song.
            if (m_IsRunning) Stop();
            while (m_IsRunning) await Task.Delay(1000);

            // Start playback.
            await AudioPlaybackAsync(client, song);
        }

        //Pauses the stream 
        public void Pause() { m_IsPlaying = false; }
    
        //Resumes the stream
        public void Resume() { m_IsPlaying = true; }

        // Stopsthe stream
        public void Stop() { if (m_Process != null) m_Process.Kill(); } // This basically stops the current loop by exiting the process

    }
}
