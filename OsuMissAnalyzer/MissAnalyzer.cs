using System;
using System.Drawing;
using osuDodgyMomentsFinder;
using ReplayAPI;
using BMAPI.v1;
using BMAPI;
using System.IO;
using System.Text;
using BMAPI.v1.HitObjects;
using System.Linq;
using Gtk;

namespace OsuMissAnalyzer
{
	public class MissAnalyzer : Gtk.DrawingArea
	{
		const int sliderGranularity = 10;
		const int size = 320;
		Options options;
		Bitmap img;
		Graphics g, gOut;
		ReplayAnalyzer re;
		Replay r;
		Beatmap b;
		Window w;
		int missNo;
		bool ring;

		[STAThread]
		public static void Main(string[] args)
		{
			Application.Init();

			MissAnalyzer m;
			if (args.Length > 0 && args[0].EndsWith(".osr"))
			{
				Console.WriteLine(args[0]);
				if (args.Length > 1 && args[1].EndsWith(".osu"))
				{
					m = new MissAnalyzer(args[0], args[1]);
				}
				else
				{
					m = new MissAnalyzer(args[0], null);
				}
			}
			else
			{
				if (args.Length > 1 && args[1].EndsWith(".osr"))
				{
					m = new MissAnalyzer(args[1], null);
				}
				else
				{
					m = new MissAnalyzer(null, null);
				}
			}
			m.w = new Window("Miss Analyzer");
			m.w.Add(m);
			m.w.Resizable = false;
			m.w.ActivateFocus();
			m.w.ShowAll();
			Application.Run();
		}
		public MissAnalyzer(string replayFile, string beatmap)
		{
			if (!File.Exists("options.cfg"))
			{
				File.Create("options.cfg");
			}
			options = new Options("options.cfg");
			if (options.Settings.ContainsKey("Size"))
			{
				int i = Convert.ToInt32(options.Settings["Size"]);
				SetSizeRequest(i, i + 40);
			}
			else
			{
				SetSizeRequest(size, size + 40);
			}
			img = new Bitmap(size, size);
			g = Graphics.FromImage(img);
			gOut = Graphics.FromHwnd(Handle);

			if (replayFile == null)
			{
				loadReplay();
				if (r == null)
				{
					Environment.Exit(1);
				}
			}
			else
			{
				r = new Replay(replayFile, true, false);
			}
			if (beatmap == null)
			{
				loadBeatmap();
				if (b == null)
				{
					Environment.Exit(1);
				}
			}
			else
			{
				b = new Beatmap(beatmap);
			}
			re = new ReplayAnalyzer(b, r);

			if (re.misses.Count == 0)
			{
				Environment.Exit(1);
			}
			missNo = 0;
		}

		private void loadReplay()
		{
			using (FileChooserDialog fd = new FileChooserDialog("Choose replay file", w, FileChooserAction.Open))
			{
				fd.Filter = new FileFilter();
				fd.Filter.AddPattern("osu! replay files|*.osu");
				int d = fd.Run();
				if (d == DialogResult.OK)
				{
					r = new Replay(fd.FileName, true, false);
				}
			}
		}
		private void loadBeatmap()
		{
			b = getBeatmapFromHash(Directory.GetCurrentDirectory(), false);
			if (b == null && options.Settings.ContainsKey("SongsDir"))
			{
				b = getBeatmapFromHash(options.Settings["SongsDir"]);
			}
			if (b == null)
			{
				using (OpenFileDialog fd2 = new OpenFileDialog())
				{
					fd2.Title = "Choose beatmap";
					fd2.Filter = "osu! beatmaps (*.osu)|*.osu";
					DialogResult d2 = fd2.ShowDialog();
					if (d2 == DialogResult.OK)
					{
						b = new Beatmap(fd2.FileName);
					}
				}
			}
		}

		protected override bool OnKeyPressEvent(Gdk.EventKey e)
		{
			
			QueueDraw();
			switch (e.Key)
			{
				case Gdk.Key.Right:
					if (missNo == re.misses.Count - 1) break;
					missNo++;
					break;
				case Gdk.Key.Left:
					if (missNo == 0) break;
					missNo--;
					break;
				case Gdk.Key.T:
					ring = !ring;
					break;
				case Gdk.Key.P:
					for (int i = 0; i < re.misses.Count; i++)
					{
						drawMiss(i);
						img.Save(r.Filename.Substring(r.Filename.LastIndexOf("\\") + 1,
													  r.Filename.Length - 5 - r.Filename.LastIndexOf("\\"))
								 + "." + i + ".png",
								 System.Drawing.Imaging.ImageFormat.Png);
					}
					break;
				case Gdk.Key.R:
					loadReplay();
					loadBeatmap();
					re = new ReplayAnalyzer(b, r);
					QueueDraw();
					missNo = 0;
					if (r == null || b == null)
					{
						Environment.Exit(1);
					}
					break;
			}
			return base.OnKeyPressEvent(e);
		}
		protected override void OnPaint(PaintEventArgs e)
		{
			base.Display
			gOut.DrawImage(drawMiss(missNo), 0, 0, size, size);
		}

