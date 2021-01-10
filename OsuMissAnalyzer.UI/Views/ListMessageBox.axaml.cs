using Avalonia;
using Avalonia.Controls;
using Avalonia.Markup.Xaml;
using OsuMissAnalyzer.UI.Models;
using OsuMissAnalyzer.UI.ViewModels;
using System.Reactive.Linq;
using System;
using System.Linq;
using Avalonia.Input;

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

        protected override void OnKeyDown(KeyEventArgs e)
        {
            switch (e.Key)
            {
                //case Key.Tab:
                //    DataGrid d = this.FindControl<DataGrid>("Results");
                //    var vm = DataContext as ListMessageBoxViewModel;
                //    d.SelectedIndex = (d.SelectedIndex + 1) % vm.Items.Count;
                //    d.ScrollIntoView(vm.Result, null);
                //    e.Handled = true;
                //    break;
                case Key.Enter:
                    e.Handled = true;
                    if (Result != null) Close(true);
                    break;
            }
           
            base.OnKeyDown(e);
        }

    }
}
