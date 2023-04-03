using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace CSV
{
    public partial class InProgress : Form
    {
        readonly Form1 form1;
        public InProgress(Form1 owner)
        {
            form1 = owner;
            InitializeComponent();
        }

        private void btnCancelOpeningFile_Click(object sender, EventArgs e)
        {
            form1.stopOpeningFile = true;
            this.Close();
        }
    }
}
