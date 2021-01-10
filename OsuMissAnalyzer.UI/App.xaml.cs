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

namespace OsuMissAnalyzer.UI
{
    public class App : Application
    {
        public static Window Window { get; private set; }
        public UIReplayLoader ReplayLoader { get; }
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
                string errorMessage = null;
                try
                {
                    Window = desktop.MainWindow = new MissWindow();
                    await ReplayLoader.Load();
                    if (ReplayLoader.Replay == null || ReplayLoader.Beatmap == null)
                    {
                        errorMessage = "Couldn't find " + (ReplayLoader.Replay == null ? "replay" : "beatmap");
                    }
                    else
                    {
                        Window.DataContext = new MissWindowViewModel(ReplayLoader);
                    }
                }
                catch (Exception e)
                {
                    errorMessage = e.Message;
                    File.WriteAllText("exception.log", e.ToString());
                }
                if (errorMessage != null)
                {
                    await ShowMessageBox($"An error has occured.\n{errorMessage}");
                    Window.Close();
                }
            }

            base.OnFrameworkInitializationCompleted();
        }

        public static async Task ShowMessageBox(string message)
        {
            Button button;
            var window = new Window
            {
                Height = 200,
                Width = 200,
                Content = new StackPanel
                {
                    Spacing = 4,
                    Children =
                    {
                        new TextBlock { Text = message, HorizontalAlignment = Avalonia.Layout.HorizontalAlignment.Center },
                        (button = new Button
                        {
                            HorizontalAlignment = Avalonia.Layout.HorizontalAlignment.Center,
                            Content = "OK",
                        })
                    }
                }
            };
            button.Click += (_, __) => window.Close();
            await window.ShowDialog(Window);
        }
    }
}