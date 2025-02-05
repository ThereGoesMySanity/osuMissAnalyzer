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
        private string[] options;

        public required string Message { get => message; set => this.RaiseAndSetIfChanged(ref message, value); }

        public required string[] Options { get => options; set => this.RaiseAndSetIfChanged(ref options, value); }
    }
}
