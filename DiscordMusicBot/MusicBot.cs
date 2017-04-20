using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Discord;
using Discord.Net.WebSockets;

namespace DiscordMusicBot {
    internal class MusicBot : IDisposable {
        private DiscordClient _client;
        private List<string> _permittedUsers;
        private Queue<string> _queue;
        private readonly string _imABot = " *I'm a Bot, beep boop blop*";

        public MusicBot() { Initialize(); }

        //init vars
        public async void Initialize() {
            ReadConfig();
            _queue = new Queue<string>();
            _client = new DiscordClient();
            _client.JoinedServer += Joined;
            _client.Ready += Ready;
            _client.MessageReceived += MessageReceived;
            await _client.Connect(Information.Token, TokenType.Bot);
        }

        //Read Config from File
        public void ReadConfig() {
            if (!File.Exists("users.txt"))
                File.Create("users.txt").Dispose();

            _permittedUsers = new List<string>(File.ReadAllLines("users.txt"));
        }

        //Joined Console Write
        private void Joined(object sender, ServerEventArgs e) { Console.WriteLine("Joined Server!"); }

        private void Ready(object sender, EventArgs e) { Console.WriteLine("Ready!"); }

        //On Private Message Received
        private async void MessageReceived(object sender, MessageEventArgs e) {
            Console.WriteLine($"User \"{e.User}\" wrote: \"{e.Message.Text}\"");

            string msg = e.Message.Text;

            if (!msg.StartsWith("!"))
                //Not a command
                return;

            #region For All Users

            if (msg.StartsWith("!help")) {
                await e.User.SendMessage(GetHelp());
                return;
            }
            if (msg.StartsWith("!queue")) {
                if (_queue.Count == 0) {
                    await e.User.SendMessage("Sorry, Song Queue is empty!" + _imABot);
                } else {
                    string queue = _queue.Aggregate("Song Queue:\n", (current, url) => current + ("    " + url + "\n"));
                    await e.User.SendMessage(queue);
                }
                return;
            }

            #endregion

            #region Only with Roles

            string[] split = msg.Split(' ');
            string command = split[0];
            string parameter = null;
            if (split.Length > 1)
                parameter = split[1];

            if (msg.StartsWith("!add")) {
                if (parameter != null) {
                    //Test for valid URL
                    bool result = Uri.TryCreate(parameter, UriKind.Absolute, out Uri uriResult)
                                  && (uriResult.Scheme == Uri.UriSchemeHttp || uriResult.Scheme == Uri.UriSchemeHttps);

                    //Answer
                    if (result) {
                        _queue.Enqueue(parameter);
                        await e.User.SendMessage("Song added, Thanks!" + _imABot);
                    } else {
                        await e.User.SendMessage("Sorry, but that was no valid URL!" + _imABot);
                    }
                } else {
                    await e.User.SendMessage("Invalid Command!\n\r" + GetHelp());
                }
                return;
            }
            if (msg.StartsWith("!addPlaylist")) {
                //TODO
                if (parameter != null) {
                    await e.User.SendMessage("Sorry, I can't add Playlists as for now! :(");

                    /*
                    //Test for valid URL
                    bool result = Uri.TryCreate(parameter, UriKind.Absolute, out Uri uriResult)
                                  && (uriResult.Scheme == Uri.UriSchemeHttp || uriResult.Scheme == Uri.UriSchemeHttps);

                    //Answer
                    if (result) {
                        _queue.Enqueue(parameter);
                        await e.User.SendMessage("Playlist added, Thanks!" + _imABot);
                    } else {
                        await e.User.SendMessage("Sorry, but that was no valid URL!" + _imABot);
                    }
                    */
                } else {
                    await e.User.SendMessage("I got confused, I don't know that command!\n\r" + GetHelp());
                }
                return;
            }
            if (msg.StartsWith("!pause")) {
                return;
            }
            if (msg.StartsWith("!play")) {
                return;
            }
            if (msg.StartsWith("!clear")) {
                _queue.Clear();
                await e.User.SendMessage("Playlist cleared!" + _imABot);
                return;
            }
            if (msg.StartsWith("!setTimeout")) {
                return;
            }

            #endregion
        }

        public async void Play() { }

        //Add Song to queue
        public void AddToQueue(string url) { }

        public string GetHelp() {
            return
                " Available Commands: \n" +
                "    !add [url] ... Adds a single Song to Music-queue\n" +
                "    !addPlaylist [playlist - url]...Adds whole playlist to Music - queue\n" +
                "    !pause...Pause the queue and current Song\n" +
                "    !play...Resume the queue and current Song\n" +
                "    !queue...Prints all queued Songs & their User\n" +
                "    !clear...Clear queue and current Song\n" +
                "    !setTimeout [timeoutInMilliseconds]...Timeout between being able to request songs\n" +
                "    !help...Prints available Commands and usage";
        }

        public void Dispose() {
            _client?.Disconnect();
            _client?.Dispose();
        }
    }
}