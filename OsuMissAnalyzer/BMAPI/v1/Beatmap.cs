using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Security.Cryptography;
using System.Threading;
using BMAPI.v1.Events;
using BMAPI.v1.HitObjects;

namespace BMAPI.v1
{
    public class BeatmapInfo
    {
        //Info
        public int? Format = null;
        public string Filename;
        public string Folder;
        public string BeatmapHash;

        //General
        public string AudioFilename;
        public int? AudioLeadIn = null;
        public int? PreviewTime = null;
        public int? Countdown = null;
        public string SampleSet;
        public float? StackLeniency = null;
        public GameMode? Mode = null;
        public int? LetterboxInBreaks = null;
        public int? SpecialStyle = null;
        public int? CountdownOffset = null;
        public OverlayOptions? OverlayPosition = null;
        public string SkinPreference;
        public int? WidescreenStoryboard = null;
        public int? UseSkinSprites = null;
        public int? StoryFireInFront = null;
        public int? EpilepsyWarning = null;
        public int? CustomSamples = null;
        public List<int> EditorBookmarks = new List<int>();
        public float? EditorDistanceSpacing = null;
        public string AudioHash;
        public bool? AlwaysShowPlayfield = null;

        //Editor (Other Editor tag stuff (v12))
        public int? GridSize = null;
        public List<int> Bookmarks = new List<int>();
        public int? BeatDivisor = null;
        public float? DistanceSpacing = null;
        public int? CurrentTime = null;
        public float? TimelineZoom = null;

        //Metadata
        public string Title;
        public string TitleUnicode;
        public string Artist;
        public string ArtistUnicode;
        public string Creator;
        public string Version;
        public string Source;
        public List<string> Tags = new List<string>();
        public int? BeatmapID = null;
        public int? BeatmapSetID = null;

        //Difficulty
        public float HPDrainRate = 5;
        private float p_CircleSize = 5;
        public float CircleSize
        {
            get
            {
                return p_CircleSize;
            }
            set
            {
                p_CircleSize = value;
                foreach(CircleObject hO in HitObjects)
                {
					hO.Radius = ((1.0f - 0.7f*(value - 5.0f) / 5.0f) / 2.0f) * 128.0f;
                }
            }
        }
        public float OverallDifficulty = 5;
        public float ApproachRate = 5;
        public float SliderMultiplier = 1.4f;
        public float SliderTickRate = 1;

        //Events
        public List<EventBase> Events = new List<EventBase>();

        //Timingpoints
        public List<TimingPoint> TimingPoints = new List<TimingPoint>();

        //Colours
        public List<Combo> ComboColours = new List<Combo>();
        public Colour SliderBorder = new Colour { R = 255, G = 255, B = 255 };

        //Hitobjects
        public List<CircleObject> HitObjects = new List<CircleObject>();
    }

    public class Beatmap : BeatmapInfo
    {
        private readonly BeatmapInfo Info = new BeatmapInfo();
        private readonly Dictionary<string, string> BM_Sections = new Dictionary<string, string>();
        private readonly List<string> WriteBuffer = new List<string>();
        private readonly Dictionary<string, int> SectionLength = new Dictionary<string, int>();

		/// <summary>
		/// Creates a new Beatmap object
		/// </summary>
		/// <param name="beatmapFile">The beatmap file to open</param>
		public Beatmap(string beatmapFile = "")
        {
            Thread.CurrentThread.CurrentCulture = new CultureInfo("en-US", false);

            //Variable init
            BM_Sections.Add("AudioFilename,AudioLeadIn,PreviewTime,Countdown,SampleSet,StackLeniency,Mode,LetterboxInBreaks,SpecialStyle,CountdownOffset," +
                            "OverlayPosition,SkinPreference,WidescreenStoryboard,UseSkinSprites,StoryFireInFront,EpilepsyWarning,CustomSamples,EditorDistanceSpacing," +
                            "AudioHash,AlwaysShowPlayfield", "General");
            BM_Sections.Add("GridSize,BeatDivisor,DistanceSpacing,CurrentTime,TimelineZoom", "Editor");
            BM_Sections.Add("Title,TitleUnicode,Artist,ArtistUnicode,Creator,Version,Source,BeatmapID,BeatmapSetID", "Metadata");
            BM_Sections.Add("HPDrainRate,CircleSize,OverallDifficulty,ApproachRate,SliderMultiplier,SliderTickRate", "Difficulty");

            if(beatmapFile != "")
            {
                if(File.Exists(beatmapFile))
                {
                    Parse(beatmapFile);
                }
            }

            recalculateStackCoordinates();
        }

