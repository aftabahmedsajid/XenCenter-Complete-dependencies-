namespace XenAdmin.Wizards.BugToolWizardFiles
{
    partial class BugToolPageDestination
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

        #region Component Designer generated code

        /// <summary> 
        /// Required method for Designer support - do not modify 
        /// the contents of this method with the code editor.
        /// </summary>
        private void InitializeComponent()
        {
            System.ComponentModel.ComponentResourceManager resources = new System.ComponentModel.ComponentResourceManager(typeof(BugToolPageDestination));
            this.labelFileLocation = new System.Windows.Forms.Label();
            this.BrowseButton = new System.Windows.Forms.Button();
            this.label2 = new System.Windows.Forms.Label();
            this.tableLayoutPanel1 = new System.Windows.Forms.TableLayoutPanel();
            this.labelName = new System.Windows.Forms.Label();
            this.m_textBoxName = new System.Windows.Forms.TextBox();
            this.m_textBoxLocation = new System.Windows.Forms.TextBox();
            this.m_ctrlError = new XenAdmin.Controls.Common.PasswordFailure();
            this.tableLayoutPanel1.SuspendLayout();
            this.SuspendLayout();
            // 
            // labelFileLocation
            // 
            resources.ApplyResources(this.labelFileLocation, "labelFileLocation");
            this.labelFileLocation.Name = "labelFileLocation";
            // 
            // BrowseButton
            // 
            resources.ApplyResources(this.BrowseButton, "BrowseButton");
            this.BrowseButton.Name = "BrowseButton";
            this.BrowseButton.UseVisualStyleBackColor = true;
            this.BrowseButton.Click += new System.EventHandler(this.BrowseButton_Click);
            // 
            // label2
            // 
            resources.ApplyResources(this.label2, "label2");
            this.tableLayoutPanel1.SetColumnSpan(this.label2, 3);
            this.label2.Name = "label2";
            // 
            // tableLayoutPanel1
            // 
            resources.ApplyResources(this.tableLayoutPanel1, "tableLayoutPanel1");
            this.tableLayoutPanel1.Controls.Add(this.label2, 0, 0);
            this.tableLayoutPanel1.Controls.Add(this.labelName, 0, 1);
            this.tableLayoutPanel1.Controls.Add(this.m_textBoxName, 1, 1);
            this.tableLayoutPanel1.Controls.Add(this.labelFileLocation, 0, 2);
            this.tableLayoutPanel1.Controls.Add(this.m_textBoxLocation, 1, 2);
            this.tableLayoutPanel1.Controls.Add(this.BrowseButton, 2, 2);
            this.tableLayoutPanel1.Controls.Add(this.m_ctrlError, 1, 3);
            this.tableLayoutPanel1.Name = "tableLayoutPanel1";
            // 
            // labelName
            // 
            resources.ApplyResources(this.labelName, "labelName");
            this.labelName.Name = "labelName";
            // 
            // m_textBoxName
            // 
            resources.ApplyResources(this.m_textBoxName, "m_textBoxName");
            this.m_textBoxName.Name = "m_textBoxName";
            this.m_textBoxName.TextChanged += new System.EventHandler(this.m_textBoxName_TextChanged);
            // 
            // m_textBoxLocation
            // 
            resources.ApplyResources(this.m_textBoxLocation, "m_textBoxLocation");
            this.m_textBoxLocation.Name = "m_textBoxLocation";
            this.m_textBoxLocation.TextChanged += new System.EventHandler(this.m_textBoxLocation_TextChanged);
            // 
            // m_ctrlError
            // 
            resources.ApplyResources(this.m_ctrlError, "m_ctrlError");
            this.m_ctrlError.Name = "m_ctrlError";
            // 
            // BugToolPageDestination
            // 
            this.Controls.Add(this.tableLayoutPanel1);
            this.DoubleBuffered = true;
            this.Name = "BugToolPageDestination";
            resources.ApplyResources(this, "$this");
            this.tableLayoutPanel1.ResumeLayout(false);
            this.tableLayoutPanel1.PerformLayout();
            this.ResumeLayout(false);

        }

        #endregion

        private System.Windows.Forms.Label labelFileLocation;
        private System.Windows.Forms.Button BrowseButton;
        private System.Windows.Forms.Label label2;
        private System.Windows.Forms.TableLayoutPanel tableLayoutPanel1;
        private System.Windows.Forms.Label labelName;
        private System.Windows.Forms.TextBox m_textBoxName;
        private System.Windows.Forms.TextBox m_textBoxLocation;
        private XenAdmin.Controls.Common.PasswordFailure m_ctrlError;
    }
}
