using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Discord;
using Discord.Audio;
using Discord.Commands;
using Discord.Rest;
using Discord.WebSocket;

namespace DiscordMusicBot {
    internal class MusicBot : IDisposable {
        private DiscordSocketClient _client;
        private IVoiceChannel _voiceChannel;
        private ISocketMessageChannel _textChannel;
        private SocketGuild _server;
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
        private readonly string[] _commands = { "!help", "!queue", "!add", "!addPlaylist", "!pause", "!play", "!clear", "!come", "!update", "!skip" };


        public MusicBot() { Initialize(); }

        //init vars
        public async void Initialize() {
            //Init Config and Queue
            ReadConfig();
            _queue = new Queue<Tuple<string, string>>();
            _tcs = new TaskCompletionSource<bool>();

            //Init & Connect Client
            _client = new DiscordSocketClient(new DiscordSocketConfig {
                LogLevel = LogSeverity.Verbose
            });

            //Logging
            _client.Log += Log;

            //-
            _client.Disconnected += Disconnected;

            //+
            _client.Connected += Connected;
            _client.Ready += Ready;

            //Message
            _client.MessageReceived += MessageReceived;

            Console.Title = "Music Bot (Connecting...)";

            await _client.StartAsync();
            await _client.LoginAsync(TokenType.Bot, Information.Token);

            //Setup Audio
            //_client.UsingAudio(x => { x.Mode = AudioMode.Outgoing; });

            InitThread();

            Status();
        }

        //Connection Lost
        private static Task Disconnected(Exception arg) {
            Print($"Connection lost! ({arg.Message})", ConsoleColor.Red);
            return Task.CompletedTask;
        }

        //Connected
        private static Task Connected() {
            Console.Title = "Music Bot (Connected)";

            Print("Connected!", ConsoleColor.Green);
            return Task.CompletedTask;
        }

        //On Bot ready
        private async Task Ready() {
            Print("Ready!", ConsoleColor.Green);

            //"Playing Nothing :/"
            await _client.SetGameAsync("Nothing :/");

            //Get Guilds / Servers
            try {
                //Server
                PrintServers();
                SocketGuild guild = _client.Guilds.FirstOrDefault(g => g.Name == Information.ServerName);

                //Text Channel
                _textChannel = guild.TextChannels.FirstOrDefault(t => t.Name == Information.TextChannelName);
                Print($"Using Text Channel: \"#{_textChannel.Name}\"", ConsoleColor.Cyan);

                //Voice Channel
                _voiceChannel = guild.VoiceChannels.FirstOrDefault(t => t.Name == Information.VoiceChannelName);
                Print($"Using Voice Channel: \"{_voiceChannel.Name}\"", ConsoleColor.Cyan);
                _audio = await _voiceChannel.ConnectAsync();
            } catch (Exception e) {
                Print("Could not join Voice/Text Channel (" + e.Message + ")", ConsoleColor.Red);
            }
        }

        //Print all Servers on Console
        private void PrintServers() {
            //Print added Servers
            Print("\n\rAdded Servers:", ConsoleColor.Cyan);
            foreach (SocketGuild server in _client.Guilds) {
                Print(server.Name == Information.ServerName
                    ? $" [x] {server.Name}"
                    : $" [ ] {server.Name}", ConsoleColor.Cyan);
            }
            Print("", ConsoleColor.Cyan);
        }

        private async Task Connect() {
            await _client.LoginAsync(TokenType.Bot, Information.Token);
            await _client.StartAsync();
        }

        //Read Config from File
        public void ReadConfig() {
            if (!File.Exists("users.txt"))
                File.Create("users.txt").Dispose();

            _permittedUsers = new List<string>(File.ReadAllLines("users.txt"));


            string msg = _permittedUsers.Aggregate("Permitted Users:\n\r    ", (current, user) => current + (user + ", "));
            Print(msg, ConsoleColor.Cyan);
        }

