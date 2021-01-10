using System;
using System.Diagnostics;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using Avalonia;
using Avalonia.Input;
using OsuMissAnalyzer.Core;
using ReactiveUI;
using System.Reactive.Linq;

namespace OsuMissAnalyzer.UI.ViewModels
{
    public class MissWindowViewModel : ViewModelBase
    {

        private MissAnalyzer analyzer;
        public MissAnalyzer Analyzer { get => analyzer; set => this.RaiseAndSetIfChanged(ref analyzer, value); }

        private UIReplayLoader loader;
        public UIReplayLoader Loader { get => loader; set => this.RaiseAndSetIfChanged(ref loader, value); }

        private Bitmap image;
        public Bitmap Image { get => image; set => this.RaiseAndSetIfChanged(ref image, value); }

        private Rect bounds;
        public Rect Bounds { get => bounds; set => this.RaiseAndSetIfChanged(ref bounds, value); }

        public Rectangle Area => new Rectangle(0, 0, (int)Bounds.Width, (int)Bounds.Height);

        public MissWindowViewModel(UIReplayLoader loader)
        {
            Initialize(loader);
            this.WhenAnyValue(x => x.Bounds, x => x.Analyzer).Subscribe(((Rect, MissAnalyzer) _) => UpdateImage());
        }
        public void Initialize(UIReplayLoader loader)
        {
            Analyzer = new MissAnalyzer(loader);
            Loader = loader;
        }
        public async void OnKeyDown(object source, KeyEventArgs e)
        {
            switch (e.Key)
            {
                case Key.Up:
                    Analyzer.ScaleChange(1);
                    break;
                case Key.Down:
                    Analyzer.ScaleChange(-1);
                    break;
                case Key.Right:
                    Analyzer.NextObject();
                    break;
                case Key.Left:
                    Analyzer.PreviousObject();
                    break;
                case Key.T:
                    Analyzer.ToggleOutlines();
                    break;
                case Key.P:
                    int i = 0;
                    foreach (var img in Analyzer.DrawAllMisses(Area))
                    {
                        string filename = $"{Path.GetFileNameWithoutExtension(Loader.Replay.Filename)}.{i++}.png";
                        img.Save(filename, ImageFormat.Png);
                    }
                    break;
                case Key.R:
                    var loader = new UIReplayLoader
                    {
                        Options = Loader.Options
                    };
                    await loader.Load();
                    Initialize(loader);
                    break;
                case Key.A:
                    Analyzer.ToggleDrawAllHitObjects();
                    break;
            }
            UpdateImage();
        }

        public void OnMouseWheel(object source, PointerWheelEventArgs e)
        {
            Analyzer.ScaleChange(Math.Sign(e.Delta.Y));
            UpdateImage();
        }

        public void UpdateImage()
        {
            if (Area.Width > 0 && Area.Height > 0)
            {
                Image = Analyzer.DrawSelectedHitObject(Area);
            }
        }
    }
}