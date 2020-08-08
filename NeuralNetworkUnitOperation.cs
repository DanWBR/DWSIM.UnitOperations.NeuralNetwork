using DWSIM.Drawing.SkiaSharp.GraphicObjects;
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
using DWSIM.UnitOperations.NeuralNetwork.Editors;
using DWSIM.UnitOperations.NeuralNetwork.Classes;
using System.Reflection;
using System.IO;
using System.Xml.Serialization;
using utils = DWSIM.UnitOperations.NeuralNetwork.Classes.Utils;
using NumSharp;
using Microsoft.Scripting.Hosting;
using Eto.Drawing;
using Point = DWSIM.DrawingTools.Point.Point;
using Font = Eto.Drawing.Font;
using DWSIM.UnitOperations.Streams;
using cpui = DWSIM.CrossPlatform.UI.Controls.ReoGrid;
using cui = unvell.ReoGrid;

namespace DWSIM.UnitOperations
{
    public class NeuralNetworkUnitOperation : UnitOperations.UnitOpBaseClass, Interfaces.IExternalUnitOperation
    {

        private String ImagePath = "";
        private SKImage Image;

        // props

        public ANNModel Model { get; set; }

        public string DataTransferScript { get; set; } = "";

        public List<Tuple<string, string, string, string, string>> InputMaps = new List<Tuple<string, string, string, string, string>>();

        public List<Tuple<string, string, string, string, string>> OutputMaps = new List<Tuple<string, string, string, string, string>>();

        public List<float> OutputVariablesValuesFromLastRun = new List<float>();

        // standard props

        public override bool MobileCompatible { get => false; }

        public override SimulationObjectClass ObjectClass { get => SimulationObjectClass.UserModels; set => base.ObjectClass = SimulationObjectClass.UserModels; }

        string IExternalUnitOperation.Name => "Neural Network Unit Operation";

        string IExternalUnitOperation.Description => "Neural Network Unit Operation";

        public string Description => "Neural Network Unit Operation";

        public string Prefix => "NNET-";


        [NonSerialized] [XmlIgnore] public FormEditorNNUO f;

