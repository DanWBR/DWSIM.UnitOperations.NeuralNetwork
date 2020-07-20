using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Reflection;
using System.Windows.Forms;

namespace DWSIM.UnitOperations.NeuralNetwork.Editors
{
    public partial class FormAbout : Form
    {
        public FormAbout()
        {
            InitializeComponent();
            lblVersion.Text = "Version " + Assembly.GetExecutingAssembly().GetName().Version.ToString();
        }

        private void button1_Click(object sender, EventArgs e)
        {
            Close();
        }
    }
}
