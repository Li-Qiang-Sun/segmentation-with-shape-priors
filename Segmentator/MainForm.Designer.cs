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
            this.segmentationMaskImage = new System.Windows.Forms.PictureBox();
            this.consoleContents = new System.Windows.Forms.TextBox();
            this.startGpuButton = new System.Windows.Forms.Button();
            this.stopButton = new System.Windows.Forms.Button();
            this.startCpuButton = new System.Windows.Forms.Button();
            this.justSegmentButton = new System.Windows.Forms.Button();
            this.switchToDfsButton = new System.Windows.Forms.Button();
            this.segmentationPropertiesGrid = new System.Windows.Forms.PropertyGrid();
            this.shapeTermsImage = new System.Windows.Forms.PictureBox();
            this.unaryTermsImage = new System.Windows.Forms.PictureBox();
            this.pauseContinueButton = new System.Windows.Forms.Button();
            ((System.ComponentModel.ISupportInitialize)(this.currentImage)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this.segmentationMaskImage)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this.shapeTermsImage)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this.unaryTermsImage)).BeginInit();
            this.SuspendLayout();
            // 
            // currentImage
            // 
            this.currentImage.BorderStyle = System.Windows.Forms.BorderStyle.FixedSingle;
            this.currentImage.Location = new System.Drawing.Point(9, 8);
            this.currentImage.Name = "currentImage";
            this.currentImage.Size = new System.Drawing.Size(420, 302);
            this.currentImage.SizeMode = System.Windows.Forms.PictureBoxSizeMode.Zoom;
            this.currentImage.TabIndex = 0;
            this.currentImage.TabStop = false;
            // 
            // segmentationMaskImage
            // 
            this.segmentationMaskImage.BorderStyle = System.Windows.Forms.BorderStyle.FixedSingle;
            this.segmentationMaskImage.Location = new System.Drawing.Point(435, 8);
            this.segmentationMaskImage.Name = "segmentationMaskImage";
            this.segmentationMaskImage.Size = new System.Drawing.Size(420, 302);
            this.segmentationMaskImage.SizeMode = System.Windows.Forms.PictureBoxSizeMode.Zoom;
            this.segmentationMaskImage.TabIndex = 1;
            this.segmentationMaskImage.TabStop = false;
            // 
            // consoleContents
            // 
            this.consoleContents.Location = new System.Drawing.Point(857, 10);
            this.consoleContents.Multiline = true;
            this.consoleContents.Name = "consoleContents";
            this.consoleContents.ReadOnly = true;
            this.consoleContents.ScrollBars = System.Windows.Forms.ScrollBars.Vertical;
            this.consoleContents.Size = new System.Drawing.Size(431, 300);
            this.consoleContents.TabIndex = 5;
            // 
            // startGpuButton
            // 
            this.startGpuButton.Location = new System.Drawing.Point(669, 635);
            this.startGpuButton.Name = "startGpuButton";
            this.startGpuButton.Size = new System.Drawing.Size(119, 31);
            this.startGpuButton.TabIndex = 6;
            this.startGpuButton.Text = "Start on GPU";
            this.startGpuButton.UseVisualStyleBackColor = true;
            this.startGpuButton.Click += new System.EventHandler(this.OnStartGpuButtonClick);
            // 
            // stopButton
            // 
            this.stopButton.Enabled = false;
            this.stopButton.Location = new System.Drawing.Point(1169, 635);
            this.stopButton.Name = "stopButton";
            this.stopButton.Size = new System.Drawing.Size(119, 31);
            this.stopButton.TabIndex = 7;
            this.stopButton.Text = "Stop";
            this.stopButton.UseVisualStyleBackColor = true;
            this.stopButton.Click += new System.EventHandler(this.OnStopButtonClick);
            // 
            // startCpuButton
            // 
            this.startCpuButton.Location = new System.Drawing.Point(794, 635);
            this.startCpuButton.Name = "startCpuButton";
            this.startCpuButton.Size = new System.Drawing.Size(119, 31);
            this.startCpuButton.TabIndex = 8;
            this.startCpuButton.Text = "Start on CPU";
            this.startCpuButton.UseVisualStyleBackColor = true;
            this.startCpuButton.Click += new System.EventHandler(this.OnStartCpuButtonClick);
            // 
            // justSegmentButton
            // 
            this.justSegmentButton.Location = new System.Drawing.Point(544, 635);
            this.justSegmentButton.Name = "justSegmentButton";
            this.justSegmentButton.Size = new System.Drawing.Size(119, 31);
            this.justSegmentButton.TabIndex = 20;
            this.justSegmentButton.Text = "Just segment";
            this.justSegmentButton.UseVisualStyleBackColor = true;
            this.justSegmentButton.Click += new System.EventHandler(this.OnJustSegmentButtonClick);
            // 
            // switchToDfsButton
            // 
            this.switchToDfsButton.Enabled = false;
            this.switchToDfsButton.Location = new System.Drawing.Point(919, 635);
            this.switchToDfsButton.Name = "switchToDfsButton";
            this.switchToDfsButton.Size = new System.Drawing.Size(119, 31);
            this.switchToDfsButton.TabIndex = 21;
            this.switchToDfsButton.Text = "Switch to DFS";
            this.switchToDfsButton.UseVisualStyleBackColor = true;
            this.switchToDfsButton.Click += new System.EventHandler(this.OnSwitchToDfsButtonClick);
            // 
            // segmentationPropertiesGrid
            // 
            this.segmentationPropertiesGrid.Location = new System.Drawing.Point(861, 316);
            this.segmentationPropertiesGrid.Name = "segmentationPropertiesGrid";
            this.segmentationPropertiesGrid.Size = new System.Drawing.Size(427, 302);
            this.segmentationPropertiesGrid.TabIndex = 24;
            this.segmentationPropertiesGrid.ToolbarVisible = false;
            // 
            // shapeTermsImage
            // 
            this.shapeTermsImage.BorderStyle = System.Windows.Forms.BorderStyle.FixedSingle;
            this.shapeTermsImage.Location = new System.Drawing.Point(435, 316);
            this.shapeTermsImage.Name = "shapeTermsImage";
            this.shapeTermsImage.Size = new System.Drawing.Size(420, 302);
            this.shapeTermsImage.SizeMode = System.Windows.Forms.PictureBoxSizeMode.Zoom;
            this.shapeTermsImage.TabIndex = 25;
            this.shapeTermsImage.TabStop = false;
            // 
            // unaryTermsImage
            // 
            this.unaryTermsImage.BorderStyle = System.Windows.Forms.BorderStyle.FixedSingle;
            this.unaryTermsImage.Location = new System.Drawing.Point(9, 316);
            this.unaryTermsImage.Name = "unaryTermsImage";
            this.unaryTermsImage.Size = new System.Drawing.Size(420, 302);
            this.unaryTermsImage.SizeMode = System.Windows.Forms.PictureBoxSizeMode.Zoom;
            this.unaryTermsImage.TabIndex = 26;
            this.unaryTermsImage.TabStop = false;
            // 
            // pauseContinueButton
            // 
            this.pauseContinueButton.Enabled = false;
            this.pauseContinueButton.Location = new System.Drawing.Point(1044, 635);
            this.pauseContinueButton.Name = "pauseContinueButton";
            this.pauseContinueButton.Size = new System.Drawing.Size(119, 31);
            this.pauseContinueButton.TabIndex = 27;
            this.pauseContinueButton.Text = "Pause";
            this.pauseContinueButton.UseVisualStyleBackColor = true;
            this.pauseContinueButton.Click += new System.EventHandler(this.OnPauseContinueButtonClick);
            // 
            // MainForm
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(1300, 678);
            this.Controls.Add(this.pauseContinueButton);
            this.Controls.Add(this.unaryTermsImage);
            this.Controls.Add(this.shapeTermsImage);
            this.Controls.Add(this.segmentationPropertiesGrid);
            this.Controls.Add(this.switchToDfsButton);
            this.Controls.Add(this.justSegmentButton);
            this.Controls.Add(this.startCpuButton);
            this.Controls.Add(this.stopButton);
            this.Controls.Add(this.startGpuButton);
            this.Controls.Add(this.consoleContents);
            this.Controls.Add(this.segmentationMaskImage);
            this.Controls.Add(this.currentImage);
            this.Name = "MainForm";
            this.Text = "Segmentation process";
            this.FormClosing += new System.Windows.Forms.FormClosingEventHandler(this.OnFormClosing);
            ((System.ComponentModel.ISupportInitialize)(this.currentImage)).EndInit();
            ((System.ComponentModel.ISupportInitialize)(this.segmentationMaskImage)).EndInit();
            ((System.ComponentModel.ISupportInitialize)(this.shapeTermsImage)).EndInit();
            ((System.ComponentModel.ISupportInitialize)(this.unaryTermsImage)).EndInit();
            this.ResumeLayout(false);
            this.PerformLayout();

        }

        #endregion

        private System.Windows.Forms.PictureBox currentImage;
        private System.Windows.Forms.PictureBox segmentationMaskImage;
        private System.Windows.Forms.TextBox consoleContents;
        private System.Windows.Forms.Button startGpuButton;
        private System.Windows.Forms.Button stopButton;
        private System.Windows.Forms.Button startCpuButton;
        private System.Windows.Forms.Button justSegmentButton;
        private System.Windows.Forms.Button switchToDfsButton;
        private System.Windows.Forms.PropertyGrid segmentationPropertiesGrid;
        private System.Windows.Forms.PictureBox shapeTermsImage;
        private System.Windows.Forms.PictureBox unaryTermsImage;
        private System.Windows.Forms.Button pauseContinueButton;
    }
}

