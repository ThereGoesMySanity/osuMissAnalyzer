using System;
using System.IO;
using Avalonia;
using Avalonia.Input;
using OsuMissAnalyzer.Core;
using ReactiveUI;
using System.Reactive.Linq;
using SixLabors.ImageSharp;

namespace OsuMissAnalyzer.UI.ViewModels
{
    public class MissWindowViewModel : ViewModelBase
    {

        private MissAnalyzer analyzer;
        public MissAnalyzer Analyzer { get => analyzer; set => this.RaiseAndSetIfChanged(ref analyzer, value); }

        private UIReplayLoader loader;
        public UIReplayLoader Loader { get => loader; set => this.RaiseAndSetIfChanged(ref loader, value); }

        private Image image;
        public Image Image { get => image; set => this.RaiseAndSetIfChanged(ref image, value); }

        private Rect bounds;
        public Rect Bounds { get => bounds; set => this.RaiseAndSetIfChanged(ref bounds, value); }

        public Rectangle Area => new Rectangle(0, 0, (int)Bounds.Width, (int)Bounds.Height);

        public MissWindowViewModel(UIReplayLoader loader)
        {
            Loader = loader;
            Analyzer = new MissAnalyzer(loader);
            this.WhenAnyValue(x => x.Bounds, x => x.Analyzer).Subscribe(((Rect, MissAnalyzer) _) => UpdateImage());
        }
        public async void OnKeyDown(object source, KeyEventArgs e)
        {
            switch (e.Key)
            {
                case Key.Up:
                    Analyzer.ScaleChange(-1);
                    break;
                case Key.Down:
                    Analyzer.ScaleChange(1);
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
                        await img.SaveAsPngAsync(filename);
                    }
                    break;
                case Key.R:
                    App.Load(new UIReplayLoader { Options = Loader.Options });
                    break;
                case Key.A:
                    Analyzer.ToggleDrawAllHitObjects();
                    break;
            }
            UpdateImage();
        }

        public void OnMouseWheel(object source, PointerWheelEventArgs e)
        {
            Analyzer.ScaleChange(-Math.Sign(e.Delta.Y));
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