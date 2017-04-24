# <img src="DiscordMusicBot/disc.png" width="42"> DiscordMusicBot
A Discord Bot for playing YouTube music.

# Installing
### Windows
* [Add this Music Bot to your Discord Server](https://discordapp.com/oauth2/authorize?client_id=304226292545486849&scope=bot)
* [Download this Music Bot](https://github.com/mrousavy/DiscordMusicBot/raw/master/Download/DiscordMusicBot.zip) and extract into **DiscordMusicBot/**
* [Modify **config.json**](#configure)
* [Install .NET Core](https://www.microsoft.com/net/download/core)
* Optionally (Reinstall newer versions):
    * [Download FFmpeg](http://ffmpeg.zeranoe.com/builds/) and place into **DiscordMusicBot/** folder
    * [Download youtube-dl](https://rg3.github.io/youtube-dl/download.html) and place into **DiscordMusicBot/** folder
    * Download [Sodium](https://discord.foxbot.me/binaries/libsodium/) and/or [Opus](https://discord.foxbot.me/binaries/opus/) and place them into **DiscordMusicBot/** folder

### Linux
* [Add this Music Bot to your Discord Server](https://discordapp.com/oauth2/authorize?client_id=304226292545486849&scope=bot)
* [Download this Music Bot](https://github.com/mrousavy/DiscordMusicBot/raw/master/Download/DiscordMusicBot.zip) and extract into **DiscordMusicBot/**
* [Modify **config.json**](#configure)
* [Install .NET Core](https://www.microsoft.com/net/download/linux)
* Optionally (Reinstall newer versions):
    * [Download FFmpeg](https://ffmpeg.org/download.html#build-linux) and place into **DiscordMusicBot/** folder
    * [Download youtube-dl](https://rg3.github.io/youtube-dl/download.html): and place into **DiscordMusicBot/** folder
        ```shell
        sudo curl -L https://yt-dl.org/downloads/latest/youtube-dl -o /usr/local/bin/youtube-dl

        sudo chmod a+rx /usr/local/bin/youtube-dl
        ```
     * Download [Sodium](https://download.libsodium.org/doc/installation/) and/or [Opus](http://opus-codec.org/downloads/) and place them into **DiscordMusicBot/** folder

# Configure
* Create your app [here](https://discordapp.com/developers/applications/me)
* Create **DiscordMusicBot/config.json** file
* Paste this into **config.json** and set your values
    ```json
    {
    "ClientId": "[app Client ID]",
    "ClientSecret": "[app secret]",
    "BotName": "[app name]",
    "Token": "[app token]",
    "ServerName": "My Discord Server",
    "VoiceChannelName": "Lounge 1",
    "TextChannelName": "general"
    }
    ```


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
