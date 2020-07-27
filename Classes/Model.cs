using DWSIM.CrossPlatform.UI.Controls.ReoGrid.IO.OpenXML.Schema;
using DWSIM.Interfaces;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Linq;
using DWSIM.ExtensionMethods;
using NumSharp;
using Eto.Forms;
using Tensorflow;
using Tensorflow.Gradients;
using static Tensorflow.Binding;
using OxyPlot;
using DWSIM.UI.Desktop.Mac.TouchBar;
using System.Xml.Serialization;

namespace DWSIM.UnitOperations.NeuralNetwork.Classes
{
    public class ANNModel : ICustomXMLSerialization, IDisposable
    {

        public enum ModelSourceType
        {
            FileSystem = 0,
            Embedded = 1
        }

        public ModelParameters Parameters { get; set; } = new ModelParameters();

        public List<List<double>> Data { get; set; } = new List<List<double>>();

        public string DataSourcePath { get; set; } = "";

        public string ModelPath { get; set; } = "";

        public string ModelName { get; set; } = "MyModel";

        public string SerializedModelData { get; set; } = "";

        public string TensorName_X { get; set; } = "Train/X:0";

        public string TensorName_Y { get; set; } = "Train/Y:0";

        public string TensorName_Output { get; set; } = "Train/out:0";

        public ModelSourceType ModelSource { get; set; } = ModelSourceType.Embedded;

        // fields

        [XmlIgnore]
        [Newtonsoft.Json.JsonIgnore]
        public Session session;

        [XmlIgnore]
        [Newtonsoft.Json.JsonIgnore]
        public NDArray x_train, y_train, x_test, y_test, yp_train, yp_test;

        [XmlIgnore]
        [Newtonsoft.Json.JsonIgnore]
        public NDArray x_train_unscaled, y_train_unscaled, x_test_unscaled, y_test_unscaled, yp_train_unscaled, yp_test_unscaled;

        [XmlIgnore]
        [Newtonsoft.Json.JsonIgnore]
        public int n_samples, n_x, n_y;

        public ANNModel()
        {

        }

        public string Summary()
        {
            var d1 = SerializedModelData;
            SerializedModelData = "";
            var data = Newtonsoft.Json.JsonConvert.SerializeObject(this, Newtonsoft.Json.Formatting.Indented);
            SerializedModelData = d1;
            return data;
        }

        public List<XElement> SaveData()
        {
            var elements = XMLSerializer.XMLSerializer.Serialize(this);
            var xel = new XElement("Data");
            foreach (var list in Data)
            {
                var xel2 = new XElement("Row");
                foreach (var d in list)
                {
                    xel2.Add(new XElement("Value", d));
                }
                xel.Add(xel2);
            }
            elements.Add(xel);
            return elements;
        }

        public bool LoadData(List<XElement> data)
        {
            XMLSerializer.XMLSerializer.Deserialize(this, data);
            var d1 = data.Where(x => x.Name == "Data").FirstOrDefault().Elements().ToList();
            Data = new List<List<double>>();
            foreach (var xel in d1)
            {
                var list = new List<double>();
                foreach (var el in xel.Elements())
                {
                    list.Add(el.Value.ToDoubleFromInvariant());
                }
                Data.Add(list);
            }
            return true;
        }

