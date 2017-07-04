using System;
using System.Drawing;
using System.Windows.Forms;
using osuDodgyMomentsFinder;
using ReplayAPI;
using BMAPI.v1;
using BMAPI;
using System.IO;
using System.Text;
using BMAPI.v1.HitObjects;
using System.Linq;

namespace OsuMissAnalyzer
{
	public class MissAnalyzer : Form
	{
		const int arrowLength = 4;
		const int sliderGranularity = 10;
		const int size = 320;
		const int maxTime = 1000;
		float scale;
		Options options;
		Bitmap img;
		Graphics g, gOut;
		ReplayAnalyzer re;
		Replay r;
		Beatmap b;
		int missNo;
		bool ring;

		[STAThread]
		public static void Main(string[] args)
		{
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
			Application.Run(m);
		}
		public MissAnalyzer(string replayFile, string beatmap)
		{
			if (!File.Exists("options.cfg"))
			{
				File.Create("options.cfg");
			}
			options = new Options("options.cfg");
			Text = "Miss Analyzer";
			if (options.Settings.ContainsKey("Size"))
			{
				int i = Convert.ToInt32(options.Settings["Size"]);
				Size = new Size(i, i + 40);
			}
			else
			{
				Size = new Size(size, size + 40);
			}
			img = new Bitmap(size, size);
			g = Graphics.FromImage(img);
			gOut = Graphics.FromHwnd(Handle);

			FormBorderStyle = FormBorderStyle.FixedSingle;
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
			scale = 1;
		}

		private void loadReplay()
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

		private void ScaleChange(int i)
		{
			scale += 0.1f * i;
		}

		protected override void OnMouseWheel(MouseEventArgs e)
		{
			base.OnMouseWheel(e);
			Invalidate();
			ScaleChange(Math.Sign(e.Delta));
		}

		protected override void OnKeyDown(KeyEventArgs e)
		{

			base.OnKeyDown(e);
			Invalidate();
			switch (e.KeyCode)
			{
				case System.Windows.Forms.Keys.Up:
					ScaleChange(1);
					break;
				case System.Windows.Forms.Keys.Down:
					ScaleChange(-1);
					break;
				case System.Windows.Forms.Keys.Right:
					if (missNo == re.misses.Count - 1) break;
					missNo++;
					break;
				case System.Windows.Forms.Keys.Left:
					if (missNo == 0) break;
					missNo--;
					break;
				case System.Windows.Forms.Keys.T:
					ring = !ring;
					break;
				case System.Windows.Forms.Keys.P:
					for (int i = 0; i < re.misses.Count; i++)
					{
						drawMiss(i);
						img.Save(r.Filename.Substring(r.Filename.LastIndexOf("\\") + 1,
													  r.Filename.Length - 5 - r.Filename.LastIndexOf("\\"))
								 + "." + i + ".png",
								 System.Drawing.Imaging.ImageFormat.Png);
					}
					break;
				case System.Windows.Forms.Keys.R:
					loadReplay();
					loadBeatmap();
					re = new ReplayAnalyzer(b, r);
					Invalidate();
					missNo = 0;
					if (r == null || b == null)
					{
						Environment.Exit(1);
					}
					break;
			}
		}
		protected override void OnPaint(PaintEventArgs e)
		{
			base.OnPaint(e);
			gOut.DrawImage(drawMiss(missNo), 0, 0, size, size);
		}

