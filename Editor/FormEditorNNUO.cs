using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace DWSIM.UnitOperations.NeuralNetwork.Editors
{
    public partial class FormEditorNNUO : WeifenLuo.WinFormsUI.Docking.DockContent
    {

        public NeuralNetworkUnitOperation SimObject;

        public bool Loaded = false;

        SharedClasses.SystemsOfUnits.Units units;

        string nf;

        public FormEditorNNUO()
        {
            InitializeComponent();
        }

        public void UpdateInfo()
        { 
        
        }

    }
}