		private void Parse(string bm)
        {
            FileInfo ffii = new FileInfo(bm);
            Info.Folder = ffii.DirectoryName;
            Info.Filename = bm;
            Info.BeatmapHash = MD5FromFile(bm);
            using(StreamReader sR = new StreamReader(bm))
            {
                string currentSection = "";

                while(sR.Peek() != -1)
                {
                    string line = sR.ReadLine();

                    //Check for section tag
                    if(line.StartsWith("["))
                    {
                        currentSection = line;
                        continue;
                    }

                    //Check for commented-out line
                    //or blank lines
                    if(line.StartsWith("//") || line.Length == 0)
                        continue;

                    //Check for version string
                    if(line.StartsWith("osu file format"))
                        Info.Format = Convert.ToInt32(line.Substring(17).Replace(Environment.NewLine, "").Replace(" ", ""));

                    //Do work for [General], [Metadata], [Difficulty] and [Editor] sections
                    if((currentSection == "[General]") || (currentSection == "[Metadata]") || (currentSection == "[Difficulty]") || (currentSection == "[Editor]"))
                    {
                        string[] reSplit = line.Split(':');
                        string cProperty = reSplit[0].TrimEnd();

                        bool isValidProperty = false;
                        foreach(string k in BM_Sections.Keys)
                        {
                            if(k.Contains(cProperty))
                                isValidProperty = true;
                        }
                        if(!isValidProperty)
                            continue;

                        //Check for blank value
                        string cValue = reSplit[1].Trim();

                        //Import properties into Info
                        switch(cProperty)
                        {
                            case "EditorBookmarks":
                                {
                                    string[] marks = cValue.Split(new[] { "," }, StringSplitOptions.RemoveEmptyEntries);
                                    foreach(string m in marks.Where(m => m != ""))
                                        Info.EditorBookmarks.Add(Convert.ToInt32(m));
                                }
                                break;
                            case "Bookmarks":
                                {
                                    string[] marks = cValue.Split(new[] { "," }, StringSplitOptions.RemoveEmptyEntries);
                                    foreach(string m in marks.Where(m => m != ""))
                                        Info.Bookmarks.Add(Convert.ToInt32(m));
                                }
                                break;
                            case "Tags":
                                string[] tags = cValue.Split(new[] { ' ' }, StringSplitOptions.RemoveEmptyEntries);
                                foreach(string t in tags)
                                    Info.Tags.Add(t);
                                break;
                            case "Mode":
                                Info.Mode = (GameMode)Convert.ToInt32(cValue);
                                break;
                            case "OverlayPosition":
                                Info.OverlayPosition = (OverlayOptions)Enum.Parse(typeof(OverlayOptions), cValue);
                                break;
                            case "AlwaysShowPlayfield":
                                Info.AlwaysShowPlayfield = Convert.ToBoolean(Convert.ToInt32(cValue));
                                break;
                            default:
                                FieldInfo fi = Info.GetType().GetField(cProperty);
                                PropertyInfo pi = Info.GetType().GetProperty(cProperty);
                                if(fi != null)
                                {
                                    if(fi.FieldType == typeof(float?))
                                        fi.SetValue(Info, (float?)Convert.ToDouble(cValue));
                                    if(fi.FieldType == typeof(float))
                                        fi.SetValue(Info, (float)Convert.ToDouble(cValue));
                                    else if((fi.FieldType == typeof(int?)) || (fi.FieldType == typeof(int)))
                                        fi.SetValue(Info, Convert.ToInt32(cValue));
                                    else if(fi.FieldType == typeof(string))
                                        fi.SetValue(Info, cValue);
                                    break;
                                }
                                if(pi.PropertyType == typeof(float?))
                                    pi.SetValue(Info, (float?)Convert.ToDouble(cValue), null);
                                if(pi.PropertyType == typeof(float))
                                    pi.SetValue(Info, (float)Convert.ToDouble(cValue), null);
                                else if((pi.PropertyType == typeof(int?)) || (pi.PropertyType == typeof(int)))
                                    pi.SetValue(Info, Convert.ToInt32(cValue), null);
                                else if(pi.PropertyType == typeof(string))
                                    pi.SetValue(Info, cValue, null);
                                break;
                        }
                        continue;
                    }

                    //The following are version-dependent, the version is stored as a numeric value inside Info.Format
                    //Do work for [Events] section
                    if(currentSection == "[Events]")
                    {
                        string[] reSplit = line.Split(',');
                        switch(reSplit[0].ToLower())
                        {
                            case "0":
                            case "1":
                            case "video":
                                Info.Events.Add(new ContentEvent
                                {
                                    Type = reSplit[0].ToLower() == "1" || reSplit[0].ToLower() == "video" ? ContentType.Video : ContentType.Image,
                                    StartTime = Convert.ToInt32(reSplit[1]),
                                    Filename = reSplit[2].Replace("\"", "")
                                });
                                break;
                            case "2":
                                Info.Events.Add(new BreakEvent
                                {
                                    StartTime = Convert.ToInt32(reSplit[1]),
                                    EndTime = Convert.ToInt32(reSplit[2])
                                });
                                break;
                            case "3":
                                Info.Events.Add(new BackgroundColourEvent
                                {
                                    StartTime = Convert.ToInt32(reSplit[1]),
                                    Colour = new Colour
                                    {
                                        R = Convert.ToInt32(reSplit[2]),
                                        G = Convert.ToInt32(reSplit[3]),
                                        B = Convert.ToInt32(reSplit[4])
                                    },
                                });
                                break;
                        }
                    }

                    //Do work for [TimingPoints] section
                    if(currentSection == "[TimingPoints]")
                    {
                        TimingPoint tempTimingPoint = new TimingPoint();

                        float[] values = { 0, 0, 4, 0, 0, 100, 0, 0, 0 };
                        string[] reSplit = line.Split(',');
                        for(int i = 0; i < reSplit.Length; i++)
                            values[i] = (float)Convert.ToDouble(reSplit[i]);
                        tempTimingPoint.InheritsBPM = !Convert.ToBoolean(Convert.ToInt32(values[6]));
                        tempTimingPoint.beatLength = values[1];
                        if(values[1] > 0)
                        {
                            tempTimingPoint.bpm = Math.Round(60000 / tempTimingPoint.beatLength);
                        }
                        else if(values[1] < 0)
                        {
                            tempTimingPoint.velocity = Math.Abs(100 / tempTimingPoint.beatLength);
                        }
                        tempTimingPoint.Time = (float)Convert.ToDouble(values[0]);
                        tempTimingPoint.BpmDelay = (float)Convert.ToDouble(values[1]);
                        tempTimingPoint.TimeSignature = Convert.ToInt32(values[2]);
                        tempTimingPoint.SampleSet = Convert.ToInt32(values[3]);
                        tempTimingPoint.CustomSampleSet = Convert.ToInt32(values[4]);
                        tempTimingPoint.VolumePercentage = Convert.ToInt32(values[5]);
                        tempTimingPoint.VisualOptions = (TimingPointOptions)Convert.ToInt32(values[7]);
                        Info.TimingPoints.Add(tempTimingPoint);
                        this.TimingPoints.Add(tempTimingPoint);
                    }
                    for(int i = 1, l = TimingPoints.Count; i < l; i++)
                    {
                        if(TimingPoints[i].bpm == 0)
                        {
                            TimingPoints[i].beatLength = TimingPoints[i - 1].beatLength;
                            TimingPoints[i].bpm = TimingPoints[i - 1].bpm;
                        }
                    }

                    //Do work for [Colours] section
                    if(currentSection == "[Colours]")
                    {
                        string property = line.Substring(0, line.IndexOf(':', 1)).Trim();
                        string value = line.Substring(line.IndexOf(':', 1) + 1).Trim();
                        string[] reSplit = value.Split(',');

                        if(property.Length > 5 && property.Substring(0, 5) == "Combo")
                        {
                            Combo newCombo = new Combo
                            {
                                Colour = new Colour
                                {
                                    R = Convert.ToInt32(reSplit[0]),
                                    G = Convert.ToInt32(reSplit[1]),
                                    B = Convert.ToInt32(reSplit[2])
                                }
                            };
                            try
                            {
                                newCombo.ComboNumber = Convert.ToInt32(property.Substring(5, 1));
                            }
                            catch
                            {
                                Debug.Assert(false, "Invalid combonumber at index 5. " + line);
                                continue;
                            }
                        }
                        else if(property.Length > 5 && property == "SliderBorder")
                        {
                            Info.SliderBorder = new Colour
                            {
                                R = Convert.ToInt32(reSplit[0]),
                                G = Convert.ToInt32(reSplit[1]),
                                B = Convert.ToInt32(reSplit[2])
                            };
                        }
                    }

                    //Do work for [HitObjects] section
                    if(currentSection == "[HitObjects]")
                    {
                        string[] reSplit = line.Split(new[] { ',' }, StringSplitOptions.RemoveEmptyEntries);
                        CircleObject newObject = new CircleObject
                        {
                            Radius = (float)(54.42 - 4.48 * Info.CircleSize),
                            Location = new Point2(Convert.ToInt32(reSplit[0]), Convert.ToInt32(reSplit[1])),
                            StartTime = (float)Convert.ToDouble(reSplit[2]),
                            Type = (HitObjectType)Convert.ToInt32(reSplit[3]),
                            Effect = (EffectType)Convert.ToInt32(reSplit[4])
                        };
                        if((newObject.Type & HitObjectType.Slider) > 0)
                        {
                            newObject = new SliderObject(newObject);
                            ((SliderObject)newObject).Velocity = Info.SliderMultiplier * TimingPointByTime(newObject.StartTime).SliderBpm / 600f;
                            switch(reSplit[5].Substring(0, 1))
                            {
                                case "B":
                                    ((SliderObject)newObject).Type = SliderType.Bezier;
                                    break;
                                case "C":
                                    ((SliderObject)newObject).Type = SliderType.CSpline;
                                    break;
                                case "L":
                                    //((SliderObject)newObject).Type = SliderType.Linear;
                                    ((SliderObject)newObject).Type = SliderType.Bezier;
                                    break;
                                case "P":
                                    ((SliderObject)newObject).Type = SliderType.PSpline;
                                    break;
                            }
                            string[] pts = reSplit[5].Split(new[] { "|" }, StringSplitOptions.None);

                            ((SliderObject)newObject).Points.Add(newObject.Location + new Point2());

                            //Always exclude index 1, this will contain the type
                            for(int i = 1; i <= pts.Length - 1; i++)
                            {
                                Point2 p = new Point2((float)Convert.ToDouble(pts[i].Substring(0, pts[i].IndexOf(":", StringComparison.InvariantCulture))),
                                                            (float)Convert.ToDouble(pts[i].Substring(pts[i].IndexOf(":", StringComparison.InvariantCulture) + 1)));
                                ((SliderObject)newObject).Points.Add(p);
                            }
                            /*
                             * var pxPerBeat      = beatmap.SliderMultiplier * 100 * timing.velocity;
        var beatsNumber    = (hitObject.pixelLength * hitObject.repeatCount) / pxPerBeat;
        hitObject.duration = Math.ceil(beatsNumber * timing.beatLength);
        hitObject.endTime  = hitObject.startTime + hitObject.duration;
                             */

                            ((SliderObject)newObject).RepeatCount = Convert.ToInt32(reSplit[6]);
                            ((SliderObject)newObject).PixelLength = Convert.ToSingle(reSplit[7]);
                            float tempMaxPoints;
                            if(float.TryParse(reSplit[7], out tempMaxPoints))
                            {
                                ((SliderObject)newObject).MaxPoints = tempMaxPoints;
                            }
                            ((SliderObject)newObject).CreateCurves();

                            var timing = TimingPointByTime(newObject.StartTime);
                            var pxPerBeat = Info.SliderMultiplier * 100 * timing.velocity;
                            var beatsNumber = ((SliderObject)newObject).PixelLength * ((SliderObject)newObject).RepeatCount / pxPerBeat;
                            var duration = (int)Math.Ceiling(beatsNumber * timing.beatLength);
                            ((SliderObject)newObject).duration = duration;

                        }
                        if((newObject.Type & HitObjectType.Spinner) > 0)
                        {
                            newObject = new SpinnerObject(newObject);
                            ((SpinnerObject)newObject).EndTime = (float)Convert.ToDouble(reSplit[5]);
                        }
                        Info.HitObjects.Add(newObject);
                    }
                }
            }

            //Copy the fields/properties of Info locally
            foreach(FieldInfo fi in Info.GetType().GetFields())
            {
                FieldInfo ff = GetType().GetField(fi.Name);
                ff.SetValue(this, fi.GetValue(Info));
            }
            foreach(PropertyInfo pi in Info.GetType().GetProperties())
            {
                PropertyInfo ff = GetType().GetProperty(pi.Name);
                ff.SetValue(this, pi.GetValue(Info, null), null);
            }
        }

