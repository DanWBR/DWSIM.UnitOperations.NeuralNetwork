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

namespace DWSIM.UnitOperations
{
    public class NeuralNetworkUnitOperation : UnitOperations.UnitOpBaseClass, Interfaces.IExternalUnitOperation
    {

        private String ImagePath = "";
        private SKImage Image;

        // props

        double mbal, pbal, ebal;

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

            return elements;
        }

        public override bool LoadData(List<XElement> data)
        {
            XMLSerializer.XMLSerializer.Deserialize(this, data);

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

        }

    }
}
