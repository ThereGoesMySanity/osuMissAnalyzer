using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using BMAPI;
using BMAPI.v1;
using BMAPI.v1.Events;
using BMAPI.v1.HitObjects;
using ReplayAPI;

namespace osuDodgyMomentsFinder
{

	/* This class is a list of pair of a clickable object and a replay frame hit
     * Initializing the class is a task of associating every keypress with an object hit
     * After that all the procedural checks on suspicious moment become possible
     */
	public class ReplayAnalyzer
	{
		//The beatmap
		private readonly Beatmap beatmap;

		//The replay
		private readonly Replay replay;

		//Circle radius
		private readonly double circleRadius;

		//hit time window
		private readonly double hitTimeWindow;

		//hit time window


		//The list of pair of a <hit, object hit>
		public List<HitFrame> hits
		{
			get;
		}
		public List<HitFrame> attemptedHits
		{
			get;
		}

		public List<CircleObject> misses
		{
			get;
		}

		private List<CircleObject> effortlessMisses
		{
			get;
		}

		private List<BreakEvent> breaks
		{
			get;
		}

		private List<SpinnerObject> spinners
		{
			get;
		}

		private List<ClickFrame> extraHits
		{
			get;
		}

		private void applyHardrock()
		{
			replay.flip();
			beatmap.applyHardRock();
		}

		private void selectBreaks()
		{
			foreach (var event1 in beatmap.Events)
			{
				if (event1.GetType() == typeof(BreakEvent))
				{
					breaks.Add((BreakEvent)event1);
				}
			}
		}

		private void selectSpinners()
		{
			foreach (var obj in beatmap.HitObjects)
			{
				if (obj.Type.HasFlag(HitObjectType.Spinner))
				{
					spinners.Add((SpinnerObject)obj);
				}
			}
		}

		private void associateHits()
		{
			int keyIndex = 0;
			var keyCounter = new KeyCounter();

			if ((replay.Mods & Mods.HardRock) > 0)
			{
				applyHardrock();
			}

			int breakIndex = 0;
			int combo = 0;

			foreach (CircleObject note in beatmap.HitObjects)
			{
				bool noteHitFlag = false;
				bool noteAttemptedHitFlag = false;

				if (note.Type.HasFlag(HitObjectType.Spinner))
					continue;

				for (int j = keyIndex; j < replay.ReplayFrames.Count; ++j)
				{
					var frame = replay.ReplayFrames[j];
					var lastKey = j > 0 ? replay.ReplayFrames[j - 1].Keys : Keys.None;

					var pressedKey = getKey(lastKey, frame.Keys);

					if (breakIndex < breaks.Count && frame.Time > breaks[breakIndex].EndTime)
					{
						++breakIndex;
					}

					if (frame.Time >= beatmap.HitObjects[0].StartTime - hitTimeWindow && 
					    (breakIndex >= breaks.Count || frame.Time < breaks[breakIndex].StartTime - hitTimeWindow))
					{
						keyCounter.Update(lastKey, frame.Keys);
					}

					frame.keyCounter = new KeyCounter(keyCounter);

					if (frame.Time - note.StartTime > hitTimeWindow)
						break;

					if (pressedKey > 0 && Math.Abs(frame.Time - note.StartTime) <= 
									(frame.Time > note.StartTime 
										&& note is SliderObject s? 
									Math.Min(hitTimeWindow, s.duration / s.RepeatCount) : hitTimeWindow))
					{
                        Point2 point = new Point2(frame.X, frame.Y);
                        if (note.ContainsPoint(point))
						{
							noteAttemptedHitFlag = true;
							++combo;
							frame.combo = combo;
							noteHitFlag = true;
							hits.Add(new HitFrame(note, frame, pressedKey));
							keyIndex = j + 1;
							break;
						}
						if (Utils.dist(note.Location.X, note.Location.Y, frame.X, frame.Y) > 150)
						{
							extraHits.Add(new ClickFrame(frame, getKey(lastKey, frame.Keys)));
						}
						else
						{
							noteAttemptedHitFlag = true;
							attemptedHits.Add(new HitFrame(note, frame, pressedKey));
						}
					}
					if (pressedKey > 0 && Math.Abs(frame.Time - note.StartTime) <= 3 * hitTimeWindow && note.ContainsPoint(new Point2(frame.X, frame.Y)))
					{
						noteAttemptedHitFlag = true;
						attemptedHits.Add(new HitFrame(note, frame, pressedKey));
					}

					frame.combo = combo;

				}

				if (!noteHitFlag)
				{
					misses.Add(note);
				}
				if (!noteAttemptedHitFlag)
				{
					effortlessMisses.Add(note);
				}
			}
		}

