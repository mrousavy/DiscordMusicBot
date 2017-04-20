using System;

namespace DiscordMusicBot {
    internal class Program {
        private static void Main(string[] args) {
            new MusicBot();
            Console.WriteLine("Starting...");

            while (true)
                Console.ReadKey();
        }
    }
}