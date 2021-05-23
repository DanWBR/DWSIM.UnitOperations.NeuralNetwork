using System;
using System.Collections.Generic;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Windows.Forms;
using DWSIM.ExtensionMethods;
using DWSIM.Interfaces;
using DWSIM.Interfaces.Enums.GraphicObjects;
using DWSIM.Thermodynamics.Streams;
using DWSIM.UnitOperations.Streams;
using cpui = DWSIM.CrossPlatform.UI.Controls.ReoGrid;
using cui = unvell.ReoGrid;

namespace DWSIM.UnitOperations.NeuralNetwork.Editors
{
    public partial class FormEditorNNUO : WeifenLuo.WinFormsUI.Docking.DockContent
    {

        public NeuralNetworkUnitOperation SimObject;

        public bool Loaded = false;

        SharedClasses.SystemsOfUnits.Units units;

        string nf;

        Dictionary<string, string> propsdict;

        string[] sheets;

        public FormEditorNNUO()
        {
            InitializeComponent();
            ShowHint = (WeifenLuo.WinFormsUI.Docking.DockState)GlobalSettings.Settings.DefaultEditFormLocation;
            propsdict = new Dictionary<string, string>();
        }

        private void FormEditorNNUO_Load(object sender, EventArgs e)
        {
            UpdateInfo();
            txtScript.SetEditorStyle("Consolas", 10, false, false);
        }

