using Avalonia;
using Avalonia.Controls;
using Avalonia.Markup.Xaml;
using OsuMissAnalyzer.UI.ViewModels;
using System;
using System.Diagnostics;
using System.Reactive.Linq;

namespace OsuMissAnalyzer.UI.Views
{
    public partial class MissWindow : Window
    {
        public MissWindow()
        {
            InitializeComponent();
            DataContextChanged += (a, b) =>
                {
                    if (DataContext != null && DataContext is MissWindowViewModel vm)
                    {
                        MissCanvas.GetObservable(BoundsProperty).Subscribe(value => vm.Bounds = value);
                        PointerWheelChanged += vm.OnMouseWheel;
                        KeyDown += vm.OnKeyDown;
                        PointerReleased += vm.OnMouseReleased;
                    }
                };
        }
    }
}
