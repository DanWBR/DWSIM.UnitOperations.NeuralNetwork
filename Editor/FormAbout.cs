using System;
using System.Reflection;
using System.Windows.Forms;
using DWSIM.ExtensionMethods;

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

        private void FormAbout_Load(object sender, EventArgs e)
        {
            this.ChangeDefaultFont();
        }
    }
}
