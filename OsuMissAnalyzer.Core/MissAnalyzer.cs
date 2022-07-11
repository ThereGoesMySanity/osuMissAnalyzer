using System;
using osuDodgyMomentsFinder;
using ReplayAPI;
using BMAPI.v1;
using BMAPI;
using BMAPI.v1.HitObjects;
using System.Linq;
using OsuMissAnalyzer.Core.Utils;
using static OsuMissAnalyzer.Core.Utils.MathUtils;
using System.Collections.Generic;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.PixelFormats;
using SixLabors.ImageSharp.Drawing.Processing;
using SixLabors.ImageSharp.Processing;
using SixLabors.ImageSharp.Drawing;
using SixLabors.Fonts;

namespace OsuMissAnalyzer.Core
{
    public class MissAnalyzer
    {
        private const int maxTime = 1000;
        private const int arrowLength = 4;
        public bool HitCircleOutlines { get; private set; } = false;
        private ReplayAnalyzer ReplayAnalyzer { get; }
        private Replay Replay { get; }
        private Beatmap Beatmap { get; }
        public int MissCount => ReplayAnalyzer.misses.Count;
        public int CurrentObject { get; set; } = 0;
        private bool drawAllHitObjects;
        private float scale = 1;


        public MissAnalyzer(IReplayLoader replayLoader) : this(replayLoader.Replay, replayLoader.Beatmap, replayLoader.ReplayAnalyzer)
        {}
        public MissAnalyzer(Replay replay, Beatmap beatmap) : this(replay, beatmap, new ReplayAnalyzer(beatmap, replay))
        {}
        public MissAnalyzer(Replay replay, Beatmap beatmap, ReplayAnalyzer analyzer)
        {
            Replay = replay;
            Beatmap = beatmap;
            ReplayAnalyzer = analyzer;
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
                CurrentObject = ReplayAnalyzer.misses.Count(x => x.StartTime < Beatmap.HitObjects[CurrentObject].StartTime);
            }
            else
            {
                drawAllHitObjects = true;
                CurrentObject = Beatmap.HitObjects.IndexOf(ReplayAnalyzer.misses[CurrentObject]);
            }
        }

        public void NextObject()
        {
            if (drawAllHitObjects ? CurrentObject < Beatmap.HitObjects.Count - 1 : CurrentObject < MissCount - 1) CurrentObject++;
        }
        public void PreviousObject()
        {
            if (CurrentObject > 0) CurrentObject--;
        }

        public void ScaleChange(int i)
        {
            scale += 0.1f * i;
            if (scale < 0.1) scale = 0.1f;
        }