		/// <summary>
		/// Draws the miss.
		/// </summary>
		/// <returns>A Bitmap containing the drawing</returns>
		/// <param name="missNum">Index of the miss as it shows up in r.misses.</param>
		private Bitmap drawMiss(int missNum)
		{
			bool hr = r.Mods.HasFlag(Mods.HardRock);
			CircleObject miss = re.misses[missNum];
			float radius = (float)miss.Radius;
			Pen circle = new Pen(Color.Gray, radius * 2 / scale);
			circle.EndCap = System.Drawing.Drawing2D.LineCap.Round;
			circle.LineJoin = System.Drawing.Drawing2D.LineJoin.Round;
			Pen p = new Pen(Color.White);
			g.FillRectangle(p.Brush, 0, 0, size, size);
			RectangleF bounds = new RectangleF((miss.Location - new Point2(size * scale / 2, size * scale / 2)).ToPointF(),
											   new SizeF(size * scale, size * scale));
			
			int i, j, y, z;
			for (y = b.HitObjects.Count(x => x.StartTime <= miss.StartTime) - 1;
				 y >= 0 && bounds.Contains(b.HitObjects[y].Location.ToPointF())
				 && miss.StartTime - b.HitObjects[y].StartTime < maxTime; y--) { }
			for (z = b.HitObjects.Count(x => x.StartTime <= miss.StartTime) - 1;
				 z < b.HitObjects.Count && bounds.Contains(b.HitObjects[z].Location.ToPointF())
				 && b.HitObjects[z].StartTime - miss.StartTime < maxTime; z++) { }
			for (i = r.ReplayFrames.Count(x => x.Time <= b.HitObjects[y + 1].StartTime);
				 i > 0 && bounds.Contains(r.ReplayFrames[i].Point)
				 && miss.StartTime - r.ReplayFrames[i].Time < maxTime; i--) { }
			for (j = r.ReplayFrames.Count(x => x.Time <= b.HitObjects[z - 1].StartTime);
				 j < r.ReplayFrames.Count - 1 && bounds.Contains(r.ReplayFrames[j].Point)
				 && r.ReplayFrames[j].Time - miss.StartTime < maxTime; j++) { }
			p.Color = Color.Gray;
			for (int q = z - 1; q > y; q--)
			{
				int c = Math.Min(255, 100 + (int)(Math.Abs(b.HitObjects[q].StartTime - miss.StartTime) * 100 / maxTime));
				if (b.HitObjects[q].Type == HitObjectType.Slider)
				{
					SliderObject slider = (SliderObject)b.HitObjects[q];
					PointF[] pt = new PointF[sliderGranularity];
					for (int x = 0; x < sliderGranularity; x++)
					{
						pt[x] = ScaleToRect(
							pSub(slider.PositionAtDistance(x * 1f * slider.PixelLength / sliderGranularity).toPoint(),
								 bounds, hr), bounds);
					}
					circle.Color = Color.LemonChiffon;
					g.DrawLines(circle, pt);
				}

				p.Color = Color.FromArgb(c == 100 ? c + 50 : c, c, c);
				if (ring)
				{
					g.DrawEllipse(p, ScaleToRect(new RectangleF(PointF.Subtract(
						pSub(b.HitObjects[q].Location.ToPointF(), bounds, hr),
						new SizeF(radius, radius).ToSize()), new SizeF(radius * 2, radius * 2)), bounds));
				}
				else
				{
					g.FillEllipse(p.Brush, ScaleToRect(new RectangleF(PointF.Subtract(
						pSub(b.HitObjects[q].Location.ToPointF(), bounds, hr),
						new SizeF(radius, radius).ToSize()), new SizeF(radius * 2, radius * 2)), bounds));
				}
			}
			float distance = 10.0001f;
			for (int k = i; k < j; k++)
			{
				PointF p1 = pSub(r.ReplayFrames[k].Point, bounds, hr);
				PointF p2 = pSub(r.ReplayFrames[k + 1].Point, bounds, hr);
				p.Color = getHitColor(b.OverallDifficulty, (int)(miss.StartTime - r.ReplayFrames[k].Time));
				g.DrawLine(p, ScaleToRect(p1, bounds), ScaleToRect(p2, bounds));
				if (distance > 10 && Math.Abs(miss.StartTime - r.ReplayFrames[k+1].Time) > 50)
				{
					Point2 v1 = new Point2(p1.X - p2.X, p1.Y - p2.Y);
					v1.Normalize();
					v1 *= (float)(Math.Sqrt(2) * arrowLength / 2);
					PointF p3 = PointF.Add(p2, new SizeF(v1.X + v1.Y, v1.Y - v1.X));
					PointF p4 = PointF.Add(p2, new SizeF(v1.X - v1.Y, v1.X + v1.Y));
					g.DrawLine(p, ScaleToRect(p2, bounds), ScaleToRect(p3, bounds));
					g.DrawLine(p, ScaleToRect(p2, bounds), ScaleToRect(p4, bounds));
					distance = 0;
				}
				else
				{
					distance += (float)Math.Sqrt((p1.X + p2.X) * (p1.X + p2.X) + (p1.Y * p2.Y) * (p1.Y * p2.Y));
				}
				if (re.getKey(k == 0 ? ReplayAPI.Keys.None : r.ReplayFrames[k - 1].Keys, r.ReplayFrames[k].Keys) > 0)
				{
					g.DrawEllipse(p, ScaleToRect(new RectangleF(PointF.Subtract(p1, new Size(3, 3)), new Size(6, 6)),
												 bounds));
				}
			}

			p.Color = Color.Black;
			Font f = new Font(FontFamily.GenericSansSerif, 12);
			g.DrawString("Miss " + (missNum + 1) + " of " + re.misses.Count, f, p.Brush, 0, 0);
			TimeSpan ts = TimeSpan.FromMilliseconds(miss.StartTime);
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
		/// <param name="s">The height of the rectangle it's being flipped in</param>
		/// <param name="hr">Whether or not Hard Rock is on.</param>
		private PointF flip(PointF p, float s, bool hr)
		{
			if (!hr) return p;
			p.Y = s - p.Y;
			return p;
		}
		/// <summary>
		/// Changes origin to top right of rect and flips p if hr is <c>true</c>.
		/// </summary>
		/// <returns>A point relative to rect</returns>
		/// <param name="p1">The point.</param>
		/// <param name="rect">The bounding rectangle to subtract from</param>
		/// <param name="hr">Whether or not Hard Rock is on.</param>
		private PointF pSub(PointF p1, RectangleF rect, bool hr)
		{
			PointF p = PointF.Subtract(p1, new SizeF(rect.Location));
			return flip(p, rect.Width, hr);
		}
		/// <summary>
		/// Scales point p by scale factor s.
		/// </summary>
		/// <returns>The scaled point.</returns>
		/// <param name="p">Point to be scaled.</param>
		/// <param name="s">Scale.</param>
		private PointF Scale(PointF p, float s)
		{
			return new PointF(p.X * s, p.Y * s);
		}
		private SizeF Scale(SizeF p, float s)
		{
			return new SizeF(p.Width * s, p.Height * s);
		}
		private RectangleF Scale(RectangleF rect, float s)
		{
			return new RectangleF(Scale(rect.Location, s), Scale(rect.Size, s));
		}
		private PointF ScaleToRect(PointF p, RectangleF rect, float sz = size)
		{
			return Scale(p, sz / rect.Width);
		}

		private RectangleF ScaleToRect(RectangleF p, RectangleF rect, float sz = size)
		{
			return new RectangleF(PointF.Subtract(ScaleToRect(PointF.Add(p.Location, Scale(p.Size, 0.5f)), rect),
												  Scale(p.Size, 0.5f * sz / rect.Width)),
								  Scale(p.Size, sz / rect.Width));
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