        public void UpdateInfo()
        {

            units = (SharedClasses.SystemsOfUnits.Units)SimObject.FlowSheet.FlowsheetOptions.SelectedUnitSystem;
            nf = SimObject.FlowSheet.FlowsheetOptions.NumberFormat;

            Loaded = false;

            Text = SimObject.GraphicObject.Tag + " (" + SimObject.GetDisplayName() + ")";

            lblTag.Text = SimObject.GraphicObject.Tag;
            if (SimObject.Calculated)
            {
                lblStatus.Text = SimObject.FlowSheet.GetTranslatedString("Calculado") + " (" + SimObject.LastUpdated.ToString() + ")";
                lblStatus.ForeColor = Color.Blue;
            }
            else
            {
                if (!SimObject.GraphicObject.Active)
                {
                    lblStatus.Text = SimObject.FlowSheet.GetTranslatedString("Inativo");
                    lblStatus.ForeColor = System.Drawing.Color.Gray;
                }
                else if (SimObject.ErrorMessage != null)
                {
                    if (SimObject.ErrorMessage.Length > 50)
                    {
                        lblStatus.Text = SimObject.FlowSheet.GetTranslatedString("Erro") + " (" + SimObject.ErrorMessage.Substring(50) + "...)";
                    }
                    else
                    {
                        lblStatus.Text = SimObject.FlowSheet.GetTranslatedString("Erro") + " (" + SimObject.ErrorMessage + ")";
                    }
                    lblStatus.ForeColor = System.Drawing.Color.Red;
                }
                else
                {
                    lblStatus.Text = SimObject.FlowSheet.GetTranslatedString("NoCalculado");
                    lblStatus.ForeColor = System.Drawing.Color.Black;
                }
            }

            lblConnectedTo.Text = "";

            if (SimObject.IsSpecAttached) lblConnectedTo.Text = SimObject.FlowSheet.SimulationObjects[SimObject.AttachedSpecId].GraphicObject.Tag;
            if (SimObject.IsAdjustAttached) lblConnectedTo.Text = SimObject.FlowSheet.SimulationObjects[SimObject.AttachedAdjustId].GraphicObject.Tag;

            // connections

            var cbMS = new DataGridViewComboBoxCell();

            cbMS.Items.Add("");
            cbMS.Items.AddRange(SimObject.FlowSheet.SimulationObjects.Values.Where(x => x.GraphicObject.ObjectType == Interfaces.Enums.GraphicObjects.ObjectType.MaterialStream).Select(x2 => x2.GraphicObject.Tag).ToArray());
            cbMS.Items.AddRange(SimObject.FlowSheet.SimulationObjects.Values.Where(x => x.GraphicObject.ObjectType == Interfaces.Enums.GraphicObjects.ObjectType.EnergyStream).Select(x2 => x2.GraphicObject.Tag).ToArray());

            ((DataGridViewComboBoxColumn)gridFeeds.Columns[2]).CellTemplate = cbMS;
            ((DataGridViewComboBoxColumn)gridProducts.Columns[2]).CellTemplate = cbMS;

            gridFeeds.Rows.Clear();

            int i = 0;
            foreach (var item in SimObject.GraphicObject.InputConnectors)
            {
                gridFeeds.Rows.Add(new object[] { i, item.ConnectorName, item.IsAttached ? item.AttachedConnector.AttachedFrom.Tag : "" });
                i++;
            }

            gridProducts.Rows.Clear();

            i = 0;
            foreach (var item in SimObject.GraphicObject.OutputConnectors)
            {
                gridProducts.Rows.Add(new object[] { i, item.ConnectorName, item.IsAttached ? item.AttachedConnector.AttachedTo.Tag : "" });
                i++;
            }

            if (GlobalSettings.Settings.OldUI)
            {
                var grid = (cui.ReoGridControl)SimObject.FlowSheet.GetSpreadsheetObject();
                sheets = grid.Worksheets.Select(x => x.Name).ToArray();
            }
            else
            {
                var grid = (cpui.ReoGridControl)SimObject.FlowSheet.GetSpreadsheetObject();
                sheets = grid.Worksheets.Select(x => x.Name).ToArray();
            }

            var cbPorts = new DataGridViewComboBoxCell();

            cbPorts.Items.Add("");
            cbPorts.Items.AddRange(new string[] { "1", "2", "3", "4", "5", "6", "7", "8", "9", "10", "11", "S", "Self" });

            ((DataGridViewComboBoxColumn)gridInputMaps.Columns[1]).CellTemplate = cbPorts;
            ((DataGridViewComboBoxColumn)gridOutputMaps.Columns[1]).CellTemplate = cbPorts;

            gridInputMaps.Rows.Clear();

            i = 0;
            foreach (var item in SimObject.InputMaps)
            {
                gridInputMaps.Rows.Add(SimObject.Model.Parameters.Labels[i], item.Item1, "", item.Item3);
                var pint = 0;
                if (item.Item1 != "")
                {
                    if (int.TryParse(item.Item1, out pint))
                    {
                        var portobj = SimObject.GraphicObject.InputConnectors[pint - 1];
                        if (portobj.IsAttached)
                        {
                            var stream = portobj.AttachedConnector.AttachedFrom;
                            var cbcell = (DataGridViewComboBoxCell)gridInputMaps.Rows[gridInputMaps.Rows.Count - 1].Cells[2];
                            cbcell.Items.Clear();
                            cbcell.Items.Add("");
                            var props = SimObject.FlowSheet.SimulationObjects[stream.Name].GetProperties(Interfaces.Enums.PropertyType.ALL);
                            foreach (var prop in props)
                            {
                                if (!propsdict.ContainsKey(SimObject.FlowSheet.GetTranslatedString(prop)))
                                    propsdict.Add(SimObject.FlowSheet.GetTranslatedString(prop), prop);
                                cbcell.Items.Add(SimObject.FlowSheet.GetTranslatedString(prop));
                            }
                            try
                            {
                                cbcell.Value = SimObject.FlowSheet.GetTranslatedString(item.Item2);
                            }
                            catch { }
                        }
                    }
                    else if (item.Item1 == "S")
                    {
                        var cbcell = (DataGridViewComboBoxCell)gridInputMaps.Rows[gridInputMaps.Rows.Count - 1].Cells[2];
                        cbcell.Items.Clear();
                        cbcell.Items.Add("");
                        cbcell.Items.AddRange(sheets);
                        try
                        {
                            cbcell.Value = SimObject.FlowSheet.GetTranslatedString(item.Item2);
                        }
                        catch { }
                    }
                    else if (item.Item1 == "Self")
                    {
                        var cbcell = (DataGridViewComboBoxCell)gridInputMaps.Rows[gridInputMaps.Rows.Count - 1].Cells[2];
                        cbcell.Items.Clear();
                        cbcell.Items.Add("");
                        var props = SimObject.GetProperties(Interfaces.Enums.PropertyType.ALL);
                        foreach (var prop in props)
                        {
                            if (!propsdict.ContainsKey(SimObject.FlowSheet.GetTranslatedString(prop)))
                                propsdict.Add(SimObject.FlowSheet.GetTranslatedString(prop), prop);
                            cbcell.Items.Add(SimObject.FlowSheet.GetTranslatedString(prop));
                        }
                        try
                        {
                            cbcell.Value = SimObject.FlowSheet.GetTranslatedString(item.Item2);
                        }
                        catch { }
                    }
                }
                i += 1;
            }

            gridOutputMaps.Rows.Clear();

            i = 0;
            foreach (var item in SimObject.OutputMaps)
            {
                gridOutputMaps.Rows.Add(SimObject.Model.Parameters.Labels_Outputs[i], item.Item1, "", item.Item3);
                var pint = 0;
                if (item.Item1 != "")
                {
                    if (int.TryParse(item.Item1, out pint))
                    {
                        var portobj = SimObject.GraphicObject.OutputConnectors[pint - 1];
                        if (portobj.IsAttached)
                        {
                            var stream = portobj.AttachedConnector.AttachedTo;
                            var cbcell = (DataGridViewComboBoxCell)gridOutputMaps.Rows[gridOutputMaps.Rows.Count - 1].Cells[2];
                            cbcell.Items.Clear();
                            cbcell.Items.Add("");
                            var props = SimObject.FlowSheet.SimulationObjects[stream.Name].GetProperties(Interfaces.Enums.PropertyType.WR);
                            foreach (var prop in props)
                            {
                                if (!propsdict.ContainsKey(SimObject.FlowSheet.GetTranslatedString(prop)))
                                    propsdict.Add(SimObject.FlowSheet.GetTranslatedString(prop), prop);
                                cbcell.Items.Add(SimObject.FlowSheet.GetTranslatedString(prop));
                            }
                            try
                            {
                                cbcell.Value = SimObject.FlowSheet.GetTranslatedString(item.Item2);
                            }
                            catch { }
                        }
                    }
                    else if (item.Item1 == "S")
                    {
                        var cbcell = (DataGridViewComboBoxCell)gridOutputMaps.Rows[gridOutputMaps.Rows.Count - 1].Cells[2];
                        cbcell.Items.Clear();
                        cbcell.Items.Add("");
                        cbcell.Items.AddRange(sheets);
                        try
                        {
                            cbcell.Value = SimObject.FlowSheet.GetTranslatedString(item.Item2);
                        }
                        catch { }
                    }
                    else if (item.Item1 == "Self")
                    {
                        var cbcell = (DataGridViewComboBoxCell)gridOutputMaps.Rows[gridOutputMaps.Rows.Count - 1].Cells[2];
                        cbcell.Items.Clear();
                        cbcell.Items.Add("");
                        var props = SimObject.GetProperties(Interfaces.Enums.PropertyType.WR);
                        foreach (var prop in props)
                        {
                            if (!propsdict.ContainsKey(SimObject.FlowSheet.GetTranslatedString(prop)))
                                propsdict.Add(SimObject.FlowSheet.GetTranslatedString(prop), prop);
                            cbcell.Items.Add(SimObject.FlowSheet.GetTranslatedString(prop));
                        }
                        try
                        {
                            cbcell.Value = SimObject.FlowSheet.GetTranslatedString(item.Item2);
                        }
                        catch { }
                    }
                }
                i += 1;
            }

            if (SimObject.Model.ModelSource == Classes.ANNModel.ModelSourceType.FileSystem)
            {
                radioButton1.Checked = true;
            }
            else
            {
                radioButton2.Checked = true;
            };

            tbModelPath.Text = SimObject.Model.ModelPath;

            if (radioButton1.Checked)
            {
                tbModelPath.Enabled = true;
                btnSearchModel.Enabled = true;
            }
            else
            {
                tbModelPath.Enabled = false;
                btnSearchModel.Enabled = false;
            }

            txtScript.Text = SimObject.DataTransferScript;

            tbSummary.Text = SimObject.Model.Summary();

            Loaded = true;

        }

