using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Markup.Xaml;
using OsuMissAnalyzer.UI.ViewModels;
using OsuMissAnalyzer.UI.Views;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Threading.Tasks;
using Avalonia.Threading;

namespace OsuMissAnalyzer.UI
{
    public class App : Application
    {
        public static Window Window { get; private set; }
        public UIReplayLoader ReplayLoader { get; private set; }
        public App() { }
        public App(UIReplayLoader replayLoader)
        {
            ReplayLoader = replayLoader;
        }

        public override void Initialize()
        {
            AvaloniaXamlLoader.Load(this);
        }

        public override async void OnFrameworkInitializationCompleted()
        {
            if (ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop)
            {
                Window = desktop.MainWindow = new MissWindow();
                if (ReplayLoader.Options.WatchDogMode && (!ReplayLoader.Options.Settings.ContainsKey("osudir") || string.IsNullOrEmpty(ReplayLoader.Options.Settings["osudir"])))
                {
                    await ShowMessageBox("OsuDir is required when WatchDogMode is enabled.");
                    Window.Close();
                    return;
                }

                await Load(ReplayLoader);
                if (ReplayLoader.Options.WatchDogMode)
                {
                    ReplayLoader.NewReplay += ReplayLoaderOnNewReplay;
                    ReplayLoader.WatchForNewReplays();
                }
            }

            base.OnFrameworkInitializationCompleted();
        }

        private void ReplayLoaderOnNewReplay(object? sender, EventArgs e)
        {
            _ = Load(ReplayLoader);
        }

        public static async Task Load(UIReplayLoader loader)
        {
            if (!Dispatcher.UIThread.CheckAccess())
            {
                await Dispatcher.UIThread.InvokeAsync(() => Load(loader));
                return;
            }
            string errorMessage, result = null;
            do
            {
                try
                {
                    if ((errorMessage = await loader.Load()) == null)
                    {
                        Window.DataContext = new MissWindowViewModel(loader);
                        return;
                    }

                    if (loader.Options.WatchDogMode)
                        return;
                }
                catch (Exception e)
                {
                    errorMessage = e.Message;
                    File.WriteAllText("exception.log", e.ToString());
                }
                if (errorMessage != null)
                {
                    result = await ShowMessageBox($"An error has occurred.\n{errorMessage}", "OK", "Reload");
                    if (result != "Reload")
                    {
                        Window.Close();
                        return;
                    }
                    else
                    {
                        loader = new UIReplayLoader { Options = loader.Options };
                    }
                }
            } while (result == "Reload");
        }

        public static async Task ShowMessageBox(string message)
        {
            await ShowMessageBox(message, "OK");
        }

        public static async Task<string> ShowMessageBox(string message, params string[] buttons)
        {
            var window = new MessageBox
            {
                DataContext = new MessageBoxViewModel
                {
                    Message = message,
                },
                Buttons = new List<string>(buttons),
            };
            return await window.ShowDialog<string>(Window);
        }
    }
}