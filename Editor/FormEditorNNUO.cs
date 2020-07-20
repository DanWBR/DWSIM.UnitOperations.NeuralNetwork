using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using DWSIM.ExtensionMethods;
using DWSIM.Interfaces;
using DWSIM.Interfaces.Enums.GraphicObjects;

namespace DWSIM.UnitOperations.NeuralNetwork.Editors
{
    public partial class FormEditorNNUO : WeifenLuo.WinFormsUI.Docking.DockContent
    {

        public NeuralNetworkUnitOperation SimObject;

        public bool Loaded = false;

        SharedClasses.SystemsOfUnits.Units units;

        string nf;

        public FormEditorNNUO()
        {
            InitializeComponent();
            ShowHint = (WeifenLuo.WinFormsUI.Docking.DockState)GlobalSettings.Settings.DefaultEditFormLocation;
        }

        private void FormEditorNNUO_Load(object sender, EventArgs e)
        {
            UpdateInfo();
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
    }
}
