using Avalonia;
using Avalonia.Controls;
using Avalonia.Markup.Xaml;
using OsuMissAnalyzer.UI.ViewModels;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Reactive.Linq;

namespace OsuMissAnalyzer.UI.Views
{
    public class MessageBox : Window
    {
        public List<string> Buttons {
            set
            {
                var buttons = this.FindControl<StackPanel>("Buttons");
                var panel = this.FindControl<Panel>("ButtonsOuter");
                buttons.Children.AddRange(
                    value.Select(v =>
                    {
                        var button = new Button
                        {
                            Content = v,
                            HorizontalAlignment = Avalonia.Layout.HorizontalAlignment.Center,
                        };
                        button.Bind(WidthProperty, panel.GetObservable(BoundsProperty).Select(bounds => Math.Min(100, (bounds.Width - buttons.Spacing * (value.Count - 1)) / value.Count)));
                        button.Click += (a, b) => this.Close(v);
                        return button;
                    }));
            }
        }
        public MessageBox()
        {
            InitializeComponent();
#if DEBUG
            this.AttachDevTools();
#endif
        }

        private void InitializeComponent()
        {
            AvaloniaXamlLoader.Load(this);
        }
    }
}
