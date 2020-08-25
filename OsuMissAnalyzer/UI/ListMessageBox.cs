using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Windows.Forms;

namespace OsuMissAnalyzer.UI
{
    public class ListMessageBox<T> : Form
    {
        private ListBox listBox;
        public ListMessageBox(String title, IEnumerable<T> items)
        {
            listBox = new ListBox();
            listBox.Size = new Size(200, 100);
            listBox.DataSource = items.ToArray();
        }
    }
}