		/// <summary>
		/// Draws the miss.
		/// </summary>
		/// <returns>A Bitmap containing the drawing</returns>
		/// <param name="missNum">Index of the miss as it shows up in r.misses.</param>
		private Bitmap drawMiss(int missNum)
		{
			//TODO: Add scale
			bool hr = r.Mods.HasFlag(Mods.HardRock);
			float radius = (float)re.misses[missNum].Radius;
			Pen circle = new Pen(Color.Gray, radius * 2);
			circle.EndCap = System.Drawing.Drawing2D.LineCap.Round;
			circle.LineJoin = System.Drawing.Drawing2D.LineJoin.Round;
			Pen p = new Pen(Color.White);
			g.FillRectangle(p.Brush, 0, 0, size, size);
			Rectangle bounds = new Rectangle((re.misses[missNum].Location - new Point2(size / 2, size / 2)).ToPoint(),
											 new Size(size, size));
			int i, j, y, z;
			for (i = r.ReplayFrames.Count(x => x.Time <= re.misses[missNum].StartTime);
				 i > 0 && bounds.Contains(r.ReplayFrames[i].Point); i--) { }
			for (j = r.ReplayFrames.Count(x => x.Time <= re.misses[missNum].StartTime);
				 j < r.ReplayFrames.Count - 1 && bounds.Contains(r.ReplayFrames[j].Point); j++) { }
			for (y = b.HitObjects.Count(x => x.StartTime <= re.misses[missNum].StartTime) - 1;
				 y >= 0 && bounds.Contains(b.HitObjects[y].Location.ToPoint()); y--) { }
			for (z = b.HitObjects.Count(x => x.StartTime <= re.misses[missNum].StartTime) - 1;
				 z < b.HitObjects.Count && bounds.Contains(b.HitObjects[z].Location.ToPoint()); z++) { }

			p.Color = Color.Gray;
			for (int q = z - 1; q > y; q--)
			{
				int c = Math.Min(255, 100 + (int)(Math.Abs(b.HitObjects[q].StartTime - re.misses[missNum].StartTime) / 10));
				if (c == 255) continue;
				if (b.HitObjects[q].Type == HitObjectType.Slider)
				{
					SliderObject slider = (SliderObject)b.HitObjects[q];
					Point[] pt = new Point[sliderGranularity];
					for (int x = 0; x < sliderGranularity; x++)
					{
						pt[x] = pSub(slider.PositionAtDistance
											   (x * 1f * slider.PixelLength / sliderGranularity).toPoint(),
											   (Size)bounds.Location, hr);
					}
					circle.Color = Color.LemonChiffon;
					g.DrawLines(circle, pt);
				}

				p.Color = Color.FromArgb(c == 100 ? c + 50 : c, c, c);
				if (ring)
				{
					g.DrawEllipse(p, new RectangleF(Point.Subtract(
										pSub(b.HitObjects[q].Location.ToPoint(), (Size)bounds.Location, hr),
										new SizeF(radius, radius).ToSize()), new SizeF(radius * 2, radius * 2)));
				}
				else
				{
					g.FillEllipse(p.Brush, new RectangleF(Point.Subtract(
										pSub(b.HitObjects[q].Location.ToPoint(), (Size)bounds.Location, hr),
										new SizeF(radius, radius).ToSize()), new SizeF(radius * 2, radius * 2)));
				}
			}

			for (int k = i; k < j - 1; k++)
			{
				Point p1 = pSub(r.ReplayFrames[k].Point, bounds.Location, hr);
				Point p2 = pSub(r.ReplayFrames[k + 1].Point, bounds.Location, hr);
				p.Color = getHitColor(b.OverallDifficulty, (int)(re.misses[missNum].StartTime - r.ReplayFrames[k].Time));	
				g.DrawLine(p, p1, p2);
				if (re.getKey(k == 0 ? ReplayAPI.Keys.None : r.ReplayFrames[k - 1].Keys, r.ReplayFrames[k].Keys) > 0)
				{
					g.DrawEllipse(p, new Rectangle(Point.Subtract(p1, new Size(3, 3)), new Size(6, 6)));
				}
			}

			p.Color = Color.Black;
			Font f = new Font(FontFamily.GenericSansSerif, 12);
			g.DrawString("Miss " + (missNum + 1) + " of " + re.misses.Count, f, p.Brush, 0, 0);
			TimeSpan ts = TimeSpan.FromMilliseconds(re.misses[missNum].StartTime);
			g.DrawString("Time: " + ts.ToString(@"mm\:ss\.fff"), f, p.Brush, 0, size - f.Height);
			return img;
		}
		/// <summary>
		/// Gets the hit window.
		/// </summary>
		/// <returns>The hit window in ms.</returns>
		/// <param name="od">OD of the map.</param>
		/// <param name="hit">Hit value (300, 100, or 50).</param>
		private static float getHitWindow(float od, int hit)
		{
			switch (hit)
			{
				case 300:
					return 79.5f - 6 * od;
				case 100:
					return 139.5f - 8 * od;
				case 50:
					return 199.5f - 10 * od;
				default:
					throw new ArgumentOutOfRangeException(nameof(hit), hit, "Hit value is not 300, 100, or 50");
			}
		}
		/// <summary>
		/// Gets the color associated with the hit window.
		/// Blue for 300s, green for 100s, purple for 50s.
		/// </summary>
		/// <returns>The hit color.</returns>
		/// <param name="od">OD of the map.</param>
		/// <param name="ms">Hit timing in ms (can be negative).</param>
		private static Color getHitColor(float od, int ms)
		{
			if (Math.Abs(ms) < getHitWindow(od, 300)) return Color.SkyBlue;
			if (Math.Abs(ms) < getHitWindow(od, 100)) return Color.SpringGreen;
			if (Math.Abs(ms) < getHitWindow(od, 50)) return Color.Purple;
			return Color.Black;
		}
		/// <summary>
		/// Returns a string representation of the given byte array in hexadecimal
		/// </summary>
		/// <returns>A string representation of the hexadecimal value of the given byte array</returns>
		/// <param name="bytes">The byte array to be converted.</param>
		/// <param name="upperCase">Whether or not to make the letter characters of the string uppercase.</param>
		public static string ToHex(byte[] bytes, bool upperCase)
		{
			StringBuilder result = new StringBuilder(bytes.Length * 2);

			for (int i = 0; i < bytes.Length; i++)
				result.Append(bytes[i].ToString(upperCase ? "X2" : "x2"));

			return result.ToString();
		}