        //On Private Message Received
        private async Task MessageReceived(SocketMessage socketMsg) {
            try {
                #region Message Filtering

                //Avoid receiving own messages
                if (socketMsg.Author.Id == _client.CurrentUser.Id) {
                    return;
                }

                Print($"User \"{socketMsg.Author}\" wrote: \"{socketMsg.Content}\"", ConsoleColor.Magenta);

                //Shorter var name
                string msg = socketMsg.Content;
                //Is MusicBot Command

                bool isCmd = _commands.Any(c => msg.StartsWith(c));

                //If is a supported command
                if (isCmd) {
                    //Avoid Spam in #general if Channel is #general
                    if (socketMsg.Channel.Name == "general") {
                        await socketMsg.DeleteAsync();
                        //await e.Channel.SendMessage("Wrong Channel!");
                        return;
                    }
                }
                //If not a supported command
                else {
                    if (socketMsg.Channel.Name == Information.TextChannelName) {
                        //Not a command
                        await socketMsg.DeleteAsync();
                    }
                    return;
                }

                #endregion

                //Direct Message Channel to Message Author
                RestDMChannel dm = await socketMsg.Author.CreateDMChannelAsync();

                //Delete Message to avoid Spam
                try {
                    await socketMsg.DeleteAsync();
                } catch {
                    // not allowed
                }

                #region For All Users

                if (msg.StartsWith("!help")) {
                    Print("User requested: Help", ConsoleColor.Magenta);
                    //Print Available Commands
                    await dm.SendMessageAsync(GetHelp());
                    return;
                } else if (msg.StartsWith("!queue")) {
                    Print("User requested: Queue", ConsoleColor.Magenta);
                    //Print Song Queue
                    if (_queue.Count == 0) {
                        await dm.SendMessageAsync("Sorry, Song Queue is empty!" + ImABot);
                    } else {
                        string queue = _queue.Aggregate("**Song Queue:**\n",
                            (current, url) => current + ($"    {url.Item2}\n"));
                        await dm.SendMessageAsync(queue);
                    }
                    return;
                }

                #endregion

                #region Only with Roles

                if (!_permittedUsers.Contains(socketMsg.Author.ToString())) {
                    await dm.SendMessageAsync("Sorry, but you're not allowed to do that!" + ImABot);
                    return;
                }

                string[] split = msg.Split(' ');
                string command = split[0].ToLower();
                string parameter = null;
                if (split.Length > 1)
                    parameter = split[1];


                #region !add

                switch (command) {
                    #region !add

                    case "!add":
                        //Add Song to Queue
                        if (parameter != null) {
                            //Test for valid URL
                            bool result = Uri.TryCreate(parameter, UriKind.Absolute, out Uri uriResult)
                                          && (uriResult.Scheme == "http" || uriResult.Scheme == "https");

                            await SendMessage($"<@{socketMsg.Author.Id}> requested {parameter}! Downloading now..." +
                                              ImABot);

                            //Answer
                            if (result) {
                                try {
                                    Print("Downloading Video...", ConsoleColor.Magenta);
                                    Tuple<string, string> vidInfo = await DownloadHelper.Download(parameter);
                                    _queue.Enqueue(vidInfo);
                                    Pause = false;
                                    Print($"Song added to playlist! (Name: \"{vidInfo.Item2}\")!", ConsoleColor.Magenta);
                                } catch (Exception ex) {
                                    Print($"Could not download Song! {ex.Message}", ConsoleColor.Red);
                                    await SendMessage(
                                        $"Sorry <@{socketMsg.Author.Id}>, unfortunately I can't play that Song!" +
                                        ImABot);
                                }
                            } else {
                                await socketMsg.Channel.SendMessageAsync(
                                    $"Sorry <@{socketMsg.Author.Id}>, but that was no valid URL!" + ImABot);
                            }
                        }
                        break;

                    #endregion

                    #region !addPlaylist

                    case "!addPlaylist":
                        Print("Add Playlist!", ConsoleColor.Magenta);
                        //Add Playlist to Queue
                        //TODO
                        if (parameter != null) {
                            await socketMsg.Channel.SendMessageAsync(
                                $"Sorry <@{socketMsg.Author.Id}>, I can't add Playlists as for now! :(");

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
                        }
                        break;

                    #endregion

                    #region !pause

                    case "!pause":
                        //Pause Song Playback
                        Pause = true;
                        Print("Playback paused!", ConsoleColor.Magenta);
                        await socketMsg.Channel.SendMessageAsync("Playback paused!" + ImABot);
                        break;

                    #endregion

                    #region !play

                    case "!play":
                        //Continue Song Playback
                        Pause = false;
                        Print("Playback continued!", ConsoleColor.Magenta);
                        await socketMsg.Channel.SendMessageAsync("Playback resumed!" + ImABot);
                        break;

                    #endregion

                    #region !clear

                    case "!clear":
                        //Clear Queue
                        Pause = true;
                        _queue.Clear();
                        Print("Playlist cleared!", ConsoleColor.Magenta);
                        await socketMsg.Channel.SendMessageAsync(
                            $"<@{socketMsg.Author.Id}> cleared the Playlist!" + ImABot);
                        break;

                    #endregion

                    #region !come

                    case "!come":
                        _audio?.Dispose();
                        _voiceChannel = (socketMsg.Author as IGuildUser)?.VoiceChannel;
                        if (_voiceChannel == null) {
                            Print("Error joining Voice Channel!", ConsoleColor.Red);
                            await dm.SendMessageAsync("I can't connect to your Voice Channel!" + ImABot);
                        } else {
                            Print($"Joined Voice Channel \"{_voiceChannel.Name}\"", ConsoleColor.Magenta);
                            _audio = await _voiceChannel.ConnectAsync();
                        }
                        break;

                    #endregion

                    #region !update

                    case "!update":
                        //Update Config
                        ReadConfig();
                        Print("User Config Updated!", ConsoleColor.Magenta);
                        await dm.SendMessageAsync("Updated Permitted Users List!");
                        break;

                    #endregion

                    #region !skip

                    case "!skip":
                        //Skip current Song
                        Skip = true;
                        Pause = false;
                        Print("Song Skipped!", ConsoleColor.Magenta);
                        await dm.SendMessageAsync("Song skipped!");
                        break;

                    #endregion

                    default:
                        // no command
                        break;
                }

                #endregion

                #endregion

            } catch (Exception ex) {
                Print(ex.Message, ConsoleColor.Red);
            }
        }

