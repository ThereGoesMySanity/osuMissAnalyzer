namespace OsuMissAnalyzer.Server.Settings
{
    public class ServerOptions
    {
        public string ServerDir { get; set; } = "";
        public bool Test { get; set; } = false;
        public ulong DumpChannel { get; set; } = 753788360425734235L;
        public ulong TestGuild { get; set; } = 753465280465862757L;
    }
}