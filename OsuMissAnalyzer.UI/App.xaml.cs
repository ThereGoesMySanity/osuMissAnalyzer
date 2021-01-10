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
                    if (!await ReplayLoader.Load()) return;
                    if (ReplayLoader.Replay == null || ReplayLoader.Beatmap == null)
                    {
                        errorMessage = "Couldn't find " + (ReplayLoader.Replay == null ? "replay" : "beatmap");
                        return;
                    }
                    Window.DataContext = new MissWindowViewModel(ReplayLoader);
                    Debug.WriteLine("testest");
                } catch (Exception e)
                {
                    errorMessage = e.Message;
                    File.WriteAllText("exception.log", e.ToString());
                }
                if (errorMessage != null)
                {
                    ShowErrorDialog(errorMessage);
                }
            }

            base.OnFrameworkInitializationCompleted();
        }

        public static void ShowErrorDialog(string message)
        {
            //TODO: error message box
        }
    }
}