using DWSIM.Drawing.SkiaSharp.GraphicObjects;
using DWSIM.DrawingTools.Point;
using DWSIM.Interfaces;
using DWSIM.Interfaces.Enums;
using DWSIM.Interfaces.Enums.GraphicObjects;
using SkiaSharp;
using System;
using System.Collections.Generic;
using System.Linq;
using DWSIM.UI.Shared;
using Eto.Forms;
using System.Xml.Linq;
using DWSIM.Thermodynamics.Streams;
using DWSIM.ExtensionMethods;
using cv = DWSIM.SharedClasses.SystemsOfUnits.Converter;
using DWSIM.UI.Desktop.Editors;
using System.Threading.Tasks;
using DWSIM.UnitOperations.NeuralNetwork.Editors;
using DWSIM.GlobalSettings;
using DWSIM.UnitOperations.NeuralNetwork.Classes;
using System.Reflection;
using System.IO;
using Tensorflow;
using System.Xml.Serialization;
using utils = DWSIM.UnitOperations.NeuralNetwork.Classes.Utils;
using NumSharp;
using System.Runtime.CompilerServices;
using System.Diagnostics.Contracts;
using System.ComponentModel;
using Microsoft.Scripting.Hosting;

namespace DWSIM.UnitOperations
{
    public class NeuralNetworkUnitOperation : UnitOperations.UnitOpBaseClass, Interfaces.IExternalUnitOperation
    {

        private String ImagePath = "";
        private SKImage Image;

        // props

        double mbal, pbal, ebal;

        public ANNModel Model { get; set; } = new ANNModel();

        public string DataTransferScript { get; set; } = "";

        public List<Tuple<string, string, string, string, string>> InputMaps = new List<Tuple<string, string, string, string, string>>();

        public List<Tuple<string, string, string, string, string>> OutputMaps = new List<Tuple<string, string, string, string, string>>();

        // standard props

        public override bool MobileCompatible { get => false; }

        public override SimulationObjectClass ObjectClass { get => SimulationObjectClass.UserModels; set => base.ObjectClass = SimulationObjectClass.UserModels; }

        string IExternalUnitOperation.Name => "Neural Network Unit Operation";

        string IExternalUnitOperation.Description => "Neural Network Unit Operation";

        public string Description => "Neural Network Unit Operation";

        public string Prefix => "NNET-";


        [NonSerialized] [System.Xml.Serialization.XmlIgnore] public FormEditorNNUO f;

        public NeuralNetworkUnitOperation() : base()
        {
            if (!LocalSettings.Initialized)
            {
                // sets the assembly resolver to find remaining DWSIM libraries on demand
                AppDomain currentDomain = AppDomain.CurrentDomain;
                currentDomain.AssemblyResolve += new ResolveEventHandler(LoadFromNestedFolder);
                LocalSettings.Initialized = true;
            }
            InitializeMappings();
        }

        public void InitializeMappings()
        {
            InputMaps = new List<Tuple<string, string, string, string, string>>();
            OutputMaps = new List<Tuple<string, string, string, string, string>>();
            for (var i = 0; i < Model.Parameters.Labels.Count - Model.Parameters.Labels_Outputs.Count; i++)
            {
                InputMaps.Add(new Tuple<string, string, string, string, string>("", "", "", "", ""));
            }
            for (var i = 0; i < Model.Parameters.Labels_Outputs.Count; i++)
            {
                OutputMaps.Add(new Tuple<string, string, string, string, string>("", "", "", "", ""));
            }
        }

        static Assembly LoadFromNestedFolder(object sender, ResolveEventArgs args)
        {
            string assemblyPath = Path.Combine(Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location), "libraries", new AssemblyName(args.Name).Name + ".dll");
            if (!File.Exists(assemblyPath))
            {
                return null;
            }
            else
            {
                Assembly assembly = Assembly.LoadFrom(assemblyPath);
                return assembly;
            }
        }

        public override void PerformPostCalcValidation()
        {
            // do nothing
        }

        public override object CloneJSON()
        {
            return Newtonsoft.Json.JsonConvert.DeserializeObject<NeuralNetworkUnitOperation>(Newtonsoft.Json.JsonConvert.SerializeObject(this));
        }

        public override object CloneXML()
        {
            ICustomXMLSerialization obj = new NeuralNetworkUnitOperation();
            obj.LoadData(this.SaveData());
            return obj;
        }

        public override void CloseEditForm()
        {
            if (f != null)
            {
                if (!f.IsDisposed)
                {
                    f.Close();
                    f = null;
                }
            }
        }