        private void button2_Click(object sender, EventArgs e)
        {
            "http://dwsim.inforside.com.br/wiki/index.php?title=Neural_Network_Unit_Operation".OpenURL();
        }

        private void button3_Click(object sender, EventArgs e)
        {
            var f = new FormAbout();
            f.ShowDialog();
        }

        private void lblTag_TextChanged(object sender, EventArgs e)
        {
            if (Loaded) ToolTipChangeTag.Show("Press ENTER to commit changes.", lblTag, new System.Drawing.Point(0, lblTag.Height + 3), 3000);
        }

        private void gridFeeds_CellValueChanged(object sender, DataGridViewCellEventArgs e)
        {
            if (Loaded)
            {
                if ((e.ColumnIndex == 2))
                {
                    var id = (int)gridFeeds.Rows[e.RowIndex].Cells[0].Value;
                    var value = gridFeeds.Rows[e.RowIndex].Cells[e.ColumnIndex].Value.ToString();
                    var connector = SimObject.GraphicObject.InputConnectors[id];
                    var direction = "In";
                    ConnectionChanged(connector, direction, value);
                }
                UpdateInfo();
            }
        }

        private void gridProducts_CellValueChanged(object sender, DataGridViewCellEventArgs e)
        {
            if (Loaded)
            {
                if ((e.ColumnIndex == 2))
                {
                    var id = (int)gridProducts.Rows[e.RowIndex].Cells[0].Value;
                    var value = gridProducts.Rows[e.RowIndex].Cells[e.ColumnIndex].Value.ToString();
                    var connector = SimObject.GraphicObject.OutputConnectors[id];
                    var direction = "Out";
                    ConnectionChanged(connector, direction, value);
                }
                UpdateInfo();
            }
        }

