using System.ComponentModel;

namespace WzVisualizer.GUI.Controls {
    partial class DataViewport {
        /// <summary> 
        /// Required designer variable.
        /// </summary>
        private IContainer components = null;

        /// <summary> 
        /// Clean up any resources being used.
        /// </summary>
        /// <param name="disposing">true if managed resources should be disposed; otherwise, false.</param>
        protected override void Dispose(bool disposing) {
            if (disposing && (components != null)) {
                components.Dispose();
            }

            base.Dispose(disposing);
        }

        #region Component Designer generated code

        /// <summary>
        /// Required method for Designer support - do not modify
        /// the contents of this method with the code editor.
        /// </summary>
        private void InitializeComponent() {
            System.ComponentModel.ComponentResourceManager resources = new System.ComponentModel.ComponentResourceManager(typeof(DataViewport));
            this.GridView = new System.Windows.Forms.DataGridView();
            this.propID = new System.Windows.Forms.DataGridViewTextBoxColumn();
            this.propBitmap = new System.Windows.Forms.DataGridViewImageColumn();
            this.propName = new System.Windows.Forms.DataGridViewTextBoxColumn();
            this.propProperties = new System.Windows.Forms.DataGridViewTextBoxColumn();
            ((System.ComponentModel.ISupportInitialize)(this.GridView)).BeginInit();
            this.SuspendLayout();
            // 
            // GridView
            // 
            this.GridView.AllowUserToAddRows = false;
            this.GridView.AllowUserToDeleteRows = false;
            this.GridView.AllowUserToOrderColumns = true;
            this.GridView.ColumnHeadersHeightSizeMode = System.Windows.Forms.DataGridViewColumnHeadersHeightSizeMode.AutoSize;
            this.GridView.Columns.AddRange(new System.Windows.Forms.DataGridViewColumn[] {
            this.propID,
            this.propBitmap,
            this.propName,
            this.propProperties});
            resources.ApplyResources(this.GridView, "GridView");
            this.GridView.Name = "GridView";
            this.GridView.ReadOnly = true;
            this.GridView.RowTemplate.Height = 50;
            // 
            // propID
            // 
            this.propID.AutoSizeMode = System.Windows.Forms.DataGridViewAutoSizeColumnMode.AllCells;
            this.propID.DataPropertyName = "IDProperty";
            resources.ApplyResources(this.propID, "propID");
            this.propID.Name = "propID";
            this.propID.ReadOnly = true;
            // 
            // propBitmap
            // 
            this.propBitmap.DataPropertyName = "ImageProperty";
            resources.ApplyResources(this.propBitmap, "propBitmap");
            this.propBitmap.Name = "propBitmap";
            this.propBitmap.ReadOnly = true;
            // 
            // propName
            // 
            this.propName.DataPropertyName = "NameProperty";
            this.propName.FillWeight = 130F;
            resources.ApplyResources(this.propName, "propName");
            this.propName.Name = "propName";
            this.propName.ReadOnly = true;
            // 
            // propProperties
            // 
            this.propProperties.AutoSizeMode = System.Windows.Forms.DataGridViewAutoSizeColumnMode.Fill;
            this.propProperties.DataPropertyName = "PropertiesProperty";
            resources.ApplyResources(this.propProperties, "propProperties");
            this.propProperties.Name = "propProperties";
            this.propProperties.ReadOnly = true;
            // 
            // DataViewport
            // 
            resources.ApplyResources(this, "$this");
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.Controls.Add(this.GridView);
            this.Name = "DataViewport";
            ((System.ComponentModel.ISupportInitialize)(this.GridView)).EndInit();
            this.ResumeLayout(false);

        }

        internal System.Windows.Forms.DataGridView GridView;

        #endregion

        private System.Windows.Forms.DataGridViewTextBoxColumn propID;
        private System.Windows.Forms.DataGridViewImageColumn propBitmap;
        private System.Windows.Forms.DataGridViewTextBoxColumn propName;
        private System.Windows.Forms.DataGridViewTextBoxColumn propProperties;
    }
}