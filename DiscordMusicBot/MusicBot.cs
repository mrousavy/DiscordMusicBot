using Discord;
using Discord.Audio;
using NAudio.Wave;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace DiscordMusicBot {
    internal class MusicBot : IDisposable {
        private DiscordClient _client;
        private Channel _textChannel;
        private List<string> _permittedUsers;
        private TaskCompletionSource<bool> _tcs;
        private bool Skip {
            get {
                bool ret = _internalSkip;
                _internalSkip = false;
                return ret;
            }
            set => _internalSkip = value;
        }
        private bool _internalSkip;

        //Song Queue, Path to files
        private Queue<Tuple<string, string>> _queue;

        private IAudioClient _audio;

        private bool Pause {
            get => _internalPause;
            set {
                new Thread(() => _tcs.TrySetResult(value)).Start();
                _internalPause = value;
            }
        }
        private bool _internalPause;
        private const string ImABot = " *I'm a Bot, beep boop blop*";
        private string[] commands = { "!help", "!queue", "!add", "!addPlaylist", "!pause", "!play", "!clear", "!come", "!update", "!skip" };

        //AUDIO FORMAT: 16-bit 48000Hz PCM

        public MusicBot() { Initialize(); }

        //init vars
        public async void Initialize() {
            //Init Config and Queue
            ReadConfig();
            _queue = new Queue<Tuple<string, string>>();
            _tcs = new TaskCompletionSource<bool>();

            //Init & Connect Client
            _client = new DiscordClient();
            _client.JoinedServer += Joined;
            //Ready
            _client.Ready += Ready;
            //Message
            _client.MessageReceived += MessageReceived;
            //Server
            _client.ServerAvailable += ServerAvailable;
            _client.LeftServer += ServerLeft;
            //Channels
            _client.ChannelDestroyed += ChannelDestroyed;
            await _client.Connect(Information.Token, TokenType.Bot);

            //Setup Audio
            _client.UsingAudio(x => { x.Mode = AudioMode.Outgoing; });

            //"Playing Nothing :/"
            _client.SetGame("Nothing :/");

            InitThread();

            Status();
        }

        private async void ServerLeft(object sender, ServerEventArgs e) {
            await Join("Server left");
        }

        private async void ChannelDestroyed(object sender, ChannelEventArgs e) {
            await Join("Channel Destroyed");
        }

        //Event on Servers available
        private async void ServerAvailable(object sender, ServerEventArgs e) {
            //Only join configured Server
            if (e.Server.Name != Information.ServerName)
                return;

            _client.ServerAvailable -= ServerAvailable;

            //Print added Servers
            Console.WriteLine("\n\rAdded Servers:");
            foreach (Server server in _client.Servers) {
                if (server.Name == Information.ServerName) {
                    Console.WriteLine($" -> {server.Name}   (Selected)");
                } else {
                    Console.WriteLine($"    {server.Name}");
                }
            }
            Console.WriteLine("");

            await Join("Server Available");
        }

        public async Task Join(string context) {
            //Join First Audio Channel
            try {
                Console.WriteLine($"Join() called from context {context}...");

                Server server = _client.FindServers(Information.ServerName).FirstOrDefault();
                if (server == null)
                    throw new Exception("No Server found!");

                List<Channel> textChannels = new List<Channel>(server.TextChannels);
                List<Channel> voiceChannels = new List<Channel>(server.VoiceChannels);

                if (textChannels.Count < 1)
                    throw new Exception("No Text Channels found!");

                if (voiceChannels.Count < 1)
                    throw new Exception("No Voice Channels found!");

                _textChannel = textChannels.FirstOrDefault(c => c.Name == Information.TextChannelName) ?? textChannels[0];
                Channel voiceChannel = voiceChannels.FirstOrDefault(c => c.Name == Information.VoiceChannelName) ?? voiceChannels[0];

                AudioService service = _client.GetService<AudioService>();
                _audio = await service.Join(voiceChannel);

                //_queue.Enqueue(new Tuple<string, string>("Resources\\Hello.mp3", "Hi!"));
                //Pause = false;

                Console.WriteLine($"Joined Text Channel \"{_textChannel.Name}\"");
                Console.WriteLine($"Joined Voice Channel \"{_audio.Channel.Name}\"");
            } catch (Exception ex) {
                Console.WriteLine("Could not join Voice Channel! (" + ex.Message + ")");
            }
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
            //Avoid receiving own messages
            if (e.User.Name == _client.CurrentUser.Name) {
                return;
            }

            Console.WriteLine($"User \"{e.User}\" wrote: \"{e.Message.Text}\"");

            //Shorter var name
            string msg = e.Message.Text;

            //Avoid Spam in #general if Channel is #general & the Message is a command
            if (e.Channel.Name == "general" && commands.Any(c => msg.StartsWith(c))) {
                //Wrong Channel
                await e.Message.Delete();
                //await e.Channel.SendMessage("Wrong Channel!");
                return;
            }

            //If not a Command
            if (!commands.Any(c => msg.StartsWith(c))) {
                //Not a command
                //await e.Message.Delete();
                await e.User.SendMessage("Sorry I don't know that Command! Type **!help** for a List of Commands!" + ImABot);
                return;
            }

            //Delete Message to avoid Spam
            await e.Message.Delete();

            #region For All Users

            if (msg.StartsWith("!help")) {
                //Print Available Commands
                await e.User.SendMessage(GetHelp());
                return;
            } else if (msg.StartsWith("!queue")) {
                //Print Song Queue
                if (_queue.Count == 0) {
                    await e.User.SendMessage("Sorry, Song Queue is empty!" + ImABot);
                } else {
                    string queue = _queue.Aggregate("Song Queue:\n", (current, url) => current + ("    " + url.Item2 + "\n"));
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
                //Add Song to Queue
                if (parameter != null) {
                    //Test for valid URL
                    bool result = Uri.TryCreate(parameter, UriKind.Absolute, out Uri uriResult)
                                  && (uriResult.Scheme == Uri.UriSchemeHttp || uriResult.Scheme == Uri.UriSchemeHttps);

                    await _textChannel.SendMessage($"@{e.User} requested {parameter}! Downloading now..." + ImABot);

                    //Answer
                    if (result) {
                        try {
                            Tuple<string, string> vidInfo = await DownloadHelper.Download(parameter);
                            _queue.Enqueue(vidInfo);
                            Pause = false;
                        } catch {
                            await _textChannel.SendMessage($"Sorry @{e.User}, unfortunately I can't play that Song!" + ImABot);
                        }
                    } else {
                        await _textChannel.SendMessage($"Sorry @{e.User}, but that was no valid URL!" + ImABot);
                    }
                } else {
                    await _textChannel.SendMessage("I got confused, I don't know that command!\n\r" + GetHelp());
                }
            } else if (msg.StartsWith("!addPlaylist")) {
                //Add Playlist to Queue
                //TODO
                if (parameter != null) {
                    await _textChannel.SendMessage($"Sorry @{e.User}, I can't add Playlists as for now! :(");

                    /*
                    //Test for valid URL
                    bool result = Uri.TryCreate(parameter, UriKind.Absolute, out Uri uriResult)
                                  && (uriResult.Scheme == Uri.UriSchemeHttp || uriResult.Scheme == Uri.UriSchemeHttps);

                    //Answer
                    if (result) {
                        _queue.Enqueue(parameter);
                        await e.Channel.SendMessage("Playlist added, Thanks!" + _imABot);
                    } else {
                        await e.Channel.SendMessage("Sorry, but that was no valid URL!" + _imABot);
                    }
                    */
                } else {
                    await e.Channel.SendMessage("I got confused, I don't know that command!\n\r" + GetHelp());
                }
            } else if (msg.StartsWith("!pause")) {
                //Pause Song Playback
                Pause = true;
                await e.Channel.SendMessage("Playback paused!" + ImABot);
            } else if (msg.StartsWith("!play")) {
                //Continue Song Playback
                Pause = false;
                await e.Channel.SendMessage("Playback resumed!" + ImABot);
            } else if (msg.StartsWith("!clear")) {
                //Clear Queue
                Pause = true;
                _queue.Clear();
                await e.Channel.SendMessage($"@{e.User} cleared the Playlist!" + ImABot);
            } else if (msg.StartsWith("!come")) {
                await e.User.SendMessage("Sorry, I can't do that yet! :(");
            } else if (msg.StartsWith("!update")) {
                //Update Config
                ReadConfig();
                await e.User.SendMessage("Updated Permitted Users List!");
            } else if (msg.StartsWith("!skip")) {
                //Skip current Song
                Skip = true;
                Pause = false;
                await e.User.SendMessage("Song skipped!");
            }

            #endregion
        }

        //Init Player Thread
        public void InitThread() {
            new Thread(MusicPlay).Start();
        }

        //Looped Music Play
        private async void MusicPlay() {
            bool next = false;

            while (true) {
                bool pause = false;
                //Next song if current is over
                if (!next) {
                    pause = await _tcs.Task;
                    _tcs = new TaskCompletionSource<bool>();
                } else {
                    next = false;
                }

                try {
                    if (_queue.Count == 0) {
                        _client.SetGame(new Game("Nothing :/"));
                        Console.WriteLine($"Now playing: Nothing");
                        await _textChannel?.SendMessage($"Now playing: Nothing");
                    } else {
                        if (!pause) {
                            int channelCount = _client.GetService<AudioService>().Config.Channels;
                            WaveFormat outFormat = new WaveFormat(48000, 16, channelCount);

                            //Get Song
                            Tuple<string, string> song = _queue.Peek();
                            //Update "Playing .."
                            _client.SetGame(new Game(song.Item2));
                            Console.WriteLine($"Now playing: {song.Item2}");
                            await _textChannel?.SendMessage($"Now playing: {song.Item2}");

                            //Init Song
                            using (Mp3FileReader mp3Reader = new Mp3FileReader(song.Item1))
                            using (MediaFoundationResampler resampler = new MediaFoundationResampler(mp3Reader, outFormat)) {
                                resampler.ResamplerQuality = 60;
                                int blockSize = outFormat.AverageBytesPerSecond / 50;
                                byte[] buffer = new byte[blockSize];
                                int byteCount;

                                while ((byteCount = resampler.Read(buffer, 0, blockSize)) > 0 && !Skip) {
                                    if (byteCount < blockSize) {
                                        // Incomplete Frame
                                        for (int i = byteCount; i < blockSize; i++)
                                            buffer[i] = 0;
                                    }
                                    // Send the buffer to Discord
                                    _audio.Send(buffer, 0, blockSize);
                                    _audio.Wait();

                                    if (Pause) {
                                        bool pauseAgain;

                                        do {
                                            pauseAgain = await _tcs.Task;
                                            _tcs = new TaskCompletionSource<bool>();
                                        } while (pauseAgain);
                                    }
                                }
                            }

                            //Finally remove item
                            _queue.Dequeue();
                            try {
                                File.Delete(song.Item1);
                            } catch {
                                // ignored
                            }
                            next = true;
                        }
                    }
                } catch {
                    //audio can't be played
                }
            }
        }


        private async void Status() {
            while (true) {
                ConnectionState state = _client.State;
                Console.Title = $"Music Bot ({state})";
                if (state != ConnectionState.Connected) {
                    await Join("Status");
                }

                await Task.Delay(1000);
            }
        }


        public string GetHelp() {
            return
                " Available Commands: \n" +
                "    !add [url] ... Adds a single Song to Music-queue\n" +
                "    !addPlaylist [playlist - url]...Adds whole playlist to Music - queue\n" +
                "    !pause...Pause the queue and current Song\n" +
                "    !play...Resume the queue and current Song\n" +
                "    !queue...Prints all queued Songs & their User\n" +
                "    !clear...Clear queue and current Song\n" +
                "    !help...Prints available Commands and usage\n" +
                "    !come...Let Bot join your Channel\n" +
                "    !update...Updates the Permitted Clients List from clients.txt";
        }

        public void Dispose() {
            _client?.Disconnect();
            _client?.Dispose();

            foreach (Tuple<string, string> song in _queue) {
                try {
                    File.Delete(song.Item1);
                } catch {
                    // ignored
                }
            }
        }
    }
}