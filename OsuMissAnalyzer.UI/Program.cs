using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using Avalonia;
using Avalonia.ReactiveUI;
using Mono.Options;

namespace OsuMissAnalyzer.UI
{
    class Program
    {
        public static bool headless = false;
        // Initialization code. Don't use any Avalonia, third-party APIs or any
        // SynchronizationContext-reliant code before AppMain is called: things aren't initialized
        // yet and stuff might break.
        public static void Main(string[] args)
        {
            Debug.Print("Starting MissAnalyser... ");
            Directory.SetCurrentDirectory(AppDomain.CurrentDomain.BaseDirectory);
            string replay = null, beatmap = null;
            List<string> extras;
            var optList = new Dictionary<string, string>();
            bool help = false;
            int getMiss = -1;
            string optionsFile = "options.cfg";

            var opts = new OptionSet() {
                { "o|osudir=", "Set osu! directory", o => optList["osudir"] = o},
                { "c|config=", "Set options.cfg", f => optionsFile = f},
                { "s|songsdir=", "Set songs directory", s => optList["songsdir"] = s},
                { "w|watchdogmode=", "watch and automatically load newest replays, requires osudir to be set", s => optList["watchdogmode"] = s},
                { "d|daemon", "Run without dialogs", d => headless = d != null},
                { "h|help", "Displays help", h => help = h != null},
                { "m|miss=", "Export miss #", (int m) => getMiss = m}
            };
            extras = opts.Parse(args);
            foreach (var arg in extras)
            {
                if (arg.EndsWith(".osu") && File.Exists(arg)) beatmap = arg;
                if (arg.EndsWith(".osr") && File.Exists(arg)) replay = arg;
            }
            if (!File.Exists(optionsFile))
            {
                File.Create(optionsFile).Close();
                Debug.Print("\nCreating options.cfg... ");
                Debug.Print("- In options.cfg, you can define various settings that impact the program. ");
                Debug.Print("- To add these to options.cfg, add a new line formatted <Setting Name>=<Value> ");
                Debug.Print("- Available settings : SongsDir | Value = Specify osu!'s songs dir.");
                Debug.Print("-                       APIKey  | Value = Your osu! API key (https://osu.ppy.sh/api/");
                Debug.Print("-                       OsuDir  | Value = Your osu! directory");
                Debug.Print("-                 WatchDogMode  | Value = true or false");
            }
            if (help)
            {
                Console.WriteLine("osu! Miss Analyzer");
                opts.WriteOptionDescriptions(Console.Out);
                return;
            }
            Options options = new Options("options.cfg", optList);
            UIReplayLoader replayLoader = new UIReplayLoader
            {
                Options = options,
                ReplayFile = replay,
                BeatmapFile = beatmap,
            };
            BuildAvaloniaApp(replayLoader).StartWithClassicDesktopLifetime(new string[] { });
        }

        // Avalonia configuration, don't remove; also used by visual designer.
        public static AppBuilder BuildAvaloniaApp(UIReplayLoader replayLoader)
            => AppBuilder.Configure<App>(() => new App(replayLoader))
                .UsePlatformDetect()
                .UseReactiveUI()
                .LogToTrace();
    }
}
