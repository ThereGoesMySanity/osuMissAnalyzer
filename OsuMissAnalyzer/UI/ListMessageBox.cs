using System;
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
    public partial class ListMessageBox : Form
    {
        private List<ReplayListItem> items;
        public ListMessageBox()
        {
            InitializeComponent();
        }

        public void SetContent(List<ReplayListItem> items)
        {
            dataGridView.Rows.Clear();
            this.items = items;
            foreach(var item in items)
            {
                dataGridView.Rows.Add(item.ToRows());
            }
        }
        public ReplayListItem GetResult()
        {
            if (dataGridView.SelectedRows.Count == 1)
            {
                return items[dataGridView.SelectedRows[0].Index];
            }
            return null;
        }

        private void SubmitResult()
        {
            DialogResult = DialogResult.OK;
            Close();
        }

        private void ListMessageBox_Load(object sender, EventArgs e)
        {

        }

        private void listBox1_SelectedIndexChanged(object sender, EventArgs e)
        {

        }

        private void dataGridView_CellDoubleClick(object sender, DataGridViewCellEventArgs e)
        {
            SubmitResult();
        }

        private void dataGridView_KeyDown(object sender, KeyEventArgs e)
        {
            if(e.KeyCode == Keys.Enter)
            {
                e.Handled = true;
                SubmitResult();
            }
        }
    }
}
