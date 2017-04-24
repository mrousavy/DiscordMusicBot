using Newtonsoft.Json;
using System.IO;

namespace DiscordMusicBot {
    internal static class Information {
        internal static Config Config => JsonConvert.DeserializeObject<Config>(File.ReadAllText("config.json"));

        internal static string ClientId => Config.ClientId;
        internal static string ClientSecret => Config.ClientSecret;
        internal static string BotName => Config.BotName;
        internal static string Token => Config.Token;
        internal static string ServerName => Config.ServerName;
        internal static string TextChannelName => Config.TextChannelName;
        internal static string VoiceChannelName => Config.VoiceChannelName;
    }

    public class Config {
        public string ClientId = "YourClientID";
        public string ClientSecret = "YourClientSecret";
        public string BotName = "YourBotName";
        public string Token = "YourBotToken";
        public string ServerName = "TheServerYouWantToConnectTo";
        public string TextChannelName = "TheTextChannelYouWantToJoin";
        public string VoiceChannelName = "TheVoiceChannelYouWantToJoin";

        public static bool operator ==(Config cfg1, Config cfg2) {
            return ReferenceEquals(cfg1, null) ? ReferenceEquals(cfg2, null) : cfg1.Equals(cfg2);
        }

        public static bool operator !=(Config cfg1, Config cfg2) {
            return !ReferenceEquals(cfg1, null) ? !ReferenceEquals(cfg2, null) : !cfg1.Equals(cfg2);
        }

        public bool Equals(Config compare) {
            if (compare == null)
                return false;

            return
                ClientId == compare.ClientId &&
                ClientSecret == compare.ClientSecret &&
                BotName == compare.BotName &&
                Token == compare.Token &&
                ServerName == compare.ServerName &&
                TextChannelName == compare.TextChannelName &&
                VoiceChannelName == compare.VoiceChannelName;
        }

        public override bool Equals(object obj) {
            return Equals(obj as Config);
        }

        public override int GetHashCode() {
            return (ClientId + ClientSecret + BotName + Token + ServerName + TextChannelName + VoiceChannelName).GetHashCode();
        }
    }
}