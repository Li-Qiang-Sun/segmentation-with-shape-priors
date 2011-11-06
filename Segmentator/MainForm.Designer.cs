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
            this.consoleContents = new System.Windows.Forms.TextBox();
            this.startGpuButton = new System.Windows.Forms.Button();
            this.stopButton = new System.Windows.Forms.Button();
            this.startCpuButton = new System.Windows.Forms.Button();
            this.label1 = new System.Windows.Forms.Label();
            this.bfsIterationsInput = new System.Windows.Forms.NumericUpDown();
            this.label2 = new System.Windows.Forms.Label();
            this.shapeTermWeightInput = new System.Windows.Forms.NumericUpDown();
            this.label3 = new System.Windows.Forms.Label();
            this.shapeEnergyWeightInput = new System.Windows.Forms.NumericUpDown();
            this.label4 = new System.Windows.Forms.Label();
            this.reportRateInput = new System.Windows.Forms.NumericUpDown();
            this.label5 = new System.Windows.Forms.Label();
            this.frontSaveRateInput = new System.Windows.Forms.NumericUpDown();
            this.justSegmentButton = new System.Windows.Forms.Button();
            this.switchToDfsButton = new System.Windows.Forms.Button();
            this.modelComboBox = new System.Windows.Forms.ComboBox();
            this.label6 = new System.Windows.Forms.Label();
            ((System.ComponentModel.ISupportInitialize)(this.currentImage)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this.resultImage)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this.bfsIterationsInput)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this.shapeTermWeightInput)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this.shapeEnergyWeightInput)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this.reportRateInput)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this.frontSaveRateInput)).BeginInit();
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
            // resultImage
            // 
            this.resultImage.BorderStyle = System.Windows.Forms.BorderStyle.FixedSingle;
            this.resultImage.Location = new System.Drawing.Point(435, 8);
            this.resultImage.Name = "resultImage";
            this.resultImage.Size = new System.Drawing.Size(420, 302);
            this.resultImage.SizeMode = System.Windows.Forms.PictureBoxSizeMode.Zoom;
            this.resultImage.TabIndex = 1;
            this.resultImage.TabStop = false;
            // 
            // consoleContents
            // 
            this.consoleContents.Location = new System.Drawing.Point(857, 10);
            this.consoleContents.Multiline = true;
            this.consoleContents.Name = "consoleContents";
            this.consoleContents.ReadOnly = true;
            this.consoleContents.ScrollBars = System.Windows.Forms.ScrollBars.Vertical;
            this.consoleContents.Size = new System.Drawing.Size(431, 396);
            this.consoleContents.TabIndex = 5;
            // 
            // startGpuButton
            // 
            this.startGpuButton.Location = new System.Drawing.Point(357, 375);
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
            this.stopButton.Location = new System.Drawing.Point(732, 375);
            this.stopButton.Name = "stopButton";
            this.stopButton.Size = new System.Drawing.Size(119, 31);
            this.stopButton.TabIndex = 7;
            this.stopButton.Text = "Stop";
            this.stopButton.UseVisualStyleBackColor = true;
            this.stopButton.Click += new System.EventHandler(this.OnStopButtonClick);
            // 
            // startCpuButton
            // 
            this.startCpuButton.Location = new System.Drawing.Point(482, 375);
            this.startCpuButton.Name = "startCpuButton";
            this.startCpuButton.Size = new System.Drawing.Size(119, 31);
            this.startCpuButton.TabIndex = 8;
            this.startCpuButton.Text = "Start on CPU";
            this.startCpuButton.UseVisualStyleBackColor = true;
            this.startCpuButton.Click += new System.EventHandler(this.OnStartCpuButtonClick);
            // 
            // label1
            // 
            this.label1.AutoSize = true;
            this.label1.Location = new System.Drawing.Point(13, 320);
            this.label1.Name = "label1";
            this.label1.Size = new System.Drawing.Size(72, 13);
            this.label1.TabIndex = 10;
            this.label1.Text = "BFS iterations";
            // 
            // bfsIterationsInput
            // 
            this.bfsIterationsInput.Location = new System.Drawing.Point(91, 317);
            this.bfsIterationsInput.Maximum = new decimal(new int[] {
            100000,
            0,
            0,
            0});
            this.bfsIterationsInput.Minimum = new decimal(new int[] {
            100,
            0,
            0,
            0});
            this.bfsIterationsInput.Name = "bfsIterationsInput";
            this.bfsIterationsInput.Size = new System.Drawing.Size(82, 20);
            this.bfsIterationsInput.TabIndex = 11;
            this.bfsIterationsInput.Value = new decimal(new int[] {
            10000,
            0,
            0,
            0});
            // 
            // label2
            // 
            this.label2.AutoSize = true;
            this.label2.Location = new System.Drawing.Point(201, 320);
            this.label2.Name = "label2";
            this.label2.Size = new System.Drawing.Size(95, 13);
            this.label2.TabIndex = 12;
            this.label2.Text = "Shape term weight";
            // 
            // shapeTermWeightInput
            // 
            this.shapeTermWeightInput.Increment = new decimal(new int[] {
            1,
            0,
            0,
            131072});
            this.shapeTermWeightInput.Location = new System.Drawing.Point(302, 317);
            this.shapeTermWeightInput.Maximum = new decimal(new int[] {
            1000,
            0,
            0,
            0});
            this.shapeTermWeightInput.Name = "shapeTermWeightInput";
            this.shapeTermWeightInput.Size = new System.Drawing.Size(82, 20);
            this.shapeTermWeightInput.TabIndex = 13;
            this.shapeTermWeightInput.Value = new decimal(new int[] {
            10,
            0,
            0,
            0});
            // 
            // label3
            // 
            this.label3.AutoSize = true;
            this.label3.Location = new System.Drawing.Point(402, 321);
            this.label3.Name = "label3";
            this.label3.Size = new System.Drawing.Size(107, 13);
            this.label3.TabIndex = 14;
            this.label3.Text = "Shape energy weight";
            // 
            // shapeEnergyWeightInput
            // 
            this.shapeEnergyWeightInput.Increment = new decimal(new int[] {
            1,
            0,
            0,
            131072});
            this.shapeEnergyWeightInput.Location = new System.Drawing.Point(508, 318);
            this.shapeEnergyWeightInput.Maximum = new decimal(new int[] {
            1000000,
            0,
            0,
            0});
            this.shapeEnergyWeightInput.Name = "shapeEnergyWeightInput";
            this.shapeEnergyWeightInput.Size = new System.Drawing.Size(82, 20);
            this.shapeEnergyWeightInput.TabIndex = 15;
            this.shapeEnergyWeightInput.Value = new decimal(new int[] {
            10,
            0,
            0,
            0});
            // 
            // label4
            // 
            this.label4.AutoSize = true;
            this.label4.Location = new System.Drawing.Point(25, 345);
            this.label4.Name = "label4";
            this.label4.Size = new System.Drawing.Size(60, 13);
            this.label4.TabIndex = 16;
            this.label4.Text = "Report rate";
            // 
            // reportRateInput
            // 
            this.reportRateInput.Location = new System.Drawing.Point(91, 343);
            this.reportRateInput.Maximum = new decimal(new int[] {
            10000000,
            0,
            0,
            0});
            this.reportRateInput.Minimum = new decimal(new int[] {
            1,
            0,
            0,
            0});
            this.reportRateInput.Name = "reportRateInput";
            this.reportRateInput.Size = new System.Drawing.Size(82, 20);
            this.reportRateInput.TabIndex = 17;
            this.reportRateInput.Value = new decimal(new int[] {
            100,
            0,
            0,
            0});
            // 
            // label5
            // 
            this.label5.AutoSize = true;
            this.label5.Location = new System.Drawing.Point(218, 345);
            this.label5.Name = "label5";
            this.label5.Size = new System.Drawing.Size(78, 13);
            this.label5.TabIndex = 18;
            this.label5.Text = "Front save rate";
            // 
            // frontSaveRateInput
            // 
            this.frontSaveRateInput.Location = new System.Drawing.Point(302, 343);
            this.frontSaveRateInput.Maximum = new decimal(new int[] {
            10000000,
            0,
            0,
            0});
            this.frontSaveRateInput.Minimum = new decimal(new int[] {
            1,
            0,
            0,
            0});
            this.frontSaveRateInput.Name = "frontSaveRateInput";
            this.frontSaveRateInput.Size = new System.Drawing.Size(82, 20);
            this.frontSaveRateInput.TabIndex = 19;
            this.frontSaveRateInput.Value = new decimal(new int[] {
            2500,
            0,
            0,
            0});
            // 
            // justSegmentButton
            // 
            this.justSegmentButton.Location = new System.Drawing.Point(232, 375);
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
            this.switchToDfsButton.Location = new System.Drawing.Point(607, 375);
            this.switchToDfsButton.Name = "switchToDfsButton";
            this.switchToDfsButton.Size = new System.Drawing.Size(119, 31);
            this.switchToDfsButton.TabIndex = 21;
            this.switchToDfsButton.Text = "Switch to DFS";
            this.switchToDfsButton.UseVisualStyleBackColor = true;
            this.switchToDfsButton.Click += new System.EventHandler(this.OnSwitchToDfsButtonClick);
            // 
            // modelComboBox
            // 
            this.modelComboBox.FormattingEnabled = true;
            this.modelComboBox.Items.AddRange(new object[] {
            "1 edge",
            "2 edges",
            "E letter"});
            this.modelComboBox.Location = new System.Drawing.Point(508, 344);
            this.modelComboBox.Name = "modelComboBox";
            this.modelComboBox.Size = new System.Drawing.Size(82, 21);
            this.modelComboBox.TabIndex = 22;
            // 
            // label6
            // 
            this.label6.AutoSize = true;
            this.label6.Location = new System.Drawing.Point(466, 347);
            this.label6.Name = "label6";
            this.label6.Size = new System.Drawing.Size(36, 13);
            this.label6.TabIndex = 23;
            this.label6.Text = "Model";
            // 
            // MainForm
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(1300, 414);
            this.Controls.Add(this.label6);
            this.Controls.Add(this.modelComboBox);
            this.Controls.Add(this.switchToDfsButton);
            this.Controls.Add(this.justSegmentButton);
            this.Controls.Add(this.frontSaveRateInput);
            this.Controls.Add(this.label5);
            this.Controls.Add(this.reportRateInput);
            this.Controls.Add(this.label4);
            this.Controls.Add(this.shapeEnergyWeightInput);
            this.Controls.Add(this.label3);
            this.Controls.Add(this.shapeTermWeightInput);
            this.Controls.Add(this.label2);
            this.Controls.Add(this.bfsIterationsInput);
            this.Controls.Add(this.label1);
            this.Controls.Add(this.startCpuButton);
            this.Controls.Add(this.stopButton);
            this.Controls.Add(this.startGpuButton);
            this.Controls.Add(this.consoleContents);
            this.Controls.Add(this.resultImage);
            this.Controls.Add(this.currentImage);
            this.Name = "MainForm";
            this.Text = "Segmentation process";
            ((System.ComponentModel.ISupportInitialize)(this.currentImage)).EndInit();
            ((System.ComponentModel.ISupportInitialize)(this.resultImage)).EndInit();
            ((System.ComponentModel.ISupportInitialize)(this.bfsIterationsInput)).EndInit();
            ((System.ComponentModel.ISupportInitialize)(this.shapeTermWeightInput)).EndInit();
            ((System.ComponentModel.ISupportInitialize)(this.shapeEnergyWeightInput)).EndInit();
            ((System.ComponentModel.ISupportInitialize)(this.reportRateInput)).EndInit();
            ((System.ComponentModel.ISupportInitialize)(this.frontSaveRateInput)).EndInit();
            this.ResumeLayout(false);
            this.PerformLayout();

        }

        #endregion

        private System.Windows.Forms.PictureBox currentImage;
        private System.Windows.Forms.PictureBox resultImage;
        private System.Windows.Forms.TextBox consoleContents;
        private System.Windows.Forms.Button startGpuButton;
        private System.Windows.Forms.Button stopButton;
        private System.Windows.Forms.Button startCpuButton;
        private System.Windows.Forms.Label label1;
        private System.Windows.Forms.NumericUpDown bfsIterationsInput;
        private System.Windows.Forms.Label label2;
        private System.Windows.Forms.NumericUpDown shapeTermWeightInput;
        private System.Windows.Forms.Label label3;
        private System.Windows.Forms.NumericUpDown shapeEnergyWeightInput;
        private System.Windows.Forms.Label label4;
        private System.Windows.Forms.NumericUpDown reportRateInput;
        private System.Windows.Forms.Label label5;
        private System.Windows.Forms.NumericUpDown frontSaveRateInput;
        private System.Windows.Forms.Button justSegmentButton;
        private System.Windows.Forms.Button switchToDfsButton;
        private System.Windows.Forms.ComboBox modelComboBox;
        private System.Windows.Forms.Label label6;
    }
}

