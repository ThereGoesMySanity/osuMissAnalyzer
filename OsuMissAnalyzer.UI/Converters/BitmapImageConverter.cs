using Avalonia.Data.Converters;
using SixLabors.ImageSharp;
using System.IO;

namespace OsuMissAnalyzer.UI.Converters
{
    public static class BitmapImageConverter
    {
        public static readonly IValueConverter BitmapToImage =
            new FuncValueConverter<Image, Avalonia.Media.Imaging.Bitmap>(b =>
            {
                if (b == null) return null;
                using (MemoryStream ms = new MemoryStream())
                {
                    b.SaveAsPng(ms);
                    ms.Position = 0;
                    return new Avalonia.Media.Imaging.Bitmap(ms);
                }
            });
    }
}