        public TimingPoint TimingPointByTime(float time)
        {
            for(var i = this.TimingPoints.Count - 1; i >= 0; i--)
            {
                if(this.TimingPoints[i].Time <= time)
                {
                    return this.TimingPoints[i];
                }
            }
            return this.TimingPoints[0];
        }

        /// <summary>
        /// Saves the beatmap
        /// </summary>
        /// <param name="filename">The file to save the beatmap as</param>
        public void Save(string filename)
        {
            WriteBuffer.Clear();
            SectionLength.Clear();

            CultureInfo lastCulture = Thread.CurrentThread.CurrentCulture;
            Thread.CurrentThread.CurrentCulture = new CultureInfo("en-US", false);
            Save("", "osu file format v13");
            FieldInfo[] newFields = GetType().GetFields();
            FieldInfo[] oldFields = Info.GetType().GetFields();

            foreach(FieldInfo f1 in newFields)
            {
                foreach(FieldInfo f2 in oldFields.Where(f2 => f1.Name == f2.Name))
                {
                    switch(f1.Name)
                    {
                        case "EditorBookmarks":
                            {
                                List<int> temps = (List<int>)f1.GetValue(this);
                                if(temps.Count != 0)
                                    Save("General", "EditorBookmarks:" + string.Join(",", temps.Select(t => t.ToString(CultureInfo.InvariantCulture)).ToArray()));
                            }
                            break;
                        case "Bookmarks":
                            {
                                List<int> temps = (List<int>)f1.GetValue(this);
                                if(temps.Count != 0)
                                    Save("Editor", "Bookmarks:" + string.Join(",", temps.Select(t => t.ToString(CultureInfo.InvariantCulture)).ToArray()));
                            }
                            break;
                        case "Tags":
                            {
                                List<string> temps = (List<string>)f1.GetValue(this);
                                if(temps.Count != 0)
                                    Save("Metadata", "Tags:" + string.Join(" ", temps.ToArray()));
                            }
                            break;
                        case "Events":
                            foreach(EventBase o in (IEnumerable<EventBase>)f1.GetValue(this))
                            {
                                if(o.GetType() == typeof(ContentEvent))
                                {
                                    ContentEvent backgroundInfo = (ContentEvent)o;
                                    Save("Events", "0," + o.StartTime + ",\"" + backgroundInfo.Filename + "\"");
                                }
                                else if(o.GetType() == typeof(BreakEvent))
                                {
                                    BreakEvent breakInfo = (BreakEvent)o;
                                    Save("Events", "2," + o.StartTime + "," + breakInfo.EndTime);
                                }
                                else if(o.GetType() == typeof(BackgroundColourEvent))
                                {
                                    BackgroundColourEvent colourInfo = (BackgroundColourEvent)o;
                                    Save("Events", "3," + o.StartTime + "," + colourInfo.Colour.R + "," + colourInfo.Colour.G + "," + colourInfo.Colour.B);
                                }
                            }
                            break;
                        case "TimingPoints":
                            {
                                foreach(TimingPoint o in (IEnumerable<TimingPoint>)f1.GetValue(this))
                                    Save("TimingPoints", o.Time + "," + o.BpmDelay + "," + o.TimeSignature + "," + o.SampleSet + "," + o.CustomSampleSet + "," + o.VolumePercentage + "," + Convert.ToInt32(!o.InheritsBPM) + "," + (int)o.VisualOptions);
                            }
                            break;
                        case "ComboColours":
                            {
                                foreach(Combo o in (IEnumerable<Combo>)f1.GetValue(this))
                                    Save("Colours", "Combo" + o.ComboNumber + ':' + o.Colour.R + "," + o.Colour.G + "," + o.Colour.B);
                            }
                            break;
                        //case "SliderBorder":
                        //    {
                        //        if (f1.GetValue(this) == f2.GetValue(Info))
                        //            continue;
                        //        Colour o = (Colour)f1.GetValue(this);
                        //        Save("Colours", "SliderBorder: " + o.R + "," + o.G + "," + o.B);
                        //    }
                        //    break;
                        case "HitObjects":
                            foreach(CircleObject obj in (IEnumerable<CircleObject>)f1.GetValue(this))
                            {
                                if(obj.GetType() == typeof(CircleObject))
                                {
                                    Save("HitObjects", obj.Location.X + "," + obj.Location.Y + "," + obj.StartTime + "," + (int)obj.Type + "," + (int)obj.Effect);
                                }
                                else if(obj.GetType() == typeof(SliderObject))
                                {
                                    SliderObject sliderInfo = (SliderObject)obj;
                                    string pointString = sliderInfo.Points.Aggregate("", (current, p) => current + ("|" + p.X + ':' + p.Y));
                                    Save("HitObjects", obj.Location.X + "," + obj.Location.Y + "," + obj.StartTime + "," + (int)obj.Type + "," + (int)obj.Effect + "," + sliderInfo.Type.ToString().Substring(0, 1) + pointString + "," + sliderInfo.RepeatCount + "," + sliderInfo.MaxPoints);
                                }
                                else if(obj.GetType() == typeof(SpinnerObject))
                                {
                                    SpinnerObject spinnerInfo = (SpinnerObject)obj;
                                    Save("HitObjects", obj.Location.X + "," + obj.Location.Y + "," + obj.StartTime + "," + (int)obj.Type + "," + (int)obj.Effect + "," + spinnerInfo.EndTime);
                                }
                            }
                            break;
                        default:
                            if(f1.Name != "Format" && f1.Name != "Filename" && f1.Name != "BeatmapHash")
                            {
                                if(f1.GetValue(this) != null)
                                {
                                    if(f2.GetValue(Info) != null)
                                    {
                                        if((f1.GetValue(this).GetType() == typeof(GameMode)) || (f1.GetValue(this).GetType() == typeof(OverlayOptions)))
                                            Save(GetSection(f1.Name), f1.Name + ':' + (int)f1.GetValue(this));
                                        else
                                            Save(GetSection(f1.Name), f1.Name + ':' + f1.GetValue(this));
                                    }
                                    else
                                    {
                                        if((f2.GetValue(Info).GetType() == typeof(GameMode)) || (f2.GetValue(Info).GetType() == typeof(OverlayOptions)))
                                            Save(GetSection(f2.Name), f2.Name + ':' + (int)f2.GetValue(Info));
                                        else
                                            Save(GetSection(f2.Name), f2.Name + ':' + f2.GetValue(Info));
                                    }
                                }
                            }
                            break;
                    }
                }
            }
            foreach(PropertyInfo f1 in GetType().GetProperties())
            {
                foreach(PropertyInfo f2 in Info.GetType().GetProperties().Where(f2 => f1.Name == f2.Name))
                {
                    if(f1.GetValue(this, null) != null)
                    {
                        if(f2.GetValue(Info, null) != null)
                        {
                            if((f1.GetValue(this, null).GetType() == typeof(GameMode)) || (f1.GetValue(this, null).GetType() == typeof(OverlayOptions)))
                                Save(GetSection(f1.Name), f1.Name + ':' + (int)f1.GetValue(this, null));
                            else
                                Save(GetSection(f1.Name), f1.Name + ':' + f1.GetValue(this, null));
                        }
                        else
                        {
                            if((f2.GetValue(Info, null).GetType() == typeof(GameMode)) || (f2.GetValue(Info, null).GetType() == typeof(OverlayOptions)))
                                Save(GetSection(f2.Name), f2.Name + ':' + (int)f2.GetValue(Info, null));
                            else
                                Save(GetSection(f2.Name), f2.Name + ':' + f2.GetValue(Info, null));
                        }
                    }
                }
            }
            FinishSave(filename);
            Thread.CurrentThread.CurrentCulture = lastCulture;
        }

