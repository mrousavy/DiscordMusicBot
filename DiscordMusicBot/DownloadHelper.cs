//using MediaToolkit;
//using MediaToolkit.Model;
using System;
using System.Diagnostics;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
//using VideoLibrary;

namespace DiscordMusicBot {
    internal class DownloadHelper {
        private static readonly string DownloadPath = Path.Combine(Directory.GetCurrentDirectory(), "Temp") /*Path.GetTempPath() ??*/ ;

        /// <summary>
        /// Download Song or Video
        /// </summary>
        /// <param name="url">The URL to the Song or Video</param>
        /// <returns>The File Location to the downloaded Song or Video</returns>
        public static async Task<string> Download(string url) {
            if (url.ToLower().Contains("youtube.com")) {
                return await DownloadFromYouTube(url);
            } else {
                throw new Exception("Video URL not supported!");
            }
        }

        /// <summary>
        /// Download Playlist
        /// </summary>
        /// <param name="url">The URL to the Playlist</param>
        /// <returns>The File Location to the downloaded Song</returns>
        public static async Task<string> DownloadPlaylist(string url) {
            if (url.ToLower().Contains("youtube.com")) {
                return await DownloadPlaylistFromYouTube(url);
            } else {
                throw new Exception("Video URL not supported!");
            }
        }

        /// <summary>
        /// Gets Title of Video or Song
        /// </summary>
        /// <param name="url">URL to Video or Song</param>
        /// <returns>The Title of the Video or Song</returns>
        public static async Task<Tuple<string, string>> GetInfo(string url) {
            if (url.ToLower().Contains("youtube.com")) {
                return await GetInfoFromYouTube(url);
            } else {
                throw new Exception("Video URL not supported!");
            }
        }


        /// <summary>
        /// Get Video Title from YouTube URL
        /// </summary>
        /// <param name="url">URL to the YouTube Video</param>
        /// <returns>The YouTube Video Title</returns>
        private static async Task<Tuple<string, string>> GetInfoFromYouTube(string url) {
            TaskCompletionSource<Tuple<string, string>> tcs = new TaskCompletionSource<Tuple<string, string>>();

            new Thread(() => {
                string title;
                string duration;

                //youtube-dl.exe
                Process youtubedl;

                //Get Video Title
                ProcessStartInfo youtubedlGetTitle = new ProcessStartInfo() {
                    FileName = "youtube-dl",
                    Arguments = $"-s -e --get-duration {url}",
                    CreateNoWindow = true,
                    RedirectStandardOutput = true,
                    /*UseShellExecute = false*/     //Linux?
                };
                youtubedl = Process.Start(youtubedlGetTitle);
                youtubedl.WaitForExit();
                //Read Title
                string[] lines = youtubedl.StandardOutput.ReadToEnd().Split('\n');
                title = lines[0];
                duration = lines[1];

                tcs.SetResult(new Tuple<string, string>(title, duration));
            }).Start();

            Tuple<string, string> result = await tcs.Task;
            if (result == null)
                throw new Exception("youtube-dl.exe failed to receive title!");

            return result;
        }

        /// <summary>
        /// Download the Video from YouTube url and extract it
        /// </summary>
        /// <param name="url">URL to the YouTube Video</param>
        /// <returns>The File Path to the downloaded mp3</returns>
        private static async Task<string> DownloadFromYouTube(string url) {
            TaskCompletionSource<string> tcs = new TaskCompletionSource<string>();

            new Thread(() => {
                string file;
                int count = 0;
                do {
                    file = Path.Combine(DownloadPath, "tempvideo" + ++count + ".%(ext)s");
                } while (File.Exists(file));

                //youtube-dl.exe
                Process youtubedl;

                //Download Video
                ProcessStartInfo youtubedlDownload = new ProcessStartInfo() {
                    FileName = "youtube-dl",
                    Arguments = $"-x --audio-format mp3 -o \"{file}\" {url}",
                    CreateNoWindow = true,
                    RedirectStandardOutput = true,
                    /*UseShellExecute = false*/     //Linux?
                };
                youtubedl = Process.Start(youtubedlDownload);
                //Wait until download is finished
                youtubedl.WaitForExit();


                file = file.Replace(".%(ext)s", ".mp3");

                if (File.Exists(file)) {
                    //Return MP3 Path & Video Title
                    tcs.SetResult(file);
                } else {
                    //Error downloading
                    tcs.SetResult(null);
                    MusicBot.Print($"Could not download Song, youtube-dl responded with: {youtubedl.StandardOutput.ReadToEnd()}", ConsoleColor.Red);
                }
            }).Start();

            string result = await tcs.Task;
            if (result == null)
                throw new Exception("youtube-dl.exe failed to download!");

            //Remove \n at end of Line
            result = result.Replace("\n", "").Replace(Environment.NewLine, "");

            return result;
        }

        /// <summary>
        /// Download the whole Playlist from YouTube url and extract it
        /// </summary>
        /// <param name="url">URL to the YouTube Playlist</param>
        /// <returns>The File Path to the downloaded mp3</returns>
        private static async Task<string> DownloadPlaylistFromYouTube(string url) {
            TaskCompletionSource<string> tcs = new TaskCompletionSource<string>();

            new Thread(() => {
                string file;
                int count = 0;
                do {
                    file = Path.Combine(DownloadPath, "tempvideo" + ++count + ".mp3");
                } while (File.Exists(file));

                //youtube-dl.exe
                Process youtubedl;

                //Download Video
                ProcessStartInfo youtubedlDownload = new ProcessStartInfo() {
                    FileName = "youtube-dl",
                    Arguments = $"--extract-audio --audio-format mp3 -o \"{file}\" {url}",
                    CreateNoWindow = true,
                    RedirectStandardOutput = true,
                    /*UseShellExecute = false*/     //Linux?
                };
                youtubedl = Process.Start(youtubedlDownload);
                //Wait until download is finished
                youtubedl.WaitForExit();

                if (File.Exists(file)) {
                    //Return MP3 Path & Video Title
                    tcs.SetResult(file);
                } else {
                    //Error downloading
                    tcs.SetResult(null);
                    MusicBot.Print($"Could not download Song, youtube-dl responded with: {youtubedl.StandardOutput.ReadToEnd()}", ConsoleColor.Red);
                }
            }).Start();

            string result = await tcs.Task;
            if (result == null)
                throw new Exception("youtube-dl.exe failed to download!");

            //Remove \n at end of Line
            result = result.Replace("\n", "").Replace(Environment.NewLine, "");

            return result;
        }
    }
}