        private void ConnectionChanged(IConnectionPoint connector, string direction, string value)
        {
            var sel = value;
            var gobj = SimObject.GraphicObject;
            var flowsheet = SimObject.GetFlowsheet();
            if ((connector.IsAttached && (sel == "")))
            {
                if (((direction == "In") && ((connector.Type == ConType.ConIn) || (connector.Type == ConType.ConEn))))
                {
                    try
                    {
                        flowsheet.DisconnectObjects(connector.AttachedConnector.AttachedFrom, gobj);
                    }
                    catch (Exception ex)
                    {
                        flowsheet.ShowMessage(ex.Message.ToString(), IFlowsheet.MessageType.GeneralError);
                    }
                    flowsheet.UpdateInterface();
                    return;
                }
                else if (((connector.Type == ConType.ConOut) || (connector.Type == ConType.ConEn)))
                {
                    try
                    {
                        flowsheet.DisconnectObjects(gobj, connector.AttachedConnector.AttachedTo);
                    }
                    catch (Exception ex)
                    {
                        flowsheet.ShowMessage(ex.Message, IFlowsheet.MessageType.GeneralError);
                    }
                    flowsheet.UpdateInterface();
                    return;
                }
            }
            if ((sel != ""))
            {
                var gobj2 = flowsheet.GetFlowsheetSimulationObject(sel).GraphicObject;
                if (((direction == "In") && ((connector.Type == ConType.ConIn) || (connector.Type == ConType.ConEn))))
                {
                    if (connector.IsAttached)
                    {
                        try
                        {
                            flowsheet.DisconnectObjects(connector.AttachedConnector.AttachedFrom, gobj);
                        }
                        catch (Exception ex)
                        {
                            flowsheet.ShowMessage(ex.Message, IFlowsheet.MessageType.GeneralError);
                        }
                    }
                    if (connector.IsEnergyConnector)
                    {
                        if (gobj2.InputConnectors[0].IsAttached)
                        {
                            flowsheet.ShowMessage("Selected object already connected to another object.", IFlowsheet.MessageType.GeneralError);
                            return;
                        }
                        try
                        {
                            flowsheet.ConnectObjects(gobj, gobj2, 0, 0);
                        }
                        catch (Exception ex)
                        {
                            flowsheet.ShowMessage(ex.Message, IFlowsheet.MessageType.GeneralError);
                        }

                    }
                    else
                    {
                        if (gobj2.OutputConnectors[0].IsAttached)
                        {
                            flowsheet.ShowMessage("Selected object already connected to another object.", IFlowsheet.MessageType.GeneralError);
                            return;
                        }

                        try
                        {
                            flowsheet.ConnectObjects(gobj2, gobj, 0, gobj.InputConnectors.IndexOf(connector));
                        }
                        catch (Exception ex)
                        {
                            flowsheet.ShowMessage(ex.Message, IFlowsheet.MessageType.GeneralError);
                        }

                    }

                }
                else if (((connector.Type == ConType.ConOut) || (connector.Type == ConType.ConEn)))
                {
                    if (gobj2.InputConnectors[0].IsAttached)
                    {
                        flowsheet.ShowMessage("Selected object already connected to another object.", IFlowsheet.MessageType.GeneralError);
                        return;
                    }

                    try
                    {
                        if (connector.IsAttached)
                        {
                            flowsheet.DisconnectObjects(gobj, connector.AttachedConnector.AttachedTo);
                        }

                        flowsheet.ConnectObjects(gobj, gobj2, gobj.OutputConnectors.IndexOf(connector), 0);
                    }
                    catch (Exception ex)
                    {
                        flowsheet.ShowMessage(ex.Message, IFlowsheet.MessageType.GeneralError);
                    }
                }

                flowsheet.UpdateInterface();
            }

        }

