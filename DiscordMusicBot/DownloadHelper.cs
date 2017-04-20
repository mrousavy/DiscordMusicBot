using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using MediaToolkit;
using MediaToolkit.Model;
using VideoLibrary;

namespace DiscordMusicBot {
    internal class DownloadHelper {
        private static readonly string DownloadPath = Path.GetTempPath();


        public static async Task<string> Download(string url) {
            if (url.ToLower().Contains("youtube.com")) {
                return await DownloadFromYouTube(url);
            } else if (url.ToLower().Contains("soundcloud.com")) {
                return await DownloadFromSoundcloud(url);
            } else {
                throw new Exception("Video URL not supported!");
            }
        }

        //Download the Video from YouTube url and extract it
        private static async Task<string> DownloadFromYouTube(string url) {
            Console.WriteLine("Downloading Audio from YouTube...");

            //Download Video
            YouTube youtube = YouTube.Default;
            Video video = (await youtube.GetAllVideosAsync(url)).OrderByDescending(vid => vid.AudioBitrate).First();

            string file;
            int count = 0;
            do {
                file = Path.Combine(DownloadPath, "tempvideo" + ++count + video.FileExtension);
            } while (File.Exists(file));

            File.WriteAllBytes(file, video.GetBytes());

            //Convert vid to mp3
            MediaFile inputFile = new MediaFile {Filename = file};
            MediaFile outputFile = new MediaFile {Filename = $"{file}.mp3"};

            using (Engine engine = new Engine()) {
                engine.GetMetadata(inputFile);
                engine.Convert(inputFile, outputFile);
            }

            File.Delete(file);

            return $"{file}.mp3";
        }

        //Download the Video from Soundcloud url and extract it
        private static async Task<string> DownloadFromSoundcloud(string url) {
            Console.WriteLine("Downloading Audio from Soundcloud...");

            //TODO

            return $".mp3";
        }
    }
}