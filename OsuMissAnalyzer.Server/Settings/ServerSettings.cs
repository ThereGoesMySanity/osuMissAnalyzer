using System;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;
using DSharpPlus.Entities;
using DSharpPlus.EventArgs;
using Mono.Options;
using Newtonsoft.Json;
using System.Reflection;
using System.Text;

namespace OsuMissAnalyzer.Server.Settings
{
    public class ServerSettings
    {
        [JsonProperty]
        public Dictionary<ulong, GuildSettings> guilds { get; set; } = new Dictionary<ulong, GuildSettings>();
        public string ServerDir = "";
        public string OsuId = "2558";
        public string OsuSecret = "";
        public string OsuApiKey = "";
        public string DiscordToken = "";
        public string WebHook = "";
        public string DiscordId = "752035690237394944";
        public string DiscordPermissions = "100416";
        [JsonIgnore]
        public bool Help = false;
        [JsonIgnore]
        public bool Link = false;
        public bool Test = false;
        [JsonIgnore]
        public bool Reload = false;
        [JsonIgnore]
        public string Apiv2Req = null;
        [JsonIgnore]
        public string GitCommit = null;

        public ulong DumpChannel = 753788360425734235L;
        public ulong TestGuild = 753465280465862757L;
        public async Task<bool> Init(string[] args)
        {
            var opts = new OptionSet() {
                {"d|dir=", "Set server storage dir (default: ./)", b => ServerDir = b},
                {"s|secret=", "Set client secret (osu!) (required)", s => OsuSecret = s},
                {"k|key=", "osu! api v1 key (required)", k => OsuApiKey = k},
                {"id=", "osu! client id (default: mine)", id => OsuId = id},
                {"t|token=", "discord bot token (required)", t => DiscordToken = t},
                {"h|help", "displays help", a => Help = a != null},
                {"l|link", "displays bot link and exits", l => Link = l != null},
                {"apiRequest=", "does api request", a => Apiv2Req = a},
                {"test", "test server only", t => Test = t != null},
                {"w|webhook=", "webhook for output", w => WebHook = w},
                {"reload", "reload databases", r => Reload = r != null},
            };
            opts.Parse(args);
            string botLink = $"https://discordapp.com/oauth2/authorize?client_id={DiscordId}&scope=bot&permissions={DiscordPermissions}";
            if (Link)
            {
                Console.WriteLine(botLink);
                return false;
            }

            if (Help || (OsuSecret.Length * OsuApiKey.Length * DiscordToken.Length) == 0)
            {
                Console.WriteLine(
$@"osu! Miss Analyzer, Discord Bot Edition
Bot link: https://discordapp.com/oauth2/authorize?client_id={DiscordId}&scope=bot&permissions={DiscordPermissions}");
                opts.WriteOptionDescriptions(Console.Out);
                return false;
            }
            if (Apiv2Req != null)
            {
                OsuApi api2 = new OsuApi(OsuId, OsuSecret, OsuApiKey);
                Console.WriteLine(await api2.GetApiv2(Apiv2Req));
                return false;
            }
            
            try
            {
                using (var stream = Assembly.GetEntryAssembly().GetManifestResourceStream("OsuMissAnalyzer.Server.Resources.GitCommit.txt"))
                using (var streamReader = new StreamReader(stream, Encoding.UTF8))
                {
                    GitCommit = streamReader.ReadToEnd();
                }
            }
            catch (Exception) { return false; }

            return true;
        }

        public GuildSettings GetGuild(DiscordChannel channel)
        {
            if (!channel.GuildId.HasValue) return GuildSettings.Default;
            else return GetGuild(channel.GuildId.Value);
        }
        public GuildSettings GetGuild(ulong id)
        {
            if (!guilds.ContainsKey(id))
            {
                guilds[id] = new GuildSettings(id);
            }
            return guilds[id];
        }

        public static ServerSettings Load()
        {
            var file = Path.Combine(AppContext.BaseDirectory, "settings.json");
            if (File.Exists(file)) 
                return JsonConvert.DeserializeObject<ServerSettings>(File.ReadAllText(file));
            return new ServerSettings();
        }

        public void Save()
        {
            using (StreamWriter writer = File.CreateText(Path.Combine(AppContext.BaseDirectory, "settings.json")))
            {
                JsonSerializer serializer = new JsonSerializer();
                serializer.Serialize(writer, this);
            }
        }
    }
}