        private void lblTag_KeyUp(object sender, KeyEventArgs e)
        {
            if (e.KeyCode == System.Windows.Forms.Keys.Enter)
            {
                if (Loaded) SimObject.GraphicObject.Tag = lblTag.Text;
                if (Loaded) SimObject.FlowSheet.UpdateOpenEditForms();
                Text = SimObject.GraphicObject.Tag + " (" + SimObject.GetDisplayName() + ")";
                ((Interfaces.IFlowsheetGUI)SimObject.FlowSheet).UpdateInterface();
            }
        }

        private void button1_Click(object sender, EventArgs e)
        {

            var f = new Wizard.ModelWizard(SimObject);

            f.Show();

            if (Eto.Forms.Application.Instance.Platform.IsWpf)
            {
                Eto.Forms.Application.Instance.Invoke(() =>
                {
                    System.Windows.Threading.Dispatcher.CurrentDispatcher.Thread.IsBackground = true;
                    System.Windows.Threading.Dispatcher.Run();
                });
            }

        }

        private void gridInputMaps_CellValueChanged(object sender, DataGridViewCellEventArgs e)
        {
            if (Loaded)
            {
                var port = gridInputMaps.Rows[e.RowIndex].Cells[1].Value.ToString();
                switch (e.ColumnIndex)
                {
                    case 1:
                        if (port != "")
                        {
                            var pint = 0;
                            if (int.TryParse(port, out pint))
                            {
                                var portobj = SimObject.GraphicObject.InputConnectors[pint - 1];
                                if (portobj.IsAttached)
                                {
                                    var stream = portobj.AttachedConnector.AttachedFrom;
                                    var cbcell = (DataGridViewComboBoxCell)gridInputMaps.Rows[e.RowIndex].Cells[2];
                                    cbcell.Items.Clear();
                                    cbcell.Items.Add("");
                                    var props = SimObject.FlowSheet.SimulationObjects[stream.Name].GetProperties(Interfaces.Enums.PropertyType.ALL);
                                    foreach (var prop in props)
                                    {
                                        if (!propsdict.ContainsKey(SimObject.FlowSheet.GetTranslatedString(prop)))
                                            propsdict.Add(SimObject.FlowSheet.GetTranslatedString(prop), prop);
                                        cbcell.Items.Add(SimObject.FlowSheet.GetTranslatedString(prop));
                                    }
                                    cbcell.Value = "";
                                }
                            }
                            else if (port == "S")
                            {
                                var cbcell = (DataGridViewComboBoxCell)gridInputMaps.Rows[gridInputMaps.Rows.Count - 1].Cells[2];
                                cbcell.Items.Clear();
                                cbcell.Items.Add("");
                                cbcell.Items.AddRange(sheets);
                                cbcell.Value = "";
                            }
                            else if (port == "Self")
                            {
                                var cbcell = (DataGridViewComboBoxCell)gridInputMaps.Rows[gridInputMaps.Rows.Count - 1].Cells[2];
                                cbcell.Items.Clear();
                                cbcell.Items.Add("");
                                var props = SimObject.GetProperties(Interfaces.Enums.PropertyType.ALL);
                                foreach (var prop in props)
                                {
                                    if (!propsdict.ContainsKey(SimObject.FlowSheet.GetTranslatedString(prop)))
                                        propsdict.Add(SimObject.FlowSheet.GetTranslatedString(prop), prop);
                                    cbcell.Items.Add(SimObject.FlowSheet.GetTranslatedString(prop));
                                }
                                cbcell.Value = "";
                            }
                        }
                        break;
                    default:
                        var value = gridInputMaps.Rows[e.RowIndex].Cells[2].Value.ToString();
                        var units = gridInputMaps.Rows[e.RowIndex].Cells[3].Value.ToString();
                        SimObject.InputMaps[e.RowIndex] = new Tuple<string, string, string, string, string>(port, propsdict[value], units, "", "");
                        UpdateInfo();
                        break;
                }
            }
        }

