using System;
using System.Collections.Generic;
using System.IO;
using OsuMissAnalyzer.Core;

namespace OsuMissAnalyzer.UI
{
	public class Options
	{
		public static Options Opts { get; set; }
		public Dictionary<string, string> Settings { get; private set; }
		public OsuDatabase Database;
		public Options(string file, Dictionary<string, string> optList)
		{
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
			Opts = this;
		}
		private void AddOption(string key, string value)
		{
			if (value.Length > 0) Settings.Add(key, value);
			if (key == "osudir")
			{
				Database = new OsuDatabase(this);
			}
		}
	}
}
