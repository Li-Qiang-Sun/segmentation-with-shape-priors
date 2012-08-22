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
            this.bestSegmentationMaskImage = new System.Windows.Forms.PictureBox();
            this.label1 = new System.Windows.Forms.Label();
            this.label2 = new System.Windows.Forms.Label();
            this.label3 = new System.Windows.Forms.Label();
            this.label4 = new System.Windows.Forms.Label();
            this.label5 = new System.Windows.Forms.Label();
            ((System.ComponentModel.ISupportInitialize)(this.currentImage)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this.segmentationMaskImage)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this.shapeTermsImage)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this.unaryTermsImage)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this.bestSegmentationMaskImage)).BeginInit();
            this.SuspendLayout();
            // 
            // currentImage
            // 
            this.currentImage.BorderStyle = System.Windows.Forms.BorderStyle.FixedSingle;
            this.currentImage.Location = new System.Drawing.Point(9, 30);
            this.currentImage.Name = "currentImage";
            this.currentImage.Size = new System.Drawing.Size(357, 280);
            this.currentImage.SizeMode = System.Windows.Forms.PictureBoxSizeMode.Zoom;
            this.currentImage.TabIndex = 0;
            this.currentImage.TabStop = false;
            // 
            // segmentationMaskImage
            // 
            this.segmentationMaskImage.BorderStyle = System.Windows.Forms.BorderStyle.FixedSingle;
            this.segmentationMaskImage.Location = new System.Drawing.Point(372, 30);
            this.segmentationMaskImage.Name = "segmentationMaskImage";
            this.segmentationMaskImage.Size = new System.Drawing.Size(357, 280);
            this.segmentationMaskImage.SizeMode = System.Windows.Forms.PictureBoxSizeMode.Zoom;
            this.segmentationMaskImage.TabIndex = 1;
            this.segmentationMaskImage.TabStop = false;
            // 
            // consoleContents
            // 
            this.consoleContents.Location = new System.Drawing.Point(1098, 8);
            this.consoleContents.Multiline = true;
            this.consoleContents.Name = "consoleContents";
            this.consoleContents.ReadOnly = true;
            this.consoleContents.ScrollBars = System.Windows.Forms.ScrollBars.Vertical;
            this.consoleContents.Size = new System.Drawing.Size(396, 610);
            this.consoleContents.TabIndex = 5;
            // 
            // startGpuButton
            // 
            this.startGpuButton.Location = new System.Drawing.Point(875, 635);
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
            this.stopButton.Location = new System.Drawing.Point(1375, 635);
            this.stopButton.Name = "stopButton";
            this.stopButton.Size = new System.Drawing.Size(119, 31);
            this.stopButton.TabIndex = 7;
            this.stopButton.Text = "Stop";
            this.stopButton.UseVisualStyleBackColor = true;
            this.stopButton.Click += new System.EventHandler(this.OnStopButtonClick);
            // 
            // startCpuButton
            // 
            this.startCpuButton.Location = new System.Drawing.Point(1000, 635);
            this.startCpuButton.Name = "startCpuButton";
            this.startCpuButton.Size = new System.Drawing.Size(119, 31);
            this.startCpuButton.TabIndex = 8;
            this.startCpuButton.Text = "Start on CPU";
            this.startCpuButton.UseVisualStyleBackColor = true;
            this.startCpuButton.Click += new System.EventHandler(this.OnStartCpuButtonClick);
            // 
            // justSegmentButton
            // 
            this.justSegmentButton.Location = new System.Drawing.Point(750, 635);
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
            this.switchToDfsButton.Location = new System.Drawing.Point(1125, 635);
            this.switchToDfsButton.Name = "switchToDfsButton";
            this.switchToDfsButton.Size = new System.Drawing.Size(119, 31);
            this.switchToDfsButton.TabIndex = 21;
            this.switchToDfsButton.Text = "Switch to DFS";
            this.switchToDfsButton.UseVisualStyleBackColor = true;
            this.switchToDfsButton.Click += new System.EventHandler(this.OnSwitchToDfsButtonClick);
            // 
            // segmentationPropertiesGrid
            // 
            this.segmentationPropertiesGrid.Location = new System.Drawing.Point(735, 316);
            this.segmentationPropertiesGrid.Name = "segmentationPropertiesGrid";
            this.segmentationPropertiesGrid.Size = new System.Drawing.Size(357, 302);
            this.segmentationPropertiesGrid.TabIndex = 24;
            this.segmentationPropertiesGrid.ToolbarVisible = false;
            // 
            // shapeTermsImage
            // 
            this.shapeTermsImage.BorderStyle = System.Windows.Forms.BorderStyle.FixedSingle;
            this.shapeTermsImage.Location = new System.Drawing.Point(372, 336);
            this.shapeTermsImage.Name = "shapeTermsImage";
            this.shapeTermsImage.Size = new System.Drawing.Size(357, 282);
            this.shapeTermsImage.SizeMode = System.Windows.Forms.PictureBoxSizeMode.Zoom;
            this.shapeTermsImage.TabIndex = 25;
            this.shapeTermsImage.TabStop = false;
            // 
            // unaryTermsImage
            // 
            this.unaryTermsImage.BorderStyle = System.Windows.Forms.BorderStyle.FixedSingle;
            this.unaryTermsImage.Location = new System.Drawing.Point(9, 336);
            this.unaryTermsImage.Name = "unaryTermsImage";
            this.unaryTermsImage.Size = new System.Drawing.Size(357, 280);
            this.unaryTermsImage.SizeMode = System.Windows.Forms.PictureBoxSizeMode.Zoom;
            this.unaryTermsImage.TabIndex = 26;
            this.unaryTermsImage.TabStop = false;
            // 
            // pauseContinueButton
            // 
            this.pauseContinueButton.Enabled = false;
            this.pauseContinueButton.Location = new System.Drawing.Point(1250, 635);
            this.pauseContinueButton.Name = "pauseContinueButton";
            this.pauseContinueButton.Size = new System.Drawing.Size(119, 31);
            this.pauseContinueButton.TabIndex = 27;
            this.pauseContinueButton.Text = "Pause";
            this.pauseContinueButton.UseVisualStyleBackColor = true;
            this.pauseContinueButton.Click += new System.EventHandler(this.OnPauseContinueButtonClick);
            // 
            // bestSegmentationMaskImage
            // 
            this.bestSegmentationMaskImage.BorderStyle = System.Windows.Forms.BorderStyle.FixedSingle;
            this.bestSegmentationMaskImage.Location = new System.Drawing.Point(735, 30);
            this.bestSegmentationMaskImage.Name = "bestSegmentationMaskImage";
            this.bestSegmentationMaskImage.Size = new System.Drawing.Size(357, 280);
            this.bestSegmentationMaskImage.SizeMode = System.Windows.Forms.PictureBoxSizeMode.Zoom;
            this.bestSegmentationMaskImage.TabIndex = 28;
            this.bestSegmentationMaskImage.TabStop = false;
            // 
            // label1
            // 
            this.label1.AutoSize = true;
            this.label1.Font = new System.Drawing.Font("Microsoft Sans Serif", 10F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, ((byte)(204)));
            this.label1.Location = new System.Drawing.Point(146, 10);
            this.label1.Name = "label1";
            this.label1.Size = new System.Drawing.Size(54, 17);
            this.label1.TabIndex = 29;
            this.label1.Text = "Status";
            // 
            // label2
            // 
            this.label2.AutoSize = true;
            this.label2.Font = new System.Drawing.Font("Microsoft Sans Serif", 10F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, ((byte)(204)));
            this.label2.Location = new System.Drawing.Point(90, 313);
            this.label2.Name = "label2";
            this.label2.Size = new System.Drawing.Size(177, 17);
            this.label2.TabIndex = 30;
            this.label2.Text = "Unary and shape terms";
            // 
            // label3
            // 
            this.label3.AutoSize = true;
            this.label3.Font = new System.Drawing.Font("Microsoft Sans Serif", 10F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, ((byte)(204)));
            this.label3.Location = new System.Drawing.Point(521, 10);
            this.label3.Name = "label3";
            this.label3.Size = new System.Drawing.Size(45, 17);
            this.label3.TabIndex = 31;
            this.label3.Text = "Mask";
            // 
            // label4
            // 
            this.label4.AutoSize = true;
            this.label4.Font = new System.Drawing.Font("Microsoft Sans Serif", 10F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, ((byte)(204)));
            this.label4.Location = new System.Drawing.Point(496, 316);
            this.label4.Name = "label4";
            this.label4.Size = new System.Drawing.Size(99, 17);
            this.label4.TabIndex = 32;
            this.label4.Text = "Shape terms";
            // 
            // label5
            // 
            this.label5.AutoSize = true;
            this.label5.Font = new System.Drawing.Font("Microsoft Sans Serif", 10F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, ((byte)(204)));
            this.label5.Location = new System.Drawing.Point(841, 10);
            this.label5.Name = "label5";
            this.label5.Size = new System.Drawing.Size(129, 17);
            this.label5.TabIndex = 33;
            this.label5.Text = "Best mask so far";
            // 
            // MainForm
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(1506, 678);
            this.Controls.Add(this.label5);
            this.Controls.Add(this.label4);
            this.Controls.Add(this.label3);
            this.Controls.Add(this.label2);
            this.Controls.Add(this.label1);
            this.Controls.Add(this.bestSegmentationMaskImage);
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
            ((System.ComponentModel.ISupportInitialize)(this.bestSegmentationMaskImage)).EndInit();
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
        private System.Windows.Forms.PictureBox bestSegmentationMaskImage;
        private System.Windows.Forms.Label label1;
        private System.Windows.Forms.Label label2;
        private System.Windows.Forms.Label label3;
        private System.Windows.Forms.Label label4;
        private System.Windows.Forms.Label label5;
    }
}

