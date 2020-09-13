using System;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net;
using System.Threading;
using System.Windows.Forms;
using BMAPI.v1;
using Newtonsoft.Json.Linq;
using osuDodgyMomentsFinder;
using ReplayAPI;
using OsuMissAnalyzer.Core;
using OsuMissAnalyzer.UI.UI;

namespace OsuMissAnalyzer.UI
{
    public class UIReplayLoader : IReplayLoader
    {
        public Replay Replay { get; private set; }
        public Beatmap Beatmap { get; private set; }
        public ReplayAnalyzer ReplayAnalyzer { get; private set; }
        public Options Options { get; private set; }

        public UIReplayLoader(Options options)
        {
            Options = options;
        }

        public bool Load(string replayFile, string beatmapFile)
        {
            Debug.Print("Loading Replay file...");

            Replay = replayFile == null ? LoadReplay() : new Replay(replayFile);
            if (Replay == null)
                return false;

            Debug.Print("Loaded replay {0}", Replay.Filename);
            Debug.Print("Loading Beatmap file...");

            Beatmap = beatmapFile == null ? LoadBeatmap(Replay) : new Beatmap(beatmapFile);
            if (Beatmap == null)
                return false;

            Debug.Print("Loaded beatmap {0}", Beatmap.Filename);
            Debug.Print("Analyzing... ");
            Debug.Print(Replay.ReplayFrames.Count.ToString());

            ReplayAnalyzer = new ReplayAnalyzer(Beatmap, Replay);

            if (ReplayAnalyzer.misses.Count == 0)
            {
                return false;
            }
            return true;
        }

        public Replay LoadReplay()
        {
            Replay replay = null;
            var messageBox = new ReplayOptionBox(Options);
            if (messageBox.ShowDialog() == DialogResult.OK)
            {
                switch (messageBox.Result)
                {
                    case ReplayFind.RECENT:
                        var replays = new DirectoryInfo(Path.Combine(Options.Settings["osudir"], "Data", "r")).GetFiles()
                                .Concat(new DirectoryInfo(Path.Combine(Options.Settings["osudir"], "Replays")).GetFiles())
                                .Where(f => f.Name.EndsWith("osr"))
                                .OrderByDescending(f => f.LastWriteTime)
                                .Take(10).Select(file => new Replay(file.FullName))
                                .OrderByDescending(re => re.PlayTime)
                                .Select(re => new ReplayListItem() { replay = re, beatmap = LoadBeatmap(re, false) });
                        var replayListForm = new ListMessageBox();
                        replayListForm.SetContent(replays.ToList());
                        if (replayListForm.ShowDialog() == DialogResult.OK && replayListForm.GetResult() != null)
                        {
                            replay = replayListForm.GetResult().replay;
                        }
                        break;
                    case ReplayFind.BEATMAP:
                        var beatmapForm = new BeatmapSearchBox();
                        beatmapForm.SetContent(Options.Database);
                        if (beatmapForm.ShowDialog() == DialogResult.OK)
                        {
                            Beatmap b = beatmapForm.Result.Load(Options.SongsFolder);
                            var beatmapListForm = new ListMessageBox();
                            beatmapListForm.SetContent(Options.GetReplaysFromBeatmap(b.BeatmapHash).Select(r => new ReplayListItem {replay = r, beatmap = b}).ToList());
                            if (beatmapListForm.ShowDialog() == DialogResult.OK && beatmapListForm.GetResult() != null)
                            {
                                replay = beatmapListForm.GetResult().replay;
                            }
                        }
                        break;
                    case ReplayFind.MANUAL:
                        using (OpenFileDialog fd = new OpenFileDialog())
                        {
                            fd.Title = "Choose replay file";
                            fd.Filter = "osu! replay files (*.osr)|*.osr";
                            DialogResult result = fd.ShowDialog();
                            if (result == DialogResult.OK)
                            {
                                replay = new Replay(fd.FileName);
                            }
                        }
                        break;
                }
            }
            if (replay == null) Program.ShowErrorDialog("Couldn't find replay");
            return replay;
        }

        public Beatmap LoadBeatmap(Replay replay, bool dialog = true)
        {
            Beatmap beatmap = null;
            if (Options.Database != null)
            {
                beatmap = Options.GetBeatmapFromHash(replay.MapHash);
            }
            if (beatmap == null)
            {
                beatmap = GetBeatmapFromHash(Directory.GetCurrentDirectory(), false);
            }
            if (beatmap == null)
            {
                beatmap = GetBeatmapFromHash(Options.SongsFolder, true);
            }
            if (beatmap == null && dialog)
            {
                Program.ShowErrorDialog("Couldn't find beatmap automatically");
                using (OpenFileDialog fd = new OpenFileDialog())
                {
                    fd.Title = "Choose beatmap";
                    fd.Filter = "osu! beatmaps (*.osu)|*.osu";
                    DialogResult d = fd.ShowDialog();
                    if (d == DialogResult.OK)
                    {
                        beatmap = new Beatmap(fd.FileName);
                    }
                }
            }
            return beatmap;
        }

        private Beatmap GetBeatmapFromHash(string dir, bool isSongsDir)
        {
            Debug.Print("\nChecking API Key...");
            JArray j = JArray.Parse("[]");
            if (Options.Settings.ContainsKey("apikey"))
            {
                Debug.Print("Found API key, searching for beatmap...");

                using (WebClient w = new WebClient())
                {
                    j = JArray.Parse(w.DownloadString("https://osu.ppy.sh/api/get_beatmaps" +
                                                            "?k=" + Options.Settings["apikey"] +
                                                            "&h=" + Replay.MapHash));
                }
            }
            else
            {
                Debug.Print("No API key found, searching manually. It could take a while...");
                Thread t = new Thread(() =>
                               MessageBox.Show("No API key found, searching manually. It could take a while..."));
            }
            if (isSongsDir)
            {
                string[] folders;

                if (j.Count > 0) folders = Directory.GetDirectories(dir, j[0]["beatmapset_id"] + "*");
                else folders = Directory.GetDirectories(dir);

                foreach (string folder in folders)
                {
                    Beatmap map = ReadFolder(folder, j.Count > 0 ? (string)j[0]["beatmap_id"] : null);
                    if (map != null) return map;
                }
            }
            else
            {
                Beatmap map = ReadFolder(dir, j.Count > 0 ? (string)j[0]["beatmap_id"] : null);
                if (map != null) return map;
            }
            return null;
        }

        private Beatmap ReadFolder(string folder, string id)
        {
            foreach (string file in Directory.GetFiles(folder, "*.osu"))
            {
                using (StreamReader f = new StreamReader(file))
                {
                    string line = f.ReadLine();
                    if (line == null)
                        continue;
                    while (!f.EndOfStream
                           && !line.StartsWith("BeatmapID"))
                    {
                        line = f.ReadLine();
                    }
                    if (line.StartsWith("BeatmapID") && id != null)
                    {
                        if (line.Substring(10) == id)
                        {
                            return new Beatmap(file);
                        }
                    }
                    else
                    {
                        if (Replay.MapHash == Beatmap.MD5FromFile(file))
                        {
                            return new Beatmap(file);
                        }
                    }
                }
            }
            return null;
        }

    }
}