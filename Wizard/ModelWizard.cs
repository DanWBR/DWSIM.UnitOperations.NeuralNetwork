using System;
using System.Linq;
using System.Threading.Tasks;
using DWSIM.Interfaces;
using DWSIM.Interfaces.Enums.GraphicObjects;
using Eto.Forms;
using c = DWSIM.UI.Shared.Common;

using cv = DWSIM.SharedClasses.SystemsOfUnits.Converter;
using DWSIM.Thermodynamics.Streams;
using DWSIM.UI.Desktop.Shared;
using DWSIM.UI.Shared;

using DWSIM.ExtensionMethods;
using System.IO;
using DWSIM.UI.Desktop.Editors;
using Eto.Drawing;
using Microsoft.ML;

namespace DWSIM.UnitOperations.NeuralNetwork.Wizard
{
    public class ModelWizard
    {

        public NeuralNetworkUnitOperation SimObject;

        private static double sf = GlobalSettings.Settings.UIScalingFactor;

        private int Width = (int)(800 * sf);
        private int Height = (int)(500 * sf);

        private string nf = "";
        private IUnitsOfMeasure su;

        public ModelWizard(NeuralNetworkUnitOperation uo)
        {
            SimObject = uo;
            Init();
        }

        void Init()
        {
                nf = SimObject.FlowSheet.FlowsheetOptions.NumberFormat;
                su = SimObject.FlowSheet.FlowsheetOptions.SelectedUnitSystem;           
        }

        public void Show()
        {

            var model = new MLContext();

            var page1 = new WizardPage();

            page1.hasBackButton = false;
            page1.hasCancelButton = true;
            page1.hasNextButton = true;
            page1.hasFinishButton = false;

            page1.cancelAction = () => page1.Close();
            page1.nextAction = () => { page1.Close(); DisplayPage2(); };

            page1.Title = "Neural Network Model Wizard";
            page1.HeaderTitle = "Step 1 - Model Management";
            page1.HeaderDescription = "Select an action.";
            page1.FooterText = "Click 'Next' to continue";

            page1.Init(Width, Height);

            var dl = c.GetDefaultContainer();
            dl.Height = Height;
            dl.Width = Width;

            dl.CreateAndAddLabelRow("General Information");

            var rl = new RadioButtonList { Orientation = Orientation.Vertical };

            rl.Spacing = new Size(5, 5);

            rl.Items.Add("Create a New Model");
            rl.Items.Add("Load an Existing Model");
            rl.Items.Add("Load and Retrain an Existing Model");
            
            rl.SelectedIndexChanged += (s, e) => {
                switch (rl.SelectedIndex)
                {
                    case 0:
                        break;
                    case 1:
                        break;
                    case 2:
                        break;
                }
            };

            dl.CreateAndAddControlRow(rl);

            page1.SuspendLayout();
            page1.ContentContainer.Add(dl);
            page1.ResumeLayout();
            page1.Show();

        }

        private void DisplayPage2()
        {

            var dl = c.GetDefaultContainer();

            var page2 = new WizardPage();

            page2.hasBackButton = true;
            page2.hasCancelButton = true;
            page2.hasNextButton = true;
            page2.hasFinishButton = false;

            page2.cancelAction = () => page2.Close();
            page2.backAction = () => { page2.Close(); Show(); };
            page2.nextAction = () =>
            {
               
            };

            page2.Title = "Neural Network Model Wizard";
            page2.HeaderTitle = "Step 2";
            page2.HeaderDescription = "";
            page2.FooterText = "Click 'Next' to continue";

            page2.Init(Width, Height);

            dl.Width = Width;

            dl.CreateAndAddLabelRow("Identification");
            
            page2.ContentContainer.Add(new Scrollable { Content = dl, Border = BorderType.None, Height = Height, Width = Width });
            page2.Show();

        }

        private void DisplayLastPage()
        {

            var page = new WizardPage();

            page.hasBackButton = true;
            page.hasCancelButton = true;
            page.hasNextButton = false;
            page.hasFinishButton = true;

            page.cancelAction = () => page.Close();
            page.finishAction = () => page.Close();

            page.backAction = () =>
            {
                page.Close();
            };

            page.Title = "Compound Creator Wizard";
            page.HeaderTitle = "Final Step - Add Compound and Export Data";
            page.HeaderDescription = "Add the compound to the simulation and/or export the created data to a file.";
            page.FooterText = "Click 'Finish' to close this wizard.";

            page.Init(Width, Height);

            var dl = c.GetDefaultContainer();
            dl.Height = Height;
            dl.Width = Width;

            page.ContentContainer.Add(dl);
            page.Show();

        }

        string FormatUnit(string units)
        {
            return " (" + units + ")";
        }

    }

}
