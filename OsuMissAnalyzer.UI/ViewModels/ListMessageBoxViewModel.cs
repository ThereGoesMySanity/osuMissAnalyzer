using OsuMissAnalyzer.UI.Models;
using ReactiveUI;
using System.Collections.Generic;
using System.Reactive.Linq;
using System;

namespace OsuMissAnalyzer.UI.ViewModels
{
    public class ListMessageBoxViewModel : ViewModelBase
    {
        private ReplayListItem result;
        private List<ReplayListItem> items;

        public ReplayListItem Result { get => result; set => this.RaiseAndSetIfChanged(ref result, value); }
        public List<ReplayListItem> Items { get => items; set => this.RaiseAndSetIfChanged(ref items, value); }

        public ListMessageBoxViewModel()
        {
            this.WhenAnyValue(x => x.Items).Subscribe(v => { if (v != null && v.Count > 0) result = v[0]; });
        }
    }
}
