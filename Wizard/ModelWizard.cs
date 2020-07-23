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
using System.Collections.Generic;
using DWSIM.CrossPlatform.UI.Controls.ReoGrid;
using DWSIM.UnitOperations.NeuralNetwork.Classes;

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

        private string DataSourcePath = "";
        private string ModelPath = "model";

        // list of data for features
        private List<List<double>> data = new List<List<double>>();
        private List<string> labels = new List<string>();
        private List<string> labels_outputs = new List<string>();

        // feature / objID / propID / propUnits
        private List<Tuple<string, string, string, string>> inputs = new List<Tuple<string, string, string, string>>();
        private List<Tuple<string, string, string, string>> outputs = new List<Tuple<string, string, string, string>>();

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

            var page1 = new WizardPage();

            page1.hasBackButton = false;
            page1.hasCancelButton = true;
            page1.hasNextButton = true;
            page1.hasFinishButton = false;

            page1.cancelAction = () => Application.Instance.Invoke(() => page1.Close());

            page1.Title = "Neural Network Model Wizard";
            page1.HeaderTitle = "Step 1 - Model Management";
            page1.HeaderDescription = "Select an action.";
            page1.FooterText = "Click 'Next' to Continue.";

            var dl = c.GetDefaultContainer();
            dl.Width = Width;

            dl.CreateAndAddLabelRow2("Select an action:");

            var rl = new RadioButtonList { Orientation = Orientation.Vertical };

            rl.Spacing = new Size(5, 5);

            rl.Items.Add("Create a New Model");
            rl.Items.Add("Load an Existing Model");
            rl.Items.Add("Load and Retrain an Existing Model");

            page1.nextAction = () =>
            {
                page1.Close();
                switch (rl.SelectedIndex)
                {
                    case 0:
                        DisplayPage_LoadData();
                        break;
                    case 1:
                        break;
                    case 2:
                        break;
                }
            };

            dl.CreateAndAddControlRow(rl);

            var scrollable = new Scrollable { Content = dl, Border = BorderType.None, Size = new Size(Width, Height), ExpandContentHeight = true };

            page1.Init(Width, Height);

            page1.ContentContainer.Add(scrollable);
            page1.Show();

        }

        private void DisplayPage_LoadData()
        {

            var page2 = new WizardPage();

            page2.hasBackButton = true;
            page2.hasCancelButton = true;
            page2.hasNextButton = true;
            page2.hasFinishButton = false;

            page2.cancelAction = () => page2.Close();
            page2.backAction = () =>
            {
                page2.Close();
                Application.Instance.Invoke(() => Show());
            };
            page2.nextAction = () =>
            {
                page2.Close();
                Application.Instance.Invoke(() => DisplayPage_DisplayData());
            };

            page2.Title = "Neural Network Model Wizard";
            page2.HeaderTitle = "Step 2 - Load Data";
            page2.HeaderDescription = "Load Data for Model Training and Testing";
            page2.FooterText = "Click 'Next' to Continue.";


            var dl = c.GetDefaultContainer();
            dl.Width = Width;

            dl.CreateAndAddLabelRow2("Select a Data Source. Supported file formats are xlsx and csv.");

            var filepicker = new FilePicker { Title = "Select a Data Source" };
            filepicker.Filters.Add(new FileFilter("All Supported File Formats", new string[] { ".xlsx", ".csv", ".txt" }));
            filepicker.Filters.Add(new FileFilter("Microsoft Excel Spreadsheets", new string[] { ".xlsx" }));
            filepicker.Filters.Add(new FileFilter("Comma-Separated Text Files", new string[] { ".csv", ".txt" }));

            filepicker.FilePathChanged += (s, e) => DataSourcePath = filepicker.FilePath;

            dl.CreateAndAddControlRow(filepicker);

            var scrollable = new Scrollable { Content = dl, Border = BorderType.None, Size = new Size(Width, Height), ExpandContentHeight = true };

            page2.Init(Width, Height);

            page2.ContentContainer.Add(scrollable);
            page2.Show();

        }

        private void DisplayPage_DisplayData()
        {

            var page = new WizardPage();

            page.hasBackButton = true;
            page.hasCancelButton = true;
            page.hasNextButton = true;
            page.hasFinishButton = false;

            page.cancelAction = () => page.Close();
            page.backAction = () =>
            {
                page.Close();
                DisplayPage_LoadData();
            };

            page.Title = "Neural Network Model Wizard";
            page.HeaderTitle = "Step 3 - Select Data";
            page.HeaderDescription = "Select the Data to Train/Test the ANN Model";
            page.FooterText = "Click and Drag over the Cells to Select a Data Range and Click 'Next' to Continue.";

            var dl = c.GetDefaultContainer();
            dl.Width = Width;

            var ReoGridControl = new ReoGridFullControl() { Size = new Size(Width, Height) };
            ReoGridControl.GridControl.bottomPanel.Visible = false;
            var stacks = ReoGridControl.Children.Where(x => x is StackLayout).ToList();
            foreach (var stack in stacks)
            {
                stack.Visible = false;
            }

            dl.CreateAndAddControlRow(ReoGridControl);

            page.nextAction = () =>
            {

                page.Close();

                var sheet = ReoGridControl.GridControl.CurrentWorksheet;
                int firstcol, lastcol, firstrow, lastrow;
                firstcol = sheet.SelectionRange.Col;
                lastcol = sheet.SelectionRange.EndCol;
                firstrow = sheet.SelectionRange.Row;
                lastrow = sheet.SelectionRange.EndRow;
                double d;
                bool hasheaders = !Double.TryParse(sheet.Cells[firstrow, firstcol].Data.ToString(), out d);

                labels = new List<string>();
                data = new List<List<double>>();
                if (hasheaders)
                {
                    for (int i = firstcol; i <= lastcol; i++)
                    {
                        labels.Add(sheet.Cells[firstrow, i].Data.ToString());
                    }
                    int counter = 0;
                    for (int i = firstrow + 1; i <= lastrow; i++)
                    {
                        data.Add(new List<double>());
                        for (int j = firstcol; j <= lastcol; j++)
                        {
                            data[counter].Add(sheet.Cells[i, j].Data.ToString().ToDoubleFromCurrent());
                        }
                        counter += 1;
                    }
                }
                else
                {
                    for (int i = firstcol; i <= lastcol; i++)
                    {
                        labels.Add("Column" + i.ToString());
                    }
                    int counter = 0;
                    for (int i = firstrow; i <= lastrow; i++)
                    {
                        data.Add(new List<double>());
                        for (int j = firstcol; j <= lastcol; j++)
                        {
                            data[counter].Add(sheet.Cells[i, j].Data.ToString().ToDoubleFromCurrent());
                        }
                        counter += 1;
                    }
                }

                Application.Instance.Invoke(() => DisplayPage_LabelData());

            };

            page.Init(Width, Height);

            page.ContentContainer.Add(dl);

            page.Show();

            if (File.Exists(DataSourcePath))
            {
                DWSIM.UI.Forms.Forms.LoadingData f = null;
                f = new DWSIM.UI.Forms.Forms.LoadingData();
                f.loadingtext.Text = "Loading Data, Please Wait...";
                f.Title = "Loading Data Source";
                f.Show();
                try
                {
                    ReoGridControl.GridControl.Load(DataSourcePath);
                    ReoGridControl.GridControl.CurrentWorksheet.SetRows(ReoGridControl.GridControl.CurrentWorksheet.MaxContentRow);
                    ReoGridControl.GridControl.CurrentWorksheet.SetCols(ReoGridControl.GridControl.CurrentWorksheet.MaxContentCol);
                }
                catch (Exception ex)
                {
                    MessageBox.Show(ex.ToString(), "Error reading data source", MessageBoxType.Error);
                }
                finally
                {
                    f.Close();
                }
            }
            else
            {
                MessageBox.Show("You didn't select a data source to open. Please go back and try again.", "Error reading data source", MessageBoxType.Error);
            }

        }

        private void DisplayPage_LabelData()
        {

            var page = new WizardPage();

            var checks = new List<CheckBox>();
            var tboxes = new List<TextBox>();

            page.hasBackButton = true;
            page.hasCancelButton = true;
            page.hasNextButton = true;
            page.hasFinishButton = false;

            page.cancelAction = () => page.Close();
            page.backAction = () =>
            {
                page.Close();
                DisplayPage_LoadData();
            };
            page.nextAction = () =>
            {
                page.Close();
                labels = new List<string>();
                labels_outputs = new List<string>();
                foreach (var item in tboxes)
                {
                    labels.Add(item.Text);
                }
                int i1 = 0;
                foreach (var item in checks)
                {
                    if (item.Checked.GetValueOrDefault())
                    {
                        labels_outputs.Add(labels[i1]);
                    }
                    i1 += 1;
                }

                Application.Instance.Invoke(() => DisplayPage_AutoML());

            };

            page.Title = "Neural Network Model Wizard";
            page.HeaderTitle = "Step 4 - Data Labelling";
            page.HeaderDescription = "Label each data column and select the output variables.";
            page.FooterText = "Click 'Next' to Continue.";

            var dl = c.GetDefaultContainer();
            dl.Width = Width;

            int i = 1;
            foreach (var item in labels)
            {
                var stack = new StackLayout
                {
                    Orientation = Orientation.Horizontal,
                    HorizontalContentAlignment = HorizontalAlignment.Left,
                    VerticalContentAlignment = VerticalAlignment.Center,
                    Padding = new Padding(5),
                    Spacing = 10
                };
                var lb = new Label() { Text = "Column " + i.ToString() + ":" };
                var tb = new TextBox { Text = item, Width = 300 };
                var check = new CheckBox { Text = "Output" };
                i += 1;
                stack.Items.Add(new StackLayoutItem(lb));
                stack.Items.Add(new StackLayoutItem(tb));
                stack.Items.Add(new StackLayoutItem(check));
                tboxes.Add(tb);
                checks.Add(check);
                dl.CreateAndAddControlRow(stack);
            }

            page.Init(Width, Height);

            var scrollable = new Scrollable { Content = dl, Border = BorderType.None, Size = new Size(Width, Height), ExpandContentHeight = true };

            page.ContentContainer.Add(scrollable);

            page.Show();

        }

        private void DisplayPage_AutoML()
        {

            var page = new WizardPage();

            var checks = new List<CheckBox>();
            var tboxes = new List<TextBox>();

            page.hasBackButton = true;
            page.hasCancelButton = true;
            page.hasNextButton = true;
            page.hasFinishButton = false;

            page.cancelAction = () => page.Close();
            page.backAction = () =>
            {
                page.Close();
                DisplayPage_LoadData();
            };
            page.nextAction = () =>
            {
                page.Close();
                labels = new List<string>();
                labels_outputs = new List<string>();
                foreach (var item in tboxes)
                {
                    labels.Add(item.Text);
                }
                int i1 = 0;
                foreach (var item in checks)
                {
                    if (item.Checked.GetValueOrDefault())
                    {
                        labels_outputs.Add(labels[i1]);
                    }
                    i1 += 1;
                }
            };

            page.Title = "Neural Network Model Wizard";
            page.HeaderTitle = "Step 5 - Automatic Model Training";
            page.HeaderDescription = "Use AutoML to .";
            page.FooterText = "Click 'Next' to Continue.";

            var dl = c.GetDefaultContainer();
            dl.Width = Width;

            page.Init(Width, Height);

            var scrollable = new Scrollable { Content = dl, Border = BorderType.None, Size = new Size(Width, Height), ExpandContentHeight = true };

            page.ContentContainer.Add(scrollable);

            page.Show();

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
