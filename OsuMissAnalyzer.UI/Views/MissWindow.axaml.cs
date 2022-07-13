using Avalonia;
using Avalonia.Controls;
using Avalonia.Markup.Xaml;
using OsuMissAnalyzer.UI.ViewModels;
using System;
using System.Diagnostics;
using System.Reactive.Linq;

namespace OsuMissAnalyzer.UI.Views
{
    public class MissWindow : Window
    {
        public MissWindow()
        {
            this.InitializeComponent();
#if DEBUG
            this.AttachDevTools();
#endif
        }

        private void InitializeComponent()
        {
            AvaloniaXamlLoader.Load(this);
            DataContextChanged += (a, b) =>
                {
                    var canvas = this.FindControl<Canvas>("MissCanvas");
                    if (DataContext != null && DataContext is MissWindowViewModel vm)
                    {
                        canvas.GetObservable(BoundsProperty).Subscribe(value => vm.Bounds = value);
                        PointerWheelChanged += vm.OnMouseWheel;
                        KeyDown += vm.OnKeyDown;
                        PointerReleased += vm.OnMouseReleased;
                    }
                };
        }
    }
}
