namespace Segmentator
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
            this.currentImage = new System.Windows.Forms.PictureBox();
            this.resultImage = new System.Windows.Forms.PictureBox();
            this.frontSizeLabel = new System.Windows.Forms.Label();
            this.currentEnergyLabel = new System.Windows.Forms.Label();
            this.processingSpeedLabel = new System.Windows.Forms.Label();
            this.consoleContents = new System.Windows.Forms.TextBox();
            ((System.ComponentModel.ISupportInitialize)(this.currentImage)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this.resultImage)).BeginInit();
            this.SuspendLayout();
            // 
            // currentImage
            // 
            this.currentImage.Location = new System.Drawing.Point(6, 9);
            this.currentImage.Name = "currentImage";
            this.currentImage.Size = new System.Drawing.Size(363, 302);
            this.currentImage.SizeMode = System.Windows.Forms.PictureBoxSizeMode.Zoom;
            this.currentImage.TabIndex = 0;
            this.currentImage.TabStop = false;
            // 
            // resultImage
            // 
            this.resultImage.Location = new System.Drawing.Point(378, 8);
            this.resultImage.Name = "resultImage";
            this.resultImage.Size = new System.Drawing.Size(363, 302);
            this.resultImage.SizeMode = System.Windows.Forms.PictureBoxSizeMode.Zoom;
            this.resultImage.TabIndex = 1;
            this.resultImage.TabStop = false;
            // 
            // frontSizeLabel
            // 
            this.frontSizeLabel.AutoSize = true;
            this.frontSizeLabel.Location = new System.Drawing.Point(3, 326);
            this.frontSizeLabel.Name = "frontSizeLabel";
            this.frontSizeLabel.Size = new System.Drawing.Size(0, 13);
            this.frontSizeLabel.TabIndex = 2;
            // 
            // currentEnergyLabel
            // 
            this.currentEnergyLabel.AutoSize = true;
            this.currentEnergyLabel.Location = new System.Drawing.Point(3, 352);
            this.currentEnergyLabel.Name = "currentEnergyLabel";
            this.currentEnergyLabel.Size = new System.Drawing.Size(0, 13);
            this.currentEnergyLabel.TabIndex = 3;
            // 
            // processingSpeedLabel
            // 
            this.processingSpeedLabel.AutoSize = true;
            this.processingSpeedLabel.Location = new System.Drawing.Point(3, 378);
            this.processingSpeedLabel.Name = "processingSpeedLabel";
            this.processingSpeedLabel.Size = new System.Drawing.Size(0, 13);
            this.processingSpeedLabel.TabIndex = 4;
            // 
            // consoleContents
            // 
            this.consoleContents.Location = new System.Drawing.Point(748, 10);
            this.consoleContents.Multiline = true;
            this.consoleContents.Name = "consoleContents";
            this.consoleContents.ReadOnly = true;
            this.consoleContents.ScrollBars = System.Windows.Forms.ScrollBars.Vertical;
            this.consoleContents.Size = new System.Drawing.Size(383, 380);
            this.consoleContents.TabIndex = 5;
            // 
            // MainForm
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(1141, 402);
            this.Controls.Add(this.consoleContents);
            this.Controls.Add(this.processingSpeedLabel);
            this.Controls.Add(this.currentEnergyLabel);
            this.Controls.Add(this.frontSizeLabel);
            this.Controls.Add(this.resultImage);
            this.Controls.Add(this.currentImage);
            this.Name = "MainForm";
            this.Text = "Segmentation process";
            ((System.ComponentModel.ISupportInitialize)(this.currentImage)).EndInit();
            ((System.ComponentModel.ISupportInitialize)(this.resultImage)).EndInit();
            this.ResumeLayout(false);
            this.PerformLayout();

        }

        #endregion

        private System.Windows.Forms.PictureBox currentImage;
        private System.Windows.Forms.PictureBox resultImage;
        private System.Windows.Forms.Label frontSizeLabel;
        private System.Windows.Forms.Label currentEnergyLabel;
        private System.Windows.Forms.Label processingSpeedLabel;
        private System.Windows.Forms.TextBox consoleContents;
    }
}

