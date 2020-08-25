using System;
using System.Diagnostics;
using System.IO;
using System.Windows.Forms;
using OsuMissAnalyzer.UI;

namespace OsuMissAnalyzer
{
    public static class Program
    {
        [STAThread]
        public static void Main(string[] args)
        {
            MissAnalyzer missAnalyzer;
            MissWindowController controller;
            MissWindow window;

            Debug.Print("Starting MissAnalyser... ");
            String replay = null, beatmap = null;
            if (args.Length > 0 && args[0].EndsWith(".osr"))
            {
                replay = args[0];
                Debug.Print("Found [{0}]", args[0]);
                if (args.Length > 1 && args[1].EndsWith(".osu"))
                {
                    Debug.Print("Found [{0}]", args[1]);
                    beatmap = args[1];
                }
            }
            else if (args.Length > 1 && args[1].EndsWith(".osr"))  //Necessary to support drag & drop
            {
                replay = args[1];
            }
            if (!File.Exists("options.cfg"))
            {
                File.Create("options.cfg").Close();
                Debug.Print("\nCreating options.cfg... ");
                Debug.Print("- In options.cfg, you can define various settings that impact the program. ");
                Debug.Print("- To add these to options.cfg, add a new line formatted <Setting Name>=<Value> ");
                Debug.Print("- Available settings : SongsDir | Value = Specify osu!'s songs dir.");
                Debug.Print("-                       APIKey  | Value = Your osu! API key (https://osu.ppy.sh/api/");
                Debug.Print("-                       OsuDir  | Value = Your osu! directory");
            }

            Options options = new Options("options.cfg");
            ReplayLoader replayLoader = new ReplayLoader();
            missAnalyzer = new MissAnalyzer(replayLoader);
            controller = new MissWindowController(missAnalyzer, replayLoader);
            window = new MissWindow(controller);
            controller.View = window;
            Application.Run(window);
        }

    }
}