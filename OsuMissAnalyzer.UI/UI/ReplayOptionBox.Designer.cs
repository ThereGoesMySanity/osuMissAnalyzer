namespace OsuMissAnalyzer.UI.UI
{
    partial class ReplayOptionBox
    {
        /// <summary>
        /// Required designer variable.
        /// </summary>
        private System.ComponentModel.IContainer components = null;

        /// <summary>
        /// Clean up any resources being used.
        /// </summary>
        /// <param name="disposing">true if managed resources should be disposed; otherwise, false.</param>
        protected override void Dispose(bool disposing)
        {
            if (disposing && (components != null))
            {
                components.Dispose();
            }
            base.Dispose(disposing);
        }

        #region Windows Form Designer generated code

        /// <summary>
        /// Required method for Designer support - do not modify
        /// the contents of this method with the code editor.
        /// </summary>
        private void InitializeComponent()
        {
            this.tableLayoutPanel1 = new System.Windows.Forms.TableLayoutPanel();
            this.label1 = new System.Windows.Forms.Label();
            this.recent = new System.Windows.Forms.Button();
            this.beatmap = new System.Windows.Forms.Button();
            this.manual = new System.Windows.Forms.Button();
            this.tableLayoutPanel1.SuspendLayout();
            this.SuspendLayout();
            // 
            // tableLayoutPanel1
            // 
            this.tableLayoutPanel1.ColumnCount = 3;
            this.tableLayoutPanel1.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Percent, 33.33333F));
            this.tableLayoutPanel1.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Percent, 33.33333F));
            this.tableLayoutPanel1.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Percent, 33.33333F));
            this.tableLayoutPanel1.Controls.Add(this.label1, 0, 0);
            this.tableLayoutPanel1.Controls.Add(this.recent, 0, 1);
            this.tableLayoutPanel1.Controls.Add(this.beatmap, 1, 1);
            this.tableLayoutPanel1.Controls.Add(this.manual, 2, 1);
            this.tableLayoutPanel1.Dock = System.Windows.Forms.DockStyle.Fill;
            this.tableLayoutPanel1.Location = new System.Drawing.Point(0, 0);
            this.tableLayoutPanel1.Name = "tableLayoutPanel1";
            this.tableLayoutPanel1.RowCount = 2;
            this.tableLayoutPanel1.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Percent, 100F));
            this.tableLayoutPanel1.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Absolute, 32F));
            this.tableLayoutPanel1.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Absolute, 20F));
            this.tableLayoutPanel1.Size = new System.Drawing.Size(277, 135);
            this.tableLayoutPanel1.TabIndex = 1;
            // 
            // label1
            // 
            this.label1.AutoSize = true;
            this.tableLayoutPanel1.SetColumnSpan(this.label1, 3);
            this.label1.Dock = System.Windows.Forms.DockStyle.Fill;
            this.label1.Font = new System.Drawing.Font("Microsoft Sans Serif", 11.25F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.label1.Location = new System.Drawing.Point(3, 0);
            this.label1.Name = "label1";
            this.label1.Size = new System.Drawing.Size(271, 103);
            this.label1.TabIndex = 3;
            this.label1.Text = "Analyze what miss?";
            this.label1.TextAlign = System.Drawing.ContentAlignment.MiddleCenter;
            // 
            // recent
            // 
            this.recent.DialogResult = System.Windows.Forms.DialogResult.OK;
            this.recent.Dock = System.Windows.Forms.DockStyle.Fill;
            this.recent.Enabled = false;
            this.recent.Location = new System.Drawing.Point(3, 106);
            this.recent.Name = "recent";
            this.recent.Size = new System.Drawing.Size(86, 26);
            this.recent.TabIndex = 0;
            this.recent.Text = "Recent";
            this.recent.UseVisualStyleBackColor = true;
            this.recent.Click += new System.EventHandler(this.recent_Click);
            // 
            // beatmap
            // 
            this.beatmap.DialogResult = System.Windows.Forms.DialogResult.OK;
            this.beatmap.Dock = System.Windows.Forms.DockStyle.Fill;
            this.beatmap.Enabled = false;
            this.beatmap.Location = new System.Drawing.Point(95, 106);
            this.beatmap.Name = "beatmap";
            this.beatmap.Size = new System.Drawing.Size(86, 26);
            this.beatmap.TabIndex = 1;
            this.beatmap.Text = "By beatmap";
            this.beatmap.UseVisualStyleBackColor = true;
            this.beatmap.Click += new System.EventHandler(this.beatmap_Click);
            // 
            // manual
            // 
            this.manual.DialogResult = System.Windows.Forms.DialogResult.OK;
            this.manual.Dock = System.Windows.Forms.DockStyle.Fill;
            this.manual.Location = new System.Drawing.Point(187, 106);
            this.manual.Name = "manual";
            this.manual.Size = new System.Drawing.Size(87, 26);
            this.manual.TabIndex = 2;
            this.manual.Text = "Manual";
            this.manual.UseVisualStyleBackColor = true;
            this.manual.Click += new System.EventHandler(this.manual_Click);
            // 
            // ReplayOptionBox
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(277, 135);
            this.Controls.Add(this.tableLayoutPanel1);
            this.FormBorderStyle = System.Windows.Forms.FormBorderStyle.FixedDialog;
            this.MaximizeBox = false;
            this.MinimizeBox = false;
            this.Name = "ReplayOptionBox";
            this.StartPosition = System.Windows.Forms.FormStartPosition.CenterScreen;
            this.Text = "osu! Miss Analyzer";
            this.TopMost = true;
            this.Load += new System.EventHandler(this.ReplayOptionBox_Load);
            this.tableLayoutPanel1.ResumeLayout(false);
            this.tableLayoutPanel1.PerformLayout();
            this.ResumeLayout(false);

        }

        #endregion

        private System.Windows.Forms.TableLayoutPanel tableLayoutPanel1;
        private System.Windows.Forms.Label label1;
        private System.Windows.Forms.Button recent;
        private System.Windows.Forms.Button beatmap;
        private System.Windows.Forms.Button manual;
    }
}