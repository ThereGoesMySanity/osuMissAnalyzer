using System;
using System.Collections.Generic;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using DSharpPlus.Entities;
using Microsoft.Extensions.Hosting;
using Newtonsoft.Json;

namespace OsuMissAnalyzer.Server.Settings
{
    public class GuildManager : IHostedService
    {
        public Dictionary<ulong, GuildSettings> guilds { get; set; } = new Dictionary<ulong, GuildSettings>();
        public Task StartAsync(CancellationToken cancellationToken)
        {
            Load();
            return Task.CompletedTask;
        }

        public Task StopAsync(CancellationToken cancellationToken)
        {
            Save();
            return Task.CompletedTask;
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

        public void Load()
        {
            var file = Path.Combine(AppContext.BaseDirectory, "guildsettings.json");
            if (File.Exists(file)) 
                guilds = JsonConvert.DeserializeObject<Dictionary<ulong, GuildSettings>>(File.ReadAllText(file));
        }

        public void Save()
        {
            using (StreamWriter writer = File.CreateText(Path.Combine(AppContext.BaseDirectory, "guildsettings.json")))
            {
                JsonSerializer serializer = new JsonSerializer();
                serializer.Serialize(writer, guilds);
            }
        }
    }
}