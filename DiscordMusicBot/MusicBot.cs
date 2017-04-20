using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Discord;
using Discord.Audio;
using MediaToolkit;
using MediaToolkit.Model;
using VideoLibrary;

namespace DiscordMusicBot {
    internal class MusicBot : IDisposable {
        private DiscordClient _client;
        private Channel _channel;
        private List<string> _permittedUsers;

        //Song Queue, Path to files
        private Queue<string> _queue;

        private IAudioClient _audio;
        private bool _pause;

        private const int ResolutionToDownload = 360;
        private const string ImABot = " *I'm a Bot, beep boop blop*";

        //AUDIO FORMAT: 16-bit 48000Hz PCM

        public MusicBot() { Initialize(); }

        //init vars
        public async void Initialize() {
            //Init Config and Queue
            ReadConfig();
            _queue = new Queue<string>();

            //Init & Connect Client
            _client = new DiscordClient();
            _client.JoinedServer += Joined;
            _client.Ready += Ready;
            _client.MessageReceived += MessageReceived;
            _client.ChannelUpdated += ChannelSwitched;
            _client.ServerUpdated += ServerSwitched;
            _client.ServerAvailable += ServerAvailable;
            await _client.Connect(Information.Token, TokenType.Bot);

            //Setup Audio
            _client.UsingAudio(x => { x.Mode = AudioMode.Outgoing; });

            //"Playing Nothing :/"
            _client.SetGame("Nothing :/");

            Console.Title = $"Music Bot ({_client.State})";
        }

        //Event on Servers available
        private async void ServerAvailable(object sender, ServerEventArgs e) {
            //Print added Servers
            Console.WriteLine("\nAdded Servers:");
            Console.Write("    ");
            foreach (Server server in _client.Servers)
                Console.Write(server.Name + ", ");
            Console.WriteLine("");

            //Join First Audio Channel
            try {
                Server server = _client.FindServers(Information.ServerName)
                    .FirstOrDefault();
                if (server == null)
                    throw new Exception("No Server found!");

                List<Channel> channels = new List<Channel>(server.VoiceChannels);
                Channel channel = channels.FirstOrDefault();
                if (channel == null)
                    throw new Exception("No Voice Channel found!");

                AudioService service = _client.GetService<AudioService>();
                _audio = await service.Join(channel);

                Console.WriteLine($"Joined Channel \"{channel.Name}\"");
            } catch (Exception ex) {
                Console.WriteLine("Could not join Voice Channel! (" + ex.Message + ")");
            }
        }

        private async void ChannelSwitched(object sender, ChannelUpdatedEventArgs e) {
            Pause();
            Console.WriteLine($"I switched channel from {e.Before} to {e.After}!");
            _channel = e.After;
            await _channel.SendMessage("Waddup" + ImABot);
            _audio = await e.After.JoinAudio();
            Play();
        }

        private static void ServerSwitched(object sender, ServerUpdatedEventArgs e) {
            Console.WriteLine($"I switched server from {e.Before} to {e.After}!");
        }

        //Read Config from File
        public void ReadConfig() {
            if (!File.Exists("users.txt"))
                File.Create("users.txt").Dispose();

            _permittedUsers = new List<string>(File.ReadAllLines("users.txt"));

            Console.WriteLine("\nPermitted Users:");
            Console.Write("    ");
            foreach (string user in _permittedUsers) {
                Console.Write(user + ", ");
            }
            Console.WriteLine("");
        }

        //Joined Console Write
        private static void Joined(object sender, ServerEventArgs e) { Console.WriteLine("Joined Server!"); }

        private static void Ready(object sender, EventArgs e) { Console.WriteLine("Ready!"); }

        //On Private Message Received
        private async void MessageReceived(object sender, MessageEventArgs e) {
            if (e.User.Name == _client.CurrentUser.Name)
                return;

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
                    await e.User.SendMessage("Sorry, Song Queue is empty!" + ImABot);
                } else {
                    string queue = _queue.Aggregate("Song Queue:\n", (current, url) => current + ("    " + url + "\n"));
                    await e.User.SendMessage(queue);
                }
                return;
            }

            #endregion

            #region Only with Roles

            if (!_permittedUsers.Contains(e.User.ToString())) {
                await e.User.SendMessage("Sorry, but you're not allowed to do that!" + ImABot);
                return;
            }

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
                        string path = await DownloadHelper.DownloadFromYouTube(parameter);
                        _queue.Enqueue(path);
                        Play();
                        await e.User.SendMessage("Song added, Thanks!" + ImABot);
                    } else {
                        await e.User.SendMessage("Sorry, but that was no valid URL!" + ImABot);
                    }
                } else {
                    await e.User.SendMessage("I got confused, I don't know that command!\n\r" + GetHelp());
                }
            } else if (msg.StartsWith("!addPlaylist")) {
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
            } else if (msg.StartsWith("!pause")) {
                Pause();
            } else if (msg.StartsWith("!play")) {
                Play();
            } else if (msg.StartsWith("!clear")) {
                _queue.Clear();
                await e.User.SendMessage("Playlist cleared!" + ImABot);
            } else if (msg.StartsWith("!come")) {
                await e.User.SendMessage("Sorry, I can't do that yet! :(");
            } else if (msg.StartsWith("!update")) {
                ReadConfig();
                await e.User.SendMessage("Updated Permitted Users List!");
            }

            #endregion
        }

        //Start/Resume the playing media
        public void Play() {
            _pause = false;

            new Thread(() => {
                try {
                    while (!_pause) {
                        byte[] bytes = File.ReadAllBytes(_queue.Dequeue());
                        _audio.Send(bytes, 0, bytes.Length);
                        _audio.Wait();
                    }
                } catch {
                    //audio can't be played
                }
            }).Start();
        }

        public void Pause() {
            _audio.Clear();
            _pause = true;
        }

        //Add Song to queue
        public void AddToQueue(string url) { _queue.Enqueue(url); }

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
                "    !help...Prints available Commands and usage\n" +
                "    !come...Let Bot join your Channel\n" +
                "    !update...Updates the Permitted Clients List from clients.txt";
        }

        public void Dispose() {
            _client?.Disconnect();
            _client?.Dispose();
        }
    }
}