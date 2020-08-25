using System;
using System.Windows.Forms;

namespace OsuMissAnalyzer.UI
{
    public class MissWindowController
    {
        public MissAnalyzer Model { get; }
        public MissWindow View { get; set; }
        public ReplayLoader Loader { get; set; }

        public MissWindowController(MissAnalyzer model, ReplayLoader loader)
        {
            Model = model;
            Loader = loader;
        }
        public void UpdateView()
        {
            View.Image = Model.DrawSelectedHitObject(View.Area);
        }
        public void OnKeyDown(KeyEventArgs e)
        {
            switch (e.KeyCode)
            {
                case System.Windows.Forms.Keys.Up:
                    Model.ScaleChange(1);
                    break;
                case System.Windows.Forms.Keys.Down:
                    Model.ScaleChange(-1);
                    break;
                case System.Windows.Forms.Keys.Right:
                    Model.NextObject();
                    break;
                case System.Windows.Forms.Keys.Left:
                    Model.PreviousObject();
                    break;
                case System.Windows.Forms.Keys.T:
                    Model.ToggleOutlines();
                    break;
                case System.Windows.Forms.Keys.P:
                    Model.DrawAllMisses(View.Area);
                    break;
                case System.Windows.Forms.Keys.R:
                    Loader.Load(null, null);
                    break;
                case System.Windows.Forms.Keys.A:
                    break;
            }
        }

        public void OnMouseWheel(MouseEventArgs e)
        {
            Model.ScaleChange(Math.Sign(e.Delta));
        }
    }
}