        public NeuralNetworkUnitOperation() : base()
        {
            if (!LocalSettings.Initialized)
            {
                // sets the assembly resolver to find remaining DWSIM libraries on demand
                AppDomain currentDomain = AppDomain.CurrentDomain;
                currentDomain.AssemblyResolve += new ResolveEventHandler(LoadFromNestedFolder);
                LocalSettings.Initialized = true;
            }
            Model = new ANNModel();
            InitializeMappings();
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

            for (var i = 0; i < 11; i++)
            {
                if (GraphicObject.InputConnectors.Count == 11)
                {
                    if (i < 10)
                    {
                        GraphicObject.InputConnectors[i].Position = new Point(x, y + (float)(i + 1) / 10f * h);
                        GraphicObject.OutputConnectors[i].Position = new Point(x + w, y + (float)(i + 1) / 10f * h);
                        GraphicObject.InputConnectors[i].ConnectorName = "Inlet Material " + (i + 1).ToString() + " (Port " + (i + 1).ToString() + ")";
                        GraphicObject.OutputConnectors[i].ConnectorName = "Outlet Material " + (i + 1).ToString() + " (Port " + (i + 1).ToString() + ")";
                    }
                    else
                    {
                        GraphicObject.InputConnectors[i].Position = new Point(x, y + (float)(i + 1) / 10f * h);
                        GraphicObject.OutputConnectors[i].Position = new Point(x + w, y + (float)(i + 1) / 10f * h);
                        GraphicObject.InputConnectors[i].ConnectorName = "Inlet Energy 1 (Port 11)";
                        GraphicObject.OutputConnectors[i].ConnectorName = "Outlet Energy 1 (Port 11)";
                    }
                }
                else
                {
                    if (i < 10)
                    {
                        var myIC = new ConnectionPoint();
                        myIC.Position = new Point(x, y + (float)(i + 1) / 10f * h);
                        myIC.Type = ConType.ConIn;
                        myIC.Direction = ConDir.Right;
                        myIC.ConnectorName = "Inlet Material " + (i + 1).ToString() + " (Port " + (i + 1).ToString() + ")";
                        GraphicObject.InputConnectors.Add(myIC);
                        var myOC = new ConnectionPoint();
                        myOC.Position = new Point(x + w, y + (float)(i + 1) / 10f * h);
                        myOC.Type = ConType.ConOut;
                        myOC.Direction = ConDir.Right;
                        myOC.ConnectorName = "Outlet Material " + (i + 1).ToString() + " (Port " + (i + 1).ToString() + ")";
                        GraphicObject.OutputConnectors.Add(myOC);
                    }
                    else
                    {
                        var myIC = new ConnectionPoint();
                        myIC.Position = new Point(x, y + (float)(i + 1) / 10f * h);
                        myIC.Type = ConType.ConEn;
                        myIC.Direction = ConDir.Right;
                        myIC.ConnectorName = "Inlet Energy 1 (Port 11)";
                        GraphicObject.InputConnectors.Add(myIC);
                        var myOC = new ConnectionPoint();
                        myOC.Position = new Point(x + w, y + (float)(i + 1) / 10f * h);
                        myOC.Type = ConType.ConEn;
                        myOC.Direction = ConDir.Right;
                        myOC.ConnectorName = "Outlet Energy 1 (Port 11)";
                        GraphicObject.OutputConnectors.Add(myOC);
                    }
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

            c.CreateAndAddButtonRow("Open Model Configuration Wizard", null, (btn, e) =>
            {
                var f = new NeuralNetwork.Wizard.ModelWizard(this);
                f.Show();
            });

            c.CreateAndAddEmptySpace();

            var dc = new DocumentControl { Height = 300, DisplayArrows = true };
            var dp1 = new DocumentPage { Closable = false, Text = "General" };
            var dp2 = new DocumentPage { Closable = false, Text = "Input Mappings" };
            var dp3 = new DocumentPage { Closable = false, Text = "Output Mappings" };
            var dp4 = new DocumentPage { Closable = false, Text = "Data Transfer Script" };
            var dp5 = new DocumentPage { Closable = false, Text = "Model Summary" };

            dc.Pages.Add(dp1);
            dc.Pages.Add(dp2);
            dc.Pages.Add(dp3);
            dc.Pages.Add(dp4);
            dc.Pages.Add(dp5);

            c.CreateAndAddControlRow(dc);

            var rl = new RadioButtonList { Orientation = Orientation.Horizontal };
            rl.Spacing = new Size(10, 10);
            rl.Items.Add("Embedded");
            rl.Items.Add("External File");
            rl.SelectedIndex = Model.ModelSource == ANNModel.ModelSourceType.Embedded ? 0 : 1;
            rl.SelectedIndexChanged += (s, e) =>
            {
                Model.ModelSource = rl.SelectedIndex == 0 ? ANNModel.ModelSourceType.Embedded : ANNModel.ModelSourceType.FileSystem;
            };
            var lay1 = DWSIM.UI.Shared.Common.GetDefaultContainer();
            lay1.CreateAndAddLabelRow2("Model Source:");
            lay1.CreateAndAddControlRow(rl);
            lay1.CreateAndAddEmptySpace();
            lay1.CreateAndAddLabelRow2("Model Path (if external):");
            var filepicker = new FilePicker { Title = "Load Model from Zip File", FileAction = Eto.FileAction.OpenFile };
            filepicker.Filters.Add(new FileFilter("Zip File", new string[] { ".zip" }));
            filepicker.FilePathChanged += (s, e) =>
            {
                Model.ModelPath = filepicker.FilePath;
                Model.ModelName = Path.GetFileNameWithoutExtension(Model.ModelPath);
            };
            lay1.CreateAndAddControlRow(filepicker);
            lay1.CreateAndAddDescriptionRow("Only use the above search button to locate the model file if the Unit Operation " +
                "is set to use a Model File and it was moved from its original location (i.e. you're receiving this simulation" +
                " from someone else). To use and configure a different model, use the Model Configuration Wizard.");
            lay1.CreateAndAddEmptySpace();
            lay1.CreateAndAddButtonRow("View Help", null, (btn, e) =>
            {
                "http://dwsim.inforside.com.br/wiki/index.php?title=Neural_Network_Unit_Operation".OpenURL();
            });
            lay1.CreateAndAddEmptySpace();
            lay1.CreateAndAddButtonRow("About", null, (btn, e) =>
            {
                Eto.Forms.Application.Instance.Invoke(() =>
                {
                    var f = new FormAbout();
                    f.ShowDialog();
                });
            });

            dp1.Content = lay1;

            var lay2 = new TableLayout() { Spacing = new Size(10, 10), Padding = new Padding(5) };
            var btn1 = new Button { Text = "Update" };
            var sed = new Eto.Forms.Controls.Scintilla.Shared.ScintillaControl();
            sed.ScriptText = DataTransferScript;
            btn1.Click += (s, e) => DataTransferScript = sed.ScriptText;
            lay2.Rows.Add(btn1);
            lay2.Rows.Add(sed);
            dp4.Content = lay2;

            var stacks = PopulateMappings();

            dp2.Content = new Scrollable { Content = stacks[0], Border = BorderType.None, ExpandContentWidth = true };
            dp3.Content = new Scrollable { Content = stacks[1], Border = BorderType.None, ExpandContentWidth = true };

            var layt = new TableLayout();
            layt.CreateAndAddControlRow(new TextArea { Text = Model.Summary(), ReadOnly = true, Font = new Font(FontFamilies.Monospace, 10.0f) }); ;
            dp5.Content = layt;

        }

        private TableLayout[] PopulateMappings()
        {

            List<string> props1, props2, props1Names, props2Names;

            var msprops1 = new MaterialStream("", "", FlowSheet, null).GetProperties(PropertyType.ALL);
            var esprops1 = new EnergyStream().GetProperties(PropertyType.ALL);
            var msprops2 = new MaterialStream("", "", FlowSheet, null).GetProperties(PropertyType.WR);

            var p1 = new List<string>();
            p1.Add("");
            p1.AddRange(msprops1);
            p1.AddRange(esprops1);

            var p2 = new List<string>();
            p2.Add("");
            p2.AddRange(msprops2);
            p2.AddRange(esprops1);

            if (GlobalSettings.Settings.OldUI)
            {
                var grid = (cui.ReoGridControl)FlowSheet.GetSpreadsheetObject();
                var sheets = grid.Worksheets.Select(x => x.Name).ToArray();
                p1.AddRange(sheets);
                p2.AddRange(sheets);
            }
            else
            {
                var grid = (cpui.ReoGridControl)FlowSheet.GetSpreadsheetObject();
                var sheets = grid.Worksheets.Select(x => x.Name).ToArray();
                p1.AddRange(sheets);
                p2.AddRange(sheets);
            }

            props1 = p1.ToList();
            props2 = p2.ToList();

            props1Names = props1.Select(x => FlowSheet.GetTranslatedString(x)).ToList();
            props2Names = props2.Select(x => FlowSheet.GetTranslatedString(x)).ToList();

            var ports = new string[] { "", "1", "2", "3", "4", "5", "6", "7", "8", "9", "10", "11", "S" }.ToList();

            var stack1 = new TableLayout();
            var stack2 = new TableLayout();

            for (var i = 0; i < InputMaps.Count(); i++)
            {
                var imap = InputMaps[i];
                var layout = UI.Shared.Common.GetDefaultContainer();
                layout.Tag = i;
                layout.CreateAndAddLabelRow("Variable: " + Model.Parameters.Labels[i]);
                layout.CreateAndAddDropDownRow("Port", ports, ports.IndexOf(imap.Item1), (s, e) =>
                {
                    imap = InputMaps[(int)layout.Tag];
                    InputMaps[(int)layout.Tag] = new Tuple<string, string, string, string, string>(ports[s.SelectedIndex], imap.Item2, imap.Item3, "", "");
                });
                layout.CreateAndAddDropDownRow("Property", props1Names, props1.IndexOf(imap.Item2), (s, e) =>
                {
                    imap = InputMaps[(int)layout.Tag];
                    InputMaps[(int)layout.Tag] = new Tuple<string, string, string, string, string>(imap.Item1, props1[s.SelectedIndex], imap.Item3, "", "");
                });
                layout.CreateAndAddStringEditorRow("Units", imap.Item3, (s, e) =>
                {
                    imap = InputMaps[(int)layout.Tag];
                    InputMaps[(int)layout.Tag] = new Tuple<string, string, string, string, string>(imap.Item1, imap.Item2, s.Text, "", "");
                });
                stack1.Rows.Add(layout);
            }

            for (var i = 0; i < OutputMaps.Count(); i++)
            {
                var imap = OutputMaps[i];
                var layout = UI.Shared.Common.GetDefaultContainer();
                layout.Tag = i;
                layout.CreateAndAddLabelRow("Variable: " + Model.Parameters.Labels_Outputs[i]);
                layout.CreateAndAddDropDownRow("Port", ports, ports.IndexOf(imap.Item1), (s, e) =>
                {
                    imap = OutputMaps[(int)layout.Tag];
                    OutputMaps[(int)layout.Tag] = new Tuple<string, string, string, string, string>(ports[s.SelectedIndex], imap.Item2, imap.Item3, "", "");
                });
                layout.CreateAndAddDropDownRow("Property", props2Names, props2.IndexOf(imap.Item2), (s, e) =>
                {
                    imap = OutputMaps[(int)layout.Tag];
                    OutputMaps[(int)layout.Tag] = new Tuple<string, string, string, string, string>(imap.Item1, props1[s.SelectedIndex], imap.Item3, "", "");
                });
                layout.CreateAndAddStringEditorRow("Units", imap.Item3, (s, e) =>
                {
                    imap = OutputMaps[(int)layout.Tag];
                    OutputMaps[(int)layout.Tag] = new Tuple<string, string, string, string, string>(imap.Item1, imap.Item2, s.Text, "", "");
                });
                stack2.Rows.Add(layout);
            }

            return new[] { stack1, stack2 };

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
            var props = base.GetDefaultProperties().ToList();
            props.Insert(0, "Status");
            return props.ToArray();
        }

        public override string[] GetProperties(PropertyType proptype)
        {
            var props = base.GetProperties(PropertyType.ALL).ToList();
            props.Insert(0, "Status");
            return props.ToArray();
        }

        public override object GetPropertyValue(string prop, IUnitsOfMeasure su = null)
        {
            var propval = base.GetPropertyValue(prop, su);
            if (propval == null)
            {
                switch (prop)
                {
                    case "Status":
                        if (Calculated) return "Solved"; else return "Not Solved";
                    default:
                        return 0.0;
                }
            }
            else
            {
                return propval;
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

            OutputVariablesValuesFromLastRun = new List<float>();
            for (var i = 0; i < pred_unscaled.shape[1]; i++)
            {
                OutputVariablesValuesFromLastRun.Add(pred_unscaled[0][i]);
                SetOutputVariableValue(i, pred_unscaled[0][i]);
            }

        }

        public float GetInputVariableValue(int index)
        {
            try
            {
                var imap = InputMaps[index];
                if (imap.Item1 == "S")
                {
                    if (GlobalSettings.Settings.OldUI)
                    {
                        var grid = (cui.ReoGridControl)FlowSheet.GetSpreadsheetObject();
                        return float.Parse(grid.Worksheets[imap.Item2].Cells[imap.Item3].Data.ToString());
                    }
                    else
                    {
                        var grid = (cpui.ReoGridControl)FlowSheet.GetSpreadsheetObject();
                        return float.Parse(grid.Worksheets[imap.Item2].Cells[imap.Item3].Data.ToString());
                    }
                }
                else
                {
                    var port = int.Parse(imap.Item1) - 1;
                    var propID = imap.Item2;
                    var units = imap.Item3;
                    var objID = GraphicObject.InputConnectors[port].AttachedConnector.AttachedFrom.Name;
                    if (GraphicObject.OutputConnectors[port].IsAttached)
                    {
                        return (float)FlowSheet.SimulationObjects[objID].GetPropertyValue(propID).ToString().ToDoubleFromCurrent().ConvertFromSI(units);
                    }
                    else
                    {
                        FlowSheet.ShowMessage(String.Format(GraphicObject.Tag + ": could not get value of input variable {0}. {1}",
                        Model.Parameters.Labels_Outputs[index],
                        "Variable is not mapped to a valid flowsheet object/property."), IFlowsheet.MessageType.Warning);
                        return float.NaN;
                    }
                }
            }
            catch (Exception ex)
            {
                FlowSheet.ShowMessage(String.Format(GraphicObject.Tag + ": could not get value of input variable {0}. {1}",
                    Model.Parameters.Labels_Outputs[index], ex.Message), IFlowsheet.MessageType.GeneralError);
                return float.NaN;
            }
        }

        public void SetOutputVariableValue(int index, float value)
        {
            try
            {
                var omap = OutputMaps[index];
                if (omap.Item1 == "S")
                {
                    if (GlobalSettings.Settings.OldUI)
                    {
                        var grid = (cui.ReoGridControl)FlowSheet.GetSpreadsheetObject();
                        grid.Worksheets[omap.Item2].Cells[omap.Item3].Data = value;
                    }
                    else
                    {
                        var grid = (cpui.ReoGridControl)FlowSheet.GetSpreadsheetObject();
                        grid.Worksheets[omap.Item2].Cells[omap.Item3].Data = value;
                    }
                }
                else
                {
                    var port = int.Parse(omap.Item1) - 1;
                    var propID = omap.Item2;
                    var units = omap.Item3;
                    if (GraphicObject.OutputConnectors[port].IsAttached)
                    {
                        var objID = GraphicObject.OutputConnectors[port].AttachedConnector.AttachedTo.Name;
                        FlowSheet.SimulationObjects[objID].SetPropertyValue(propID, ((double)value).ConvertToSI(units));
                    }
                    else
                    {
                        FlowSheet.ShowMessage(String.Format(GraphicObject.Tag + ": could not set output variable {0}={1}. {2}",
                        Model.Parameters.Labels_Outputs[index],
                        value.ToString(), "Variable is not mapped to a valid flowsheet object/property."), IFlowsheet.MessageType.Warning);
                    }
                }
            }
            catch (Exception ex)
            {
                FlowSheet.ShowMessage(String.Format(GraphicObject.Tag + ": could not set output variable {0}={1}. {2}",
                    Model.Parameters.Labels_Outputs[index],
                    value.ToString(), ex.Message), IFlowsheet.MessageType.GeneralError);
            }
        }

        public void WriteOutputVariableTo(string varname, string objtag, string prop, string units)
        {
            var obj = FlowSheet.GetFlowsheetSimulationObject(objtag);
            var propID = obj.GetProperties(PropertyType.ALL).Where(x => prop == FlowSheet.GetTranslatedString(x)).FirstOrDefault();
            if (propID != null)
            {
                obj.SetPropertyValue(propID,
                    OutputVariablesValuesFromLastRun[Model.Parameters.Labels_Outputs.IndexOf(varname)].ToString().ToDoubleFromCurrent().ConvertToSI(units));
            }
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
            }
        }



    }
}