		public Keys getKey(Keys last, Keys current)
		{
			Keys res = Keys.None;
			if (!last.HasFlag(Keys.M1) && current.HasFlag(Keys.M1) && !current.HasFlag(Keys.K1))
				res |= Keys.M1;
			if (!last.HasFlag(Keys.M2) && current.HasFlag(Keys.M2) && !current.HasFlag(Keys.K2))
				res |= Keys.M2;
			if (!last.HasFlag(Keys.K1) && current.HasFlag(Keys.K1))
				res |= Keys.K1 | Keys.M1;
			if (!last.HasFlag(Keys.K2) && current.HasFlag(Keys.K2))
				res |= Keys.K2 | Keys.M2;
			return res;
		}

		private List<double> calcPressIntervals()
		{
			List<double> result = new List<double>();

			bool k1 = false, k2 = false;
			double k1_timer = 0, k2_timer = 0;
			foreach (var frame in replay.ReplayFrames)
			{
				var hit = hits.Find(x => x.frame.Equals(frame));

				if (!ReferenceEquals(hit, null) && hit.note.Type == HitObjectType.Circle)
				{
					if (!k1 && frame.Keys.HasFlag(Keys.K1))
						k1 = true;

					if (!k2 && frame.Keys.HasFlag(Keys.K2))
						k2 = true;
				}

				//k1
				if (k1 && frame.Keys.HasFlag(Keys.K1))
				{
					k1_timer += frame.TimeDiff;
				}

				if (k1 && !frame.Keys.HasFlag(Keys.K1))
				{
					k1 = false;
					result.Add(k1_timer);
					k1_timer = 0;
				}

				//k2
				if (k2 && frame.Keys.HasFlag(Keys.K2))
				{
					k2_timer += frame.TimeDiff;
				}

				if (k2 && !frame.Keys.HasFlag(Keys.K2))
				{
					k2 = false;
					result.Add(k2_timer);
					k2_timer = 0;
				}
			}

			if (result.Count == 0)
				result.Add(-1);

			return result;
		}

		private List<KeyValuePair<HitFrame, HitFrame>> checkTappingConsistency()
		{
			var times = new List<KeyValuePair<HitFrame, HitFrame>>();

			double limit = 90 * (replay.Mods.HasFlag(Mods.DoubleTime) ? 1.5 : 1);

			for (int i = 0; i < hits.Count - 1; ++i)
			{
				HitFrame hit1 = hits[i], hit2 = hits[i + 1];

				if ((hit2.frame.Time - hit1.frame.Time <= limit || hit2.note.StartTime - hit1.note.StartTime <= limit) && (hit1.key & hit2.key) > 0)
					times.Add(new KeyValuePair<HitFrame, HitFrame>(hit1, hit2));
			}

			return times;
		}

		private List<ReplayFrame> findCursorTeleports()
		{
			var times = new List<ReplayFrame>();

			int spinnerIndex = 0;

			for (int i = 2; i < replay.times.Count - 1; ++i)
			{
				var frame = replay.times[i + 1];

				if (spinnerIndex < spinners.Count && frame.Time > spinners[spinnerIndex].EndTime)
				{
					++spinnerIndex;
				}

				if (isTeleport(frame) && (spinnerIndex >= spinners.Count || frame.Time < spinners[spinnerIndex].StartTime))
				{
					times.Add(frame);
				}
			}

			return times;
		}

		private bool isTeleport(ReplayFrame frame)
		{
			if (frame.travelledDistanceDiff >= 40 && double.IsInfinity(frame.speed))
				return true;

			return frame.travelledDistanceDiff >= 150 && frame.speed >= 6;
		}

		public string outputDistances()
		{
			string res = "";
			foreach (var value in findCursorTeleports())
			{
				res += value.travelledDistanceDiff + ",";
			}
			return res.Remove(res.Length - 1);
		}

		private double calculateAverageFrameTimeDiff()
		{
			return replay.times.ConvertAll(x => x.TimeDiff).Where(x => x > 0 && x < 30).Average();
		}

