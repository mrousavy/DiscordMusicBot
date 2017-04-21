using System.IO;
using Newtonsoft.Json;

namespace DiscordMusicBot {
    internal static class Information {
        internal static Config Config => JsonConvert.DeserializeObject<Config>(File.ReadAllText("config.json"));

        internal static string ClientId => Config.ClientId;
        internal static string ClientSecret => Config.ClientSecret;
        internal static string BotName => Config.BotName;
        internal static string Token => Config.Token;
        internal static string ServerName => Config.ServerName;
        internal static string ChannelName => Config.ChannelName;
    }

    public class Config {
        public string ClientId = "304226292545486849";
        public string ClientSecret = "bVaQVYbg3XKVGKKllvcbGPtIzaHzkc8o";
        public string BotName = "TheBotsAreTakingOver";
        public string Token = "MzA0MjI2MjkyNTQ1NDg2ODQ5.C9pRVw.EA5DylXkdJhI7wHiuu_YiicJ-gg";
        public string ServerName = "Universeller Homoserver";
        public string ChannelName = "Lounge 1";
    }
}
