using System;
using System.Diagnostics;
using System.IO;
using System.Runtime.InteropServices;
using System.Threading;
using System.Threading.Tasks;
using Newtonsoft.Json;

namespace DiscordMusicBot {
    internal class Program {
        private static MusicBot _bot;

        private static void Main(string[] args) {
            Console.CursorVisible = false;
            DisableMouse();
            Console.Title = "Music Bot (Loading...)";

            Console.WriteLine("(Press Ctrl + C or close this Window to exit Bot)");

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
                MusicBot.Print("Your config.json has incorrect formatting, or is not readable!", ConsoleColor.Red);
                MusicBot.Print(e.Message, ConsoleColor.Red);

                try {
                    //Open up for editing
                    Process.Start("config.json");
                } catch {
                    // file not found, process not started, etc.
                }

                Console.ReadKey();
                return;
            }

            _handler += Handler;
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

        private static void DisableMouse() {
            const uint ENABLE_QUICK_EDIT = 0x0040;
            const uint ENABLE_MOUSE_INPUT = 0x0010;

            IntPtr consoleHandle = GetConsoleWindow();

            if (!GetConsoleMode(consoleHandle, out uint consoleMode)) {
                // error
                return;
            }

            // Clear quick edit & Mouse input flags
            consoleMode &= ~ENABLE_QUICK_EDIT;
            consoleMode &= ~ENABLE_MOUSE_INPUT;

            if (!SetConsoleMode(consoleHandle, consoleMode)) {
                // error
            }
        }



        [DllImport("Kernel32")]
        private static extern bool SetConsoleCtrlHandler(EventHandler handler, bool add);

        [DllImport("kernel32.dll", SetLastError = true)]
        private static extern bool GetConsoleMode(IntPtr hConsoleHandle, out uint lpMode);

        [DllImport("kernel32.dll", SetLastError = true)]
        private static extern bool SetConsoleMode(IntPtr hConsoleHandle, uint dwMode);

        [DllImport("kernel32.dll", SetLastError = true)]
        private static extern IntPtr GetConsoleWindow();

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

            Console.ReadKey();
            return false;
        }
    }
}