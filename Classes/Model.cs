using DWSIM.CrossPlatform.UI.Controls.ReoGrid.IO.OpenXML.Schema;
using DWSIM.Interfaces;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Linq;
using DWSIM.ExtensionMethods;

namespace DWSIM.UnitOperations.NeuralNetwork.Classes
{
    public class ANNModel: ICustomXMLSerialization
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

        public ModelSourceType ModelSource { get; set; } = ModelSourceType.Embedded;

        public ANNModel()
        {

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
    }
}
