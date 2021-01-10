using Avalonia;
using Avalonia.Controls;
using Avalonia.Markup.Xaml;
using OsuMissAnalyzer.UI.ViewModels;
using System;

namespace OsuMissAnalyzer.UI.Views
{
    public class ReplayOptionBox : Window
    {
        public ReplayFind Result => (DataContext as ReplayOptionBoxViewModel).Result.Value;
        public ReplayOptionBox()
        {
            this.InitializeComponent();
#if DEBUG
            this.AttachDevTools();
#endif
            DataContextChanged += (a, b) =>
            {
                if (DataContext != null) (DataContext as ReplayOptionBoxViewModel).CloseAction = (b) => Close(b);
            };
        }

        private void InitializeComponent()
        {
            AvaloniaXamlLoader.Load(this);
        }
    }
}
