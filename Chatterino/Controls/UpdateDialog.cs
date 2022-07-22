using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace Chatterino.Controls
{
    public partial class UpdateDialog : Form
    {
        public UpdateDialog(string patchNotes, string versionNumber)
        {
            InitializeComponent();
            txtPatchNotes.ScrollBars = ScrollBars.Vertical;
            txtPatchNotes.ReadOnly = true;
            txtPatchNotes.Text = patchNotes;
            txtPatchNotes.Font = new Font(txtPatchNotes.Font.FontFamily, 10);
            txtPatchNotes.BackColor = Color.White;
            lblVersion.Text = "Chatterino V" + versionNumber + " is available Patchnotes:";
            Icon = App.Icon;
        }
    }
}
