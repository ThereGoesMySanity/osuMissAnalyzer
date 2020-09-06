using System;
using System.Drawing.Imaging;
using System.IO;
using System.Windows.Forms;
using OsuMissAnalyzer.Core;

namespace OsuMissAnalyzer.UI
{
    public class MissWindowController
    {
        public MissAnalyzer Model { get; }
        public MissWindow View { get; set; }
        public UIReplayLoader Loader { get; set; }

        public MissWindowController(MissAnalyzer model, UIReplayLoader loader)
        {
            Model = model;
            Loader = loader;
        }
        public void UpdateView()
        {
            View.Image = Model.DrawSelectedHitObject(View.Area);
            View.Invalidate();
        }
        public void OnKeyDown(KeyEventArgs e)
        {
            switch (e.KeyCode)
            {
                case Keys.Up:
                    Model.ScaleChange(1);
                    break;
                case Keys.Down:
                    Model.ScaleChange(-1);
                    break;
                case Keys.Right:
                    Model.NextObject();
                    break;
                case Keys.Left:
                    Model.PreviousObject();
                    break;
                case Keys.T:
                    Model.ToggleOutlines();
                    break;
                case Keys.P:
                    int i = 0;
                    foreach (var img in Model.DrawAllMisses(View.Area))
                    {
                        string filename = $"{Path.GetFileNameWithoutExtension(Loader.Replay.Filename)}.{i++}.png";
                        img.Save(filename, System.Drawing.Imaging.ImageFormat.Png);
                    }
                    break;
                case Keys.R:
                    Loader.Load(null, null);
                    break;
                case Keys.A:
                    Model.ToggleDrawAllHitObjects();
                    break;
            }
            UpdateView();
        }

        public void OnMouseWheel(MouseEventArgs e)
        {
            Model.ScaleChange(Math.Sign(e.Delta));
            UpdateView();
        }
    }
}