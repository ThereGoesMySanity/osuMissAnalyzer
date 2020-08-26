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
using OsuMissAnalyzer.UI;
using ReplayAPI;

namespace OsuMissAnalyzer
{
    public class ReplayLoader
    {
        public Replay Replay { get; private set; }
        public Beatmap Beatmap { get; private set; }
        public ReplayAnalyzer ReplayAnalyzer { get; private set; }
        public bool Load(string replayFile, string beatmapFile)
        {
            Debug.Print("Loading Replay file...");

            Replay = replayFile == null ? LoadReplay() : new Replay(replayFile, true, false);
            if (Replay == null) return false;

            Debug.Print("Loaded replay {0}", Replay.Filename);
            Debug.Print("Loading Beatmap file...");

            Beatmap = beatmapFile == null ? LoadBeatmap(Replay) : new Beatmap(beatmapFile);
            if (Beatmap == null) return false;

            Debug.Print("Loaded beatmap {0}", Beatmap.Filename);
            Debug.Print("Analyzing... ");
            Debug.Print(Replay.ReplayFrames.Count.ToString());

            ReplayAnalyzer = new ReplayAnalyzer(Beatmap, Replay);

            if (ReplayAnalyzer.misses.Count == 0)
            {
                Debug.Print("There is no miss in this replay. ");
                Console.ReadLine();
                return false;
            }
            return true;
        }

        public Replay LoadReplay()
        {
            Replay r = null;
            if (Options.Opts.Settings.ContainsKey("osudir"))
            {
                if (MessageBox.Show("Analyze a recent replay?", "Miss Analyzer", MessageBoxButtons.YesNo, MessageBoxIcon.Question) == DialogResult.Yes)
                {
                    var replays = new DirectoryInfo(Path.Combine(Options.Opts.Settings["osudir"], "Data", "r")).GetFiles()
                            .Concat(new DirectoryInfo(Path.Combine(Options.Opts.Settings["osudir"], "Replays")).GetFiles())
                            .Where(f => f.Name.EndsWith("osr"))
                            .OrderByDescending(f => f.LastWriteTime)
                            .Take(5).Select(file => new Replay(file.FullName, true, false))
                            .OrderByDescending(re => re.PlayTime)
                            .Select(re => new ReplayListItem() { replay = re, beatmap = LoadBeatmap(re, false) });
                    var replayListForm = new ListMessageBox();
                    replayListForm.SetContent(replays.ToList());
                    if (replayListForm.ShowDialog() == DialogResult.OK && replayListForm.GetResult() != null)
                    {
                        r = replayListForm.GetResult().replay;
                    }
                }
            }
            if (r == null)
            {
                using (OpenFileDialog fd = new OpenFileDialog())
                {
                    fd.Title = "Choose replay file";
                    fd.Filter = "osu! replay files (*.osr)|*.osr";
                    DialogResult d = fd.ShowDialog();
                    if (d == DialogResult.OK)
                    {
                        r = new Replay(fd.FileName, true, false);
                    }
                }
            }
            return r;
        }

        public Beatmap LoadBeatmap(Replay r, bool dialog = true)
        {
            Beatmap b = null;
            if (Options.Opts.OsuDb != null)
            {
                b = Options.Opts.OsuDb.GetBeatmap(r.MapHash);
            }
            else
            {
                b = getBeatmapFromHash(Directory.GetCurrentDirectory(), false);
                if (b == null)
                {
                    if (Options.Opts.Settings.ContainsKey("songsdir"))
                    {
                        b = getBeatmapFromHash(Options.Opts.Settings["songsdir"]);
                    }
                    else if (Options.Opts.Settings.ContainsKey("osudir")
                      && File.Exists(Path.Combine(Options.Opts.Settings["osudir"], "Songs"))
                      )
                    {
                        b = getBeatmapFromHash(Path.Combine(Options.Opts.Settings["osudir"], "Songs"));
                    }
                    else if (dialog)
                    {
                        using (OpenFileDialog fd = new OpenFileDialog())
                        {
                            fd.Title = "Choose beatmap";
                            fd.Filter = "osu! beatmaps (*.osu)|*.osu";
                            DialogResult d = fd.ShowDialog();
                            if (d == DialogResult.OK)
                            {
                                b = new Beatmap(fd.FileName);
                            }
                        }
                    }
                }
            }
            return b;
        }

        private Beatmap getBeatmapFromHash(string dir, bool songsDir = true)
        {
            Debug.Print("\nChecking API Key...");
            JArray j = JArray.Parse("[]");
            if (Options.Opts.Settings.ContainsKey("apikey"))
            {
                Debug.Print("Found API key, searching for beatmap...");

                using (WebClient w = new WebClient())
                {
                    j = JArray.Parse(w.DownloadString("https://osu.ppy.sh/api/get_beatmaps" +
                                                            "?k=" + Options.Opts.Settings["apikey"] +
                                                            "&h=" + Replay.MapHash));
                }
            }
            else
            {
                Debug.Print("No API key found, searching manually. It could take a while...");
                Thread t = new Thread(() =>
                               MessageBox.Show("No API key found, searching manually. It could take a while..."));
            }
            if (songsDir)
            {
                string[] folders;

                if (j.Count > 0) folders = Directory.GetDirectories(dir, j[0]["beatmapset_id"] + "*");
                else folders = Directory.GetDirectories(dir);

                foreach (string folder in folders)
                {
                    Beatmap map = readFolder(folder, j.Count > 0 ? (string)j[0]["beatmap_id"] : null);
                    if (map != null) return map;
                }
            }
            else
            {
                Beatmap map = readFolder(dir, j.Count > 0 ? (string)j[0]["beatmap_id"] : null);
                if (map != null) return map;
            }
            return null;
        }

        private Beatmap readFolder(string folder, string id)
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