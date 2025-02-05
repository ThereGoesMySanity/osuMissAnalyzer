using System;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net;
using System.Threading;
using BMAPI.v1;
using Newtonsoft.Json.Linq;
using osuDodgyMomentsFinder;
using ReplayAPI;
using OsuMissAnalyzer.Core;
using Avalonia.Controls;
using System.Threading.Tasks;
using System.Collections.Generic;
using OsuMissAnalyzer.UI.ViewModels;
using OsuMissAnalyzer.UI.Views;
using OsuMissAnalyzer.UI.Models;
using System.Net.Http;
using Avalonia.Platform.Storage;

namespace OsuMissAnalyzer.UI
{
    public class UIReplayLoader : IReplayLoader
    {
        public Replay? Replay { get; set; }
        public Beatmap? Beatmap { get; set; }
        public ReplayAnalyzer? ReplayAnalyzer { get; private set; }
        public required Options Options { get; set; }
        public string? ReplayFile { get; set; }
        public string? BeatmapFile { get; set; }
        public event EventHandler? NewReplay;
        private FileSystemWatcher[]? fileSystemWatchers;

        public ColorScheme ColorScheme => ColorScheme.Parse(Options.Settings.GetValueOrDefault("colorscheme", "")) ?? ColorScheme.Default;

        public async Task<string?> Load()
        {
            Debug.Print("Loading Replay file...");

            Replay = ReplayFile == null ? await LoadReplay() : new Replay(ReplayFile);
            if (Replay == null)
                return "Couldn't find replay";

            Debug.Print("Loaded replay {0}", Replay.Filename);
            Debug.Print("Loading Beatmap file...");

            Beatmap ??= BeatmapFile == null ? await LoadBeatmap(Replay) : new Beatmap(BeatmapFile);
            if (Beatmap == null)
                return "Couldn't find beatmap";

            Debug.Print("Loaded beatmap {0}", Beatmap.Filename);
            Debug.Print("Analyzing... ");

            ReplayAnalyzer = new ReplayAnalyzer(Beatmap, Replay);

            if (ReplayAnalyzer.misses.Count == 0)
            {
                return "No misses found";
            }
            return null;
        }

        private string osuReplaysDir => Path.Combine(Options.Settings["osudir"], "Data", "r");
        private string userReplaysDir => Path.Combine(Options.Settings["osudir"], "Replays");

        private IEnumerable<FileInfo> LocalReplaysList
        {
            get
            {
                IEnumerable<FileInfo> files = new DirectoryInfo(osuReplaysDir).GetFiles();
                var replayDir = new DirectoryInfo(userReplaysDir);
                if (replayDir.Exists)
                    files = files.Concat(replayDir.GetFiles());

                return files;
            }
        }

        public async Task<Replay?> LoadReplay()
        {
            Replay? replay = null;
            var messageBox = new ReplayOptionBox
            {
                DataContext = new ReplayOptionBoxViewModel(Options)
            };

            bool skipLoadDialog = Options.WatchDogMode;
            if (skipLoadDialog || await messageBox.ShowDialog<bool>(App.Window))
            {
                var userResult = skipLoadDialog ? ReplayFind.WATCHDOG : messageBox.Result;
                switch (userResult)
                {
                    case ReplayFind.RECENT:
                    case ReplayFind.WATCHDOG:
                        var files = LocalReplaysList;
                        var replaysEnumerable = files.Where(f => f.Name.EndsWith("osr"))
                            .OrderByDescending(f => f.LastWriteTime)
                            .Select(file => new Replay(file.FullName))
                            .Where(re => re.GameMode == GameModes.osu)
                            .Take(10);
                        if (userResult != ReplayFind.WATCHDOG)
                            replaysEnumerable = replaysEnumerable.OrderByDescending(re => re.PlayTime);

                        var replays = (await Task.WhenAll(replaysEnumerable
                                    .Select(async re =>
                                    {
                                        var map = await LoadBeatmap(re, false);
                                        return map is not null?
                                                new ReplayListItem { Replay = re, Beatmap = map }
                                                : null;
                                    })))
                                    .Where(re => re != null).Select(re => re!)
                                    .ToList();
                        var replayListForm = new ListMessageBox
                        {
                            DataContext = new ListMessageBoxViewModel
                            {
                                Items = replays,
                            },
                        };
                        if (userResult == ReplayFind.WATCHDOG && replays.Count > 0)
                        {
                            replay = replays[0].Replay;
                            Beatmap = replays[0].Beatmap;
                        }
                        else if (await replayListForm.ShowDialog<bool>(App.Window))
                        {
                            replay = replayListForm.Result.Replay;
                            Beatmap = replayListForm.Result.Beatmap;
                        }
                        break;
                    case ReplayFind.BEATMAP:
                        var beatmapForm = new BeatmapSearchBox
                        {
                            DataContext = new BeatmapSearchBoxViewModel(Options)
                        };
                        if (await beatmapForm.ShowDialog<bool>(App.Window))
                        {
                            Beatmap b = beatmapForm.Result.Load(Options.SongsFolder);
                            var beatmapListForm = new ListMessageBox
                            {
                                DataContext = new ListMessageBoxViewModel
                                {
                                    Items = Options.GetReplaysFromBeatmap(b.BeatmapHash).Select(r => new ReplayListItem { Replay = r, Beatmap = b }).ToList(),
                                }
                            };
                            if (await beatmapListForm.ShowDialog<bool>(App.Window))
                            {
                                replay = beatmapListForm.Result.Replay;
                                Beatmap = beatmapListForm.Result.Beatmap;
                            }
                        }
                        break;
                    case ReplayFind.MANUAL:
                        var result = await App.Window.StorageProvider.OpenFilePickerAsync(new FilePickerOpenOptions()
                        {
                            Title = "Choose replay file",
                            FileTypeFilter = [new FilePickerFileType("osu! replay files (*.osr)") { Patterns = ["*.osr"] }],
                            AllowMultiple = false
                        });
                        if (result != null && result.Count > 0)
                        {
                            replay = new Replay(result[0].TryGetLocalPath());
                        }
                        break;
                }
            }
            return replay;
        }

