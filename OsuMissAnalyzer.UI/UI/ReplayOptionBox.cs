using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace OsuMissAnalyzer.UI.UI
{
    public enum ReplayFind { RECENT, BEATMAP, MANUAL }
    public partial class ReplayOptionBox : Form
    {
        public ReplayFind Result;
        public ReplayOptionBox(Options opts)
        {
            InitializeComponent();
            if (opts.HasDatabase)
            {
                recent.Enabled = true;
                beatmap.Enabled = true;
            }
        }

        private void ReplayOptionBox_Load(object sender, EventArgs e)
        {

        }

        private void recent_Click(object sender, EventArgs e)
        {
            Result = ReplayFind.RECENT;
        }

        private void beatmap_Click(object sender, EventArgs e)
        {
            Result = ReplayFind.BEATMAP;
        }

        private void manual_Click(object sender, EventArgs e)
        {
            Result = ReplayFind.MANUAL;
        }
    }
}
