namespace WzVisualizer
{
    partial class PropertiesViewer
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
            System.ComponentModel.ComponentResourceManager resources = new System.ComponentModel.ComponentResourceManager(typeof(PropertiesViewer));
            this.PropertiesBox = new System.Windows.Forms.TextBox();
            this.SuspendLayout();
            // 
            // PropertiesBox
            // 
            this.PropertiesBox.BackColor = System.Drawing.SystemColors.Control;
            this.PropertiesBox.BorderStyle = System.Windows.Forms.BorderStyle.None;
            resources.ApplyResources(this.PropertiesBox, "PropertiesBox");
            this.PropertiesBox.Name = "PropertiesBox";
            this.PropertiesBox.ReadOnly = true;
            // 
            // PropertiesViewer
            // 
            resources.ApplyResources(this, "$this");
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.Controls.Add(this.PropertiesBox);
            this.Name = "PropertiesViewer";
            this.FormClosing += new System.Windows.Forms.FormClosingEventHandler(this.PropertiesViewer_FormClosing);
            this.ResumeLayout(false);
            this.PerformLayout();

        }

        #endregion

        internal System.Windows.Forms.TextBox PropertiesBox;
    }
}