        //Init Player Thread
        public void InitThread() {
            new Thread(MusicPlay).Start();
        }

        //Get ffmpeg Audio Procecss
        private static Process GetFfmpeg(string path) {
            ProcessStartInfo ffmpeg = new ProcessStartInfo {
                FileName = "ffmpeg",
                Arguments = $"-i {path} -ac 2 -f s16le -ar 48000 pipe:1",
                UseShellExecute = false,
                RedirectStandardOutput = true,
            };
            return Process.Start(ffmpeg);
        }

        //Send Audio with ffmpeg
        private async Task SendAudio(string path) {
            // Create FFmpeg using the previous example
            Process ffmpeg = GetFfmpeg(path);
            using (Stream output = ffmpeg.StandardOutput.BaseStream) {
                using (AudioOutStream discord = _audio.CreatePCMStream(AudioApplication.Mixed, 1920)) {
                    //Adjust?
                    int bufferSize = _voiceChannel.Bitrate * 8;

                    int bytesSent = 0;
                    int errors = 0;
                    byte[] buffer = new byte[bufferSize];

                    while (bytesSent < output.Length && !Skip && errors < 10) {
                        try {
                            await output.ReadAsync(buffer, bytesSent, bufferSize);
                            await discord.WriteAsync(buffer, bytesSent, bufferSize);

                            if (Pause) {
                                bool pauseAgain;

                                do {
                                    pauseAgain = await _tcs.Task;
                                    _tcs = new TaskCompletionSource<bool>();
                                } while (pauseAgain);
                            }

                            bytesSent += 1024;
                        } catch {
                            errors++;
                            // could not send
                        }
                    }
                }
            }
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
                        await _client.SetGameAsync("Nothing :/");
                        Print("Now playing: Nothing", ConsoleColor.Magenta);
                        await SendMessage("Now playing: **Nothing**");
                    } else {
                        if (!pause) {
                            //Get Song
                            Tuple<string, string> song = _queue.Peek();
                            //Update "Playing .."
                            await _client.SetGameAsync(song.Item2, song.Item1);
                            Print($"Now playing: {song.Item2}", ConsoleColor.Magenta);
                            await SendMessage($"Now playing: **{song.Item2}**");

                            await SendAudio(song.Item1);

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

        //Refresh Status of DiscordClient
        private async void Status() {
            while (true) {
                ConnectionState state = _client.ConnectionState;
                Console.Title = $"Music Bot ({state})";
                if (state == ConnectionState.Disconnected) {
                    await Task.Delay(5000);
                    // if still not connected, try joining
                    if (state == ConnectionState.Disconnected) {
                        await Connect();
                    }
                }

                await Task.Delay(5000);
            }
        }

        //Return Help
        public string GetHelp() {
            return
                " **Available Commands:** \n" +
                "    !add [url]                    ... *Adds a single Song to Music-queue*\n" +
                "    !addPlaylist [playlist - url] ... *Adds whole playlist to Music - queue*\n" +
                "    !pause                        ... *Pause the queue and current Song*\n" +
                "    !play                         ... *Resume the queue and current Song*\n" +
                "    !queue                        ... *Prints all queued Songs & their User*\n" +
                "    !clear                        ... *Clear queue and current Song*\n" +
                "    !help                         ... *Prints available Commands and usage*\n" +
                "    !come                         ... *Let Bot join your Channel*\n" +
                "    !update                       ... *Updates the Permitted Clients List from clients.txt*";
        }

        //Log DiscordBot Messages to console
        private static Task Log(LogMessage arg) {
            switch (arg.Severity) {
                case LogSeverity.Critical:
                case LogSeverity.Error:
                    Console.ForegroundColor = ConsoleColor.Red;
                    break;
                case LogSeverity.Debug:
                case LogSeverity.Verbose:
                    Console.ForegroundColor = ConsoleColor.Gray;
                    break;
                case LogSeverity.Warning:
                    Console.ForegroundColor = ConsoleColor.Yellow;
                    break;
                case LogSeverity.Info:
                    Console.ForegroundColor = ConsoleColor.Green;
                    break;
                default:
                    break;
            }
            Console.WriteLine($"[{arg.Severity}] [{arg.Source}] [{arg.Message}]");

            Console.ResetColor();
            return Task.CompletedTask;
        }

        //Log own Messages to console
        public static void Print(string message, ConsoleColor color) {
            Console.ForegroundColor = color;
            Console.WriteLine(message);
            Console.ResetColor();
        }

        //Send Message to channel
        public async Task SendMessage(string message) {
            if (_textChannel != null)
                await _textChannel.SendMessageAsync(message);
        }

        //Dispose this Object
        public void Dispose() {
            _client.Log -= Log;

            Print("Shutting down...", ConsoleColor.Red);

            //Run File Delete on new Thread
            new Thread(() => {
                foreach (Tuple<string, string> song in _queue) {
                    try {
                        File.Delete(song.Item1);
                    } catch {
                        // ignored
                    }
                }
            }).Start();

            DisposeAsync().GetAwaiter().GetResult();
        }

        //Dispose this Object (Async)
        private async Task DisposeAsync() {
            try {
                await _client.StopAsync();
                await _client.LogoutAsync();
            } catch {
                // could not disconnect
            } finally {
                _client?.Dispose();
            }
        }
    }
}