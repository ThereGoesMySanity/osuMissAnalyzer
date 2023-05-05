using SixLabors.ImageSharp;

namespace OsuMissAnalyzer.Server.Settings
{
    public class ServerOptions
    {
        public string ServerDir { get; set; }
        public bool Test { get; set; }
        public ulong DumpChannel { get; set; }
        public ulong TestGuild { get; set; }
        public int Size { get; set; }

        public Rectangle Area => new Rectangle(0, 0, Size, Size);
        public string HelpMessage { get; set; } = @"osu! Miss Analyzer bot
```
Usage:
  /user {recent|top} <username> [<index>]
    Finds #index recent/top play for username (index defaults to 1)
  /beatmap <beatmap id/beatmap link> [<index>]
    Finds #index score on beatmap (index defaults to 1)

Automatically responds to >rs from owo bot if the replay is saved online
Automatically responds to uploaded replay files
Click ""Add to Server"" on the bot's profile to get this bot in your server!
DM ThereGoesMySanity#2622 if you need help
```
Full readme and source at https://github.com/ThereGoesMySanity/osuMissAnalyzer/tree/missAnalyzer/OsuMissAnalyzer.Server";
    }
}