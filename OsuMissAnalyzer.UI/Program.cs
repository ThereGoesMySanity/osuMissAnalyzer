using System;
using Avalonia;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Logging.Serilog;
using Avalonia.ReactiveUI;

namespace OsuMissAnalyzer.UI
{
    class Program
    {
        public static bool headless = false;
        public static Dictionary<string, string> optList;
        // Initialization code. Don't use any Avalonia, third-party APIs or any
        // SynchronizationContext-reliant code before AppMain is called: things aren't initialized
        // yet and stuff might break.
        public static void Main(string[] args)
        {
            MissAnalyzer missAnalyzer;
            MissWindowController controller;
            MissWindow window;
            Debug.Print("Starting MissAnalyser... ");
            Directory.SetCurrentDirectory(AppDomain.CurrentDomain.BaseDirectory);
            string replay = null, beatmap = null;
            List<string> extras;
            optList = new Dictionary<string, string>();
            bool help = false;
            int getMiss = -1;
            string optionsFile = "options.cfg";

            var opts = new OptionSet() {
                { "o|osudir=", "Set osu! directory", o => optList["osudir"] = o},
                { "c|config=", "Set options.cfg", f => optionsFile = f},
                { "s|songsdir=", "Set songs directory", s => optList["songsdir"] = s},
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
            }
            if (help)
            {
                Console.WriteLine("osu! Miss Analyzer");
                opts.WriteOptionDescriptions(Console.Out);
                return;
            }
            BuildAvaloniaApp().StartWithClassicDesktopLifetime();
        }

        // Avalonia configuration, don't remove; also used by visual designer.
        public static AppBuilder BuildAvaloniaApp()
            => AppBuilder.Configure<App>()
                .UsePlatformDetect()
                .LogToDebug()
                .UseReactiveUI();
    }
}