        public void CreateConnectors()
        {

            float w, h, x, y;
            w = GraphicObject.Width;
            h = GraphicObject.Height;
            x = GraphicObject.X;
            y = GraphicObject.Y;

            for (var i = 0; i < 10; i++)
            {
                if (GraphicObject.InputConnectors.Count == 10)
                {
                    GraphicObject.InputConnectors[i].Position = new Point(x, y + (float)(i + 1) / 10f * h);
                    GraphicObject.OutputConnectors[i].Position = new Point(x + w, y + (float)(i + 1) / 10f * h);
                    GraphicObject.InputConnectors[i].ConnectorName = "Inlet Port #" + (i + 1).ToString();
                    GraphicObject.OutputConnectors[i].ConnectorName = "Outlet Port #" + (i + 1).ToString();
                }
                else
                {
                    var myIC = new ConnectionPoint();
                    myIC.Position = new Point(x, y + (float)(i + 1) / 10f * h);
                    myIC.Type = ConType.ConIn;
                    myIC.Direction = ConDir.Right;
                    myIC.ConnectorName = "Inlet Port #" + (i + 1).ToString();
                    GraphicObject.InputConnectors.Add(myIC);

                    var myOC = new ConnectionPoint();
                    myOC.Position = new Point(x + w, y + (float)(i + 1) / 10f * h);
                    myOC.Type = ConType.ConOut;
                    myOC.Direction = ConDir.Right;
                    myOC.ConnectorName = "Outlet Port #" + (i + 1).ToString();
                    GraphicObject.OutputConnectors.Add(myOC);
                }
            }

            GraphicObject.EnergyConnector.Active = false;

        }

        public override void DisplayEditForm()
        {
            if (f == null)
            {
                f = new FormEditorNNUO { SimObject = this };
                f.Tag = "ObjectEditor";
                FlowSheet.DisplayForm(f);
            }
            else
            {
                if (f.IsDisposed)
                {
                    f = new FormEditorNNUO { SimObject = this };
                    f.Tag = "ObjectEditor";
                    FlowSheet.DisplayForm(f);
                }
                else
                {
                    f.Activate();
                }
            }
        }

        public void Draw(object g)
        {

            SKCanvas canvas = (SKCanvas)g;

            if (Image == null)
            {
                ImagePath = System.IO.Path.GetTempFileName();

                var bitmap = new System.Drawing.Bitmap(System.Reflection.Assembly.GetExecutingAssembly().GetManifestResourceStream("DWSIM.UnitOperations.NeuralNetwork.Resources.icons8-artificial_intelligence.png"));
                bitmap.Save(ImagePath);

                using (var streamBG = new System.IO.FileStream(ImagePath, System.IO.FileMode.Open))
                {
                    using (var bitmap2 = SKBitmap.Decode(streamBG))
                    {
                        Image = SKImage.FromBitmap(bitmap2);
                    }
                }
            }

            try
            {
                System.IO.File.Delete(ImagePath);
            }
            catch { }

            using (var p = new SKPaint { IsAntialias = GlobalSettings.Settings.DrawingAntiAlias, FilterQuality = SKFilterQuality.High })
            {
                canvas.DrawImage(Image, new SKRect(GraphicObject.X, GraphicObject.Y, GraphicObject.X + GraphicObject.Width, GraphicObject.Y + GraphicObject.Height), p);
            }

        }

        public override string GetDisplayDescription()
        {
            return Description;
        }

        public override string GetDisplayName()
        {
            return "Neural Network Unit Operation";
        }

        public override string ComponentDescription
        {
            get => Description;
            set => _ = value;
        }

        public override object GetIconBitmap()
        {
            return new System.Drawing.Bitmap(System.Reflection.Assembly.GetExecutingAssembly().GetManifestResourceStream("DWSIM.UnitOperations.NeuralNetwork.Resources.icons8-artificial_intelligence.png"));
        }

        public void PopulateEditorPanel(object container)
        {

            var su = FlowSheet.FlowsheetOptions.SelectedUnitSystem;
            var nf = FlowSheet.FlowsheetOptions.NumberFormat;

            var c = (DynamicLayout)container;

            c.CreateAndAddEmptySpace();

            c.CreateAndAddEmptySpace();

            c.CreateAndAddButtonRow("Open Model Wizard", null, (btn, e) =>
            {
                var f = new NeuralNetwork.Wizard.ModelWizard(this);
                f.Show();
            });

            c.CreateAndAddEmptySpace();

            c.CreateAndAddButtonRow("View Help", null, (btn, e) =>
            {
                "http://dwsim.inforside.com.br/wiki/index.php?title=Neural_Network_Unit_Operation".OpenURL();
            });

            c.CreateAndAddEmptySpace();

            c.CreateAndAddButtonRow("About", null, (btn, e) =>
            {
                Eto.Forms.Application.Instance.Invoke(() =>
                {
                    var f = new FormAbout();
                    f.ShowDialog();
                });
            });

        }

