using System;
using System.Drawing;
using System.Windows.Forms;

namespace OsuMissAnalyzer.UI
{
    public class MissWindow : Form
    {
        public Rectangle Area { get; }
        public MissWindowController Controller { get; }
        public Bitmap Image { get; set; }

        private readonly Graphics gOut;
        private const int size = 480;
        public MissWindow(MissWindowController controller)
        {
            Text = "Miss Analyzer";
            Size = new Size(size, size + SystemInformation.CaptionHeight);
            Area = base.ClientRectangle;
            gOut = Graphics.FromHwnd(Handle);

            FormBorderStyle = FormBorderStyle.FixedSingle;
            Controller = controller;
            Controller.View = this;
            Controller.UpdateView();
        }
        protected override void OnMouseWheel(MouseEventArgs e)
        {
            base.OnMouseWheel(e);
            Controller.OnMouseWheel(e);
        }
        protected override void OnKeyDown(KeyEventArgs e)
        {
            base.OnKeyDown(e);
            Controller.OnKeyDown(e);
        }

        protected override void OnPaint(PaintEventArgs e)
        {
            base.OnPaint(e);
            if (Image != null)
                gOut.DrawImage(Image, Area);
        }

    }
}