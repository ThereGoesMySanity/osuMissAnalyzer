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
    public class ServerSettings : IDisposable
    {
        [JsonProperty]
        [JsonIgnore]
        public bool Help = false;
        [JsonIgnore]
        public bool Link = false;
        [JsonIgnore]
        public bool Reload = false;
        [JsonIgnore]
        public string Apiv2Req = null;
        [JsonIgnore]
        public string GitCommit = null;

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
                OsuApi api2 = new OsuApi(ServerContext.webClient, OsuId, OsuSecret, OsuApiKey);
                Console.WriteLine(await api2.GetApiv2(Apiv2Req));
                return false;
            }
            

            return true;
        }


        public void Dispose()
        {
            Save();
            GC.SuppressFinalize(this);
        }
    }
}