		/// <summary>
		/// Flips point about the center of the screen if the Hard Rock mod is on, does nothing otherwise.
		/// </summary>
		/// <returns>A possibly-flipped pooint.</returns>
		/// <param name="p">The point to be flipped.</param>
		/// <param name="hr">Whether or not Hard Rock is on.</param>
		private Point flip(Point p, bool hr)
		{
			if (!hr) return p;
			p.Y = size - p.Y;
			return p;
		}

		/// <summary>
		/// Subtracts two points and flips them if hr is <c>true</c>.
		/// </summary>
		/// <returns>The difference between p1 and p2, possibly also flipped.</returns>
		/// <param name="p1">The first point.</param>
		/// <param name="p2">The point to be subtracted</param>
		/// <param name="hr">Whether or not Hard Rock is on.</param>
		private Point pSub(Point p1, Point p2, bool hr)
		{
			return pSub(p1, (Size)p2, hr);
		}
		/// <summary>
		/// Subtracts two points and flips them if hr is <c>true</c>.
		/// </summary>
		/// <returns>The difference between p1 and p2, possibly also flipped.</returns>
		/// <param name="p1">The first point.</param>
		/// <param name="p2">The size to be subtracted</param>
		/// <param name="hr">Whether or not Hard Rock is on.</param>
		private Point pSub(Point p1, Size p2, bool hr)
		{
			Point p = Point.Subtract(p1, p2);
			return flip(p, hr);
		}

		private Beatmap getBeatmapFromHash(string dir, bool recurse = true)
		{
			foreach (string s in Directory.GetFiles(dir, "*.osu",
											recurse ? SearchOption.AllDirectories : SearchOption.TopDirectoryOnly))
			{
				if (Beatmap.MD5FromFile(s) == r.MapHash)
				{
					return new Beatmap(s);
				}
			}
			return null;
		}
	}
}
