using ReactiveUI;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace OsuMissAnalyzer.UI.ViewModels
{
    class MessageBoxViewModel : ViewModelBase
    {
        private string message;

        public string Message { get => message; set => this.RaiseAndSetIfChanged(ref message, value); }
    }
}
