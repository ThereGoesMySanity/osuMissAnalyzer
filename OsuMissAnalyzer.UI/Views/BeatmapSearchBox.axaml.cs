using Avalonia;
using Avalonia.Controls;
using Avalonia.Markup.Xaml;
using OsuDbAPI;
using OsuMissAnalyzer.UI.ViewModels;

namespace OsuMissAnalyzer.UI.Views
{
    public class BeatmapSearchBox : Window
    {
        public Beatmap Result => (DataContext as BeatmapSearchBoxViewModel).Result;

        public BeatmapSearchBox()
        {
            this.InitializeComponent();
#if DEBUG
            this.AttachDevTools();
#endif
        }

        public void Close(bool success)
        {
            base.Close(success);
        }

        private void InitializeComponent()
        {
            AvaloniaXamlLoader.Load(this);
        }

    }
}
