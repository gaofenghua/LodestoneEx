namespace TransactionServerModules
{
	partial class FormJpg
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
			this.pbJpg = new System.Windows.Forms.PictureBox();
			((System.ComponentModel.ISupportInitialize)(this.pbJpg)).BeginInit();
			this.SuspendLayout();
			// 
			// pbJpg
			// 
			this.pbJpg.Dock = System.Windows.Forms.DockStyle.Fill;
			this.pbJpg.Location = new System.Drawing.Point(0, 0);
			this.pbJpg.Name = "pbJpg";
			this.pbJpg.Size = new System.Drawing.Size(543, 389);
			this.pbJpg.SizeMode = System.Windows.Forms.PictureBoxSizeMode.StretchImage;
			this.pbJpg.TabIndex = 0;
			this.pbJpg.TabStop = false;
			// 
			// FormJpg
			// 
			this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
			this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
			this.ClientSize = new System.Drawing.Size(543, 389);
			this.Controls.Add(this.pbJpg);
			this.Name = "FormJpg";
			this.Text = "FormJpg";
			((System.ComponentModel.ISupportInitialize)(this.pbJpg)).EndInit();
			this.ResumeLayout(false);

		}

		#endregion

		public System.Windows.Forms.PictureBox pbJpg;



	}
}