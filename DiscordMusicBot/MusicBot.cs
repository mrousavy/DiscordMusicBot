using System;
using System.Collections.Generic;
using System.Text;

namespace DiscordMusicBot {
    internal class MusicBot {
        public MusicBot() { }


        public void Help() {
            Console.WriteLine(
                "!add [url] ... Adds a single Song to Music-queue\n" +
                "!addPlaylist[playlist - url]...Adds whole playlist to Music - queue\n" +
                "!pause...Pause the queue and current Song\n" +
                "!play...Resume the queue and current Song\n" +
                "!queue...Prints all queued Songs & their User\n" +
                "!clear...Clear queue and current Song\n" +
                "!setRole[minRole]...Minimum Client Role to request Songs\n" +
                "!setTimeout[timeoutInMilliseconds]...Timeout between being able to request songs\n" +
                "!help...Prints available Commands and usage");
        }
    }
}