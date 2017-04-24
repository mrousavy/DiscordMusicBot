# <img src="https://image.freepik.com/free-icon/music-disc-with-white-details_318-43070.jpg" width="42"> DiscordMusicBot
A Discord Bot for playing YouTube music.

[Click here to add this MusicBot to your Server](https://discordapp.com/oauth2/authorize?client_id=304226292545486849&scope=bot)

[Click here to download Music Bot](https://github.com/mrousavy/DiscordMusicBot/raw/master/Download/DiscordMusicBot.zip)

![neat GIF](https://laughingsquid.com/wp-content/uploads/2015/06/floating-record-1.gif)

# Installing
#### Windows
* [Install .NET Core](https://www.microsoft.com/net/download/core)
* [Install FFmpeg](http://ffmpeg.zeranoe.com/builds/)
* Install [youtube-dl](https://rg3.github.io/youtube-dl/download.html) ([Direct Link 2017.04.17](https://yt-dl.org/downloads/2017.04.17/youtube-dl.exe))
* Optionally: Install [Sodium](https://download.libsodium.org/doc/installation/) and/or [Opus](http://opus-codec.org/downloads/) and place them into DiscordMusicBot folder

#### Linux

* [Install .NET Core](https://www.microsoft.com/net/download/linux)
* [Install FFmpeg](https://ffmpeg.org/download.html#build-linux)
* Install **youtube-dl**
    ```Bash
    sudo curl -L https://yt-dl.org/downloads/latest/youtube-dl -o /usr/local/bin/youtube-dl

    sudo chmod a+rx /usr/local/bin/youtube-dl
    ```
* Optionally: Install [Sodium](https://download.libsodium.org/doc/installation/) and/or [Opus](http://opus-codec.org/downloads/) and place them into DiscordMusicBot folder


# Commands

`!add [url]`                            ...     Adds a single Song to Music-queue

`!addPlaylist [playlist-url]`           ...     Adds whole playlist to Music-queue

`!pause`                                ...     Pause the queue and current Song

`!play`                                 ...     Resume the queue and current Song

`!queue`                                ...     Prints all queued Songs & their User

`!clear`                                ...     Clear queue and current Song

`!setTimeout [timeoutInMilliseconds]`   ...     Timeout between being able to request songs

`!help`                                 ...     Prints available Commands and usage

`!come`                                 ...     Let Bot join your Channel

`!update`                               ...     Updates the Permitted Clients List from clients.txt

`!skip`                                 ...     Skips the current Song