        public object ReturnInstance(string typename)
        {
            return new NeuralNetworkUnitOperation();
        }

        public override void UpdateEditForm()
        {
            if (f != null)
            {
                if (!f.IsDisposed)
                {
                    if (f.InvokeRequired)
                    {
                        f.UIThreadInvoke(() => f.UpdateInfo());
                    }
                    else
                    {
                        f.UpdateInfo();
                    }
                }
            }
        }

        // load/save

        public override List<XElement> SaveData()
        {
            var elements = XMLSerializer.XMLSerializer.Serialize(this);
            var xel = new XElement("InputMaps");
            foreach (var item in InputMaps)
            {
                xel.Add(new XElement("Mapping", new XAttribute("Value1", item.Item1),
                    new XAttribute("Value2", item.Item2),
                    new XAttribute("Value3", item.Item3),
                    new XAttribute("Value4", item.Item4),
                    new XAttribute("Value5", item.Item5)));
            }
            elements.Add(xel);
            var xel2 = new XElement("OutputMaps");
            foreach (var item in OutputMaps)
            {
                xel2.Add(new XElement("Mapping", new XAttribute("Value1", item.Item1),
                    new XAttribute("Value2", item.Item2),
                    new XAttribute("Value3", item.Item3),
                    new XAttribute("Value4", item.Item4),
                    new XAttribute("Value5", item.Item5)));
            }
            elements.Add(xel2);
            return elements;
        }

        public override bool LoadData(List<XElement> data)
        {
            XMLSerializer.XMLSerializer.Deserialize(this, data);
            var d1 = data.Where(x => x.Name == "InputMaps").FirstOrDefault()?.Elements().ToList();
            InputMaps = new List<Tuple<string, string, string, string, string>>();
            if (d1 != null)
            {
                foreach (var xel in d1)
                {
                    InputMaps.Add(new Tuple<string, string, string, string, string>(xel.Attribute("Value1").Value,
                        xel.Attribute("Value2").Value,
                        xel.Attribute("Value3").Value,
                        xel.Attribute("Value4").Value,
                        xel.Attribute("Value5").Value));
                }
            }
            var d2 = data.Where(x => x.Name == "OutputMaps").FirstOrDefault()?.Elements().ToList();
            OutputMaps = new List<Tuple<string, string, string, string, string>>();
            if (d2 != null)
            {
                foreach (var xel in d2)
                {
                    OutputMaps.Add(new Tuple<string, string, string, string, string>(xel.Attribute("Value1").Value,
                        xel.Attribute("Value2").Value,
                        xel.Attribute("Value3").Value,
                        xel.Attribute("Value4").Value,
                        xel.Attribute("Value5").Value));
                }
            }
            if (InputMaps.Count + OutputMaps.Count != Model.Parameters.Labels.Count)
            {
                InitializeMappings();
            }
            return true;
        }

        // display properties

        public override string[] GetDefaultProperties()
        {
            return new string[] { "Status", "Mass Balance Residual", "Pressure Balance Residual", "Energy Balance Residual" };
        }

        public override string[] GetProperties(PropertyType proptype)
        {
            return new string[] { "Status", "Mass Balance Residual", "Pressure Balance Residual", "Energy Balance Residual" };
        }

        public override object GetPropertyValue(string prop, IUnitsOfMeasure su = null)
        {
            switch (prop)
            {
                case "Status":
                    if (Calculated) return "Solved"; else return "Not Solved";
                case "Mass Balance Residual":
                    return mbal;
                case "Pressure Balance Residual":
                    return pbal;
                case "Energy Balance Residual":
                    return ebal;
                default:
                    return 0.0;
            }
        }

        public override string GetPropertyUnit(string prop, IUnitsOfMeasure su = null)
        {
            return "";
        }

