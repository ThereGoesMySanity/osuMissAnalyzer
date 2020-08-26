using System;
using System.Drawing;
using System.Windows.Forms;
using osuDodgyMomentsFinder;
using ReplayAPI;
using BMAPI.v1;
using BMAPI;
using System.IO;
using BMAPI.v1.HitObjects;
using System.Linq;
using Newtonsoft.Json.Linq;
using System.Net;
using System.Diagnostics;
using System.Threading;
using OsuMissAnalyzer.Utils;
using static OsuMissAnalyzer.Utils.MathUtils;
using OsuMissAnalyzer.UI;
using System.Collections;
using System.Collections.Generic;

namespace OsuMissAnalyzer
{
    public class MissAnalyzer
    {
        private const int maxTime = 1000;
        private const int arrowLength = 4;
        private const int sliderGranularity = 10;
        public bool HitCircleOutlines { get; private set; } = false;
        private ReplayLoader ReplayLoader;
        private ReplayAnalyzer ReplayAnalyzer => ReplayLoader.ReplayAnalyzer;
        private Replay Replay => ReplayLoader.Replay;
        private Beatmap Beatmap => ReplayLoader.Beatmap;
        private int hitObjIndex = 0;
        private bool drawAllHitObjects;
        private float scale = 1;



        public MissAnalyzer(ReplayLoader replayLoader)
        {
            ReplayLoader = replayLoader;
        }

        public void ToggleOutlines()
        {
            HitCircleOutlines = !HitCircleOutlines;
        }

        public void ToggleDrawAllHitObjects()
        {
            if (drawAllHitObjects)
            {
                drawAllHitObjects = false;
                hitObjIndex = ReplayAnalyzer.misses.Count(x => x.StartTime < Beatmap.HitObjects[hitObjIndex].StartTime);
            }
            else
            {
                drawAllHitObjects = true;
                hitObjIndex = Beatmap.HitObjects.IndexOf(ReplayAnalyzer.misses[hitObjIndex]);
            }
        }

        public void NextObject()
        {
            if (drawAllHitObjects ? hitObjIndex < Beatmap.HitObjects.Count - 1 : hitObjIndex < ReplayAnalyzer.misses.Count - 1) hitObjIndex++;
        }
        public void PreviousObject()
        {
            if (hitObjIndex > 0) hitObjIndex--;
        }

        public void ScaleChange(int i)
        {
            scale += 0.1f * i;
            if (scale < 0.1) scale = 0.1f;
        }

