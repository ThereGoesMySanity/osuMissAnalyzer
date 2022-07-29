using System;
using System.Linq;
using System.Numerics;
using System.Reflection;
using System.Runtime.Serialization;
using SixLabors.ImageSharp;

namespace OsuMissAnalyzer.Core
{
    public class ColorScheme
    {
        public enum Type {Light, Dark};
        public static readonly ColorScheme Default = new("Default", Type.Light)
        {
            BackgroundColor = Color.White,
            PlayfieldColor = Color.DarkGray,
            TextColor = Color.Black,
            LineColor = Color.Black,
            MidpointColor = Color.Red,
            Color300 = Color.FromRgb(23, 175, 235),
            Color100 = Color.FromRgb(0, 255, 60),
            Color50 = Color.Purple,
            CircleStartColor = Color.FromRgb(100, 100, 100),
            CircleEndColor = Color.FromRgb(200, 200, 200),
            CircleSelectedColor = Color.FromRgb(150, 100, 100),
            SliderColor = Color.DarkGoldenrod,
        };
        public static readonly ColorScheme Dark = new("Dark", Type.Dark)
        {
            BackgroundColor = Color.FromRgb(16, 16, 16),
            PlayfieldColor = Color.DarkGray,
            TextColor = Color.LightGrey,
            LineColor = Color.LightGrey,
            MidpointColor = Color.Red,
            Color300 = Color.SkyBlue,
            Color100 = Color.SpringGreen,
            Color50 = Color.Violet,
            CircleStartColor = Color.FromRgb(120, 120, 120),
            CircleEndColor = Color.FromRgb(20, 20, 20),
            CircleSelectedColor = Color.FromRgb(150, 100, 100),
            SliderColor = Color.Goldenrod,
        };
        public string Name { get; init; }
        public Type SchemeType;
        public ColorScheme(string name, Type type) { Name = name; SchemeType = type; }

        public Color BackgroundColor { get; init; }
        public Color PlayfieldColor { get; init; }
        public Color TextColor { get; init; }
        public Color LineColor { get; init; }
        public Color MidpointColor { get; init; }
        public Color Color300 { get; init; }
        public Color Color100 { get; init; }
        public Color Color50 { get; init; }
        public Color CircleStartColor { get; init; }
        public Color CircleEndColor { get; init; }
        public Color CircleSelectedColor { get; init; }
        public Color SliderColor { get; init; }
        public Color GetCircleColor(float time)
        {
            if (time == 0) return CircleSelectedColor;
            Vector4 start = ((Vector4)CircleStartColor);
            Vector4 end = ((Vector4)CircleEndColor);
            return new Color(Vector4.Lerp(start, end, time));
        }

        public static ColorScheme Parse(string s) {
            foreach (var scheme in typeof(ColorScheme).GetFields(BindingFlags.Public | BindingFlags.Static)
                    .Where(f => f.FieldType == typeof(ColorScheme))
                    .Select(f => (ColorScheme)f.GetValue(null)))
            {
                if (scheme.Name.Equals(s, StringComparison.OrdinalIgnoreCase)) return scheme;
            }
            return null;
        }
    }
}