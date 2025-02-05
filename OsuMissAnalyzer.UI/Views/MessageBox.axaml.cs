using Avalonia;
using Avalonia.Controls;
using Avalonia.Interactivity;
using Avalonia.Markup.Xaml;
using OsuMissAnalyzer.UI.ViewModels;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Reactive.Linq;

namespace OsuMissAnalyzer.UI.Views
{
    public partial class MessageBox : Window
    {
        public MessageBox()
        {
            InitializeComponent();
        }

        private void ButtonClicked(object? sender, RoutedEventArgs args)
        {
            if (sender is Button b) Close(b.Content as string);
        }
    }
}
