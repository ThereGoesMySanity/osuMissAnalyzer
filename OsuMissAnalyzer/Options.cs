using System;
using System.Collections.Generic;
using System.IO;
using System.Windows.Forms;

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
					string s = f.ReadLine();
					Settings.Add(s.Split(new char[]{ '=' }, 2)[0], s.Split(new char[] { '=' }, 2)[1]);
				}
			}
		}
	}
}
