using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using BMAPI.v1;
using OsuDbAPI;
using OsuMissAnalyzer.Core;
using ReplayAPI;

namespace OsuMissAnalyzer.UI
{
	public class Options
	{
		public Dictionary<string, string> Settings { get; private set; }
		public OsuDbFile Database;
		public ScoresDb ScoresDb;
		public bool OsuDirAccessible { get; private set; }
        public string SongsFolder => Settings.ContainsKey("songsdir") ? Settings["songsdir"] : Path.Combine(Settings["osudir"], "Songs");
		public Options(string file, Dictionary<string, string> optList)
		{
			OsuDirAccessible = false;
			Settings = new Dictionary<string, string>();
			using (StreamReader f = new StreamReader(file))
			{
				while (!f.EndOfStream)
				{
					string[] s = f.ReadLine().Trim().Split(new char[] { '=' }, 2);
					AddOption(s[0].ToLower(), s[1]);
				}
			}
			foreach(var kv in optList)
			{
				AddOption(kv.Key, kv.Value);
			}
		}
		public BMAPI.v1.Beatmap GetBeatmapFromHash(string mapHash)
		{
			return Database.GetBeatmapFromHash(mapHash)?.Load(SongsFolder);
		}
		public BMAPI.v1.Beatmap GetBeatmapFromId(int mapId)
        {
			return Database.GetBeatmapFromId(mapId)?.Load(SongsFolder);
        }
		public List<Replay> GetReplaysFromBeatmap(string beatmapHash)
		{
			return ScoresDb.scores.GetValueOrDefault(beatmapHash, new Score[] { }).Select(s => new Replay(Path.Combine(Settings["osudir"], "Data", "r", s.filename))).ToList();
		}
		private void AddOption(string key, string value)
		{
			if (value.Length > 0) Settings.Add(key, value);
			if (key == "osudir"
				&& File.Exists(Path.Combine(value, "osu!.db"))
				&& File.Exists(Path.Combine(value, "scores.db"))
				&& Directory.Exists(Path.Combine(value, "Data", "r")))
			{
				Database = new OsuDbFile(Path.Combine(value, "osu!.db"), byHash: true);
				ScoresDb = new ScoresDb(Path.Combine(value, "scores.db"));
				OsuDirAccessible = true;
			}
		}
	}
}
