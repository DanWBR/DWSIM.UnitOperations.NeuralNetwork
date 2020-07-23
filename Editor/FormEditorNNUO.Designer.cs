namespace DWSIM.UnitOperations.NeuralNetwork.Editors
{
    partial class FormEditorNNUO
    {
        /// <summary>
        /// Required designer variable.
        /// </summary>
        private System.ComponentModel.IContainer components = null;

        /// <summary>
        /// Clean up any resources being used.
        /// </summary>
        /// <param name="disposing">true if managed resources should be disposed; otherwise, false.</param>
        protected override void Dispose(bool disposing)
        {
            if (disposing && (components != null))
            {
                components.Dispose();
            }
            base.Dispose(disposing);
        }

        #region Windows Form Designer generated code

        /// <summary>
        /// Required method for Designer support - do not modify
        /// the contents of this method with the code editor.
        /// </summary>
        private void InitializeComponent()
        {
            this.components = new System.ComponentModel.Container();
            this.GroupBox5 = new System.Windows.Forms.GroupBox();
            this.lblTag = new System.Windows.Forms.TextBox();
            this.lblConnectedTo = new System.Windows.Forms.Label();
            this.lblStatus = new System.Windows.Forms.Label();
            this.Label13 = new System.Windows.Forms.Label();
            this.Label12 = new System.Windows.Forms.Label();
            this.Label11 = new System.Windows.Forms.Label();
            this.button3 = new System.Windows.Forms.Button();
            this.button2 = new System.Windows.Forms.Button();
            this.ToolTipChangeTag = new System.Windows.Forms.ToolTip(this.components);
            this.tabControl1 = new System.Windows.Forms.TabControl();
            this.tabPage1 = new System.Windows.Forms.TabPage();
            this.TabControl2 = new System.Windows.Forms.TabControl();
            this.TabPage4 = new System.Windows.Forms.TabPage();
            this.gridFeeds = new System.Windows.Forms.DataGridView();
            this.Column2 = new System.Windows.Forms.DataGridViewTextBoxColumn();
            this.c1 = new System.Windows.Forms.DataGridViewTextBoxColumn();
            this.c2 = new System.Windows.Forms.DataGridViewComboBoxColumn();
            this.TabPage5 = new System.Windows.Forms.TabPage();
            this.gridProducts = new System.Windows.Forms.DataGridView();
            this.Column3 = new System.Windows.Forms.DataGridViewTextBoxColumn();
            this.DataGridViewTextBoxColumn1 = new System.Windows.Forms.DataGridViewTextBoxColumn();
            this.DataGridViewComboBoxColumn1 = new System.Windows.Forms.DataGridViewComboBoxColumn();
            this.tabPage2 = new System.Windows.Forms.TabPage();
            this.tabPage3 = new System.Windows.Forms.TabPage();
            this.rtbAnnotations = new Extended.Windows.Forms.RichTextBoxExtended();
            this.button1 = new System.Windows.Forms.Button();
            this.GroupBox5.SuspendLayout();
            this.tabControl1.SuspendLayout();
            this.tabPage1.SuspendLayout();
            this.TabControl2.SuspendLayout();
            this.TabPage4.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)(this.gridFeeds)).BeginInit();
            this.TabPage5.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)(this.gridProducts)).BeginInit();
            this.tabPage3.SuspendLayout();
            this.SuspendLayout();
            // 
            // GroupBox5
            // 
            this.GroupBox5.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
            this.GroupBox5.Controls.Add(this.lblTag);
            this.GroupBox5.Controls.Add(this.lblConnectedTo);
            this.GroupBox5.Controls.Add(this.lblStatus);
            this.GroupBox5.Controls.Add(this.Label13);
            this.GroupBox5.Controls.Add(this.Label12);
            this.GroupBox5.Controls.Add(this.Label11);
            this.GroupBox5.Location = new System.Drawing.Point(10, 7);
            this.GroupBox5.Name = "GroupBox5";
            this.GroupBox5.Size = new System.Drawing.Size(386, 98);
            this.GroupBox5.TabIndex = 10;
            this.GroupBox5.TabStop = false;
            this.GroupBox5.Text = "General Info";
            // 
            // lblTag
            // 
            this.lblTag.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
            this.lblTag.Location = new System.Drawing.Point(133, 19);
            this.lblTag.Name = "lblTag";
            this.lblTag.Size = new System.Drawing.Size(247, 20);
            this.lblTag.TabIndex = 24;
            this.lblTag.TextChanged += new System.EventHandler(this.lblTag_TextChanged);
            this.lblTag.KeyUp += new System.Windows.Forms.KeyEventHandler(this.lblTag_KeyUp);
            // 
            // lblConnectedTo
            // 
            this.lblConnectedTo.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
            this.lblConnectedTo.AutoSize = true;
            this.lblConnectedTo.ImeMode = System.Windows.Forms.ImeMode.NoControl;
            this.lblConnectedTo.Location = new System.Drawing.Point(132, 72);
            this.lblConnectedTo.Name = "lblConnectedTo";
            this.lblConnectedTo.Size = new System.Drawing.Size(38, 13);
            this.lblConnectedTo.TabIndex = 20;
            this.lblConnectedTo.Text = "Objeto";
            // 
            // lblStatus
            // 
            this.lblStatus.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
            this.lblStatus.AutoSize = true;
            this.lblStatus.ImeMode = System.Windows.Forms.ImeMode.NoControl;
            this.lblStatus.Location = new System.Drawing.Point(132, 47);
            this.lblStatus.Name = "lblStatus";
            this.lblStatus.Size = new System.Drawing.Size(38, 13);
            this.lblStatus.TabIndex = 19;
            this.lblStatus.Text = "Objeto";
            // 
            // Label13
            // 
            this.Label13.AutoSize = true;
            this.Label13.ImeMode = System.Windows.Forms.ImeMode.NoControl;
            this.Label13.Location = new System.Drawing.Point(9, 72);
            this.Label13.Name = "Label13";
            this.Label13.Size = new System.Drawing.Size(51, 13);
            this.Label13.TabIndex = 17;
            this.Label13.Text = "Linked to";
            // 
            // Label12
            // 
            this.Label12.AutoSize = true;
            this.Label12.ImeMode = System.Windows.Forms.ImeMode.NoControl;
            this.Label12.Location = new System.Drawing.Point(9, 47);
            this.Label12.Name = "Label12";
            this.Label12.Size = new System.Drawing.Size(37, 13);
            this.Label12.TabIndex = 16;
            this.Label12.Text = "Status";
            // 
            // Label11
            // 
            this.Label11.AutoSize = true;
            this.Label11.ImeMode = System.Windows.Forms.ImeMode.NoControl;
            this.Label11.Location = new System.Drawing.Point(9, 22);
            this.Label11.Name = "Label11";
            this.Label11.Size = new System.Drawing.Size(38, 13);
            this.Label11.TabIndex = 14;
            this.Label11.Text = "Object";
            // 
            // button3
            // 
            this.button3.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
            this.button3.Location = new System.Drawing.Point(10, 487);
            this.button3.Name = "button3";
            this.button3.Size = new System.Drawing.Size(386, 23);
            this.button3.TabIndex = 15;
            this.button3.Text = "About";
            this.button3.UseVisualStyleBackColor = true;
            this.button3.Click += new System.EventHandler(this.button3_Click);
            // 
            // button2
            // 
            this.button2.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
            this.button2.Location = new System.Drawing.Point(10, 457);
            this.button2.Name = "button2";
            this.button2.Size = new System.Drawing.Size(386, 23);
            this.button2.TabIndex = 14;
            this.button2.Text = "View Help";
            this.button2.UseVisualStyleBackColor = true;
            this.button2.Click += new System.EventHandler(this.button2_Click);
            // 
            // ToolTipChangeTag
            // 
            this.ToolTipChangeTag.ToolTipIcon = System.Windows.Forms.ToolTipIcon.Info;
            this.ToolTipChangeTag.ToolTipTitle = "Info";
            // 
            // tabControl1
            // 
            this.tabControl1.Anchor = ((System.Windows.Forms.AnchorStyles)((((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Bottom) 
            | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
            this.tabControl1.Controls.Add(this.tabPage1);
            this.tabControl1.Controls.Add(this.tabPage2);
            this.tabControl1.Controls.Add(this.tabPage3);
            this.tabControl1.Location = new System.Drawing.Point(10, 143);
            this.tabControl1.Name = "tabControl1";
            this.tabControl1.SelectedIndex = 0;
            this.tabControl1.Size = new System.Drawing.Size(386, 308);
            this.tabControl1.TabIndex = 16;
            // 
            // tabPage1
            // 
            this.tabPage1.Controls.Add(this.TabControl2);
            this.tabPage1.Location = new System.Drawing.Point(4, 22);
            this.tabPage1.Name = "tabPage1";
            this.tabPage1.Padding = new System.Windows.Forms.Padding(3);
            this.tabPage1.Size = new System.Drawing.Size(378, 282);
            this.tabPage1.TabIndex = 0;
            this.tabPage1.Text = "Connections";
            this.tabPage1.UseVisualStyleBackColor = true;
            // 
            // TabControl2
            // 
            this.TabControl2.Controls.Add(this.TabPage4);
            this.TabControl2.Controls.Add(this.TabPage5);
            this.TabControl2.Dock = System.Windows.Forms.DockStyle.Fill;
            this.TabControl2.Location = new System.Drawing.Point(3, 3);
            this.TabControl2.Name = "TabControl2";
            this.TabControl2.SelectedIndex = 0;
            this.TabControl2.Size = new System.Drawing.Size(372, 276);
            this.TabControl2.TabIndex = 1;
            // 
            // TabPage4
            // 
            this.TabPage4.Controls.Add(this.gridFeeds);
            this.TabPage4.Location = new System.Drawing.Point(4, 22);
            this.TabPage4.Name = "TabPage4";
            this.TabPage4.Padding = new System.Windows.Forms.Padding(3);
            this.TabPage4.Size = new System.Drawing.Size(364, 250);
            this.TabPage4.TabIndex = 0;
            this.TabPage4.Text = "Inlet Ports";
            this.TabPage4.UseVisualStyleBackColor = true;
            // 
            // gridFeeds
            // 
            this.gridFeeds.AllowUserToAddRows = false;
            this.gridFeeds.AllowUserToDeleteRows = false;
            this.gridFeeds.AutoSizeColumnsMode = System.Windows.Forms.DataGridViewAutoSizeColumnsMode.Fill;
            this.gridFeeds.ColumnHeadersHeightSizeMode = System.Windows.Forms.DataGridViewColumnHeadersHeightSizeMode.AutoSize;
            this.gridFeeds.Columns.AddRange(new System.Windows.Forms.DataGridViewColumn[] {
            this.Column2,
            this.c1,
            this.c2});
            this.gridFeeds.Dock = System.Windows.Forms.DockStyle.Fill;
            this.gridFeeds.Location = new System.Drawing.Point(3, 3);
            this.gridFeeds.Name = "gridFeeds";
            this.gridFeeds.RowHeadersVisible = false;
            this.gridFeeds.Size = new System.Drawing.Size(358, 244);
            this.gridFeeds.TabIndex = 0;
            this.gridFeeds.CellValueChanged += new System.Windows.Forms.DataGridViewCellEventHandler(this.gridFeeds_CellValueChanged);
            // 
            // Column2
            // 
            this.Column2.HeaderText = "id";
            this.Column2.Name = "Column2";
            this.Column2.Visible = false;
            // 
            // c1
            // 
            this.c1.HeaderText = "Name";
            this.c1.Name = "c1";
            this.c1.ReadOnly = true;
            // 
            // c2
            // 
            this.c2.HeaderText = "Material Stream";
            this.c2.Name = "c2";
            // 
            // TabPage5
            // 
            this.TabPage5.Controls.Add(this.gridProducts);
            this.TabPage5.Location = new System.Drawing.Point(4, 22);
            this.TabPage5.Name = "TabPage5";
            this.TabPage5.Padding = new System.Windows.Forms.Padding(3);
            this.TabPage5.Size = new System.Drawing.Size(364, 250);
            this.TabPage5.TabIndex = 1;
            this.TabPage5.Text = "Outlet Ports";
            this.TabPage5.UseVisualStyleBackColor = true;
            // 
            // gridProducts
            // 
            this.gridProducts.AllowUserToAddRows = false;
            this.gridProducts.AllowUserToDeleteRows = false;
            this.gridProducts.AutoSizeColumnsMode = System.Windows.Forms.DataGridViewAutoSizeColumnsMode.Fill;
            this.gridProducts.ColumnHeadersHeightSizeMode = System.Windows.Forms.DataGridViewColumnHeadersHeightSizeMode.AutoSize;
            this.gridProducts.Columns.AddRange(new System.Windows.Forms.DataGridViewColumn[] {
            this.Column3,
            this.DataGridViewTextBoxColumn1,
            this.DataGridViewComboBoxColumn1});
            this.gridProducts.Dock = System.Windows.Forms.DockStyle.Fill;
            this.gridProducts.Location = new System.Drawing.Point(3, 3);
            this.gridProducts.Name = "gridProducts";
            this.gridProducts.RowHeadersVisible = false;
            this.gridProducts.Size = new System.Drawing.Size(358, 244);
            this.gridProducts.TabIndex = 1;
            this.gridProducts.CellValueChanged += new System.Windows.Forms.DataGridViewCellEventHandler(this.gridProducts_CellValueChanged);
            // 
            // Column3
            // 
            this.Column3.HeaderText = "id";
            this.Column3.Name = "Column3";
            this.Column3.Visible = false;
            // 
            // DataGridViewTextBoxColumn1
            // 
            this.DataGridViewTextBoxColumn1.HeaderText = "Name";
            this.DataGridViewTextBoxColumn1.Name = "DataGridViewTextBoxColumn1";
            this.DataGridViewTextBoxColumn1.ReadOnly = true;
            // 
            // DataGridViewComboBoxColumn1
            // 
            this.DataGridViewComboBoxColumn1.HeaderText = "Material Stream";
            this.DataGridViewComboBoxColumn1.Name = "DataGridViewComboBoxColumn1";
            // 
            // tabPage2
            // 
            this.tabPage2.Location = new System.Drawing.Point(4, 22);
            this.tabPage2.Name = "tabPage2";
            this.tabPage2.Padding = new System.Windows.Forms.Padding(3);
            this.tabPage2.Size = new System.Drawing.Size(378, 282);
            this.tabPage2.TabIndex = 1;
            this.tabPage2.Text = "Model Configuration";
            this.tabPage2.UseVisualStyleBackColor = true;
            // 
            // tabPage3
            // 
            this.tabPage3.Controls.Add(this.rtbAnnotations);
            this.tabPage3.Location = new System.Drawing.Point(4, 22);
            this.tabPage3.Name = "tabPage3";
            this.tabPage3.Size = new System.Drawing.Size(378, 282);
            this.tabPage3.TabIndex = 2;
            this.tabPage3.Text = "Annotations";
            // 
            // rtbAnnotations
            // 
            this.rtbAnnotations.Dock = System.Windows.Forms.DockStyle.Fill;
            this.rtbAnnotations.Location = new System.Drawing.Point(0, 0);
            this.rtbAnnotations.Name = "rtbAnnotations";
            this.rtbAnnotations.Rtf = "{\\rtf1\\ansi\\ansicpg1252\\deff0\\nouicompat\\deflang1046{\\fonttbl{\\f0\\fnil Microsoft " +
    "Sans Serif;}}\r\n{\\*\\generator Riched20 10.0.18362}\\viewkind4\\uc1 \r\n\\pard\\f0\\fs17\\" +
    "par\r\n}\r\n";
            this.rtbAnnotations.Size = new System.Drawing.Size(378, 282);
            this.rtbAnnotations.TabIndex = 1;
            this.rtbAnnotations.ToolbarVisible = false;
            // 
            // button1
            // 
            this.button1.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
            this.button1.Location = new System.Drawing.Point(10, 111);
            this.button1.Name = "button1";
            this.button1.Size = new System.Drawing.Size(386, 23);
            this.button1.TabIndex = 17;
            this.button1.Text = "Open Model Configuration Wizard";
            this.button1.UseVisualStyleBackColor = true;
            this.button1.Click += new System.EventHandler(this.button1_Click);
            // 
            // FormEditorNNUO
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(405, 518);
            this.Controls.Add(this.button1);
            this.Controls.Add(this.tabControl1);
            this.Controls.Add(this.button3);
            this.Controls.Add(this.button2);
            this.Controls.Add(this.GroupBox5);
            this.Font = new System.Drawing.Font("Microsoft Sans Serif", 8.25F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.Name = "FormEditorNNUO";
            this.Text = "FormEditorNNUO";
            this.Load += new System.EventHandler(this.FormEditorNNUO_Load);
            this.GroupBox5.ResumeLayout(false);
            this.GroupBox5.PerformLayout();
            this.tabControl1.ResumeLayout(false);
            this.tabPage1.ResumeLayout(false);
            this.TabControl2.ResumeLayout(false);
            this.TabPage4.ResumeLayout(false);
            ((System.ComponentModel.ISupportInitialize)(this.gridFeeds)).EndInit();
            this.TabPage5.ResumeLayout(false);
            ((System.ComponentModel.ISupportInitialize)(this.gridProducts)).EndInit();
            this.tabPage3.ResumeLayout(false);
            this.ResumeLayout(false);

        }

        #endregion

        public System.Windows.Forms.GroupBox GroupBox5;
        public System.Windows.Forms.TextBox lblTag;
        public System.Windows.Forms.Label lblConnectedTo;
        public System.Windows.Forms.Label lblStatus;
        public System.Windows.Forms.Label Label13;
        public System.Windows.Forms.Label Label12;
        public System.Windows.Forms.Label Label11;
        private System.Windows.Forms.Button button3;
        private System.Windows.Forms.Button button2;
        internal System.Windows.Forms.ToolTip ToolTipChangeTag;
        private System.Windows.Forms.TabControl tabControl1;
        private System.Windows.Forms.TabPage tabPage1;
        internal System.Windows.Forms.TabControl TabControl2;
        internal System.Windows.Forms.TabPage TabPage4;
        internal System.Windows.Forms.DataGridView gridFeeds;
        internal System.Windows.Forms.DataGridViewTextBoxColumn Column2;
        internal System.Windows.Forms.DataGridViewTextBoxColumn c1;
        internal System.Windows.Forms.DataGridViewComboBoxColumn c2;
        internal System.Windows.Forms.TabPage TabPage5;
        internal System.Windows.Forms.DataGridView gridProducts;
        internal System.Windows.Forms.DataGridViewTextBoxColumn Column3;
        internal System.Windows.Forms.DataGridViewTextBoxColumn DataGridViewTextBoxColumn1;
        internal System.Windows.Forms.DataGridViewComboBoxColumn DataGridViewComboBoxColumn1;
        private System.Windows.Forms.TabPage tabPage2;
        private System.Windows.Forms.TabPage tabPage3;
        public Extended.Windows.Forms.RichTextBoxExtended rtbAnnotations;
        private System.Windows.Forms.Button button1;
    }
}