        public async Task<Beatmap?> LoadBeatmap(Replay replay, bool dialog = true)
        {
            Beatmap? beatmap = null;
            try
            {
                if (Options.OsuDirAccessible)
                {
                    beatmap = Options.GetBeatmapFromHash(replay.MapHash);
                }
                //if (beatmap == null)
                else
                {
                    beatmap = await GetBeatmapFromHash(Directory.GetCurrentDirectory(), false);
                    if (Options.SongsFolder != null) beatmap ??= await GetBeatmapFromHash(Options.SongsFolder, true);
                }
            }
            catch (Exception)
            {
            }
            if (beatmap == null && dialog)
            {
                await App.ShowMessageBox("Couldn't find beatmap automatically");
                var result = await App.Window.StorageProvider.OpenFilePickerAsync(new FilePickerOpenOptions()
                {
                    Title = "Choose beatmap",
                    FileTypeFilter = [new FilePickerFileType("osu! beatmap files") { Patterns = ["*.osu"] }],
                    AllowMultiple = false
                });
                if (result != null && result.Count > 0)
                {
                    beatmap = new Beatmap(result[0].TryGetLocalPath());
                    if (beatmap.BeatmapHash != replay.MapHash)
                    {
                        string? dir = Path.GetDirectoryName(result[0].TryGetLocalPath());
                        if (dir is not null) beatmap = ReadFolder(dir, null);
                    }
                }
            }
            return beatmap;
        }

        private async Task<Beatmap?> GetBeatmapFromHash(string dir, bool isSongsDir)
        {
            Debug.Print("\nChecking API Key...");
            JArray j = [];
            if (Options.Settings.TryGetValue("apikey", out string? apikey))
            {
                Debug.Print("Found API key, searching for beatmap...");
                using HttpClient http = new();
                j = JArray.Parse(await (await http.GetAsync("https://osu.ppy.sh/api/get_beatmaps" +
                                                        "?k=" + apikey +
                                                        "&h=" + Replay!.MapHash)).Content.ReadAsStringAsync());
            }
            else if(isSongsDir)
            {
                Debug.Print("No API key found, searching manually. It could take a while...");
                _ = App.ShowMessageBox("No API key found, searching manually.\nIt could take a while...");
            }
            return await Task.Run(() =>
            {
                if (isSongsDir)
                {
                    string[] folders;

                    if (j.Count > 0) folders = Directory.GetDirectories(dir, j[0]["beatmapset_id"] + "*");
                    else folders = Directory.GetDirectories(dir);

                    foreach (string folder in folders)
                    {
                        Beatmap? map = ReadFolder(folder, j.Count > 0 ? (string?)j[0]["beatmap_id"] : null);
                        if (map != null) return map;
                    }
                }
                else
                {
                    Beatmap? map = ReadFolder(dir, j.Count > 0 ? (string?)j[0]["beatmap_id"] : null);
                    if (map != null) return map;
                }
                return null;
            });
        }

        private Beatmap? ReadFolder(string folder, string? id)
        {
            foreach (string file in Directory.GetFiles(folder, "*.osu"))
            {
                if (id != null)
                {
                    using StreamReader f = new(file);
                    while (!f.EndOfStream)
                    {
                        var line = f.ReadLine();
                        if (line is not null
                            && line.StartsWith("BeatmapID")
                            && line[10..] == id)
                        {
                            return new Beatmap(file);
                        }
                    }
                }
                else if (Replay!.MapHash == Beatmap.MD5FromFile(file))
                {
                    return new Beatmap(file);
                }
            }
            return null;
        }

        public void WatchForNewReplays()
        {
            if (fileSystemWatchers != null)
                return;
            fileSystemWatchers = [new FileSystemWatcher(osuReplaysDir), new FileSystemWatcher(userReplaysDir)];
            foreach (var fileSystemWatcher in fileSystemWatchers)
            {
                fileSystemWatcher.Filter = "*.osr";
                fileSystemWatcher.Created += FileSystemWatcherOnCreated;
                fileSystemWatcher.Changed += FileSystemWatcherOnCreated;
                fileSystemWatcher.EnableRaisingEvents = true;
            }
        }

        public void StopWatchingForNewReplays()
        {
            if (fileSystemWatchers == null)
                return;

            foreach (var fileSystemWatcher in fileSystemWatchers)
            {
                fileSystemWatcher.EnableRaisingEvents = false;
                fileSystemWatcher.Dispose();
            }

            fileSystemWatchers = null;
        }

        private void FileSystemWatcherOnCreated(object sender, FileSystemEventArgs e)
        {
            OnNewReplay();
        }

        protected virtual void OnNewReplay()
        {
            NewReplay?.Invoke(this, EventArgs.Empty);
        }
    }
}