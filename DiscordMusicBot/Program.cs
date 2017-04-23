using Newtonsoft.Json;
using System;
using System.Diagnostics;
using System.IO;
using System.Runtime.InteropServices;
using System.Threading;
using System.Threading.Tasks;

namespace DiscordMusicBot {
    internal class Program {
        private static MusicBot _bot;


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

            _handler += new EventHandler(Handler);
            SetConsoleCtrlHandler(_handler, true);

            Do();

            //Thread Block
            Thread.Sleep(-1);
        }


        private static async void Do() {
            _bot = new MusicBot();

            //Async Thread Block
            await Task.Delay(-1);
        }



        [DllImport("Kernel32")]
        private static extern bool SetConsoleCtrlHandler(EventHandler handler, bool add);

        private delegate bool EventHandler(CtrlType sig);
        private static EventHandler _handler;

        private enum CtrlType {
            CTRL_C_EVENT = 0,
            CTRL_BREAK_EVENT = 1,
            CTRL_CLOSE_EVENT = 2,
            CTRL_LOGOFF_EVENT = 5,
            CTRL_SHUTDOWN_EVENT = 6
        }

        private static bool Handler(CtrlType sig) {
            _bot.Dispose();

            return false;
        }
    }
}