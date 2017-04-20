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

        //Download the Video from YouTube url and extract it
        public static async Task<string> DownloadFromYouTube(string url) {
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
    }
}