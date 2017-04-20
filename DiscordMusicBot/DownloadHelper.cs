using MediaToolkit;
using MediaToolkit.Model;
using System;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using VideoLibrary;

namespace DiscordMusicBot {
    internal class DownloadHelper {
        private static readonly string DownloadPath = Path.GetTempPath();


        public static async Task<Tuple<string, string>> Download(string url) {
            if (url.ToLower().Contains("youtube.com")) {
                return await DownloadFromYouTube(url);
            } else if (url.ToLower().Contains("soundcloud.com")) {
                return await DownloadFromSoundcloud(url);
            } else {
                throw new Exception("Video URL not supported!");
            }
        }

        //Download the Video from YouTube url and extract it
        private static async Task<Tuple<string, string>> DownloadFromYouTube(string url) {
            //Download Video
            YouTube youtube = YouTube.Default;
            YouTubeVideo video = (await youtube.GetAllVideosAsync(url)).OrderByDescending(vid => vid.AudioBitrate).First();

            Console.WriteLine($"Downloading \"{video.Title}\" from YouTube...");

            string file;
            int count = 0;
            do {
                file = Path.Combine(DownloadPath, "tempvideo" + ++count + video.FileExtension);
            } while (File.Exists(file));

            File.WriteAllBytes(file, video.GetBytes());

            //Convert vid to mp3
            MediaFile inputFile = new MediaFile { Filename = file };
            MediaFile outputFile = new MediaFile { Filename = $"{file}.mp3" };

            using (Engine engine = new Engine()) {
                engine.GetMetadata(inputFile);
                engine.Convert(inputFile, outputFile);
            }

            Console.WriteLine("Done!");

            File.Delete(file);

            return new Tuple<string, string>($"{file}.mp3", video.Title);
        }

        //Download the Video from Soundcloud url and extract it
        private static async Task<Tuple<string, string>> DownloadFromSoundcloud(string url) {
            Console.WriteLine("Downloading Audio from Soundcloud...");

            //TODO

            return new Tuple<string, string>("", "");
        }
    }
}