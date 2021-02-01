using OsuDbAPI;
using ReactiveUI;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Data;
using System.Diagnostics;
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
        public string SearchText { get => searchText; set => this.RaiseAndSetIfChanged(ref searchText, value); }
        public ObservableCollection<Beatmap> Results { get; set; }

        private Task updateTask;
        private CancellationTokenSource source;
        private string searchText;
        private Beatmap result;

        private List<Beatmap> beatmaps;

        public BeatmapSearchBoxViewModel(Options options)
        {
            Results = new ObservableCollection<Beatmap>();
            beatmaps = options.Database.Beatmaps.Where(b => b.Hash != null && options.ScoresDb.scores.ContainsKey(b.Hash)).ToList();
            beatmaps.ForEach(b => Results.Add(b));
            this.WhenAnyValue(x => x.SearchText).Subscribe(value => StartSearch(false));
        }

        public void StartSearch(bool full)
        {
            if (SearchText == null) return;
            string text = SearchText;
            string[] termsOld = textOld.Split(new[] { ',', ' ', '!' }, StringSplitOptions.RemoveEmptyEntries).ToArray();
            string[] terms = text.Split(new[] { ',', ' ', '!' }, StringSplitOptions.RemoveEmptyEntries).ToArray();

            if (updateTask != null && !updateTask.IsCompleted)
            {
                source.Cancel();
                try
                {
                    updateTask.Wait();
                }
                catch (OperationCanceledException) { }
                catch (AggregateException ex) when (ex.InnerException is OperationCanceledException) { }
                finally { source.Dispose(); }
            }

            source = new CancellationTokenSource();
            updateTask = Task.Run(() =>
                {
                    var token = source.Token;
                    List<Beatmap> items = Results.Cast<Beatmap>().ToList();
                    if (full || termsOld.Any(o => !text.Contains(o)))
                    {
                        items = new List<Beatmap>(beatmaps);
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
                            textOld = text;
                        }
                    }
                }
            );

        }
    }
}