        private void Save(string section, string contents)
        {
            if(section == "")
                WriteBuffer.Add(contents);
            else if(WriteBuffer.Contains("[" + section + "]") == false)
            {
                WriteBuffer.Add("");
                WriteBuffer.Add("[" + section + "]");
                WriteBuffer.Add(contents);
                SectionLength.Add(section, 1);
            }
            else
            {
                if(WriteBuffer.IndexOf("[" + section + "]") + SectionLength[section] == WriteBuffer.Count)
                {
                    WriteBuffer.Add(contents);
                    SectionLength[section] += 1;
                }
                else
                {
                    WriteBuffer.Insert(WriteBuffer.IndexOf("[" + section + "]") + SectionLength[section] + 1, contents);
                    SectionLength[section] += 1;
                }
            }
        }

        private void FinishSave(string filename)
        {
            using(StreamWriter sw = new StreamWriter(filename))
            {
                foreach(string l in WriteBuffer)
                    sw.WriteLine(l);
            }
        }

        private string GetSection(string name)
        {
            foreach(string k in BM_Sections.Keys.Where(k => k.Contains(name)))
                return BM_Sections[k];
            return "";
        }

        public static string MD5FromFile(string fileName)
        {
            using(MD5 md5 = MD5.Create())
            {
                using(FileStream stream = File.OpenRead(fileName))
                    return BitConverter.ToString(md5.ComputeHash(stream)).Replace("-", "").ToLower();
            }
        }

