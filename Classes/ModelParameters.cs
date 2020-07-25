using DWSIM.Interfaces;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Linq;

namespace DWSIM.UnitOperations.NeuralNetwork.Classes
{
    public class ModelParameters : ICustomXMLSerialization
    {

        public float MinScale { get; set; } = -1.0f;

        public float MaxScale { get; set; } = 1.0f;

        public List<float> MinValues { get; set; } = new List<float>();

        public List<float> MaxValues { get; set; } = new List<float>();

        public int NumberOfEpochs { get; set; } = 1000;

        public float RelativeMSETolerance { get; set; } = 1E-3f;

        public float AbsoluteMSETolerance { get; set; } = 1E-4f;

        public int NumberOfLayers { get; set; } = 3;

        public int NumberOfNeuronsOnFirstLayer { get; set; } = 160;

        public int BatchSize { get; set; } = 10;

        public float LearningRate { get; set; } = 0.01f;

        public float SplitFactor { get; set; } = 0.7f;

        public List<string> Labels { get; set; } = new List<string>();

        public  List<string> Labels_Outputs { get; set; } = new List<string>();

        public bool LoadData(List<XElement> data)
        {
            return XMLSerializer.XMLSerializer.Deserialize(this, data);
        }

        public List<XElement> SaveData()
        {
            return XMLSerializer.XMLSerializer.Serialize(this);
        }

    }
}
