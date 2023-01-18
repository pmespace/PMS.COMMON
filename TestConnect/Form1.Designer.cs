namespace TestConnect
{
	partial class Form1
	{
		/// <summary>
		///  Required designer variable.
		/// </summary>
		private System.ComponentModel.IContainer components = null;

		/// <summary>
		///  Clean up any resources being used.
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
		///  Required method for Designer support - do not modify
		///  the contents of this method with the code editor.
		/// </summary>
		private void InitializeComponent()
		{
			this.tbIP = new System.Windows.Forms.TextBox();
			this.pbConnect = new System.Windows.Forms.Button();
			this.udPort = new System.Windows.Forms.NumericUpDown();
			this.label1 = new System.Windows.Forms.Label();
			this.udTimeout = new System.Windows.Forms.NumericUpDown();
			((System.ComponentModel.ISupportInitialize)(this.udPort)).BeginInit();
			((System.ComponentModel.ISupportInitialize)(this.udTimeout)).BeginInit();
			this.SuspendLayout();
			// 
			// tbIP
			// 
			this.tbIP.Location = new System.Drawing.Point(27, 18);
			this.tbIP.Name = "tbIP";
			this.tbIP.Size = new System.Drawing.Size(173, 23);
			this.tbIP.TabIndex = 0;
			this.tbIP.Text = "192.168.0.225";
			// 
			// pbConnect
			// 
			this.pbConnect.Location = new System.Drawing.Point(294, 16);
			this.pbConnect.Name = "pbConnect";
			this.pbConnect.Size = new System.Drawing.Size(75, 23);
			this.pbConnect.TabIndex = 1;
			this.pbConnect.Text = "connect";
			this.pbConnect.UseVisualStyleBackColor = true;
			this.pbConnect.Click += new System.EventHandler(this.pbConnect_Click);
			// 
			// udPort
			// 
			this.udPort.Location = new System.Drawing.Point(206, 18);
			this.udPort.Maximum = new decimal(new int[] {
            65535,
            0,
            0,
            0});
			this.udPort.Minimum = new decimal(new int[] {
            1,
            0,
            0,
            0});
			this.udPort.Name = "udPort";
			this.udPort.Size = new System.Drawing.Size(82, 23);
			this.udPort.TabIndex = 2;
			this.udPort.Value = new decimal(new int[] {
            2018,
            0,
            0,
            0});
			// 
			// label1
			// 
			this.label1.AutoSize = true;
			this.label1.Location = new System.Drawing.Point(375, 20);
			this.label1.Name = "label1";
			this.label1.Size = new System.Drawing.Size(38, 15);
			this.label1.TabIndex = 3;
			this.label1.Text = "label1";
			// 
			// udTimeout
			// 
			this.udTimeout.Location = new System.Drawing.Point(206, 53);
			this.udTimeout.Maximum = new decimal(new int[] {
            10,
            0,
            0,
            0});
			this.udTimeout.Minimum = new decimal(new int[] {
            1,
            0,
            0,
            0});
			this.udTimeout.Name = "udTimeout";
			this.udTimeout.Size = new System.Drawing.Size(82, 23);
			this.udTimeout.TabIndex = 4;
			this.udTimeout.Value = new decimal(new int[] {
            1,
            0,
            0,
            0});
			// 
			// Form1
			// 
			this.AutoScaleDimensions = new System.Drawing.SizeF(7F, 15F);
			this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
			this.ClientSize = new System.Drawing.Size(800, 450);
			this.Controls.Add(this.udTimeout);
			this.Controls.Add(this.label1);
			this.Controls.Add(this.udPort);
			this.Controls.Add(this.pbConnect);
			this.Controls.Add(this.tbIP);
			this.Name = "Form1";
			this.Text = "Form1";
			((System.ComponentModel.ISupportInitialize)(this.udPort)).EndInit();
			((System.ComponentModel.ISupportInitialize)(this.udTimeout)).EndInit();
			this.ResumeLayout(false);
			this.PerformLayout();

		}

		#endregion

		private TextBox tbIP;
		private Button pbConnect;
		private NumericUpDown udPort;
		private Label label1;
		private NumericUpDown udTimeout;
	}
}