		private double calculateAverageFrameTimeDiffv2()
		{
			int count = 0;
			int sum = 0;

			for (int i = 1; i < replay.times.Count - 1; i++)
			{
				if (!replay.times[i - 1].Keys.HasFlag(Keys.K1) && !replay.times[i - 1].Keys.HasFlag(Keys.K2) && !replay.times[i - 1].Keys.HasFlag(Keys.M1) && !replay.times[i - 1].Keys.HasFlag(Keys.M2) &&
					!replay.times[i].Keys.HasFlag(Keys.K1) && !replay.times[i].Keys.HasFlag(Keys.K2) && !replay.times[i].Keys.HasFlag(Keys.M1) && !replay.times[i].Keys.HasFlag(Keys.M2) &&
					!replay.times[i + 1].Keys.HasFlag(Keys.K1) && !replay.times[i + 1].Keys.HasFlag(Keys.K2) && !replay.times[i + 1].Keys.HasFlag(Keys.M1) && !replay.times[i + 1].Keys.HasFlag(Keys.M2))
				{
					count++;
					sum += replay.times[i].TimeDiff;
				}
			}

			if (count == 0)
			{
				return -1.0;
			}

			return (double)sum / count;
		}

		private List<double> speedList()
		{
			return replay.times.ConvertAll(x => x.speed);
		}

		private List<double> accelerationList()
		{
			return replay.times.ConvertAll(x => x.acceleration);
		}

		public string outputSpeed()
		{
			string res = speedList().Aggregate("", (current, value) => current + (value + ","));
			return res.Remove(res.Length - 1);
		}

		public string outputAcceleration()
		{
			string res = replay.times.ConvertAll(x => x.acceleration).Aggregate("", (current, value) => current + (value + ","));
			return res.Remove(res.Length - 1);
		}

		public string outputTime()
		{
			string res = replay.times.ConvertAll(x => x.Time).Aggregate("", (current, value) => current + (value + ","));
			return res.Remove(res.Length - 1);
		}


		private List<HitFrame> findOverAimHits()
		{
			var result = new List<HitFrame>();
			int keyIndex = 0;

			foreach (var t in hits)
			{
				var note = t.note;

				//searches for init circle object hover
				for (int j = keyIndex; j < replay.ReplayFrames.Count; ++j)
				{
					ReplayFrame frame = replay.ReplayFrames[j];
					if (!note.ContainsPoint(new Point2(frame.X, frame.Y)) ||
					   !(Math.Abs(frame.Time - note.StartTime) <= hitTimeWindow)) continue;

					while (note.ContainsPoint(new Point2(frame.X, frame.Y)) && frame.Time < t.frame.Time)
					{
						++j;
						frame = replay.ReplayFrames[j];
					}

					if (!note.ContainsPoint(new Point2(frame.X, frame.Y)))
					{
						result.Add(t);
					}
				}
			}
			return result;
		}


		//Recalculate the highest CS value for which the player would still have the same amount of misses
		private double bestCSValue()
		{
			double pixelPerfect = findBestPixelHit();

			double y = pixelPerfect * circleRadius;

			double x = (54.42 - y) / 4.48;

			return x;
		}

		public double calcAccelerationVariance()
		{
			return Utils.variance(accelerationList());
		}

		public string outputMisses()
		{
			string res = "";
			misses.ForEach(note => res += "Didn't find the hit for " + note.StartTime);
			return res;
		}

		private double calcTimeWindow(double OD)
		{
			return -12 * OD + 259.5;
		}

		public ReplayAnalyzer(Beatmap beatmap, Replay replay)
		{
			this.beatmap = beatmap;
			this.replay = replay;

			if (!replay.fullLoaded)
				throw new Exception(replay.Filename + " IS NOT FULL");

			multiplier = replay.Mods.HasFlag(Mods.DoubleTime) ? 1.5 : 1;

			circleRadius = beatmap.HitObjects[0].Radius;
			hitTimeWindow = calcTimeWindow(beatmap.OverallDifficulty);

			hits = new List<HitFrame>();
			attemptedHits = new List<HitFrame>();
			misses = new List<CircleObject>();
			effortlessMisses = new List<CircleObject>();
			extraHits = new List<ClickFrame>();
			breaks = new List<BreakEvent>();
			spinners = new List<SpinnerObject>();

			selectBreaks();
			selectSpinners();
			associateHits();
		}


		private double findBestPixelHit()
		{
			return hits.Max(pair => Utils.pixelPerfectHitFactor(pair.frame, pair.note));
		}

		public List<double> findPixelPerfectHits(double threshold)
		{
			List<double> result = new List<double>();

			foreach (var pair in hits)
			{
				double factor = Utils.pixelPerfectHitFactor(pair.frame, pair.note);

				if (factor >= threshold)
				{
					result.Add(pair.note.StartTime);
				}
			}


			return result;
		}

		private List<HitFrame> findAllPixelHits()
		{
			var pixelPerfectHits = new List<HitFrame>();

			foreach (var pair in hits)
			{
				pixelPerfectHits.Add(pair);
			}

			return pixelPerfectHits;
		}


