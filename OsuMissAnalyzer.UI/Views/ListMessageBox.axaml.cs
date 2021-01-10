using Avalonia;
using Avalonia.Controls;
using Avalonia.Markup.Xaml;
using OsuMissAnalyzer.UI.Models;
using OsuMissAnalyzer.UI.ViewModels;
using System.Diagnostics;

namespace OsuMissAnalyzer.UI.Views
{
    public class ListMessageBox : Window
    {
        public ReplayListItem Result => (DataContext as ListMessageBoxViewModel).Result;
        public ListMessageBox()
        {
            this.InitializeComponent();
#if DEBUG
            this.AttachDevTools();
#endif
        }

        public void Close(bool ok)
        {
            base.Close(ok);
        }

        private void InitializeComponent()
        {
            AvaloniaXamlLoader.Load(this);
        }
    }
}