        public override void Calculate(object args = null)
        {

            RunDataTransferScript();

            if (Model.session == null)
            {
                if (Model.ModelSource == ANNModel.ModelSourceType.FileSystem)
                {
                    Model.session = utils.LoadGraphFromZip(Model.ModelPath);
                }
                else
                {
                    using (var ms = utils.Base64ToStream(Model.SerializedModelData))
                    {
                        Model.session = utils.LoadGraphFromStream(ms);
                    }
                }
            }

            Model.session.graph.as_default();

            var outlayer = Model.session.graph.get_tensor_by_name(Model.Parameters.TensorName_Output);
            var X = Model.session.graph.get_tensor_by_name(Model.Parameters.TensorName_X);
            var Y = Model.session.graph.get_tensor_by_name(Model.Parameters.TensorName_Y);

            var nt = Model.Parameters.Labels.Count;
            var no = Model.Parameters.Labels_Outputs.Count;

            var inputvars = new List<float>();
            for (var i = 0; i < nt - no; i++)
            {
                inputvars.Add(GetInputVariableValue(i));
            }

            var input = new NDArray(inputvars.ToArray());

            input = input.reshape(1, nt - no);

            var input_scaled = new NDArray(np.float32, input.shape);

            for (var i = 0; i < input.shape[0]; i++)
            {
                for (var j = 0; j < input.shape[1]; j++)
                {
                    input_scaled[i][j] = utils.Scale(input[i][j],
                    Model.Parameters.MinValues[j],
                    Model.Parameters.MaxValues[j],
                    Model.Parameters.MinScale,
                    Model.Parameters.MaxScale);
                }
            }

            var pred_scaled = Model.session.run(outlayer, (X, input_scaled));

            var pred_unscaled = new NDArray(np.float32, pred_scaled.shape);

            var idx = Model.Parameters.Labels.IndexOf(Model.Parameters.Labels_Outputs.First());

            for (var i = 0; i < pred_scaled.shape[0]; i++)
            {
                for (var j = 0; j < pred_scaled.shape[1]; j++)
                {
                    pred_unscaled[i][j] = utils.UnScale(pred_scaled[i][j],
                    Model.Parameters.MinValues[idx + j],
                    Model.Parameters.MaxValues[idx + j],
                    Model.Parameters.MinScale,
                    Model.Parameters.MaxScale);
                }
            }

            for (var i = 0; i < pred_unscaled.shape[1]; i++)
            {
                SetOutputVariableValue(i, pred_unscaled[0][i]);
            }

        }

        public float GetInputVariableValue(int index)
        {
            var imap = InputMaps[index];
            var port = int.Parse(imap.Item1) - 1;
            var propID = imap.Item2;
            var units = imap.Item3;
            var objID = GraphicObject.InputConnectors[port].AttachedConnector.AttachedFrom.Name;
            return (float)FlowSheet.SimulationObjects[objID].GetPropertyValue(propID).ToString().ToDoubleFromCurrent().ConvertFromSI(units);
        }

        public void SetOutputVariableValue(int index, float value)
        {
            var omap = OutputMaps[index];
            var port = int.Parse(omap.Item1) - 1;
            var propID = omap.Item2;
            var units = omap.Item3;
            var objID = GraphicObject.OutputConnectors[port].AttachedConnector.AttachedTo.Name;
            FlowSheet.SimulationObjects[objID].SetPropertyValue(propID, ((double)value).ConvertToSI(units));
        }

        public Tuple<string, string, string, string, string> GetInputVariableMap(string varname)
        {
            return InputMaps[Model.Parameters.Labels.IndexOf(varname)];        
        }

        public Tuple<string, string, string, string, string> GetOutputVariableMap(string varname)
        {
            return OutputMaps[Model.Parameters.Labels_Outputs.IndexOf(varname)];
        }

        public void RunDataTransferScript()
        {

            ScriptScope scope;
            ScriptEngine engine;

            var opts = new Dictionary<string, object>();
            opts["Frames"] = Microsoft.Scripting.Runtime.ScriptingRuntimeHelpers.True;
            engine = IronPython.Hosting.Python.CreateEngine(opts);
            engine.Runtime.LoadAssembly(typeof(System.String).Assembly);
            engine.Runtime.LoadAssembly(typeof(Thermodynamics.BaseClasses.ConstantProperties).Assembly);
            engine.Runtime.LoadAssembly(typeof(Drawing.SkiaSharp.GraphicsSurface).Assembly);
            scope = engine.CreateScope();
            scope.SetVariable("Flowsheet", this.FlowSheet);
            scope.SetVariable("This", this);
            var source = engine.CreateScriptSourceFromString(DataTransferScript, Microsoft.Scripting.SourceCodeKind.Statements);
            try
            {
                source.Execute(scope);
            }
            catch (Exception ex)
            {
                var ops = engine.GetService<ExceptionOperations>();
                FlowSheet.ShowMessage("Error running script: " + ops.FormatException(ex).ToString(), IFlowsheet.MessageType.GeneralError);
            }
            finally
            {
                engine.Runtime.Shutdown();
                engine = null;
                scope = null;
                source = null;
            }
        }



    }
}
