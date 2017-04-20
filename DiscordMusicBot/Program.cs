using System;

namespace DiscordMusicBot {
    internal class Program {
        private static void Main(string[] args) {
            Console.WriteLine("Starting...");
            Console.WriteLine("(Press Ctrl + C to exit Bot)");

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