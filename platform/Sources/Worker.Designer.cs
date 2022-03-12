namespace VirtualEnvironment.Platform
{
    partial class Worker
    {
        private System.ComponentModel.IContainer components = null;

        protected override void Dispose(bool disposing)
        {
            if (disposing && (components != null))
            {
                components.Dispose();
            }
            base.Dispose(disposing);
        }

        #region Vom Windows Form-Designer generierter Code

        /// <summary>
        /// Required method for Designer support - do not modify
        /// the contents of this method with the code editor.
        /// </summary>
        private void InitializeComponent()
        {
            System.ComponentModel.ComponentResourceManager resources = new System.ComponentModel.ComponentResourceManager(typeof(Worker));
            this.PictureBox = new System.Windows.Forms.PictureBox();
            this.Output = new System.Windows.Forms.Label();
            this.Label = new System.Windows.Forms.Label();
            this.Progress = new System.Windows.Forms.Label();
            ((System.ComponentModel.ISupportInitialize)(this.PictureBox)).BeginInit();
            this.SuspendLayout();
            // 
            // PictureBox
            // 
            this.PictureBox.BackgroundImage = ((System.Drawing.Image)(resources.GetObject("PictureBox.BackgroundImage")));
            this.PictureBox.BackgroundImageLayout = System.Windows.Forms.ImageLayout.Center;
            this.PictureBox.Location = new System.Drawing.Point(13, 13);
            this.PictureBox.Margin = new System.Windows.Forms.Padding(0);
            this.PictureBox.Name = "PictureBox";
            this.PictureBox.Size = new System.Drawing.Size(72, 72);
            this.PictureBox.TabIndex = 0;
            this.PictureBox.TabStop = false;
            // 
            // Output
            // 
            this.Output.Location = new System.Drawing.Point(91, 13);
            this.Output.Margin = new System.Windows.Forms.Padding(0);
            this.Output.Name = "Output";
            this.Output.Size = new System.Drawing.Size(300, 72);
            this.Output.TabIndex = 1;
            this.Output.TextAlign = System.Drawing.ContentAlignment.MiddleLeft;
            // 
            // Label
            // 
            this.Label.AutoSize = true;
            this.Label.ForeColor = System.Drawing.SystemColors.GrayText;
            this.Label.Location = new System.Drawing.Point(332, 79);
            this.Label.Margin = new System.Windows.Forms.Padding(4, 0, 4, 0);
            this.Label.Name = "Label";
            this.Label.Size = new System.Drawing.Size(77, 17);
            this.Label.TabIndex = 2;
            this.Label.Text = "seanox.com";
            this.Label.TextAlign = System.Drawing.ContentAlignment.MiddleRight;
            // 
            // Progress
            // 
            this.Progress.BackColor = System.Drawing.SystemColors.ControlDark;
            this.Progress.Location = new System.Drawing.Point(24, 85);
            this.Progress.Margin = new System.Windows.Forms.Padding(0);
            this.Progress.Name = "Progress";
            this.Progress.Size = new System.Drawing.Size(50, 3);
            this.Progress.TabIndex = 3;
            this.Progress.Visible = false;
            // 
            // Worker
            // 
            this.AllowDrop = true;
            this.AutoScaleDimensions = new System.Drawing.SizeF(7F, 17F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.BackColor = System.Drawing.SystemColors.Control;
            this.ClientSize = new System.Drawing.Size(400, 98);
            this.ControlBox = false;
            this.Controls.Add(this.Progress);
            this.Controls.Add(this.Label);
            this.Controls.Add(this.Output);
            this.Controls.Add(this.PictureBox);
            this.Font = new System.Drawing.Font("", 8.25F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.FormBorderStyle = System.Windows.Forms.FormBorderStyle.FixedToolWindow;
            this.Margin = new System.Windows.Forms.Padding(4);
            this.MaximizeBox = false;
            this.MinimizeBox = false;
            this.Name = "Worker";
            this.ShowIcon = false;
            this.ShowInTaskbar = false;
            this.StartPosition = System.Windows.Forms.FormStartPosition.CenterScreen;
            this.TopMost = true;
            ((System.ComponentModel.ISupportInitialize)(this.PictureBox)).EndInit();
            this.ResumeLayout(false);
            this.PerformLayout();
        }

        #endregion

        private System.Windows.Forms.PictureBox PictureBox;
        private System.Windows.Forms.Label Output;
        private System.Windows.Forms.Label Label;
        private System.Windows.Forms.Label Progress;
    }
}