        private void gridOutputMaps_CellValueChanged(object sender, DataGridViewCellEventArgs e)
        {
            if (Loaded)
            {
                var port = gridOutputMaps.Rows[e.RowIndex].Cells[1].Value.ToString();
                switch (e.ColumnIndex)
                {
                    case 1:
                        if (port != "")
                        {
                            var pint = 0;
                            if (int.TryParse(port, out pint))
                            {
                                var portobj = SimObject.GraphicObject.OutputConnectors[pint - 1];
                                if (portobj.IsAttached)
                                {
                                    var stream = portobj.AttachedConnector.AttachedTo;
                                    var cbcell = (DataGridViewComboBoxCell)gridOutputMaps.Rows[e.RowIndex].Cells[2];
                                    cbcell.Items.Clear();
                                    cbcell.Items.Add("");
                                    var props = SimObject.FlowSheet.SimulationObjects[stream.Name].GetProperties(Interfaces.Enums.PropertyType.WR);
                                    foreach (var prop in props)
                                    {
                                        if (!propsdict.ContainsKey(SimObject.FlowSheet.GetTranslatedString(prop)))
                                            propsdict.Add(SimObject.FlowSheet.GetTranslatedString(prop), prop);
                                        cbcell.Items.Add(SimObject.FlowSheet.GetTranslatedString(prop));
                                    }
                                    //cbcell.Items.AddRange(sheets);
                                    cbcell.Value = "";
                                }
                            }
                            else if (port == "S")
                            {
                                var cbcell = (DataGridViewComboBoxCell)gridOutputMaps.Rows[gridOutputMaps.Rows.Count - 1].Cells[2];
                                cbcell.Items.Clear();
                                cbcell.Items.Add("");
                                cbcell.Items.AddRange(sheets);
                                cbcell.Value = "";
                            }
                            else if (port == "Self")
                            {
                                var cbcell = (DataGridViewComboBoxCell)gridOutputMaps.Rows[gridOutputMaps.Rows.Count - 1].Cells[2];
                                cbcell.Items.Clear();
                                cbcell.Items.Add("");
                                var props = SimObject.GetProperties(Interfaces.Enums.PropertyType.WR);
                                foreach (var prop in props)
                                {
                                    if (!propsdict.ContainsKey(SimObject.FlowSheet.GetTranslatedString(prop)))
                                        propsdict.Add(SimObject.FlowSheet.GetTranslatedString(prop), prop);
                                    cbcell.Items.Add(SimObject.FlowSheet.GetTranslatedString(prop));
                                }
                                cbcell.Value = "";
                            }
                        }
                        break;
                    default:
                        var value = gridOutputMaps.Rows[e.RowIndex].Cells[2].Value.ToString();
                        var units = gridOutputMaps.Rows[e.RowIndex].Cells[3].Value.ToString();
                        var index = ((DataGridViewComboBoxCell)gridOutputMaps.Rows[e.RowIndex].Cells[2]).Items.IndexOf(value);
                        SimObject.OutputMaps[e.RowIndex] = new Tuple<string, string, string, string, string>(port, propsdict[value], units, "", "");
                        UpdateInfo();
                        break;
                }

            }
        }

        private void gridInputMaps_DataError(object sender, DataGridViewDataErrorEventArgs e)
        {
        }

        private void gridOutputMaps_DataError(object sender, DataGridViewDataErrorEventArgs e)
        {
        }

        private void radioButton1_CheckedChanged(object sender, EventArgs e)
        {
            if (Loaded)
            {
                if (radioButton1.Checked)
                {
                    SimObject.Model.ModelSource = Classes.ANNModel.ModelSourceType.FileSystem;
                    tbModelPath.Enabled = true;
                    btnSearchModel.Enabled = true;
                }
                else
                {
                    SimObject.Model.ModelSource = Classes.ANNModel.ModelSourceType.Embedded;
                    tbModelPath.Enabled = false;
                    btnSearchModel.Enabled = false;
                }
            }
        }

        private void txtScript_TextChanged(object sender, EventArgs e)
        {
            SimObject.DataTransferScript = txtScript.Text;
        }

        private void btnSearchModel_Click(object sender, EventArgs e)
        {
            if (openFileDialog1.ShowDialog() == DialogResult.OK)
            {
                tbModelPath.Text = openFileDialog1.FileName;
                SimObject.Model.ModelPath = openFileDialog1.FileName;
                SimObject.Model.ModelSource = Classes.ANNModel.ModelSourceType.FileSystem;
            }
        }

    }
}