        public Bitmap DrawSelectedHitObject(Rectangle area) { return DrawHitObject(hitObjIndex, area); }
        /// <summary>
        /// Draws the miss.
        /// </summary>
        /// <returns>A Bitmap containing the drawing</returns>
        /// <param name="num">Index of the miss as it shows up in r.misses.</param>
        private Bitmap DrawHitObject(int num, Rectangle area)
        {
            Bitmap img = new Bitmap(area.Width, area.Height);
            Graphics g = Graphics.FromImage(img);

            bool hr = Replay.Mods.HasFlag(Mods.HardRock);
            CircleObject hitObject;
            if (drawAllHitObjects) hitObject = Beatmap.HitObjects[num];
            else hitObject = ReplayAnalyzer.misses[num];
            float radius = (float)hitObject.Radius;
            Pen circle = new Pen(Color.Gray, radius * 2 / scale);
            circle.StartCap = System.Drawing.Drawing2D.LineCap.Round;
            circle.EndCap = System.Drawing.Drawing2D.LineCap.Round;
            circle.LineJoin = System.Drawing.Drawing2D.LineJoin.Round;
            Pen p = new Pen(Color.White);
            g.FillRectangle(p.Brush, area);
            RectangleF bounds = new RectangleF(PointF.Subtract(hitObject.Location.ToPointF(), MathUtils.Scale(area.Size, scale / 2)),
                MathUtils.Scale(area.Size, scale));

            int i, j, y, z;
            for (y = Beatmap.HitObjects.Count(x => x.StartTime <= hitObject.StartTime) - 1;
                y >= 0 && bounds.Contains(Beatmap.HitObjects[y].Location.ToPointF())
                && hitObject.StartTime - Beatmap.HitObjects[y].StartTime < maxTime;
                y--)
            {
            }
            for (z = Beatmap.HitObjects.Count(x => x.StartTime <= hitObject.StartTime) - 1;
                z < Beatmap.HitObjects.Count && bounds.Contains(Beatmap.HitObjects[z].Location.ToPointF())
                && Beatmap.HitObjects[z].StartTime - hitObject.StartTime < maxTime;
                z++)
            {
            }
            for (i = Replay.ReplayFrames.Count(x => x.Time <= Beatmap.HitObjects[y + 1].StartTime);
                i > 0 && bounds.Contains(Replay.ReplayFrames[i].GetPointF())
                && hitObject.StartTime - Replay.ReplayFrames[i].Time < maxTime;
                i--)
            {
            }
            for (j = Replay.ReplayFrames.Count(x => x.Time <= Beatmap.HitObjects[z - 1].StartTime);
                j < Replay.ReplayFrames.Count - 1 && bounds.Contains(Replay.ReplayFrames[j].GetPointF())
                && Replay.ReplayFrames[j].Time - hitObject.StartTime < maxTime;
                j++)
            {
            }
            p.Color = Color.Gray;
            for (int q = z - 1; q > y; q--)
            {
                int c = Math.Min(255, 100 + (int)(Math.Abs(Beatmap.HitObjects[q].StartTime - hitObject.StartTime) * 100 / maxTime));
                if (Beatmap.HitObjects[q].Type == HitObjectType.Slider)
                {
                    SliderObject slider = (SliderObject)Beatmap.HitObjects[q];
                    PointF[] pt = new PointF[sliderGranularity];
                    for (int x = 0; x < sliderGranularity; x++)
                    {
                        pt[x] = ScaleToRect(
                            pSub(slider.PositionAtDistance(x * 1f * slider.PixelLength / sliderGranularity).ToPoint(),
                                bounds, hr), bounds, area);
                    }
                    circle.Color = Color.LemonChiffon;
                    g.DrawLines(circle, pt);
                }

                p.Color = Color.FromArgb(c == 100 ? c + 50 : c, c, c);
                if (HitCircleOutlines)
                {
                    g.DrawEllipse(p, ScaleToRect(new RectangleF(PointF.Subtract(
                        pSub(Beatmap.HitObjects[q].Location.ToPointF(), bounds, hr),
                        new SizeF(radius, radius).ToSize()), new SizeF(radius * 2, radius * 2)), bounds, area));
                }
                else
                {
                    g.FillEllipse(p.Brush, ScaleToRect(new RectangleF(PointF.Subtract(
                        pSub(Beatmap.HitObjects[q].Location.ToPointF(), bounds, hr),
                        new SizeF(radius, radius).ToSize()), new SizeF(radius * 2, radius * 2)), bounds, area));
                }
            }
            float distance = 10.0001f;
            for (int k = i; k < j; k++)
            {
                PointF p1 = pSub(Replay.ReplayFrames[k].GetPointF(), bounds, hr);
                PointF p2 = pSub(Replay.ReplayFrames[k + 1].GetPointF(), bounds, hr);
                p.Color = GetHitColor(Beatmap.OverallDifficulty, (int)(hitObject.StartTime - Replay.ReplayFrames[k].Time));
                g.DrawLine(p, ScaleToRect(p1, bounds, area), ScaleToRect(p2, bounds, area));
                if (distance > 10 && Math.Abs(hitObject.StartTime - Replay.ReplayFrames[k + 1].Time) > 50)
                {
                    Point2 v1 = new Point2(p1.X - p2.X, p1.Y - p2.Y);
                    if (v1.Length > 0)
                    {
                        v1.Normalize();
                        v1 *= (float)(Math.Sqrt(2) * arrowLength / 2);
                        PointF p3 = PointF.Add(p2, new SizeF(v1.X + v1.Y, v1.Y - v1.X));
                        PointF p4 = PointF.Add(p2, new SizeF(v1.X - v1.Y, v1.X + v1.Y));
                        p2 = ScaleToRect(p2, bounds, area);
                        p3 = ScaleToRect(p3, bounds, area);
                        p4 = ScaleToRect(p4, bounds, area);
                        g.DrawLine(p, p2, p3);
                        g.DrawLine(p, p2, p4);
                    }
                    distance = 0;
                }
                else
                {
                    distance += new Point2(p1.X - p2.X, p1.Y - p2.Y).Length;
                }
                if (ReplayAnalyzer.getKey(k == 0 ? ReplayAPI.Keys.None : Replay.ReplayFrames[k - 1].Keys, Replay.ReplayFrames[k].Keys) > 0)
                {
                    g.DrawEllipse(p, ScaleToRect(new RectangleF(PointF.Subtract(p1, new Size(3, 3)), new Size(6, 6)),
                        bounds, area));
                }
            }

            p.Color = Color.Black;
            Font f = new Font(FontFamily.GenericSansSerif, 12);
            g.DrawString(Beatmap.ToString(), f, p.Brush, 0, 0);
            if (drawAllHitObjects) g.DrawString("Object " + (num + 1) + " of " + Beatmap.HitObjects.Count, f, p.Brush, 0, f.Height);
            else g.DrawString("Miss " + (num + 1) + " of " + ReplayAnalyzer.misses.Count, f, p.Brush, 0, f.Height);
            TimeSpan ts = TimeSpan.FromMilliseconds(hitObject.StartTime);
            g.DrawString("Time: " + ts.ToString(@"mm\:ss\.fff"), f, p.Brush, 0, area.Height - f.Height);
            return img;
        }

        public IEnumerable<Bitmap> DrawAllMisses(Rectangle area)
        {
            bool savedAllVal = drawAllHitObjects;
            drawAllHitObjects = false;
            var images = Enumerable.Range(0, ReplayAnalyzer.misses.Count).Select(i => DrawHitObject(i, area));
            drawAllHitObjects = savedAllVal;
            return images;
        }

        /// <summary>
        /// Gets the hit window.
        /// </summary>
        /// <returns>The hit window in ms.</returns>
        /// <param name="od">OD of the map.</param>
        /// <param name="hit">Hit value (300, 100, or 50).</param>
        private static float GetHitWindow(float od, int hit)
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
        private static Color GetHitColor(float od, int ms)
        {
            if (Math.Abs(ms) < GetHitWindow(od, 300)) return Color.SkyBlue;
            if (Math.Abs(ms) < GetHitWindow(od, 100)) return Color.SpringGreen;
            if (Math.Abs(ms) < GetHitWindow(od, 50)) return Color.Purple;
            return Color.Black;
        }
    }
}