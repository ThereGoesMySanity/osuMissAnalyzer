using OsuMissAnalyzer.UI.Models;
using ReactiveUI;
using System.Collections.Generic;

namespace OsuMissAnalyzer.UI.ViewModels
{
    public class ListMessageBoxViewModel : ViewModelBase
    {
        private ReplayListItem result;
        private List<ReplayListItem> items;

        public ReplayListItem Result { get => result; set => this.RaiseAndSetIfChanged(ref result, value); }
        public List<ReplayListItem> Items { get => items; set => this.RaiseAndSetIfChanged(ref items, value); }
    }
}
