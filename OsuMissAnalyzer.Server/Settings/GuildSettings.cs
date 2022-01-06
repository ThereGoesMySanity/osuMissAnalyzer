using System.Collections.Generic;
using System.Linq;
using System.Reflection;

namespace OsuMissAnalyzer.Server.Settings
{
    public class GuildSettings
    {
        public static GuildSettings Default = new GuildSettings(0);
        public ulong Id { get; private set; }
        public bool Compact { get; set; } = false;
        public bool Tracking { get; set; } = false;
        public bool AutoResponses { get; set; } = true;
        public int MaxButtons { get; set; } = 10;
        public GuildSettings(ulong guildId)
        {
            Id = guildId;
        }
        public Dictionary<string, string> GetSettings()
        {
            return this.GetType().GetProperties().Where(p => p.SetMethod.IsPublic)
                .ToDictionary(p => p.Name.ToLower(), p => p.GetValue(this).ToString());
        }
        public bool SetSetting(string setting, string value)
        {
            var property = this.GetType().GetProperty(setting, BindingFlags.IgnoreCase | BindingFlags.Public | BindingFlags.Instance);
            if (property != null)
            {
                var parsedValue = property.PropertyType == typeof(string)? value
                            : property.PropertyType.GetMethod("Parse", new[] {typeof(string)}).Invoke(null, new object[]{value});
                property.SetValue(this, parsedValue);
                return true;
            }
            return false;
        }
    }
}