        public void PrepareData()
        {

            var transfdata = Data.Select(x => x.ToArray()).ToArray();

            Parameters.MinValues = new List<float>();
            Parameters.MaxValues = new List<float>();

            for (var j = 0; j < Data[0].Count(); j++)
            {
                Parameters.MinValues.Add(1E20f);
                Parameters.MaxValues.Add(-1E20f);
            }

            for (var i = 0; i < Data.Count(); i++)
            {
                for (var j = 0; j < Data[0].Count(); j++)
                {
                    if (transfdata[i][j] < Parameters.MinValues[j])
                        Parameters.MinValues[j] = (float)transfdata[i][j];
                    if (transfdata[i][j] > Parameters.MaxValues[j])
                        Parameters.MaxValues[j] = (float)transfdata[i][j];
                }
            }

            for (var i = 0; i < Data.Count(); i++)
            {
                for (var j = 0; j < Data[0].Count(); j++)
                {
                    transfdata[i][j] = Classes.Utils.Scale((float)transfdata[i][j],
                        Parameters.MinValues[j],
                        Parameters.MaxValues[j],
                        Parameters.MinScale,
                        Parameters.MaxScale);
                }
            }

            var ndata = np.array(transfdata);

            n_samples = ndata.shape[0];

            // Training and test data
            var train_start = 0;
            var train_end = Math.Floor(Parameters.SplitFactor * n_samples);
            var test_start = train_end + 1;
            var test_end = n_samples;

            var data_train = ndata[np.arange(train_start, train_end)];
            var data_test = ndata[np.arange(test_start, test_end)];

            // Build x and y

            var idx = Parameters.Labels.IndexOf(Parameters.Labels_Outputs.First());
            var nouts = Parameters.Labels.Count;

            x_train = data_train[Slice.All, "0:" + idx.ToString()];
            y_train = data_train[Slice.All, idx.ToString() + ":" + nouts.ToString()];
            x_test = data_test[Slice.All, "0:" + idx.ToString()];
            y_test = data_test[Slice.All, idx.ToString() + ":" + nouts.ToString()];

            // Number of variables in training data

            n_x = x_train.shape[1];
            n_y = y_train.shape[1];

        }

