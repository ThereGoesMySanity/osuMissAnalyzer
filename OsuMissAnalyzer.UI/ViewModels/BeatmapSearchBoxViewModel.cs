using OsuDbAPI;
using ReactiveUI;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace OsuMissAnalyzer.UI.ViewModels
{
    public class BeatmapSearchBoxViewModel : ViewModelBase
    {
        string textOld = "";
        public Beatmap Result { get => result; set => this.RaiseAndSetIfChanged(ref result, value); }
        public OsuDbFile Database { get => database; set => this.RaiseAndSetIfChanged(ref database, value); }
        public string SearchText { get => searchText; set => this.RaiseAndSetIfChanged(ref searchText, value); }
        public ObservableCollection<Beatmap> Results { get; set; }

        private Task updateTask;
        private CancellationTokenSource source;
        private string searchText;
        private OsuDbFile database;
        private Beatmap result;

        public BeatmapSearchBoxViewModel(OsuDbFile database)
        {
            Results = new ObservableCollection<Beatmap>();
            Database = database;
            this.WhenAnyValue(x => x.SearchText).Subscribe(value => TextChanged());
        }

        public async void TextChanged()
        {
            string[] termsOld = textOld.Split(new[] { ',', ' ', '!' }, StringSplitOptions.RemoveEmptyEntries).ToArray();
            string[] terms = SearchText.Split(new[] { ',', ' ', '!' }, StringSplitOptions.RemoveEmptyEntries).ToArray();

            if (updateTask != null && !updateTask.IsCompleted)
            {
                source.Cancel();
                try
                {
                    await updateTask;
                }
                catch (OperationCanceledException) { }
                finally { source.Dispose(); }
            }

            source = new CancellationTokenSource();
            updateTask = Task.Run(() =>
                {
                    var token = source.Token;
                    List<Beatmap> items = Results.Cast<Beatmap>().ToList();
                    if (termsOld.Any(o => !SearchText.Contains(o)))
                    {
                        items = new List<Beatmap>(Database.Beatmaps);
                    }
                    items.RemoveAll(o =>
                    {
                        token.ThrowIfCancellationRequested();
                        return terms.Any(t => o.SearchableTerms.All(s => s.IndexOf(t, StringComparison.InvariantCultureIgnoreCase) == -1));
                    });
                    if (!token.IsCancellationRequested)
                    {
                        lock (Results)
                        {
                            Results.Clear();
                            items.ForEach(i => Results.Add(i));
                        }
                    }
                }
            );

            textOld = SearchText;
        }
    }
}