		public List<HitFrame> findSortedPixelPerfectHits(int maxSize, double threshold)
		{
			var pixelPerfectHits = (from pair in hits let factor = pair.Perfectness where factor >= threshold select pair).ToList();

			pixelPerfectHits.Sort((a, b) => b.Perfectness.CompareTo(a.Perfectness));

			return pixelPerfectHits.GetRange(0, Math.Min(maxSize, pixelPerfectHits.Count));
		}


		private double ur = -1;

		private double unstableRate()
		{
			if (ur >= 0)
				return ur;
			var values = hits.ConvertAll(pair => (double)pair.frame.Time - pair.note.StartTime);
			ur = 10 * Utils.variance(values);
			return ur;
		}

		private readonly double multiplier;


		public StringBuilder MainInfo()
		{
			var sb = new StringBuilder();

			sb.AppendLine("GENERIC INFO");

			sb.AppendLine(misses.Count > replay.CountMiss
				? $"WARNING! The detected number of misses is not consistent with the replay: {misses.Count} VS. {replay.CountMiss} (notepad user or missed on spinners or BUG in the code <- MOST LIKELY )"
				: $"Misses: {misses.Count}");

			sb.AppendLine($"Unstable rate: {unstableRate()}");

			if (unstableRate() < 47.5 * multiplier)
			{
				sb.AppendLine("WARNING! Unstable rate is too low (auto)");
			}

			sb.AppendLine($"The best CS value: {bestCSValue()}");
			sb.AppendLine($"Average frame time difference: {calculateAverageFrameTimeDiff()}ms");

			double averageFrameTimeDiffv2 = calculateAverageFrameTimeDiffv2();
			sb.AppendLine($"Average frame time difference v2 (aim only!): {averageFrameTimeDiffv2}ms");

			if ((replay.Mods.HasFlag(Mods.DoubleTime) || replay.Mods.HasFlag(Mods.NightCore)) && averageFrameTimeDiffv2 < 17.35
			   || !replay.Mods.HasFlag(Mods.HalfTime) && averageFrameTimeDiffv2 < 12.3)
			{
				sb.AppendLine("WARNING! Average frame time difference is not consistent with the speed-modifying gameplay mods (timewarp)!" + Environment.NewLine);
			}

			var keyPressIntervals = calcPressIntervals();

			double averageKeyPressTime = Utils.median(keyPressIntervals) / multiplier;
			sb.AppendLine($"Median Key press time interval: {averageKeyPressTime:0.00}ms");
			sb.AppendLine($"Min Key press time interval: {keyPressIntervals.Min() / multiplier:0.00}ms");

			if (averageKeyPressTime < 30)
			{
				sb.AppendLine("WARNING! Average Key press time interval is inhumanly low (timewarp/relax)!");
			}

			sb.AppendLine($"Extra hits: {extraHits.Count}");

			if (replay.Mods.HasFlag(Mods.NoFail))
			{
				sb.AppendLine($"Pass: {replay.IsPass()}");
			}

			return sb;
		}

		public StringBuilder CursorInfo()
		{
			var sb = new StringBuilder();
			sb.AppendLine("Cursor movement Info");

			var cursorAcceleration = accelerationList();
			sb.AppendLine($"Cursor acceleration mean: {cursorAcceleration.Average()}");
			sb.AppendLine($"Cursor acceleration variance: {Utils.variance(cursorAcceleration)}");

			return sb;
		}


		public StringBuilder PixelPerfectRawData()
		{
			var sb = new StringBuilder();
			var pixelPerfectHits = findAllPixelHits();

			foreach (var hit in pixelPerfectHits)
			{
				sb.Append(hit).Append(',');
			}

			return sb;
		}

		public StringBuilder TimeFramesRawData()
		{
			var sb = new StringBuilder();
			var timeFrames = replay.ReplayFrames.ConvertAll(x => x.TimeDiff).Where(x => x > 0);

			foreach (int frame in timeFrames)
			{
				sb.Append(frame).Append(',');
			}

			return sb;
		}

		public StringBuilder TravelledDistanceDiffRawData()
		{
			var sb = new StringBuilder();
			var timeFrames = replay.ReplayFrames.ConvertAll(x => x.travelledDistanceDiff);

			foreach (double frame in timeFrames)
			{
				sb.Append(frame).Append(',');
			}

			return sb;
		}

		public StringBuilder SpeedRawData()
		{
			var sb = new StringBuilder();
			var timeFrames = replay.ReplayFrames.ConvertAll(x => x.speed);

			foreach (double frame in timeFrames)
			{
				sb.Append(frame).Append(',');
			}

			return sb;
		}