        public void Train(IFlowsheet flowsheet, TextArea ta = null, Eto.OxyPlot.Plot plot = null)
        {

            var nl = Environment.NewLine;

            var g = tf.Graph();

            g.as_default();

            if (session != null) { session.Dispose(); session = null; }

            session = tf.Session(graph: g);

            tf_with(tf.variable_scope("Train"), delegate
            {

                if (flowsheet != null)
                {
                    flowsheet.ShowMessage("Training Started...", IFlowsheet.MessageType.Information);
                }
                else
                {
                    Application.Instance.Invoke(() =>
                    {
                        ta.Append("Training Started..." + nl, true);
                    });
                }

                // tf Graph Input

                var X = tf.placeholder(tf.float32, shape: (-1, n_x), name: "X");
                var Y = tf.placeholder(tf.float32, shape: (-1, n_y), name: "Y");

                Tensor outlayer = null;

                var sigma = 1.0f;
                var weight_initializer = tf.variance_scaling_initializer(mode: "FAN_AVG", uniform: true, factor: sigma);
                var bias_initializer = tf.zeros_initializer;

                var n_neurons_1 = Parameters.NumberOfNeuronsOnFirstLayer;
                var n_neurons_2 = n_neurons_1 / 2;
                var n_neurons_3 = n_neurons_2 / 2;
                var n_neurons_4 = n_neurons_3 / 2;

                RefVariable W_hidden_1, W_hidden_2, W_hidden_3, W_hidden_4, W_out;
                RefVariable bias_hidden_1, bias_hidden_2, bias_hidden_3, bias_hidden_4, bias_out;
                Tensor hidden_1, hidden_2, hidden_3, hidden_4;

                switch (Parameters.NumberOfLayers)
                {
                    case 2:
                        // Hidden weights
                        W_hidden_1 = tf.Variable(weight_initializer.call(new int[] { n_x, n_neurons_1 }, dtype: TF_DataType.TF_FLOAT), name: "W1");
                        bias_hidden_1 = tf.Variable(bias_initializer.call(n_neurons_1, dtype: TF_DataType.TF_FLOAT), name: "b1");
                        W_hidden_2 = tf.Variable(weight_initializer.call(new int[] { n_neurons_1, n_neurons_2 }, dtype: TF_DataType.TF_FLOAT), name: "W2");
                        bias_hidden_2 = tf.Variable(bias_initializer.call(n_neurons_2, dtype: TF_DataType.TF_FLOAT), name: "b2");
                        // Output weights
                        W_out = tf.Variable(weight_initializer.call(new int[] { n_neurons_2, n_y }, dtype: TF_DataType.TF_FLOAT), name: "Wout");
                        bias_out = tf.Variable(bias_initializer.call(n_y, dtype: TF_DataType.TF_FLOAT), name: "bout");
                        // Hidden layer
                        hidden_1 = tf.nn.relu(tf.add(tf.matmul(X, W_hidden_1), bias_hidden_1), name: "h1");
                        hidden_2 = tf.nn.relu(tf.add(tf.matmul(hidden_1, W_hidden_2), bias_hidden_2), name: "h2");
                        // Output layer
                        outlayer = tf.add(tf.matmul(hidden_2, W_out), bias_out, name: "out");
                        break;
                    case 3:
                        // Hidden weights
                        W_hidden_1 = tf.Variable(weight_initializer.call(new int[] { n_x, n_neurons_1 }, dtype: TF_DataType.TF_FLOAT), name: "W1");
                        bias_hidden_1 = tf.Variable(bias_initializer.call(n_neurons_1, dtype: TF_DataType.TF_FLOAT), name: "b1");
                        W_hidden_2 = tf.Variable(weight_initializer.call(new int[] { n_neurons_1, n_neurons_2 }, dtype: TF_DataType.TF_FLOAT), name: "W2");
                        bias_hidden_2 = tf.Variable(bias_initializer.call(n_neurons_2, dtype: TF_DataType.TF_FLOAT), name: "b2");
                        W_hidden_3 = tf.Variable(weight_initializer.call(new int[] { n_neurons_2, n_neurons_3 }, dtype: TF_DataType.TF_FLOAT), name: "W3");
                        bias_hidden_3 = tf.Variable(bias_initializer.call(n_neurons_3, dtype: TF_DataType.TF_FLOAT), name: "b3");
                        // Output weights
                        W_out = tf.Variable(weight_initializer.call(new int[] { n_neurons_3, n_y }, dtype: TF_DataType.TF_FLOAT), name: "Wout");
                        bias_out = tf.Variable(bias_initializer.call(n_y, dtype: TF_DataType.TF_FLOAT), name: "bout");
                        // Hidden layer
                        hidden_1 = tf.nn.relu(tf.add(tf.matmul(X, W_hidden_1), bias_hidden_1), name: "h1");
                        hidden_2 = tf.nn.relu(tf.add(tf.matmul(hidden_1, W_hidden_2), bias_hidden_2), name: "h2");
                        hidden_3 = tf.nn.relu(tf.add(tf.matmul(hidden_2, W_hidden_3), bias_hidden_3), name: "h3");
                        // Output layer
                        outlayer = tf.add(tf.matmul(hidden_3, W_out), bias_out, name: "out");
                        break;
                    case 4:
                        // Hidden weights
                        W_hidden_1 = tf.Variable(weight_initializer.call(new int[] { n_x, n_neurons_1 }, dtype: TF_DataType.TF_FLOAT), name: "W1");
                        bias_hidden_1 = tf.Variable(bias_initializer.call(n_neurons_1, dtype: TF_DataType.TF_FLOAT), name: "b1");
                        W_hidden_2 = tf.Variable(weight_initializer.call(new int[] { n_neurons_1, n_neurons_2 }, dtype: TF_DataType.TF_FLOAT), name: "W2");
                        bias_hidden_2 = tf.Variable(bias_initializer.call(n_neurons_2, dtype: TF_DataType.TF_FLOAT), name: "b2");
                        W_hidden_3 = tf.Variable(weight_initializer.call(new int[] { n_neurons_2, n_neurons_3 }, dtype: TF_DataType.TF_FLOAT), name: "W3");
                        bias_hidden_3 = tf.Variable(bias_initializer.call(n_neurons_3, dtype: TF_DataType.TF_FLOAT), name: "b3");
                        W_hidden_4 = tf.Variable(weight_initializer.call(new int[] { n_neurons_3, n_neurons_4 }, dtype: TF_DataType.TF_FLOAT), name: "W4");
                        bias_hidden_4 = tf.Variable(bias_initializer.call(n_neurons_4, dtype: TF_DataType.TF_FLOAT), name: "b4");
                        // Output weights
                        W_out = tf.Variable(weight_initializer.call(new int[] { n_neurons_4, n_y }, dtype: TF_DataType.TF_FLOAT), name: "Wout");
                        bias_out = tf.Variable(bias_initializer.call(n_y, dtype: TF_DataType.TF_FLOAT), name: "bout");
                        // Hidden layer
                        hidden_1 = tf.nn.relu(tf.add(tf.matmul(X, W_hidden_1), bias_hidden_1), name: "h1");
                        hidden_2 = tf.nn.relu(tf.add(tf.matmul(hidden_1, W_hidden_2), bias_hidden_2), name: "h2");
                        hidden_3 = tf.nn.relu(tf.add(tf.matmul(hidden_2, W_hidden_3), bias_hidden_3), name: "h3");
                        hidden_4 = tf.nn.relu(tf.add(tf.matmul(hidden_3, W_hidden_4), bias_hidden_4), name: "h4");
                        // Output layer
                        outlayer = tf.add(tf.matmul(hidden_4, W_out), bias_out, name: "out");
                        break;
                }

                // Mean squared error
                var mse = tf.reduce_sum(tf.pow(outlayer - Y, 2.0f), name: "mse");

                var learn_rate = tf.constant(Parameters.LearningRate);

                var opt = tf.train.AdamOptimizer(learn_rate).minimize(mse);

                // Fit neural net

                var batch_size = Parameters.BatchSize;

                var mse_train = new List<float>();
                var mse_test = new List<float>();

                // Initialize the variables (i.e. assign their default value)

                var init = tf.global_variables_initializer();

                // Run the initializer

                session.run(init);

                // Start training

                var epochs = Parameters.NumberOfEpochs;

                foreach (var e in range(epochs))
                {

                    // Shuffle training data
                    var shuffle_indices = np.random.permutation(np.arange(len(x_train)));

                    var shuffled_x = new NDArray(np.float32, x_train.shape);
                    var shuffled_y = new NDArray(np.float32, y_train.shape);

                    int i0 = 0;
                    foreach (var idx0 in shuffle_indices)
                    {
                        shuffled_x[i0] = x_train[idx0];
                        shuffled_y[i0] = y_train[idx0];
                        i0 += 1;
                    }

                    // Minibatch training
                    foreach (var i in range(0, len(y_train) / batch_size))
                    {
                        var start = i * batch_size;

                        var batch_x = shuffled_x[start.ToString() + ":" + (start + batch_size).ToString(), Slice.All];
                        var batch_y = shuffled_y[start.ToString() + ":" + (start + batch_size).ToString(), Slice.All];

                        // Run optimizer with batch
                        session.run(opt, (X, batch_x), (Y, batch_y));

                        // Show progress
                        var divrem = 0;
                        Math.DivRem(e, 5, out divrem);

                        if (divrem == 0)
                        {
                            // MSE train and test
                            mse_train.Add(session.run(mse, (X, x_train), (Y, y_train)));
                            mse_test.Add(session.run(mse, (X, x_test), (Y, y_test)));
                            if (flowsheet != null)
                            {
                                flowsheet.ShowMessage("Epoch: " + e.ToString(), IFlowsheet.MessageType.Information);
                                flowsheet.ShowMessage("MSE (training): " + mse_train.Last().ToString(), IFlowsheet.MessageType.Information);
                                flowsheet.ShowMessage("MSE (testing): " + mse_test.Last().ToString(), IFlowsheet.MessageType.Information);
                            }
                            else
                            {
                                Application.Instance.Invoke(() =>
                                {
                                    ta.Append("Epoch: " + e.ToString() + nl, true);
                                    ta.Append("MSE (training): " + mse_train.Last().ToString() + nl, true);
                                    ta.Append("MSE (testing): " + mse_test.Last().ToString() + nl, true);
                                    (plot.Model.Series[0] as OxyPlot.Series.LineSeries).Points.Add(new DataPoint(e, mse_train.Last()));
                                    (plot.Model.Series[1] as OxyPlot.Series.LineSeries).Points.Add(new DataPoint(e, mse_test.Last()));
                                    plot.Model.InvalidatePlot(true);
                                });
                            }
                            if (e > 10 &&
                                (Math.Abs(mse_train.Last() - mse_train[mse_train.Count - 2]) / mse_train[mse_train.Count - 2] <
                                Parameters.RelativeMSETolerance)) break;
                        }
                    }
                }

                if (flowsheet != null)
                {
                    flowsheet.ShowMessage("Training Finished!", IFlowsheet.MessageType.Information);
                }
                else
                {
                    Application.Instance.Invoke(() =>
                    {
                        ta.Append("Training Finished!" + nl, true);
                    });
                }

                x_test_unscaled = new NDArray(np.float32, x_test.shape);
                x_train_unscaled = new NDArray(np.float32, x_train.shape);

                for (var i = 0; i < x_test.shape[0]; i++)
                {
                    for (var j = 0; j < x_test.shape[1]; j++)
                    {
                        x_test_unscaled[i][j] = Classes.Utils.UnScale(x_test[i][j],
                        Parameters.MinValues[j],
                        Parameters.MaxValues[j],
                        Parameters.MinScale,
                        Parameters.MaxScale);
                    }
                }

                for (var i = 0; i < x_train.shape[0]; i++)
                {
                    for (var j = 0; j < x_train.shape[1]; j++)
                    {
                        x_train_unscaled[i][j] = Classes.Utils.UnScale(x_train[i][j],
                        Parameters.MinValues[j],
                        Parameters.MaxValues[j],
                        Parameters.MinScale,
                        Parameters.MaxScale);
                    }
                }

                var idx = Parameters.Labels.IndexOf(Parameters.Labels_Outputs.First());

                y_test_unscaled = new NDArray(np.float32, y_test.shape);
                y_train_unscaled = new NDArray(np.float32, y_train.shape);

                for (var i = 0; i < y_test.shape[0]; i++)
                {
                    for (var j = 0; j < y_test.shape[1]; j++)
                    {
                        y_test_unscaled[i][j] = Classes.Utils.UnScale(y_test[i][j],
                        Parameters.MinValues[idx + j],
                        Parameters.MaxValues[idx + j],
                        Parameters.MinScale,
                        Parameters.MaxScale);
                    }
                }

                for (var i = 0; i < y_train.shape[0]; i++)
                {
                    for (var j = 0; j < y_train.shape[1]; j++)
                    {
                        y_train_unscaled[i][j] = Classes.Utils.UnScale(y_train[i][j],
                        Parameters.MinValues[idx + j],
                        Parameters.MaxValues[idx + j],
                        Parameters.MinScale,
                        Parameters.MaxScale);
                    }
                }

                yp_test = session.run(outlayer, (X, x_test));
                yp_train = session.run(outlayer, (X, x_train));

                yp_test_unscaled = new NDArray(np.float32, yp_test.shape);
                yp_train_unscaled = new NDArray(np.float32, yp_train.shape);

                for (var i = 0; i < yp_test.shape[0]; i++)
                {
                    for (var j = 0; j < yp_test.shape[1]; j++)
                    {
                        yp_test_unscaled[i][j] = Classes.Utils.UnScale(yp_test[i][j],
                        Parameters.MinValues[idx + j],
                        Parameters.MaxValues[idx + j],
                        Parameters.MinScale,
                        Parameters.MaxScale);
                    }
                }

                for (var i = 0; i < yp_train.shape[0]; i++)
                {
                    for (var j = 0; j < yp_train.shape[1]; j++)
                    {
                        yp_train_unscaled[i][j] = Classes.Utils.UnScale(yp_train[i][j],
                        Parameters.MinValues[idx + j],
                        Parameters.MaxValues[idx + j],
                        Parameters.MinScale,
                        Parameters.MaxScale);
                    }
                }

                // Testing example

                var training_cost = session.run(mse, (X, x_train), (Y, y_train));
                var testing_cost = session.run(mse, (X, x_test), (Y, y_test));
                var diff = Math.Abs((float)training_cost - (float)testing_cost);

                if (flowsheet != null)
                {
                    flowsheet.ShowMessage($"Training Cost = {testing_cost}", IFlowsheet.MessageType.Information);
                    flowsheet.ShowMessage($"Testing Cost = {testing_cost}", IFlowsheet.MessageType.Information);
                    flowsheet.ShowMessage($"Absolute MSE = {diff}", IFlowsheet.MessageType.Information);
                }
                else
                {
                    Application.Instance.Invoke(() =>
                    {
                        ta.Append($"Training Cost = {testing_cost}" + nl, true);
                        ta.Append($"Testing Cost = {testing_cost}" + nl, true);
                        ta.Append($"Absolute MSE = {diff}" + nl, true);
                    });
                }

            });

        }

        bool disposedValue = false;

        public void Dispose()
        {
            // Check to see if Dispose has already been called.
            if (!disposedValue)
            {
                if (session != null)
                {
                    session.Dispose();
                    session = null;
                }
                //Note disposing has been done.
                disposedValue = true;
            }
        }

    }
}
