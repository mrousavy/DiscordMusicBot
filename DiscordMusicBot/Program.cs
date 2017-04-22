using System;
using System.Diagnostics;
using System.IO;
using Newtonsoft.Json;

namespace DiscordMusicBot {
    internal class Program {
        private static void Main(string[] args) {
            Console.WriteLine("Starting...");
            Console.WriteLine("(Press Ctrl + C to exit Bot)");

            try {
                string json = File.ReadAllText("config.json");
                Config cfg = JsonConvert.DeserializeObject<Config>(json);
                
                if (cfg == new Config())
                    throw new Exception("Config is default Config!");
            } catch (Exception e) {
                Console.WriteLine("Your config.json has incorrect formatting, or is not readable!");
                Console.WriteLine(e.Message);

                try {
                    //Open up for editing
                    Process.Start("config.json");
                } catch {
                    // file not found, process not started, etc.
                }

                return;
            }

            MusicBot bot = new MusicBot();
            bool loop = true;

            Console.CancelKeyPress += delegate {
                bot.Dispose();
                loop = false;
            };

            while (loop)
                Console.ReadKey();
        }
    }
}