using Avalonia;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Markup.Xaml;
using OsuMissAnalyzer.UI.ViewModels;
using OsuMissAnalyzer.UI.Views;

namespace OsuMissAnalyzer.UI
{
    public class App : Application
    {
        public override void Initialize()
        {
            AvaloniaXamlLoader.Load(this);
        }

        public override void OnFrameworkInitializationCompleted()
        {
            if (ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop)
            {
                string errorMessage = null;
                try
                {
                    Options options = new Options("options.cfg", Program.optList);
                    UIReplayLoader replayLoader = new UIReplayLoader(options);
                    if (!replayLoader.Load(replay, beatmap)) return;
                    if (replayLoader.Replay == null || replayLoader.Beatmap == null)
                    {
                        errorMessage = "Couldn't find " + (replayLoader.Replay == null ? "replay" : "beatmap");
                        return;
                    }

                    desktop.MainWindow = new MainWindow
                    {
                        DataContext = new MainWindowViewModel(),
                    };
                    // missAnalyzer = new MissAnalyzer(replayLoader);
                    // controller = new MissWindowController(missAnalyzer, replayLoader);
                    // window = new MissWindow(controller);
                    // Application.Run(window);
                } catch (Exception e)
                {
                    errorMessage = e.Message;
                    File.WriteAllText("exception.log", e.ToString());
                }
                if (errorMessage != null)
                {
                    ShowErrorDialog(message);
                }
            }

            base.OnFrameworkInitializationCompleted();
        }

        public static void ShowErrorDialog(string message)
        {
            if (!headless) //TODO: error message box
            else Debug.Print(message);
        }
    }
}