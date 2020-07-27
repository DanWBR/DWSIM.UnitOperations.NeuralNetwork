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

using NumSharp;
using Tensorflow;
using Tensorflow.Gradients;
using static Tensorflow.Binding;
using System.Diagnostics;

using System.IO.Compression;
using OxyPlot;
using OxyPlot.Axes;
using OxyPlot.Series;

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

        private ANNModel CurrentModel;

        public ModelWizard(NeuralNetworkUnitOperation uo)
        {
            SimObject = uo;
            Init();
        }

        void Init()
        {
            nf = SimObject.FlowSheet.FlowsheetOptions.NumberFormat;
            su = SimObject.FlowSheet.FlowsheetOptions.SelectedUnitSystem;
            CurrentModel = new ANNModel();
        }

        public void Show()
        {

            var page1 = new WizardPage();

            page1.hasBackButton = false;
            page1.hasCancelButton = true;
            page1.hasNextButton = true;
            page1.hasFinishButton = false;

            page1.cancelAction = () =>
            {
                page1.Close();
            };


            page1.Title = "Neural Network Model Wizard";
            page1.HeaderTitle = "Step 1 - Model Management";
            page1.HeaderDescription = "Select an Action";
            page1.FooterText = "Click 'Next' to Continue.";

            var dl = c.GetDefaultContainer();
            dl.Width = Width;

            dl.CreateAndAddLabelRow2("Select an action:");

            dl.CreateAndAddEmptySpace();

            dl.CreateAndAddEmptySpace();

            var rl = new RadioButtonList { Orientation = Orientation.Vertical };

            rl.Spacing = new Size(10, 10);

            rl.Items.Add("Create and Train a New Model");
            rl.Items.Add("Load an Existing Model");

            rl.SelectedIndex = 0;

            page1.nextAction = () =>
            {
                switch (rl.SelectedIndex)
                {
                    case 0:
                        CurrentModel = new ANNModel();
                        Application.Instance.Invoke(() => DisplayPage_LoadData());
                        break;
                    case 1:
                        CurrentModel = new ANNModel();
                        Application.Instance.Invoke(() => DisplayPage_LoadModel());
                        break;
                }
                page1.Close();
            };

            dl.CreateAndAddControlRow(rl);

            var scrollable = new Scrollable { Content = dl, Border = BorderType.None, Size = new Size(Width, Height), ExpandContentHeight = true };

            page1.Init(Width, Height);
            page1.Topmost = false;
            page1.ContentContainer.Add(scrollable);
            page1.Show();

        }

        // create and train a new model

        private void DisplayPage_LoadData()
        {

            var page2 = new WizardPage();

            page2.hasBackButton = true;
            page2.hasCancelButton = true;
            page2.hasNextButton = true;
            page2.hasFinishButton = false;

            page2.cancelAction = () =>
            {
                page2.Close();
            };

            page2.backAction = () =>
            {
                Application.Instance.Invoke(() => Show());
                page2.Close();
            };
            page2.nextAction = () =>
            {
                Application.Instance.Invoke(() => DisplayPage_DisplayData());
                page2.Close();
            };

            page2.Title = "Neural Network Model Wizard";
            page2.HeaderTitle = "Step 2 - Load Data";
            page2.HeaderDescription = "Load Data for Model Training and Testing";
            page2.FooterText = "Click 'Next' to Continue.";

            var dl = c.GetDefaultContainer();
            dl.Width = Width;

            dl.CreateAndAddLabelRow2("Select a Data Source. Supported File Formats: XLSX, CSV.");

            var filepicker = new FilePicker { Title = "Select a Data Source" };
            filepicker.Filters.Add(new FileFilter("All Supported File Formats", new string[] { ".xlsx", ".csv", ".txt" }));
            filepicker.Filters.Add(new FileFilter("Microsoft Excel Spreadsheets", new string[] { ".xlsx" }));
            filepicker.Filters.Add(new FileFilter("Comma-Separated Text Files", new string[] { ".csv", ".txt" }));

            if (File.Exists(CurrentModel.DataSourcePath)) filepicker.FilePath = CurrentModel.DataSourcePath;

            filepicker.FilePathChanged += (s, e) => CurrentModel.DataSourcePath = filepicker.FilePath;

            dl.CreateAndAddControlRow(filepicker);

            var scrollable = new Scrollable { Content = dl, Border = BorderType.None, Size = new Size(Width, Height), ExpandContentHeight = true };

            page2.Init(Width, Height);
            page2.Topmost = false;

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

            page.cancelAction = () =>
            {
                page.Close();
            };

            page.backAction = () =>
            {
                Application.Instance.Invoke(() => DisplayPage_LoadData());
                page.Close();
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

                CurrentModel.Parameters.Labels = new List<string>();
                CurrentModel.Data = new List<List<double>>();
                if (hasheaders)
                {
                    for (int i = firstcol; i <= lastcol; i++)
                    {
                        CurrentModel.Parameters.Labels.Add(sheet.Cells[firstrow, i].Data.ToString());
                    }
                    int counter = 0;
                    for (int i = firstrow + 1; i <= lastrow; i++)
                    {
                        CurrentModel.Data.Add(new List<double>());
                        for (int j = firstcol; j <= lastcol; j++)
                        {
                            CurrentModel.Data[counter].Add(sheet.Cells[i, j].Data.ToString().ToDoubleFromCurrent());
                        }
                        counter += 1;
                    }
                }
                else
                {
                    for (int i = firstcol; i <= lastcol; i++)
                    {
                        CurrentModel.Parameters.Labels.Add("Column" + i.ToString());
                    }
                    int counter = 0;
                    for (int i = firstrow; i <= lastrow; i++)
                    {
                        CurrentModel.Data.Add(new List<double>());
                        for (int j = firstcol; j <= lastcol; j++)
                        {
                            CurrentModel.Data[counter].Add(sheet.Cells[i, j].Data.ToString().ToDoubleFromCurrent());
                        }
                        counter += 1;
                    }
                }

                Application.Instance.Invoke(() => DisplayPage_LabelData());

            };

            page.Init(Width, Height);
            page.Topmost = false;

            page.ContentContainer.Add(dl);

            page.Show();

            if (File.Exists(CurrentModel.DataSourcePath))
            {
                DWSIM.UI.Forms.Forms.LoadingData f = null;
                f = new DWSIM.UI.Forms.Forms.LoadingData();
                f.loadingtext.Text = "Loading Data, Please Wait...";
                f.Title = "Loading Data Source";
                f.Show();
                Application.Instance.AsyncInvoke(() =>
                {
                    try
                    {
                        ReoGridControl.GridControl.Load(CurrentModel.DataSourcePath);
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
                });
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

            page.cancelAction = () =>
            {
                page.Close();
            };

            page.backAction = () =>
            {
                Application.Instance.Invoke(() => DisplayPage_DisplayData());
                page.Close();
            };
            page.nextAction = () =>
            {
                CurrentModel.Parameters.Labels = new List<string>();
                CurrentModel.Parameters.Labels_Outputs = new List<string>();
                foreach (var item in tboxes)
                {
                    CurrentModel.Parameters.Labels.Add(item.Text);
                }
                int i1 = 0;
                foreach (var item in checks)
                {
                    if (item.Checked.GetValueOrDefault())
                    {
                        CurrentModel.Parameters.Labels_Outputs.Add(CurrentModel.Parameters.Labels[i1]);
                    }
                    i1 += 1;
                }
                Application.Instance.Invoke(() => DisplayPage_ModelParameters());
                page.Close();
            };

            page.Title = "Neural Network Model Wizard";
            page.HeaderTitle = "Step 4 - Data Labelling";
            page.HeaderDescription = "Label each data column and select the output variables.";
            page.FooterText = "Click 'Next' to Continue.";

            var dl = c.GetDefaultContainer();
            dl.Width = Width;

            dl.CreateAndAddLabelRow2("The Output Variables must be grouped at the end of the list.");

            int i = 1;
            foreach (var item in CurrentModel.Parameters.Labels)
            {
                var stack = new StackLayout
                {
                    Orientation = Orientation.Horizontal,
                    HorizontalContentAlignment = Eto.Forms.HorizontalAlignment.Left,
                    VerticalContentAlignment = Eto.Forms.VerticalAlignment.Center,
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
            page.Topmost = false;

            var scrollable = new Scrollable { Content = dl, Border = BorderType.None, Size = new Size(Width, Height), ExpandContentHeight = true };

            page.ContentContainer.Add(scrollable);

            page.Show();

        }

        private void DisplayPage_ModelParameters()
        {

            var page = new WizardPage();

            var checks = new List<CheckBox>();
            var tboxes = new List<TextBox>();

            page.hasBackButton = true;
            page.hasCancelButton = true;
            page.hasNextButton = true;
            page.hasFinishButton = false;

            page.cancelAction = () =>
            {
                page.Close();
            };

            page.backAction = () =>
            {
                Application.Instance.Invoke(() => DisplayPage_LabelData());
                page.Close();
            };
            page.nextAction = () =>
            {
                Application.Instance.Invoke(() => DisplayPage_ML());
                page.Close();
            };

            page.Title = "Neural Network Model Wizard";
            page.HeaderTitle = "Step 5 - Model Training Parameters";
            page.HeaderDescription = "Configure Model Training Parameters";
            page.FooterText = "Click 'Next' to Continue.";

            var dl = c.GetDefaultContainer();
            dl.Width = Width;

            var p = CurrentModel.Parameters;

            dl.CreateAndAddTextBoxRow(nf, "Minimum Scaled Value", p.MinScale, (tb, e) =>
            {
                if (tb.Text.IsValidDouble())
                {
                    p.MinScale = (float)tb.Text.ToDoubleFromCurrent();
                }
            });

            dl.CreateAndAddTextBoxRow(nf, "Maximum Scaled Value", p.MaxScale, (tb, e) =>
            {
                if (tb.Text.IsValidDouble())
                {
                    p.MinScale = (float)tb.Text.ToDoubleFromCurrent();
                }
            });

            dl.CreateAndAddTextBoxRow("N0", "Number of Training Epochs (Iterations)", p.NumberOfEpochs, (tb, e) =>
            {
                if (tb.Text.IsValidDouble())
                {
                    p.NumberOfEpochs = (int)tb.Text.ToDoubleFromCurrent();
                }
            });

            dl.CreateAndAddTextBoxRow("N0", "Training Batch Size", p.BatchSize, (tb, e) =>
            {
                if (tb.Text.IsValidDouble())
                {
                    p.BatchSize = (int)tb.Text.ToDoubleFromCurrent();
                }
            });

            dl.CreateAndAddTextBoxRow("N0", "Number of Network Layers (Min = 2, Max = 4)", p.NumberOfLayers, (tb, e) =>
            {
                if (tb.Text.IsValidDouble())
                {
                    var val = (int)tb.Text.ToDoubleFromCurrent();
                    if (val < 2) val = 2;
                    if (val > 4) val = 4;
                    p.NumberOfLayers = val;
                }
            });

            dl.CreateAndAddTextBoxRow("N0", "Number of Neurons on First Layer", p.NumberOfNeuronsOnFirstLayer, (tb, e) =>
            {
                if (tb.Text.IsValidDouble())
                {
                    p.NumberOfNeuronsOnFirstLayer = (int)tb.Text.ToDoubleFromCurrent();
                }
            });

            dl.CreateAndAddTextBoxRow(nf, "Optimizer Learning Rate", p.LearningRate, (tb, e) =>
            {
                if (tb.Text.IsValidDouble())
                {
                    p.LearningRate = (float)tb.Text.ToDoubleFromCurrent();
                }
            });

            dl.CreateAndAddTextBoxRow("G6", "Minimum Relative Tolerance for MSE", p.RelativeMSETolerance, (tb, e) =>
            {
                if (tb.Text.IsValidDouble())
                {
                    p.RelativeMSETolerance = (float)tb.Text.ToDoubleFromCurrent();
                }
            });

            dl.CreateAndAddTextBoxRow("G6", "Minimum Absolute Tolerance for MSE", p.AbsoluteMSETolerance, (tb, e) =>
            {
                if (tb.Text.IsValidDouble())
                {
                    p.AbsoluteMSETolerance = (float)tb.Text.ToDoubleFromCurrent();
                }
            });

            dl.CreateAndAddTextBoxRow(nf, "Split Factor for Data Training/Testing", p.SplitFactor, (tb, e) =>
            {
                if (tb.Text.IsValidDouble())
                {
                    p.SplitFactor = (float)tb.Text.ToDoubleFromCurrent();
                }
            });

            page.Init(Width, Height);
            page.Topmost = false;

            var scrollable = new Scrollable { Content = dl, Border = BorderType.None, Size = new Size(Width, Height), ExpandContentHeight = true };

            page.ContentContainer.Add(scrollable);

            page.Show();

        }

        private void DisplayPage_ML()
        {

            var page = new WizardPage();

            var checks = new List<CheckBox>();
            var tboxes = new List<TextBox>();

            page.hasBackButton = true;
            page.hasCancelButton = true;
            page.hasNextButton = true;
            page.hasFinishButton = false;

            page.cancelAction = () =>
            {
                page.Close();
            };

            page.backAction = () =>
            {
                Application.Instance.Invoke(() => DisplayPage_ModelParameters());
                page.Close();
            };
            page.nextAction = () =>
            {
                Application.Instance.Invoke(() => DisplayPage_Results());
                page.Close();
            };

            page.Title = "Neural Network Model Wizard";
            page.HeaderTitle = "Step 6 - Model Training and Evaluation";
            page.HeaderDescription = "Click on the 'Train and Evaluate' Button to Train your Model";
            page.FooterText = "Click 'Next' to Continue.";

            var dl = c.GetDefaultContainer();
            dl.Width = Width;

            TextArea tb = new TextArea
            {
                Width = 350,
                Font = new Font(FontFamilies.Monospace, 10.0f)
            };

            var plot = new Eto.OxyPlot.Plot();

            plot.Model = new PlotModel();
            plot.Model.Background = OxyPlot.OxyColors.White;
            plot.Model.TitleFontSize = 12;
            plot.Model.SubtitleFontSize = 10;
            plot.Model.Axes.Add(new OxyPlot.Axes.LinearAxis()
            {
                MajorGridlineStyle = OxyPlot.LineStyle.Dash,
                MinorGridlineStyle = OxyPlot.LineStyle.Dot,
                Position = OxyPlot.Axes.AxisPosition.Bottom,
                FontSize = 10,
                Title = "Epoch",
                Key = "x",
            });
            plot.Model.Axes.Add(new OxyPlot.Axes.LinearAxis()
            {
                MajorGridlineStyle = OxyPlot.LineStyle.Dash,
                MinorGridlineStyle = OxyPlot.LineStyle.Dot,
                Position = OxyPlot.Axes.AxisPosition.Left,
                FontSize = 10,
                Title = "MSE"
            });
            plot.Model.LegendFontSize = 11;
            plot.Model.LegendPlacement = OxyPlot.LegendPlacement.Outside;
            plot.Model.LegendOrientation = OxyPlot.LegendOrientation.Horizontal;
            plot.Model.LegendPosition = OxyPlot.LegendPosition.BottomCenter;
            plot.Model.TitleHorizontalAlignment = OxyPlot.TitleHorizontalAlignment.CenteredWithinView;
            plot.Model.AddLineSeries(new double[] { }, new double[] { }, OxyColors.Red, "Training");
            plot.Model.AddLineSeries(new double[] { }, new double[] { }, OxyColors.Blue, "Testing");
            plot.Model.Title = "Model Training Results";

            var tl = new TableLayout(new TableRow(tb, plot)) { Spacing = new Size(10, 10), Height = 440 };

            dl.CreateAndAddLabelAndButtonRow("Training Results", "Train and Evaluate", null, (btn, e) =>
            {
                Task.Factory.StartNew(() =>
                {
                    CurrentModel.PrepareData();
                    CurrentModel.Train(null, tb, plot);
                });
            });

            dl.CreateAndAddControlRow(tl);

            page.Init(Width, Height);
            page.Topmost = false;

            var scrollable = new Scrollable { Content = dl, Border = BorderType.None, Size = new Size(Width, Height), ExpandContentHeight = true };

            page.ContentContainer.Add(scrollable);

            page.Show();

        }

        private void DisplayPage_Results()
        {

            var page = new WizardPage();

            var checks = new List<CheckBox>();
            var tboxes = new List<TextBox>();

            page.hasBackButton = true;
            page.hasCancelButton = true;
            page.hasNextButton = true;
            page.hasFinishButton = false;

            page.cancelAction = () =>
            {
                page.Close();
            };

            page.backAction = () =>
            {
                page.Close();
                DisplayPage_ML();
            };
            page.nextAction = () =>
            {
                Application.Instance.Invoke(() => DisplayPage_SaveModel());
                page.Close();
            };

            page.Title = "Neural Network Model Wizard";
            page.HeaderTitle = "Step 7 - Trained Model Predictions";
            page.HeaderDescription = "View and Compare the Results Predicted by the Trained Model";
            page.FooterText = "Click 'Next' to Continue.";

            var dl = c.GetDefaultContainer();
            dl.Width = Width;

            var plot = new Eto.OxyPlot.Plot();

            plot.Model = new PlotModel();
            plot.Model.Background = OxyPlot.OxyColors.White;
            plot.Model.TitleFontSize = 12;
            plot.Model.SubtitleFontSize = 10;
            plot.Model.Axes.Add(new OxyPlot.Axes.LinearAxis()
            {
                MajorGridlineStyle = OxyPlot.LineStyle.Dash,
                MinorGridlineStyle = OxyPlot.LineStyle.Dot,
                Position = OxyPlot.Axes.AxisPosition.Bottom,
                FontSize = 10,
                Title = "Data Row",
                Key = "x",
            });
            plot.Model.Axes.Add(new OxyPlot.Axes.LinearAxis()
            {
                MajorGridlineStyle = OxyPlot.LineStyle.Dash,
                MinorGridlineStyle = OxyPlot.LineStyle.Dot,
                Position = OxyPlot.Axes.AxisPosition.Left,
                FontSize = 10,
                Title = "Output Vars"
            });
            plot.Model.LegendFontSize = 11;
            plot.Model.LegendPlacement = OxyPlot.LegendPlacement.Outside;
            plot.Model.LegendOrientation = OxyPlot.LegendOrientation.Horizontal;
            plot.Model.LegendPosition = OxyPlot.LegendPosition.BottomCenter;
            plot.Model.TitleHorizontalAlignment = OxyPlot.TitleHorizontalAlignment.CenteredWithinView;
            plot.Model.Title = "Training Dataset";
            plot.Width = 380;

            var plot2 = new Eto.OxyPlot.Plot();

            plot2.Model = new PlotModel();
            plot2.Model.Background = OxyPlot.OxyColors.White;
            plot2.Model.TitleFontSize = 12;
            plot2.Model.SubtitleFontSize = 10;
            plot2.Model.Axes.Add(new OxyPlot.Axes.LinearAxis()
            {
                MajorGridlineStyle = OxyPlot.LineStyle.Dash,
                MinorGridlineStyle = OxyPlot.LineStyle.Dot,
                Position = OxyPlot.Axes.AxisPosition.Bottom,
                FontSize = 10,
                Title = "Data Row",
                Key = "x",
            });
            plot2.Model.Axes.Add(new OxyPlot.Axes.LinearAxis()
            {
                MajorGridlineStyle = OxyPlot.LineStyle.Dash,
                MinorGridlineStyle = OxyPlot.LineStyle.Dot,
                Position = OxyPlot.Axes.AxisPosition.Left,
                FontSize = 10,
                Title = "Output Vars"
            });
            plot2.Model.LegendFontSize = 11;
            plot2.Model.LegendPlacement = OxyPlot.LegendPlacement.Outside;
            plot2.Model.LegendOrientation = OxyPlot.LegendOrientation.Horizontal;
            plot2.Model.LegendPosition = OxyPlot.LegendPosition.BottomCenter;
            plot2.Model.TitleHorizontalAlignment = OxyPlot.TitleHorizontalAlignment.CenteredWithinView;
            plot2.Model.Title = "Testing Dataset";
            plot2.Width = 380;

            // training plot

            var xseries = new List<double>();
            for (var j = 0; j < CurrentModel.x_train_unscaled.shape[0]; j++)
            {
                xseries.Add(j);
            }

            for (var i = 0; i < CurrentModel.y_train_unscaled.shape[1]; i++)
            {
                var yseries = new List<float>();
                for (var j = 0; j < CurrentModel.y_train_unscaled.shape[0]; j++)
                {
                    yseries.Add(CurrentModel.y_train_unscaled[j][i]);
                }
                plot.Model.AddScatterSeries(xseries, yseries.Select(x => (double)x).ToList());
                plot.Model.Series.Last().Title = CurrentModel.Parameters.Labels_Outputs[i];
                ((ScatterSeries)plot.Model.Series.Last()).MarkerSize = 3.0;
                ((ScatterSeries)plot.Model.Series.Last()).MarkerType = MarkerType.Circle;
            }

            for (var i = 0; i < CurrentModel.yp_train_unscaled.shape[1]; i++)
            {
                var yseries = new List<float>();
                for (var j = 0; j < CurrentModel.yp_train_unscaled.shape[0]; j++)
                {
                    yseries.Add(CurrentModel.yp_train_unscaled[j][i]);
                }
                plot.Model.AddLineSeries(xseries, yseries.Select(x => (double)x).ToList(), CurrentModel.Parameters.Labels_Outputs[i] + " (predicted)");
            }

            // testing plot

            var xseries2 = new List<double>();
            for (var j = 0; j < CurrentModel.x_test_unscaled.shape[0]; j++)
            {
                xseries2.Add(j);
            }

            for (var i = 0; i < CurrentModel.y_test_unscaled.shape[1]; i++)
            {
                var yseries = new List<float>();
                for (var j = 0; j < CurrentModel.y_test_unscaled.shape[0]; j++)
                {
                    yseries.Add(CurrentModel.y_test_unscaled[j][i]);
                }
                plot2.Model.AddScatterSeries(xseries2, yseries.Select(x => (double)x).ToList());
                plot2.Model.Series.Last().Title = CurrentModel.Parameters.Labels_Outputs[i];
                ((ScatterSeries)plot2.Model.Series.Last()).MarkerSize = 3.0;
                ((ScatterSeries)plot2.Model.Series.Last()).MarkerType = MarkerType.Circle;
            }

            for (var i = 0; i < CurrentModel.yp_test_unscaled.shape[1]; i++)
            {
                var yseries = new List<float>();
                for (var j = 0; j < CurrentModel.yp_test_unscaled.shape[0]; j++)
                {
                    yseries.Add(CurrentModel.yp_test_unscaled[j][i]);
                }
                plot2.Model.AddLineSeries(xseries2, yseries.Select(x => (double)x).ToList(), CurrentModel.Parameters.Labels_Outputs[i] + " (predicted)");
            }

            plot.Model.InvalidatePlot(true);
            plot2.Model.InvalidatePlot(true);

            var tl = new TableLayout(new TableRow(plot, plot2)) { Spacing = new Size(10, 10), Height = 440 };

            dl.CreateAndAddControlRow(tl);

            page.Init(Width, Height);
            page.Topmost = false;

            var scrollable = new Scrollable { Content = dl, Border = BorderType.None, Size = new Size(Width, Height), ExpandContentHeight = true };

            page.ContentContainer.Add(scrollable);

            page.Show();

        }

        private void DisplayPage_SaveModel()
        {

            var page = new WizardPage();

            var checks = new List<CheckBox>();
            var tboxes = new List<TextBox>();

            page.hasBackButton = true;
            page.hasCancelButton = true;
            page.hasNextButton = false;
            page.hasFinishButton = true;

            page.cancelAction = () =>
            {
                page.Close();
            };

            page.backAction = () =>
            {
                page.Close();
                DisplayPage_ML();
            };
            page.finishAction = () =>
            {
                page.Close();
                SimObject.UpdateEditForm();
            };

            page.Title = "Neural Network Model Wizard";
            page.HeaderTitle = "Step 8 - Save Model";
            page.HeaderDescription = "Save the Model to a File or Embed it on the Unit Operation Block.";
            page.FooterText = "Click 'Finish' to close this Wizard.";

            var dl = c.GetDefaultContainer();
            dl.Width = Width;

            dl.CreateAndAddLabelRow("Save Model to Zip File");

            var filepicker = new FilePicker { Title = "Save Model to Zip File", FileAction = Eto.FileAction.SaveFile };
            filepicker.Filters.Add(new FileFilter("Zip File", new string[] { ".zip" }));

            filepicker.FilePathChanged += (s, e) => CurrentModel.ModelPath = filepicker.FilePath;

            dl.CreateAndAddControlRow(filepicker);

            dl.CreateAndAddBoldLabelAndButtonRow("Save Model", "Save", null, (btn, e) =>
            {
                try
                {
                    if (File.Exists(CurrentModel.ModelPath))
                    {
                        MessageBox.Show("The save file already exists. Please create a new file and try again.", "Error", MessageBoxType.Error);
                        return;
                    }
                    Classes.Utils.SaveGraphToZip(CurrentModel.session, CurrentModel, CurrentModel.ModelPath);
                    SimObject.Model = CurrentModel;
                    SimObject.InitializeMappings();
                    MessageBox.Show(String.Format("Model saved successfully to '{0}'", CurrentModel.ModelPath), "Save Model", MessageBoxType.Information);
                }
                catch (Exception ex)
                {
                    MessageBox.Show(ex.ToString(), "Error", MessageBoxType.Error);
                }
            });

            dl.CreateAndAddLabelRow("Embed Model Data");

            dl.CreateAndAddBoldLabelAndButtonRow("Embed Model", "Embed", null, (btn, e) =>
            {
                try
                {
                    using (var ms = new MemoryStream())
                    {
                        Classes.Utils.SaveGraphToZipStream(CurrentModel.session, CurrentModel, ms);
                        SimObject.Model = CurrentModel;
                        ms.Position = 0;
                        SimObject.Model.SerializedModelData = Classes.Utils.StreamToBase64(ms);
                        SimObject.InitializeMappings();
                        MessageBox.Show(String.Format("Model successfully embedded in Unit Operation.", CurrentModel.ModelPath), "Embed Model", MessageBoxType.Information);
                    }
                }
                catch (Exception ex)
                {
                    MessageBox.Show(ex.ToString(), "Error", MessageBoxType.Error);
                }
            });

            page.Init(Width, Height);
            page.Topmost = false;

            var scrollable = new Scrollable { Content = dl, Border = BorderType.None, Size = new Size(Width, Height), ExpandContentHeight = true };

            page.ContentContainer.Add(scrollable);

            page.Show();

        }

        // load and setup an existing model

        private void DisplayPage_LoadModel()
        {

            var page = new WizardPage();

            var checks = new List<CheckBox>();
            var tboxes = new List<TextBox>();

            page.hasBackButton = true;
            page.hasCancelButton = true;
            page.hasNextButton = true;
            page.hasFinishButton = false;

            page.cancelAction = () =>
            {
                page.Close();
            };

            page.backAction = () =>
            {
                page.Close();
                Show();
            };
            page.nextAction = () =>
            {
                if (CurrentModel.ModelSource == ANNModel.ModelSourceType.Embedded)
                {
                    try
                    {
                        using (var ms = new MemoryStream())
                        {
                            Classes.Utils.SaveGraphToZipStream(CurrentModel.session, CurrentModel, ms);
                            SimObject.Model = CurrentModel;
                            ms.Position = 0;
                            SimObject.Model.SerializedModelData = Classes.Utils.StreamToBase64(ms);
                            MessageBox.Show(String.Format("Model successfully embedded in Unit Operation.", CurrentModel.ModelPath), "Embed Model", MessageBoxType.Information);
                        }
                    }
                    catch (Exception ex)
                    {
                        MessageBox.Show(ex.ToString(), "Error", MessageBoxType.Error);
                    }
                }
                Application.Instance.Invoke(() => DisplayPage_DefineLabels());
                page.Close();
            };

            page.Title = "Neural Network Model Wizard";
            page.HeaderTitle = "Step 2 - Load Model";
            page.HeaderDescription = "Load an Existing Model from a ZIP file.";
            page.FooterText = "Click 'Next' to continue.";

            var dl = c.GetDefaultContainer();
            dl.Width = Width;

            dl.CreateAndAddLabelRow("Load Model from Zip File");

            var filepicker = new FilePicker { Title = "Load Model from Zip File", FileAction = Eto.FileAction.OpenFile };
            filepicker.Filters.Add(new FileFilter("Zip File", new string[] { ".zip" }));

            filepicker.FilePathChanged += (s, e) => CurrentModel.ModelPath = filepicker.FilePath;

            dl.CreateAndAddControlRow(filepicker);

            dl.CreateAndAddEmptySpace();

            dl.CreateAndAddCheckBoxRow("Embed Model Data", true, (chk, e) =>
            {
                if (chk.Checked.GetValueOrDefault())
                {
                    CurrentModel.ModelSource = ANNModel.ModelSourceType.Embedded;
                }
                else
                {
                    CurrentModel.ModelSource = ANNModel.ModelSourceType.FileSystem;
                }
            });

            page.Init(Width, Height);
            page.Topmost = false;

            var scrollable = new Scrollable { Content = dl, Border = BorderType.None, Size = new Size(Width, Height), ExpandContentHeight = true };

            page.ContentContainer.Add(scrollable);

            page.Show();

        }

        private void DisplayPage_DefineLabels()
        {

            int no = 1;
            bool scaled = false;

            var page = new WizardPage();

            var checks = new List<CheckBox>();
            var tboxes = new List<TextBox>();

            page.hasBackButton = true;
            page.hasCancelButton = true;
            page.hasNextButton = true;
            page.hasFinishButton = false;

            page.cancelAction = () =>
            {
                page.Close();
            };

            page.backAction = () =>
            {
                page.Close();
                Application.Instance.Invoke(() => DisplayPage_LoadModel());
            };

            TextArea tb = new TextArea
            {
                Height = 300,
                Font = new Font(FontFamilies.Monospace, 10.0f)
            };

            page.nextAction = () =>
            {
                try
                {
                    var lines = tb.Text.Split(new string[] { "\r\n", "\n" }, StringSplitOptions.RemoveEmptyEntries);
                    CurrentModel.Parameters.Labels = new List<string>();
                    CurrentModel.Parameters.Labels_Outputs = new List<string>();
                    foreach (var line in lines)
                    {
                        CurrentModel.Parameters.Labels.Add(line);
                    }
                    for (var i = lines.Count() - no; i < lines.Count(); i++)
                    {
                        CurrentModel.Parameters.Labels_Outputs.Add(lines[i]);
                    }
                    if (scaled)
                    {
                        Application.Instance.Invoke(() => DisplayPage_DefineVariableLimits());
                    }
                    else
                    {
                        Application.Instance.Invoke(() => DisplayPage_LoadedModelParameters());
                    }
                }
                catch (Exception ex)
                {
                    MessageBox.Show(ex.ToString(), "Error", MessageBoxType.Error);
                }
                page.Close();
            };

            page.Title = "Neural Network Model Wizard";
            page.HeaderTitle = "Step 3 - Define Labels";
            page.HeaderDescription = "Define the Labels of the Variables in the Model.";
            page.FooterText = "Click 'Next' to continue.";

            var dl = c.GetDefaultContainer();
            dl.Width = Width;

            dl.CreateAndAddLabelRow2("Enter the variable labels (names), one per line. The output variables must be the last on the list.");

            dl.CreateAndAddControlRow(tb);

            dl.CreateAndAddNumericEditorRow("Number of Output Variables", no, 1.0, 100.0, 0, (ns, e) =>
            {
                no = (int)ns.Value;
            });

            dl.CreateAndAddCheckBoxRow("Variables are Scaled", scaled, (chk, e) =>
            {
                scaled = chk.Checked.GetValueOrDefault();
            });

            page.Init(Width, Height);
            page.Topmost = false;

            var scrollable = new Scrollable { Content = dl, Border = BorderType.None, Size = new Size(Width, Height), ExpandContentHeight = true };

            page.ContentContainer.Add(scrollable);

            page.Show();

        }

        private void DisplayPage_DefineVariableLimits()
        {

            var page = new WizardPage();

            var checks = new List<CheckBox>();
            var tboxes = new List<TextBox>();

            page.hasBackButton = true;
            page.hasCancelButton = true;
            page.hasNextButton = true;
            page.hasFinishButton = false;

            page.cancelAction = () =>
            {
                page.Close();
            };

            page.backAction = () =>
            {
                page.Close();
                Application.Instance.Invoke(() => DisplayPage_LoadModel());
            };

            TextArea tb = new TextArea
            {
                Height = 300,
                Font = new Font(FontFamilies.Monospace, 10.0f)
            };

            page.nextAction = () =>
            {
                try
                {
                    var lines = tb.Text.Split(new string[] { "\r\n", "\n" }, StringSplitOptions.RemoveEmptyEntries);
                    CurrentModel.Parameters.MinValues = new List<float>();
                    CurrentModel.Parameters.MaxValues = new List<float>();
                    for (var i = 0; i < lines.Count(); i++)
                    {
                        CurrentModel.Parameters.MinValues.Add((float)lines[i].Split(';')[0].ToDoubleFromCurrent());
                        CurrentModel.Parameters.MaxValues.Add((float)lines[i].Split(';')[1].ToDoubleFromCurrent());
                    }
                    Application.Instance.Invoke(() => DisplayPage_LoadedModelParameters());
                }
                catch (Exception ex)
                {
                    MessageBox.Show(ex.ToString(), "Error", MessageBoxType.Error);
                }
                page.Close();
            };

            page.Title = "Neural Network Model Wizard";
            page.HeaderTitle = "Step 3A - Define Variable Limits";
            page.HeaderDescription = "Define the minimum and maximum values of the variables according to the data used to train the model.";
            page.FooterText = "Click 'Next' to continue.";

            var dl = c.GetDefaultContainer();
            dl.Width = Width;

            dl.CreateAndAddLabelRow2("Enter the variables' minimum and maximum values separated by a semicolon (;), including the output ones, one pair per line.");

            dl.CreateAndAddControlRow(tb);

            page.Init(Width, Height);
            page.Topmost = false;

            var scrollable = new Scrollable { Content = dl, Border = BorderType.None, Size = new Size(Width, Height), ExpandContentHeight = true };

            page.ContentContainer.Add(scrollable);

            page.Show();

        }

        private void DisplayPage_LoadedModelParameters()
        {

            var page = new WizardPage();

            var checks = new List<CheckBox>();
            var tboxes = new List<TextBox>();

            page.hasBackButton = true;
            page.hasCancelButton = true;
            page.hasNextButton = false;
            page.hasFinishButton = true;

            page.cancelAction = () =>
            {
                page.Close();
            };

            page.backAction = () =>
            {
                Application.Instance.Invoke(() => DisplayPage_LabelData());
                page.Close();
            };
            page.finishAction = () =>
            {
                page.Close();
            };

            page.Title = "Neural Network Model Wizard";
            page.HeaderTitle = "Step 4 - Loaded Model Parameters";
            page.HeaderDescription = "Configure Loaded Model Parameters";
            page.FooterText = "Click 'Finish' to Close this Wizard.";

            var dl = c.GetDefaultContainer();
            dl.Width = Width;

            var p = CurrentModel.Parameters;

            dl.CreateAndAddTextBoxRow(nf, "Minimum Scaled Value", p.MinScale, (tb, e) =>
            {
                if (tb.Text.IsValidDouble())
                {
                    p.MinScale = (float)tb.Text.ToDoubleFromCurrent();
                }
            });

            dl.CreateAndAddTextBoxRow(nf, "Maximum Scaled Value", p.MaxScale, (tb, e) =>
            {
                if (tb.Text.IsValidDouble())
                {
                    p.MinScale = (float)tb.Text.ToDoubleFromCurrent();
                }
            });

            dl.CreateAndAddStringEditorRow("Tensor Name for Input Variables (X)", CurrentModel.Parameters.TensorName_X, (tb, e) =>
            {
                CurrentModel.Parameters.TensorName_X = tb.Text;
            });

            dl.CreateAndAddStringEditorRow("Tensor Name for Output Variables (Y)", CurrentModel.Parameters.TensorName_Y, (tb, e) =>
            {
                CurrentModel.Parameters.TensorName_Y = tb.Text;
            });

            dl.CreateAndAddStringEditorRow("Tensor Name for Output Layer", CurrentModel.Parameters.TensorName_Output, (tb, e) =>
            {
                CurrentModel.Parameters.TensorName_Output = tb.Text;
            });

            page.Init(Width, Height);
            page.Topmost = false;

            var scrollable = new Scrollable { Content = dl, Border = BorderType.None, Size = new Size(Width, Height), ExpandContentHeight = true };

            page.ContentContainer.Add(scrollable);

            page.Show();

        }

    }
}
