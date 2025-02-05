using Avalonia;
using Avalonia.Controls;
using Avalonia.Markup.Xaml;
using OsuMissAnalyzer.UI.ViewModels;
using System;

namespace OsuMissAnalyzer.UI.Views
{
    public partial class ReplayOptionBox : Window
    {
        public ReplayFind? Result => (DataContext as ReplayOptionBoxViewModel)!.Result;
        public ReplayOptionBox()
        {
            InitializeComponent();
            DataContextChanged += (a, b) =>
            {
                if (DataContext != null) (DataContext as ReplayOptionBoxViewModel)!.CloseAction = (b) => Close(b);
            };
        }
    }
}