        public Image DrawSelectedHitObject(Rectangle area) { return DrawHitObject(CurrentObject, area); }
        /// <summary>
        /// Draws the miss.
        /// </summary>
        /// <returns>A Bitmap containing the drawing</returns>
        /// <param name="num">Index of the miss as it shows up in r.misses.</param>
        public Image DrawHitObject(int num, Rectangle area)
        {
            Image img = new Image<Rgba32>(area.Width, area.Height, Color.White);

            img.Mutate(g => 
            {
                bool hr = Replay.Mods.HasFlag(Mods.HardRock);
                CircleObject hitObject;
                if (drawAllHitObjects) hitObject = Beatmap.HitObjects[num];
                else hitObject = ReplayAnalyzer.misses[num];
                float radius = (float)hitObject.Radius;
                
                Func<Color, Pen> circlePen = color => new Pen(color, radius * 2 / scale)
                {
                    EndCapStyle = EndCapStyle.Round,
                    JointStyle = JointStyle.Round,
                };

                Func<Color, Pen> linePen = color => new Pen(color, 1);

                RectangleF bounds = new RectangleF(PointF.Subtract(hitObject.Location.ToPointF(), Scale(area.Size, scale / 2)),
                    Scale(area.Size, scale));

                int replayFramesStart, replayFramesEnd, hitObjectsStart, hitObjectsEnd;

                for (hitObjectsStart = Beatmap.HitObjects.Count(x => x.StartTime <= hitObject.StartTime) - 1;
                    hitObjectsStart >= 0 && bounds.Contains(Beatmap.HitObjects[hitObjectsStart].Location.ToPointF())
                    && hitObject.StartTime - Beatmap.HitObjects[hitObjectsStart].StartTime < maxTime;
                    hitObjectsStart--) ;

                for (hitObjectsEnd = Beatmap.HitObjects.Count(x => x.StartTime <= hitObject.StartTime) - 1;
                    hitObjectsEnd < Beatmap.HitObjects.Count && bounds.Contains(Beatmap.HitObjects[hitObjectsEnd].Location.ToPointF())
                    && Beatmap.HitObjects[hitObjectsEnd].StartTime - hitObject.StartTime < maxTime;
                    hitObjectsEnd++) ;

                for (replayFramesStart = Replay.ReplayFrames.Count(x => x.Time <= Beatmap.HitObjects[hitObjectsStart + 1].StartTime);
                    replayFramesStart > 1 && replayFramesStart < Replay.ReplayFrames.Count && bounds.Contains(Replay.ReplayFrames[replayFramesStart].GetPointF())
                    && hitObject.StartTime - Replay.ReplayFrames[replayFramesStart].Time < maxTime;
                    replayFramesStart--) ;

                for (replayFramesEnd = Replay.ReplayFrames.Count(x => x.Time <= Beatmap.HitObjects[hitObjectsEnd - 1].StartTime);
                    replayFramesEnd < Replay.ReplayFrames.Count - 1 && (replayFramesEnd < 2 || bounds.Contains(Replay.ReplayFrames[replayFramesEnd - 2].GetPointF()))
                    && Replay.ReplayFrames[replayFramesEnd].Time - hitObject.StartTime < maxTime;
                    replayFramesEnd++) ;

                g.Draw(linePen(Color.DarkGray), Rectangle.Round(ScaleToRect(new RectangleF(pSub(new PointF(0, hr? 384 : 0), bounds, hr), new SizeF(512, 384)), bounds, area)));
                
                for (int q = hitObjectsEnd - 1; q > hitObjectsStart; q--)
                {
                    byte c = (byte)Math.Min(255, 100 + (int)(Math.Abs(Beatmap.HitObjects[q].StartTime - hitObject.StartTime) * 100 / maxTime));
                    if (Beatmap.HitObjects[q].Type.HasFlag(HitObjectType.Slider))
                    {
                        SliderObject slider = (SliderObject)Beatmap.HitObjects[q];
                        PointF[] pt = slider.Curves.SelectMany(curve => curve.CurveSnapshots)
                            .Select(c => c.point + slider.StackOffset.ToVector2())
                            .Select(s => ScaleToRect(pSub(s.ToPointF(), bounds, hr), bounds, area)).ToArray();
                        if (pt.Length > 1) g.DrawLines(circlePen(Color.DarkGoldenrod.WithAlpha(80 / 255f)), pt);
                    }

                    var color = Color.FromRgb((byte)(c == 100 ? c + 50 : c), c, c);
                    var circleRect = ScaleToRect(new RectangleF(PointF.Subtract(
                            pSub(Beatmap.HitObjects[q].Location.ToPointF(), bounds, hr),
                            (Size)new SizeF(radius, radius)), new SizeF(radius * 2, radius * 2)), bounds, area);
                    var circle = new EllipsePolygon(RectangleF.Center(circleRect), circleRect.Size);
                    if (HitCircleOutlines)
                    {
                        g.Draw(linePen(color), circle);
                    }
                    else
                    {
                        g.Fill(color, circle);
                    }
                }
                float distance = 10.0001f;
                for (int k = replayFramesStart; k < replayFramesEnd - 2; k++)
                {
                    PointF p1 = pSub(Replay.ReplayFrames[k].GetPointF(), bounds, hr);
                    PointF p2 = pSub(Replay.ReplayFrames[k + 1].GetPointF(), bounds, hr);
                    var pen = linePen(GetHitColor(Beatmap.OverallDifficulty, (int)(hitObject.StartTime - Replay.ReplayFrames[k].Time)));
                    g.DrawLines(pen, ScaleToRect(p1, bounds, area), ScaleToRect(p2, bounds, area));
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
                            g.DrawLines(pen, p2, p3);
                            g.DrawLines(pen, p2, p4);
                        }
                        distance = 0;
                    }
                    else
                    {
                        distance += new Point2(p1.X - p2.X, p1.Y - p2.Y).Length;
                    }
                    if (ReplayAnalyzer.getKey(k == 0 ? Keys.None : Replay.ReplayFrames[k - 1].Keys, Replay.ReplayFrames[k].Keys) > 0)
                    {
                        EllipsePolygon circle = new EllipsePolygon(ScaleToRect(p1, bounds, area), ScaleToRect(new SizeF(6, 6), bounds, area));
                        g.Draw(pen, circle);
                    }
                }

                var textColor = Color.Black;
                int textSize = 16;
                Font f = new Font(SystemFonts.Get("Segoe UI"), textSize);
                var opts = new TextOptions(f)
                {
                    WrappingLength = area.Width,
                };
                
                var textBounds = TextMeasurer.MeasureBounds(Beatmap.ToString(), opts);
                g.DrawText(opts, Beatmap.ToString(), textColor);


                if (drawAllHitObjects) g.DrawText($"Object {num + 1} of {Beatmap.HitObjects.Count}", f, textColor, new PointF(0, textBounds.Bottom));
                else g.DrawText($"Miss {num + 1} of {MissCount}", f, textColor, new PointF(0, textBounds.Bottom));

                float time = hitObject.StartTime;
                if (Replay.Mods.HasFlag(Mods.DoubleTime)) time /= 1.5f;
                else if (Replay.Mods.HasFlag(Mods.HalfTime)) time /= 0.75f;
                TimeSpan ts = TimeSpan.FromMilliseconds(time);
                g.DrawText($"Time: {ts:mm\\:ss\\.fff}", f, textColor, new PointF(0, area.Height - textSize * 1.5f));
            });
            return img;
        }

        public IEnumerable<Image> DrawAllMisses(Rectangle area)
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