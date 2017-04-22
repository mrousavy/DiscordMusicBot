using Newtonsoft.Json;
using System;
using System.Diagnostics;
using System.IO;

namespace DiscordMusicBot {
    internal class Program {
        private static void Main(string[] args) {
            Console.WriteLine("Starting...");
            Console.WriteLine("(Press Ctrl + C to exit Bot)");

            try {
                #region JSON.NET
                string json = File.ReadAllText("config.json");
                Config cfg = JsonConvert.DeserializeObject<Config>(json);

                if (cfg == new Config())
                    throw new Exception("Please insert values into Config.json!");
                #endregion


                #region TXT Reading
                //string[] config = File.ReadAllLines("config.txt");
                //Config cfg = new Config() {
                //    BotName = config[0].Split(':')[1],
                //    ChannelName = config[1].Split(':')[1],
                //    ClientId = config[2].Split(':')[1],
                //    ClientSecret = config[3].Split(':')[1],
                //    ServerName = config[4].Split(':')[1],
                //    Token = config[5].Split(':')[1],
                //};
                #endregion
            } catch (Exception e) {
                Console.WriteLine("Your config.json has incorrect formatting, or is not readable!");
                Console.WriteLine(e.Message);

                try {
                    //Open up for editing
                    Process.Start("config.json");
                } catch {
                    // file not found, process not started, etc.
                }

                Console.ReadKey();
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