		public StringBuilder AccelerationRawData()
		{
			var sb = new StringBuilder();
			var timeFrames = replay.ReplayFrames.ConvertAll(x => x.acceleration);

			foreach (double frame in timeFrames)
			{
				sb.Append(frame).Append(',');
			}

			return sb;
		}

		public StringBuilder HitErrorRawData()
		{
			var sb = new StringBuilder();
			var timeFrames = hits.ConvertAll(x => x.note.StartTime - x.frame.Time);

			foreach (float frame in timeFrames)
			{
				sb.Append(frame).Append(',');
			}

			return sb;
		}

		public StringBuilder PressKeyIntevalsRawData()
		{
			var sb = new StringBuilder();
			var timeFrames = calcPressIntervals();

			foreach (double frame in timeFrames)
			{
				sb.Append(frame).Append(',');
			}

			return sb;
		}

		public StringBuilder PixelPerfectInfo()
		{
			var sb = new StringBuilder();
			sb.AppendLine("- [PIXEL PERFECTS]:");

			var pixelHits = findAllPixelHits();
			var values = pixelHits.Select(x => x.Perfectness).ToList();
			double bestPxPerfect = pixelHits.Max(a => a.Perfectness);
			sb.AppendLine($"The best pixel perfect hit: {bestPxPerfect}");
			double median = values.Median();
			double variance = Utils.variance(values);
			sb.AppendLine($"Median pixel perfect hit: {median}");
			sb.AppendLine($"Perfectness variance: {variance}");

			if (bestPxPerfect < 0.5 || variance < 0.01 || median < 0.2)
			{
				sb.AppendLine("WARNING! Player is aiming the notes too consistently (autohack)");
				sb.AppendLine();
			}

			var pixelperfectHits = pixelHits.Where(x => x.Perfectness > 0.98);
			var hitFrames = pixelperfectHits as HitFrame[] ?? pixelperfectHits.ToArray();
			sb.AppendLine($"Pixel perfect hits: {hitFrames.Length}");

			foreach (var hit in hitFrames)
			{
				sb.AppendLine($"* {hit}");
			}

			var unrealisticPixelPerfects = hitFrames.Where(x => x.Perfectness > 0.99);

			if (unrealisticPixelPerfects.Count() > 15)
			{
				sb.AppendLine("WARNING! Player is constantly doing pixel perfect hits (relax)" + Environment.NewLine);
			}

			return sb;
		}

		public StringBuilder OveraimsInfo()
		{
			var sb = new StringBuilder();
			sb.AppendLine("- [OVER-AIM]:");

			var overAims = findOverAimHits();
			sb.AppendLine($"Over-aim count: {overAims.Count}");

			foreach (var hit in overAims)
			{
				sb.AppendLine($"* {hit}");
			}

			return sb;
		}

		public StringBuilder TeleportsInfo()
		{
			var sb = new StringBuilder();
			sb.AppendLine("- [CURSOR TELEPORTS]:");

			var teleports = findCursorTeleports();
			sb.AppendLine($"Teleport count: {teleports.Count}");

			foreach (var frame in teleports)
			{
				sb.AppendLine($"* {frame.Time}ms {frame.travelledDistanceDiff}px");
			}

			return sb;
		}

		public StringBuilder ExtraHitsInfo()
		{
			var sb = new StringBuilder();
			sb.AppendLine("- [EXTRA HITS]");
			sb.AppendLine($"Extra hits count: {extraHits.Count}");

			foreach (var frame in extraHits)
			{
				sb.AppendLine(frame.ToString());
			}

			return sb;
		}

		public StringBuilder EffortlessMissesInfo()
		{
			var sb = new StringBuilder();
			sb.AppendLine("- [EFFORTLESS MISSES]");
			sb.AppendLine($"Effortless misses count: {effortlessMisses.Count}");

			foreach (var note in effortlessMisses)
			{
				sb.AppendLine($"{note} missed without a corresponding hit");
			}

			return sb;
		}

		public StringBuilder SingletapsInfo()
		{
			var sb = new StringBuilder();
			sb.AppendLine("Singletaps");

			var singletaps = checkTappingConsistency();
			sb.AppendLine($"Fast singletaps count: {singletaps.Count}");

			foreach (var frame in singletaps)
			{
				sb.AppendLine($"* Object at {frame.Key.note.StartTime}ms {frame.Key.key} singletapped with next at {frame.Value.note.StartTime} ({(frame.Value.frame.Time - frame.Key.frame.Time) / multiplier}ms real frame time diff) - {frame.Key.frame.Time - frame.Key.note.StartTime}ms and {frame.Value.frame.Time - frame.Value.note.StartTime}ms error.");
			}

			return sb;
		}

	}
}
