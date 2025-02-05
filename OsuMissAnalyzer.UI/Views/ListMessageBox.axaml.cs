using Avalonia.Controls;
using OsuMissAnalyzer.UI.Models;
using OsuMissAnalyzer.UI.ViewModels;
using Avalonia.Input;
using Avalonia.Interactivity;

namespace OsuMissAnalyzer.UI.Views
{
    public partial class ListMessageBox : Window
    {
        public ReplayListItem Result => (DataContext as ListMessageBoxViewModel)!.Result;
        public ListMessageBox()
        {
            InitializeComponent();
            Results.LoadingRow += (_, e) =>
            {
                e.Row.DoubleTapped += (o, args) => Close(Result != null);
            };
        }

        public void ButtonClicked(object? sender, RoutedEventArgs args)
        {
            if (sender is Button b) Close((b.Content as string) == "Ok");
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
