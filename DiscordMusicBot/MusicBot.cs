using Discord;
using Discord.Audio;
using Discord.Rest;
using Discord.WebSocket;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace DiscordMusicBot {
    internal class MusicBot : IDisposable {
        private DiscordSocketClient _client;
        private IVoiceChannel _voiceChannel;
        private ITextChannel _textChannel;
        private List<string> _permittedUsers;
        private TaskCompletionSource<bool> _tcs;
        private CancellationTokenSource _disposeToken;
        private IAudioClient _audio;
        private const string ImABot = " *I'm a Bot, beep boop blop*";
        private readonly string[] _commands = { "!help", "!queue", "!add", "!addPlaylist", "!pause", "!play", "!clear", "!come", "!update", "!skip" };

        /// <summary>
        /// Tuple(FilePath, Video Name, Duration, Requested by)
        /// </summary>
        private Queue<Tuple<string, string, string, string>> _queue;

        private bool Pause {
            get => _internalPause;
            set {
                new Thread(() => _tcs.TrySetResult(value)).Start();
                _internalPause = value;
            }
        }
        private bool _internalPause;
        private bool Skip {
            get {
                bool ret = _internalSkip;
                _internalSkip = false;
                return ret;
            }
            set => _internalSkip = value;
        }
        private bool _internalSkip;

        public bool IsDisposed;


        public MusicBot() { Initialize(); }

        //init vars
        public async void Initialize() {
            //Init Config and Queue
            ReadConfig();
            _queue = new Queue<Tuple<string, string, string, string>>();
            _tcs = new TaskCompletionSource<bool>();
            _disposeToken = new CancellationTokenSource();

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
            _client.Ready += OnReady;

            //Message
            _client.MessageReceived += OnMessageReceived;

            Console.Title = "Music Bot (Connecting...)";

            await _client.StartAsync();
            await _client.LoginAsync(TokenType.Bot, Information.Token);

            //Setup Audio
            //_client.UsingAudio(x => { x.Mode = AudioMode.Outgoing; });

            InitThread();

            Status();
        }

        #region Events

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


        //Pass Ready Event on to Async Method (so nothing blocks)
        private Task OnReady() {
            Ready();
            return Task.CompletedTask;
        }

        //On Bot ready
        private async void Ready() {
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

        //Pass MessageReceived Event on to Async Method (so nothing blocks)
        private Task OnMessageReceived(SocketMessage socketMsg) {
            MessageReceived(socketMsg);
            return Task.CompletedTask;
        }

        //On Message Received (async)
        private async void MessageReceived(SocketMessage socketMsg) {
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
                    await dm.SendMessageAsync(
                        $"Use these *Commands* by sending me a **private Message**, or writing in **#{Information.TextChannelName}**!" + ImABot,
                        embed: GetHelp(socketMsg.Author.ToString()));
                    return;
                } else if (msg.StartsWith("!queue")) {
                    Print("User requested: Queue", ConsoleColor.Magenta);
                    //Print Song Queue
                    await SendQueue(_textChannel);
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



                switch (command) {
                    #region !add

                    case "!add":
                        //Add Song to Queue
                        if (parameter != null) {
                            using (_textChannel.EnterTypingState()) {

                                //Test for valid URL
                                bool result = Uri.TryCreate(parameter, UriKind.Absolute, out Uri uriResult)
                                          && (uriResult.Scheme == "http" || uriResult.Scheme == "https");

                                //Answer
                                if (result) {
                                    try {
                                        Print("Downloading Video...", ConsoleColor.Magenta);

                                        Tuple<string, string> info = await DownloadHelper.GetInfo(parameter);
                                        await SendMessage($"<@{socketMsg.Author.Id}> requested \"{info.Item1}\" ({info.Item2})! Downloading now..." +
                                                          ImABot);

                                        //Download
                                        string file = await DownloadHelper.Download(parameter);
                                        var vidInfo = new Tuple<string, string, string, string>(file, info.Item1, info.Item2, socketMsg.Author.ToString());

                                        _queue.Enqueue(vidInfo);
                                        Pause = false;
                                        Print($"Song added to playlist! ({vidInfo.Item2} ({vidInfo.Item3}))!", ConsoleColor.Magenta);
                                    } catch (Exception ex) {
                                        Print($"Could not download Song! {ex.Message}", ConsoleColor.Red);
                                        await SendMessage(
                                            $"Sorry <@{socketMsg.Author.Id}>, unfortunately I can't play that Song!" +
                                            ImABot);
                                    }
                                } else {
                                    await _textChannel.SendMessageAsync(
                                        $"Sorry <@{socketMsg.Author.Id}>, but that was not a valid URL!" + ImABot);
                                }
                            }
                        }
                        break;

                    #endregion

                    #region !addPlaylist

                    case "!addPlaylist":
                        //Add Song to Queue
                        if (parameter != null) {
                            using (_textChannel.EnterTypingState()) {

                                //Test for valid URL
                                bool result = Uri.TryCreate(parameter, UriKind.Absolute, out Uri uriResult)
                                              && (uriResult.Scheme == "http" || uriResult.Scheme == "https");

                                //Answer
                                if (result) {
                                    try {
                                        Print("Downloading Playlist...", ConsoleColor.Magenta);

                                        Tuple<string, string> info = await DownloadHelper.GetInfo(parameter);
                                        await SendMessage($"<@{socketMsg.Author.Id}> requested Playlist \"{info.Item1}\" ({info.Item2})! Downloading now..." +
                                                          ImABot);

                                        //Download
                                        string file = await DownloadHelper.DownloadPlaylist(parameter);
                                        var vidInfo = new Tuple<string, string, string, string>(file, info.Item1, info.Item2, socketMsg.Author.ToString());

                                        _queue.Enqueue(vidInfo);
                                        Pause = false;
                                        Print($"Playlist added to playlist! (\"{vidInfo.Item2}\" ({vidInfo.Item2}))!", ConsoleColor.Magenta);
                                    } catch (Exception ex) {
                                        Print($"Could not download Playlist! {ex.Message}", ConsoleColor.Red);
                                        await SendMessage(
                                            $"Sorry <@{socketMsg.Author.Id}>, unfortunately I can't play that Playlist!" +
                                            ImABot);
                                    }
                                } else {
                                    await _textChannel.SendMessageAsync(
                                        $"Sorry <@{socketMsg.Author.Id}>, but that was not a valid URL!" + ImABot);
                                }
                            }
                        }
                        break;

                    #endregion

                    #region !pause

                    case "!pause":
                        //Pause Song Playback
                        Pause = true;
                        Print("Playback paused!", ConsoleColor.Magenta);
                        await _textChannel.SendMessageAsync($"<@{socketMsg.Author}> paused playback!" + ImABot);
                        break;

                    #endregion

                    #region !play

                    case "!play":
                        //Continue Song Playback
                        Pause = false;
                        Print("Playback continued!", ConsoleColor.Magenta);
                        await _textChannel.SendMessageAsync($"<@{socketMsg.Author}> resumed playback!" + ImABot);
                        break;

                    #endregion

                    #region !clear

                    case "!clear":
                        //Clear Queue
                        Pause = true;
                        _queue.Clear();
                        Print("Playlist cleared!", ConsoleColor.Magenta);
                        await SendMessage(
                            $"<@{socketMsg.Author.Id}> cleared the Playlist!" + ImABot);
                        break;

                    #endregion

                    #region !come

                    case "!come":
                        _audio?.Dispose();
                        _voiceChannel = (socketMsg.Author as IGuildUser)?.VoiceChannel;
                        if (_voiceChannel == null) {
                            Print("Error joining Voice Channel!", ConsoleColor.Red);
                            await socketMsg.Channel.SendMessageAsync($"I can't connect to your Voice Channel <@{socketMsg.Author}>!" + ImABot);
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
                        Print("Song Skipped!", ConsoleColor.Magenta);
                        await _textChannel.SendMessageAsync($"<@{socketMsg.Author}> skipped **{_queue.Peek().Item2}**!");
                        //Skip current Song
                        Skip = true;
                        Pause = false;
                        break;

                    #endregion

                    default:
                        // no command
                        break;
                }

                #endregion

            } catch (Exception ex) {
                Print(ex.Message, ConsoleColor.Red);
            }
        }

        #endregion

        #region Discord Helper

        //Login as Bot and Start Bot
        private async Task Connect() {
            await _client.LoginAsync(TokenType.Bot, Information.Token);
            await _client.StartAsync();
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

        //Send Message to channel
        public async Task SendMessage(string message) {
            if (_textChannel != null)
                await _textChannel.SendMessageAsync(message);
        }

        //Send Song queue in channel
        private async Task SendQueue(IMessageChannel channel) {
            EmbedBuilder builder = new EmbedBuilder() {
                Author = new EmbedAuthorBuilder { Name = "Music Bot Song Queue" },
                Footer = new EmbedFooterBuilder() { Text = "(I don't actually sing)" },
                Color = Pause ? new Color(244, 67, 54) /*Red*/ : new Color(00, 99, 33) /*Green*/
            };
            //builder.ThumbnailUrl = "some cool url";
            builder.Url = "http://github.com/mrousavy/DiscordMusicBot";

            if (_queue.Count == 0) {
                await channel.SendMessageAsync("Sorry, Song Queue is empty! Add some songs with the `!add [url]` command!" + ImABot);
            } else {
                foreach (Tuple<string, string, string, string> song in _queue) {
                    builder.AddField($"{song.Item2} ({song.Item3})", $"by {song.Item4}");
                }

                await channel.SendMessageAsync("", embed: builder.Build());
            }
        }

        //Return Bot Help
        public Embed GetHelp(string user) {
            EmbedBuilder builder = new EmbedBuilder() {
                Title = "Music Bot Help",
                Description = _permittedUsers.Contains(user) ?
                                    "You are allowed to use **every** command." :
                                    "You are only allowed to use `!help` and `!queue`",
                Color = new Color(102, 153, 255)
            };
            //builder.ThumbnailUrl = "https://raw.githubusercontent.com/mrousavy/DiscordMusicBot/master/DiscordMusicBot/disc.png"; //Music Bot Icon
            builder.Url = "http://github.com/mrousavy/DiscordMusicBot";

            builder.AddField("`!help`", "Prints available Commands and usage");
            builder.AddField("`!queue`", "Prints all queued Songs & their User");

            builder.AddField("`!add [url]`", "Adds a single Song to Music-queue");
            builder.AddField("`!addPlaylist [url]`", "Adds whole playlist to Music-queue");
            builder.AddField("`!pause`", "Pause the queue and current Song");
            builder.AddField("`!play`", "Resume the queue and current Song");
            builder.AddField("`!clear`", "Clear queue and current Song");
            builder.AddField("`!come`", "Let Bot join your Channel");
            builder.AddField("`!update`", "Updates Permitted Clients from File");


            return builder.Build();
        }

        //Dispose this Object (Async)
        private async Task DisposeAsync() {
            try {
                await _client.StopAsync();
                await _client.LogoutAsync();
            } catch {
                // could not disconnect
            }
            _client?.Dispose();
        }

        //Refresh Status of DiscordClient
        private async void Status() {
            try {
                while (!_disposeToken.IsCancellationRequested) {
                    ConnectionState state = _client.ConnectionState;
                    Console.Title = $"Music Bot ({state})";
                    if (state == ConnectionState.Disconnected) {
                        await Task.Delay(5000, _disposeToken.Token);
                        // if still not connected, try joining
                        if (state == ConnectionState.Disconnected) {
                            await Connect();
                        }
                    }

                    await Task.Delay(5000, _disposeToken.Token);
                }
            } catch (TaskCanceledException) {
                // _disposeToken Cancelled called
            }
        }

        #endregion

        #region Helper

        //Log own Messages to console
        public static void Print(string message, ConsoleColor color) {
            Console.ForegroundColor = color;
            Console.WriteLine(message);
            Console.ResetColor();
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

        //Read Config from File
        public void ReadConfig() {
            if (!File.Exists("users.txt"))
                File.Create("users.txt").Dispose();

            _permittedUsers = new List<string>(File.ReadAllLines("users.txt"));


            string msg = _permittedUsers.Aggregate("Permitted Users:\n\r    ", (current, user) => current + (user + ", "));
            Print(msg, ConsoleColor.Cyan);
        }

        //Init Player Thread
        public void InitThread() {
            //TODO: Main Thread or New Thread?
            //MusicPlay();
            new Thread(MusicPlay).Start();
        }


        #endregion

        #region Audio
        //Audio: PCM | 48000hz | mp3

        //Get ffmpeg Audio Procecss
        private static Process GetFfmpeg(string path) {
            ProcessStartInfo ffmpeg = new ProcessStartInfo {
                FileName = "ffmpeg",
                Arguments = $"-hide_banner -loglevel panic -i \"{path}\" -ac 2 -f s16le -ar 48000 pipe:1",
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

                    //DOES NOT WORK
                    //Adjust?
                    int bufferSize = 1024;

                    int bytesSent = 0;
                    int errors = 0;
                    byte[] buffer = new byte[bufferSize];

                    while (
                        !Skip &&                                // If Skip is set to true, stop sending and set back to false (with getter)
                        errors < 5 &&                           // After 5 unsuccessful sendings, stop sending
                        !_disposeToken.IsCancellationRequested  // On Cancel/Dispose requested, stop sending
                        ) {
                        try {
                            int read = await output.ReadAsync(buffer, 0, bufferSize, _disposeToken.Token);
                            await discord.WriteAsync(buffer, 0, read, _disposeToken.Token);

                            if (Pause) {
                                bool pauseAgain;

                                do {
                                    pauseAgain = await _tcs.Task;
                                    _tcs = new TaskCompletionSource<bool>();
                                } while (pauseAgain);
                            }

                            bytesSent += read;
                        } catch {
                            errors++;
                            // could not send
                        }
                    }

                    await discord.FlushAsync();
                }
            }

            ffmpeg.WaitForExit();
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
                            var song = _queue.Peek();
                            //Update "Playing .."
                            await _client.SetGameAsync(song.Item2, song.Item1);
                            Print($"Now playing: {song.Item2} ({song.Item3})", ConsoleColor.Magenta);
                            await SendMessage($"Now playing: **{song.Item2}** ({song.Item3})");

                            await SendAudio(song.Item1);

                            try {
                                File.Delete(song.Item1);
                            } catch {
                                // ignored
                            } finally {
                                //Finally remove song from playlist
                                _queue.Dequeue();
                            }
                            next = true;
                        }
                    }
                } catch {
                    //audio can't be played
                }
            }
        }

        #endregion

        //Dispose this Object
        public void Dispose() {
            IsDisposed = true;
            _disposeToken.Cancel();

            Print("Shutting down...", ConsoleColor.Red);

            //Run File Delete on new Thread
            new Thread(() => {
                foreach (var song in _queue) {
                    try {
                        File.Delete(song.Item1);
                    } catch {
                        // ignored
                    }
                }
            }).Start();

            DisposeAsync().GetAwaiter().GetResult();
        }
    }
}