        private void recalculateStackCoordinates()
        {
            double ApproachTimeWindow = 1800 - 120 * ApproachRate;
            double stackTimeWindow = (ApproachTimeWindow * (StackLeniency.HasValue ? StackLeniency.Value : 7));

			//Console.WriteLine(ApproachTimeWindow);
			//Console.WriteLine(StackLeniency);
			//Console.WriteLine(stackTimeWindow);
			//float shiftValue = 3.5f;
            float shiftValue = 1.5f;

            Dictionary<int, Point2> toChange = new Dictionary<int, Point2>();
            for(int i = HitObjects.Count - 1; i >= 0; --i)
            {
                /* not used
                 * if (HitObjects[i].Location == new Point2(360,128))
                {
                    int u = 0;
                }*/

                for(int j = i - 1; j >= 0; --j)
                {
                    if(HitObjects[i].StartTime - HitObjects[j].StartTime <= stackTimeWindow)
                    {
                        if(HitObjects[i].Location == HitObjects[j].Location)
                        {
                            var newPoint = toChange.ContainsKey(i) ? new Point2(toChange[i].X - shiftValue, toChange[i].Y - shiftValue) : new Point2(HitObjects[i].Location.X - shiftValue, HitObjects[i].Location.Y - shiftValue);
                            toChange.Add(j, newPoint);
                            //HitObjects[j].Location = new Point2(HitObjects[i].Location.X - 4, HitObjects[i].Location.Y - 4);
                            break;
                        }
                    }
                    else
                    {
                        break;
                    }
                }
            }
            foreach(var pair in toChange)
            {
                HitObjects[pair.Key].Location = pair.Value;
            }

        }

        private bool hardRock = false;

        public void applyHardRock()
        {
            if (hardRock)
                return;
            hardRock = true;

            CircleSize = CircleSize * 1.3f;
            OverallDifficulty = OverallDifficulty * 1.4f;
            ApproachRate = ApproachRate * 1.4f;
            if (CircleSize > 10)
                CircleSize = 10;
            if (OverallDifficulty > 10)
                OverallDifficulty = 10;
            if (ApproachRate > 10)
                ApproachRate = 10;
        }

		public override string ToString()
		{
			return this.Artist + " " + this.Title + " [" + this.Version + "]";
		}
	}
}
