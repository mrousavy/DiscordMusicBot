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
        private List<string> _permittedUsers;
        private TaskCompletionSource<bool> _tcs;
        private bool _skip {
            get {
                bool ret = _internalSkip;
                _internalSkip = false;
                return ret;
            }
            set {
                _internalSkip = value;
            }
        }
        private bool _internalSkip;

        //Song Queue, Path to files
        private Queue<Tuple<string, string>> _queue;

        private IAudioClient _audio;
        private bool _pause {
            get {
                return _internalPause;
            }
            set {
                new Thread(() => _tcs.TrySetResult(value)).Start();
                _internalPause = value;
            }
        }
        private bool _internalPause;
        private const string ImABot = " *I'm a Bot, beep boop blop*";

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
            _client.Ready += Ready;
            _client.MessageReceived += MessageReceived;
            _client.ServerAvailable += ServerAvailable;
            await _client.Connect(Information.Token, TokenType.Bot);

            //Setup Audio
            _client.UsingAudio(x => { x.Mode = AudioMode.Outgoing; });

            //"Playing Nothing :/"
            _client.SetGame("Nothing :/");

            InitThread();

            Console.Title = $"Music Bot ({_client.State})";
        }

        //Event on Servers available
        private async void ServerAvailable(object sender, ServerEventArgs e) {
            _client.ServerAvailable -= ServerAvailable;

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
                Channel channel = channels[4];
                if (channel == null)
                    throw new Exception("No Voice Channel found!");

                AudioService service = _client.GetService<AudioService>();
                _audio = await service.Join(channel);

                Console.WriteLine($"Joined Channel \"{_audio.Channel.Name}\"");
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
            if (e.User.Name == _client.CurrentUser.Name)
                return;

            Console.WriteLine($"User \"{e.User}\" wrote: \"{e.Message.Text}\"");

            string msg = e.Message.Text;

            if (!msg.StartsWith("!"))
                //Not a command
                return;

            #region For All Users

            if (msg.StartsWith("!help")) {
                //Print Available Commands
                await e.User.SendMessage(GetHelp());
                return;
            }
            if (msg.StartsWith("!queue")) {
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

                    //Answer
                    if (result) {
                        try {
                            await e.Channel.SendMessage("Okay, give me one sec to download!" + ImABot);
                            Tuple<string, string> vidInfo = await DownloadHelper.Download(parameter);
                            _queue.Enqueue(vidInfo);
                            Play();
                            await e.Channel.SendMessage("Song added, Thanks!" + ImABot);
                        } catch {
                            await e.Channel.SendMessage("Unfortunately I can't play that Song!" + ImABot);
                        }
                    } else {
                        await e.Channel.SendMessage("Sorry, but that was no valid URL!" + ImABot);
                    }
                } else {
                    await e.Channel.SendMessage("I got confused, I don't know that command!\n\r" + GetHelp());
                }
            } else if (msg.StartsWith("!addPlaylist")) {
                //Add Playlist to Queue
                //TODO
                if (parameter != null) {
                    await e.Channel.SendMessage("Sorry, I can't add Playlists as for now! :(");

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
                Pause();
                await e.Channel.SendMessage("Playback paused!" + ImABot);
            } else if (msg.StartsWith("!play")) {
                //Continue Song Playback
                Play();
                await e.Channel.SendMessage("Playback resumed!" + ImABot);
            } else if (msg.StartsWith("!clear")) {
                //Clear Queue
                Pause();
                _queue.Clear();
                await e.Channel.SendMessage($"{e.User} cleared the Playlist!" + ImABot);
            } else if (msg.StartsWith("!come")) {
                await e.User.SendMessage("Sorry, I can't do that yet! :(");
            } else if (msg.StartsWith("!update")) {
                //Update Config
                ReadConfig();
                await e.User.SendMessage("Updated Permitted Users List!");
            } else if (msg.StartsWith("!skip")) {
                //Skip current Song
                _skip = true;
                Play();
                await e.User.SendMessage("Song skipped!");
            }

            #endregion
        }

        //Start/Resume the playing media
        public void Play() {
            _pause = false;
        }

        public void Pause() {
            _pause = true;
        }

        //Init Player Thread
        public void InitThread() {
            new Thread(async () => {
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
                        } else {
                            if (!pause) {
                                var channelCount = _client.GetService<AudioService>().Config.Channels;
                                WaveFormat OutFormat = new WaveFormat(48000, 16, channelCount);

                                //Get Song
                                Tuple<string, string> song = _queue.Dequeue();
                                //Update "Playing .."
                                _client.SetGame(new Game(song.Item2));

                                //Init Song
                                using (var MP3Reader = new Mp3FileReader(song.Item1))
                                using (var resampler = new MediaFoundationResampler(MP3Reader, OutFormat)) {
                                    resampler.ResamplerQuality = 60;
                                    int blockSize = OutFormat.AverageBytesPerSecond / 50;
                                    byte[] buffer = new byte[blockSize];
                                    int byteCount;

                                    while ((byteCount = resampler.Read(buffer, 0, blockSize)) > 0 && !_skip) {
                                        if (byteCount < blockSize) {
                                            // Incomplete Frame
                                            for (int i = byteCount; i < blockSize; i++)
                                                buffer[i] = 0;
                                        }
                                        // Send the buffer to Discord
                                        _audio.Send(buffer, 0, blockSize);
                                        _audio.Wait();

                                        if (_pause) {
                                            bool pauseAgain;

                                            do {
                                                pauseAgain = await _tcs.Task;
                                                _tcs = new TaskCompletionSource<bool>();
                                            } while (pauseAgain);
                                        }
                                    }
                                }

                                try {
                                    File.Delete(song.Item1);
                                } catch { }
                                next = true;
                            }
                        }
                    } catch {
                        //audio can't be played
                    }
                }
            }).Start();
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