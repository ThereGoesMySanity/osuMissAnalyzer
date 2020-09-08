using OsuDbAPI;
using System;
using System.Collections;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace OsuMissAnalyzer.UI
{
    public partial class BeatmapSearchBox : Form
    {
        OsuDbFile database;
        string textOld = "";
        public BeatmapSearchBox()
        {
            InitializeComponent();
        }

        public void SetContent(OsuDbFile database)
        {
            this.database = database;
            listBox1.Items.AddRange(database.Beatmaps.ToArray());
        }
        public Beatmap Result => listBox1.SelectedItem as Beatmap;

        private void SubmitResult()
        {
            DialogResult = DialogResult.OK;
            Close();
        }

        private void ListMessageBox_Load(object sender, EventArgs e)
        {

        }

        private void textBox1_TextChanged(object sender, EventArgs e)
        {
            string[] termsOld = textOld.Split(new[] { ',', ' ', '!' }, StringSplitOptions.RemoveEmptyEntries).ToArray();
            string[] terms = textBox1.Text.Split(new[] { ',', ' ', '!' }, StringSplitOptions.RemoveEmptyEntries).ToArray();

            List <Beatmap> items = listBox1.Items.Cast<Beatmap>().ToList();
            listBox1.Items.Clear();

            if (termsOld.Any(o => !textBox1.Text.Contains(o)))
            {
                items = new List<Beatmap>(database.Beatmaps);
            }
            Filter(items, terms);
            listBox1.Items.AddRange(items.ToArray());

            textOld = textBox1.Text;
        }
        private void Filter(List<Beatmap> list, string[] terms)
        {
            list.RemoveAll(o => terms.Any(t => o.SearchableTerms.All(s => s.IndexOf(t, StringComparison.InvariantCultureIgnoreCase) == -1)));
        }

        private void listBox1_DoubleClick(object sender, EventArgs e)
        {
            SubmitResult();
        }
    }
}
