namespace Research.GraphBasedShapePrior.Tools.SegmentPenaltyPrototype
{
    partial class MainForm
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
            this.solutionPictureBox = new System.Windows.Forms.PictureBox();
            this.problemPropertiesGrid = new System.Windows.Forms.PropertyGrid();
            this.calculateButton = new System.Windows.Forms.Button();
            ((System.ComponentModel.ISupportInitialize)(this.solutionPictureBox)).BeginInit();
            this.SuspendLayout();
            // 
            // solutionPictureBox
            // 
            this.solutionPictureBox.Anchor = ((System.Windows.Forms.AnchorStyles)((((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Bottom) 
            | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
            this.solutionPictureBox.Location = new System.Drawing.Point(246, 12);
            this.solutionPictureBox.Name = "solutionPictureBox";
            this.solutionPictureBox.Size = new System.Drawing.Size(643, 613);
            this.solutionPictureBox.TabIndex = 0;
            this.solutionPictureBox.TabStop = false;
            // 
            // problemPropertiesGrid
            // 
            this.problemPropertiesGrid.Location = new System.Drawing.Point(12, 12);
            this.problemPropertiesGrid.Name = "problemPropertiesGrid";
            this.problemPropertiesGrid.Size = new System.Drawing.Size(228, 572);
            this.problemPropertiesGrid.TabIndex = 1;
            // 
            // calculateButton
            // 
            this.calculateButton.Location = new System.Drawing.Point(12, 590);
            this.calculateButton.Name = "calculateButton";
            this.calculateButton.Size = new System.Drawing.Size(228, 35);
            this.calculateButton.TabIndex = 2;
            this.calculateButton.Text = "Calculate";
            this.calculateButton.UseVisualStyleBackColor = true;
            this.calculateButton.Click += new System.EventHandler(this.OnCalculateButtonClick);
            // 
            // MainForm
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(893, 629);
            this.Controls.Add(this.calculateButton);
            this.Controls.Add(this.problemPropertiesGrid);
            this.Controls.Add(this.solutionPictureBox);
            this.Name = "MainForm";
            this.Text = "Prototype";
            ((System.ComponentModel.ISupportInitialize)(this.solutionPictureBox)).EndInit();
            this.ResumeLayout(false);

        }

        #endregion

        private System.Windows.Forms.PictureBox solutionPictureBox;
        private System.Windows.Forms.PropertyGrid problemPropertiesGrid;
        private System.Windows.Forms.Button calculateButton;
    }
}

