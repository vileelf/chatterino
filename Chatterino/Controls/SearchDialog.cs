using System;
using System.Collections.Generic;
using Chatterino.Common;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace Chatterino.Controls
{
    public partial class SearchDialog : Form
    {
        public string Value
        {
            get { return textBox.Text; }
            set { textBox.Text = value; }
        }

        public delegate void Callback(DialogResult result, string value);
        public SearchDialog(string title, Callback cb)
        {
            InitializeComponent();

            TopMost = AppSettings.WindowTopMost;

            AppSettings.WindowTopMostChanged += (s, e) =>
            {
                TopMost = AppSettings.WindowTopMost;
            };

            StartPosition = FormStartPosition.CenterScreen;

            Text = title;

            KeyPreview = true;
            KeyDown += (s, e) =>
            {
                if (e.Shift && e.KeyCode == Keys.Enter)
                {
                    e.Handled = true;

                    btnPrev.PerformClick();
                }
                else if (e.KeyCode == Keys.Enter)
                {
                    e.Handled = true;

                    btnNext.PerformClick();
                }
                else if (e.KeyCode == Keys.Escape)
                {
                    e.Handled = true;
                    cancelButton.PerformClick();
                }
            };
            cancelButton.Click += (s, e) =>
            {
                DialogResult = DialogResult.Cancel;
                cb(DialogResult, textBox.Text);
                this.Close();
            };
            btnNext.Click += (s, e) =>
            {
                DialogResult = DialogResult.Yes;
                cb(DialogResult, textBox.Text);
            };
            btnPrev.Click += (s, e) =>
            {
                DialogResult = DialogResult.No;
                cb(DialogResult, textBox.Text);
            };
            FormClosing += (s, e) =>
            {
                DialogResult = DialogResult.Cancel;
                cb(DialogResult, textBox.Text);
            };
        }
    }
}
