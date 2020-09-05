using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Windows.Forms;
using Mono.Options;

namespace OsuMissAnalyzer.UI
{
    public static class Program
    {
        public static bool headless = false;
        [STAThread]
        public static void Main(string[] args)
        {
            MissAnalyzer missAnalyzer;
            MissWindowController controller;
            MissWindow window;
            Debug.Print("Starting MissAnalyser... ");
            String replay = null, beatmap = null;
            List<string> extras;
            Dictionary<string, string> optList = new Dictionary<string, string>();
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

            Options options = new Options("options.cfg", optList);
            try
            {
                ReplayLoader replayLoader = new ReplayLoader();
                if (!replayLoader.Load(replay, beatmap)) return;
                if (replayLoader.Replay == null || replayLoader.Beatmap == null)
                {
                    ShowErrorDialog("Couldn't find " + (replayLoader.Replay == null ? "replay" : "beatmap"));
                    return;
                }

                missAnalyzer = new MissAnalyzer(replayLoader);
                controller = new MissWindowController(missAnalyzer, replayLoader);
                window = new MissWindow(controller);
                Application.Run(window);
            } catch (Exception e)
            {
                ShowErrorDialog(e.Message);
                File.WriteAllText("exception.log", e.ToString());
            }
        }
        public static void ShowErrorDialog(string message)
        {
            if (!headless) MessageBox.Show(message, "Error");
            else Debug.Print(message);
        }

    }
}