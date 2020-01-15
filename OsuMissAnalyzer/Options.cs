using System.Collections.Generic;
using System.IO;

namespace OsuMissAnalyzer
{
	public class Options
	{
		public Dictionary<string, string> Settings { get; private set; }
		public Options(string file)
		{
			Settings = new Dictionary<string, string>();
			using (StreamReader f = new StreamReader(file))
			{
				while (!f.EndOfStream)
				{
					string[] s = f.ReadLine().Trim().Split(new char[] { '=' }, 2);
					if(s[1].Length > 0) Settings.Add(s[0].ToLower(), s[1]);
				}
			}
		}
	}
}
