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
        public string ClientId = "YourClientID";
        public string ClientSecret = "YourClientSecret";
        public string BotName = "YourBotName";
        public string Token = "YourBotToken";
        public string ServerName = "TheServerYouWantToConnectTo";
        public string ChannelName = "TheVoiceChannelYouWantToJoin";

        public static bool operator ==(Config cfg1, Config cfg2) {
            return ReferenceEquals(cfg1, null) ? ReferenceEquals(cfg2, null) : cfg1.Equals(cfg2);
        }

        public static bool operator !=(Config cfg1, Config cfg2) {
            return !ReferenceEquals(cfg1, null) ? !ReferenceEquals(cfg2, null) : !cfg1.Equals(cfg2);
        }

        public bool Equals(Config compare) {
            return
                ClientId == compare.ClientId &&
                ClientSecret == compare.ClientSecret &&
                BotName == compare.BotName &&
                Token == compare.Token &&
                ServerName == compare.ServerName &&
                ChannelName == compare.ChannelName;
        }

        public override int GetHashCode() {
            return (ClientId + ClientSecret + BotName + Token + ServerName + ChannelName).GetHashCode();
        }
    }
}
