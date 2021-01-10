using ReactiveUI;
using System;

namespace OsuMissAnalyzer.UI.ViewModels
{
    public enum ReplayFind { RECENT, BEATMAP, MANUAL }
    public class ReplayOptionBoxViewModel : ViewModelBase
    {
        private ReplayFind? result;

        public ReplayFind? Result { get => result; set => this.RaiseAndSetIfChanged(ref result, value); }

        public Options Options { get; }

        public Action<bool> CloseAction { get; set; }

        public ReplayOptionBoxViewModel(Options options)
        {
            Options = options;
        }


        public void Close(ReplayFind? result)
        {
            Result = result;
            CloseAction(result != null);
        }
    }
}
