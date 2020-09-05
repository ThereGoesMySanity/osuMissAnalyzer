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
        private int result = 0;
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
            dataGridView.Rows[0].Selected = true;
        }
        public ReplayListItem GetResult()
        {
            return items[result];
        }

        private void SubmitResult()
        {
            if (dataGridView.SelectedRows.Count == 1)
            {
                result = dataGridView.SelectedRows[0].Index;
            }
            DialogResult = DialogResult.OK;
            Close();
        }

        private void ListMessageBox_Load(object sender, EventArgs e)
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
                SubmitResult();
                e.Handled = true;
            }
        }
    }
}
