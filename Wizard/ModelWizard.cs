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
            CurrentModel = SimObject.Model;
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

            rl.SelectedIndex = 0;

            page1.nextAction = () =>
            {
                switch (rl.SelectedIndex)
                {
                    case 0:
                        CurrentModel = new ANNModel();
                        DisplayPage_LoadData();
                        break;
                    case 1:
                        break;
                    case 2:
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

            page.cancelAction = () => page.Close();
            page.backAction = () =>
            {
                DisplayPage_LoadData();
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

            page.cancelAction = () => page.Close();
            page.backAction = () =>
            {
                DisplayPage_LoadData();
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

            page.cancelAction = () => page.Close();
            page.backAction = () =>
            {
                DisplayPage_LabelData();
                page.Close();
            };
            page.nextAction = () =>
            {
                Application.Instance.Invoke(() => DisplayPage_ML());
                page.Close();
            };

            page.Title = "Neural Network Model Wizard";
            page.HeaderTitle = "Step 5 - Model Parameters";
            page.HeaderDescription = "Configure Model Parameters.";
            page.FooterText = "Click 'Next' to Continue.";

            var dl = c.GetDefaultContainer();
            dl.Width = Width;

            var p = CurrentModel.Parameters;

            dl.CreateAndAddTextBoxRow(nf, "Minimum Scaled Value", p.MinScale, (tb, e) => {
                if (tb.Text.IsValidDouble())
                {
                    p.MinScale = (float)tb.Text.ToDoubleFromCurrent();
                }
            });

            dl.CreateAndAddTextBoxRow(nf, "Maximum Scaled Value", p.MaxScale, (tb, e) => {
                if (tb.Text.IsValidDouble())
                {
                    p.MinScale = (float)tb.Text.ToDoubleFromCurrent();
                }
            });

            dl.CreateAndAddTextBoxRow("N0", "Number of Training Epochs (Iterations)", p.NumberOfEpochs, (tb, e) => {
                if (tb.Text.IsValidDouble())
                {
                    p.NumberOfEpochs = (int)tb.Text.ToDoubleFromCurrent();
                }
            });

            dl.CreateAndAddTextBoxRow("N0", "Number of Network Layers (Min = 2, Max = 4)", p.NumberOfLayers, (tb, e) => {
                if (tb.Text.IsValidDouble())
                {
                    var val = (int)tb.Text.ToDoubleFromCurrent();
                    if (val < 2) val = 2;
                    if (val > 4) val = 4;
                    p.NumberOfLayers = val;
                }
            });

            dl.CreateAndAddTextBoxRow(nf, "Optimizer Learning Rate", p.LearningRate, (tb, e) => {
                if (tb.Text.IsValidDouble())
                {
                    p.LearningRate = (float)tb.Text.ToDoubleFromCurrent();
                }
            });

            dl.CreateAndAddTextBoxRow("G6", "Minimum Relative Tolerance for MSE", p.RelativeMSETolerance, (tb, e) => {
                if (tb.Text.IsValidDouble())
                {
                    p.RelativeMSETolerance = (float)tb.Text.ToDoubleFromCurrent();
                }
            });

            dl.CreateAndAddTextBoxRow(nf, "Split Factor for Data Training/Testing", p.SplitFactor, (tb, e) => {
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

            page.cancelAction = () => page.Close();
            page.backAction = () =>
            {
                page.Close();
                DisplayPage_ModelParameters();
            };
            page.nextAction = () =>
            {
                page.Close();
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
            };

            page.Title = "Neural Network Model Wizard";
            page.HeaderTitle = "Step 6 - Model Training and Evaluation";
            page.HeaderDescription = "Click on the Train and Evaluate Button to Train your Model.";
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
                Task.Factory.StartNew(() => Run(tb, plot));
            });

            dl.CreateAndAddControlRow(tl);

            page.Init(Width, Height);
            page.Topmost = false;

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

        NDArray x_train, y_train, x_test, y_test;
        int n_samples, n_x, n_y;

        public bool Run(TextArea ta, Eto.OxyPlot.Plot plot)
        {
            tf.compat.v1.disable_eager_execution();

            PrepareData();

            RunModel(ta, plot);

            return true;
        }

        public void RunModel(TextArea ta, Eto.OxyPlot.Plot plot)
        {

            var nl = Environment.NewLine;

            // tf Graph Input

            var X = tf.placeholder(tf.float32, shape: (-1, n_x));
            var Y = tf.placeholder(tf.float32, shape: (-1, n_y));

            var n_neurons_1 = 120;
            var n_neurons_2 = 60;
            var n_neurons_3 = 30;
            var n_neurons_4 = 15;

            var sigma = 1.0f;
            var weight_initializer = tf.variance_scaling_initializer(mode: "FAN_AVG", uniform: true, factor: sigma);
            var bias_initializer = tf.zeros_initializer;

            // Hidden weights

            var W_hidden_1 = tf.Variable(weight_initializer.call(new int[] { n_x, n_neurons_1 }, dtype: TF_DataType.TF_FLOAT));
            var bias_hidden_1 = tf.Variable(bias_initializer.call(n_neurons_1, dtype: TF_DataType.TF_FLOAT));
            var W_hidden_2 = tf.Variable(weight_initializer.call(new int[] { n_neurons_1, n_neurons_2 }, dtype: TF_DataType.TF_FLOAT));
            var bias_hidden_2 = tf.Variable(bias_initializer.call(n_neurons_2, dtype: TF_DataType.TF_FLOAT));
            var W_hidden_3 = tf.Variable(weight_initializer.call(new int[] { n_neurons_2, n_neurons_3 }, dtype: TF_DataType.TF_FLOAT));
            var bias_hidden_3 = tf.Variable(bias_initializer.call(n_neurons_3, dtype: TF_DataType.TF_FLOAT));
            var W_hidden_4 = tf.Variable(weight_initializer.call(new int[] { n_neurons_3, n_neurons_4 }, dtype: TF_DataType.TF_FLOAT));
            var bias_hidden_4 = tf.Variable(bias_initializer.call(n_neurons_4, dtype: TF_DataType.TF_FLOAT));

            // Output weights

            var W_out = tf.Variable(weight_initializer.call(new int[] { n_neurons_4, n_y }, dtype: TF_DataType.TF_FLOAT));
            var bias_out = tf.Variable(bias_initializer.call(n_y, dtype: TF_DataType.TF_FLOAT));

            // Hidden layer

            var hidden_1 = tf.nn.relu(tf.add(tf.matmul(X, W_hidden_1), bias_hidden_1));
            var hidden_2 = tf.nn.relu(tf.add(tf.matmul(hidden_1, W_hidden_2), bias_hidden_2));
            var hidden_3 = tf.nn.relu(tf.add(tf.matmul(hidden_2, W_hidden_3), bias_hidden_3));
            var hidden_4 = tf.nn.relu(tf.add(tf.matmul(hidden_3, W_hidden_4), bias_hidden_4));

            // Output layer

            var outlayer = tf.add(tf.matmul(hidden_4, W_out), bias_out, name: "out");

            // Mean squared error

            var mse = tf.reduce_sum(tf.pow(outlayer - Y, 2.0f)) / (2.0f * n_samples);

            var opt = tf.train.AdamOptimizer(0.01f).minimize(mse);

            // Initialize the variables (i.e. assign their default value)

            var init = tf.global_variables_initializer();

            // Fit neural net

            var batch_size = 5;
            var mse_train = new List<float>();
            var mse_test = new List<float>();

            // Start training
            using (var sess = tf.Session())
            {

                // Run the initializer

                sess.run(init);

                var epochs = CurrentModel.Parameters.NumberOfEpochs;

                foreach (var e in range(epochs))
                {

                    // Shuffle training data
                    var shuffle_indices = np.random.permutation(np.arange(len(x_train)));

                    var shuffled_x = new NDArray(np.float32, x_train.shape);
                    var shuffled_y = new NDArray(np.float32, y_train.shape);
                    int i0 = 0;
                    foreach (var idx in shuffle_indices)
                    {
                        shuffled_x[i0] = x_train[idx];
                        shuffled_y[i0] = y_train[idx];
                        i0 += 1;
                    }

                    // Minibatch training
                    foreach (var i in range(0, len(y_train) / batch_size))
                    {
                        var start = i * batch_size;

                        var batch_x = shuffled_x[start.ToString() + ":" + (start + batch_size).ToString(), Slice.All];
                        var batch_y = shuffled_y[start.ToString() + ":" + (start + batch_size).ToString(), Slice.All];
                        // Run optimizer with batch
                        sess.run(opt, (X, batch_x), (Y, batch_y));
                        // Show progress
                        var divrem = 0;
                        Math.DivRem(i, 5, out divrem);
                        if (divrem == 0)
                        {
                            // MSE train and test
                            mse_train.Add(sess.run(mse, (X, x_train), (Y, y_train)));
                            mse_test.Add(sess.run(mse, (X, x_test), (Y, y_test)));
                            Application.Instance.Invoke(() =>
                            {
                                ta.Append("Epoch: " + e.ToString() + nl, true);
                                ta.Append("MSE (training): " + mse_train.Last().ToString() + nl, true);
                                ta.Append("MSE (testing): " + mse_test.Last().ToString() + nl, true);
                                (plot.Model.Series[0] as OxyPlot.Series.LineSeries).Points.Add(new DataPoint(e , mse_train.Last()));
                                (plot.Model.Series[1] as OxyPlot.Series.LineSeries).Points.Add(new DataPoint(e, mse_test.Last()));
                                plot.Model.InvalidatePlot(true);
                            });
                            if (e > 10 &&
                                (Math.Abs(mse_train.Last() - mse_train[mse_train.Count - 2]) / mse_train[mse_train.Count - 2] <
                                CurrentModel.Parameters.RelativeMSETolerance)) break;
                        }
                    }
                }

                var training_cost = sess.run(mse, (X, x_train), (Y, y_train));

                Application.Instance.Invoke(() =>
                {
                    ta.Append("Optimization Finished!" + nl, true);
                    ta.Append($"Training cost={training_cost}" + nl, true);
                });
                var pred = sess.run(outlayer, (X, x_test));
                var pred_unscaled = new NDArray(np.float32, pred.shape);

                for (var i = 0; i < y_test.shape[0]; i++)
                {
                    for (var j = 0; j < y_test.shape[1]; j++)
                    {
                        pred_unscaled[i][j] = UnScale(pred[i][j],
                        CurrentModel.Parameters.MinValue,
                        CurrentModel.Parameters.MaxValue,
                        CurrentModel.Parameters.MinScale,
                        CurrentModel.Parameters.MaxScale);
                    }
                }

                // Testing example

                var testing_cost = sess.run(tf.reduce_sum(tf.pow(pred - Y, 2.0f)) / (2.0f * x_test.shape[0]), (X, x_test), (Y, y_test));
                var diff = Math.Abs((float)training_cost - (float)testing_cost);

                Application.Instance.Invoke(() =>
                {
                    ta.Append("Testing... (Mean square loss Comparison)" + nl, true);
                    ta.Append($"Testing cost={testing_cost}" + nl, true);
                    ta.Append($"Absolute mean square loss difference: {diff}" + nl, true);
                });

                var saver = tf.train.Saver();
                var tempdir = CreateUniqueTempDirectory();
                saver.save(sess, Path.Combine(tempdir, CurrentModel.ModelName));

                var zippath = "C:/Users/Daniel/Desktop/" + CurrentModel.ModelName + ".zip";

                ZipFile.CreateFromDirectory(tempdir, zippath);

            }
        }

        public string CreateUniqueTempDirectory()
        {
            var uniqueTempDir = Path.GetFullPath(Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString()));
            Directory.CreateDirectory(uniqueTempDir);
            return uniqueTempDir;
        }

        public void PrepareData()
        {

            var transfdata = CurrentModel.Data.Select(x => x.ToArray()).ToArray();

            CurrentModel.Parameters.MinValue = 1E20f;
            CurrentModel.Parameters.MaxValue = -1E20f;

            for (var i = 0; i < CurrentModel.Data.Count(); i++)
            {
                for (var j = 0; j < CurrentModel.Data[0].Count(); j++)
                {
                    if (transfdata[i][j] < CurrentModel.Parameters.MinValue)
                        CurrentModel.Parameters.MinValue = (float)transfdata[i][j];
                    if (transfdata[i][j] > CurrentModel.Parameters.MaxValue)
                        CurrentModel.Parameters.MaxValue = (float)transfdata[i][j];
                }
            }

            for (var i = 0; i < CurrentModel.Data.Count(); i++)
            {
                for (var j = 0; j < CurrentModel.Data[0].Count(); j++)
                {
                    transfdata[i][j] = Scale((float)transfdata[i][j],
                        CurrentModel.Parameters.MinValue,
                        CurrentModel.Parameters.MaxValue,
                        CurrentModel.Parameters.MinScale,
                        CurrentModel.Parameters.MaxScale);
                }
            }

            var ndata = np.array(transfdata);

            n_samples = ndata.shape[0];

            // Training and test data
            var train_start = 0;
            var train_end = Math.Floor(CurrentModel.Parameters.SplitFactor * n_samples);
            var test_start = train_end + 1;
            var test_end = n_samples;

            var data_train = ndata[np.arange(train_start, train_end)];
            var data_test = ndata[np.arange(test_start, test_end)];

            // Build x and y

            var idx = CurrentModel.Parameters.Labels.IndexOf(CurrentModel.Parameters.Labels_Outputs.First());
            var nouts = CurrentModel.Parameters.Labels.Count;

            x_train = data_train[Slice.All, "0:" + idx.ToString()];
            y_train = data_train[Slice.All, idx.ToString() + ":" + nouts.ToString()];
            x_test = data_test[Slice.All, "0:" + idx.ToString()];
            y_test = data_test[Slice.All, idx.ToString() + ":" + nouts.ToString()];

            // Number of variables in training data

            n_x = x_train.shape[1];
            n_y = y_train.shape[1];

        }

        private float Scale(float value, float min, float max, float minScale, float maxScale)
        {
            float scaled = minScale + (value - min) / (max - min) * (maxScale - minScale);
            return scaled;
        }

        private float UnScale(float scaledvalue, float min, float max, float minScale, float maxScale)
        {
            float unscaled = min + (scaledvalue - minScale) * (max - min) / (maxScale - minScale);
            return unscaled;
        }

    }
}
