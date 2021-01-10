using Avalonia;
using Avalonia.Controls;
using Avalonia.Input;
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
            this.FindControl<TextBox>("SearchBox").KeyDown += SearchBox_KeyDown;
        }

        private void SearchBox_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Enter)
            {
                e.Handled = true;
                (DataContext as BeatmapSearchBoxViewModel).StartSearch(true);
            }
        }
    }
}
