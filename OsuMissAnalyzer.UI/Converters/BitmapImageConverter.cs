using Avalonia.Data.Converters;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing.Imaging;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace OsuMissAnalyzer.UI.Converters
{
    public static class BitmapImageConverter
    {
        public static readonly IValueConverter BitmapToImage =
            new FuncValueConverter<System.Drawing.Bitmap, Avalonia.Media.Imaging.Bitmap>(b =>
            {
                if (b == null) return null;
                using (MemoryStream ms = new MemoryStream())
                {
                    b.Save(ms, ImageFormat.Png);
                    ms.Position = 0;
                    return new Avalonia.Media.Imaging.Bitmap(ms);
                }
            });
    }
}
