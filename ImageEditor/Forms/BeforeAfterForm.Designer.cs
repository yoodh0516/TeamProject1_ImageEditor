namespace ImageEditor.Forms
{
    partial class BeforeAfterForm
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
            panel1 = new Panel();
            picBefore = new PictureBox();
            panel2 = new Panel();
            picAfter = new PictureBox();
            button1 = new Button();
            label1 = new Label();
            label2 = new Label();
            panel1.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)picBefore).BeginInit();
            panel2.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)picAfter).BeginInit();
            SuspendLayout();
            // 
            // panel1
            // 
            panel1.Controls.Add(picBefore);
            panel1.Location = new Point(0, 44);
            panel1.Name = "panel1";
            panel1.Size = new Size(490, 389);
            panel1.TabIndex = 0;
            // 
            // picBefore
            // 
            picBefore.Dock = DockStyle.Fill;
            picBefore.Location = new Point(0, 0);
            picBefore.Name = "picBefore";
            picBefore.Size = new Size(490, 389);
            picBefore.SizeMode = PictureBoxSizeMode.Zoom;
            picBefore.TabIndex = 0;
            picBefore.TabStop = false;
            // 
            // panel2
            // 
            panel2.Controls.Add(picAfter);
            panel2.Location = new Point(493, 44);
            panel2.Name = "panel2";
            panel2.Size = new Size(490, 389);
            panel2.TabIndex = 1;
            // 
            // picAfter
            // 
            picAfter.Dock = DockStyle.Fill;
            picAfter.Location = new Point(0, 0);
            picAfter.Name = "picAfter";
            picAfter.Size = new Size(490, 389);
            picAfter.SizeMode = PictureBoxSizeMode.Zoom;
            picAfter.TabIndex = 1;
            picAfter.TabStop = false;
            // 
            // button1
            // 
            button1.Location = new Point(838, 439);
            button1.Name = "button1";
            button1.Size = new Size(134, 60);
            button1.TabIndex = 2;
            button1.Text = "닫기";
            button1.UseVisualStyleBackColor = true;
            button1.Click += button1_Click;
            // 
            // label1
            // 
            label1.AutoSize = true;
            label1.Font = new Font("맑은 고딕", 20.25F, FontStyle.Bold, GraphicsUnit.Point);
            label1.Location = new Point(187, 4);
            label1.Name = "label1";
            label1.Size = new Size(102, 37);
            label1.TabIndex = 3;
            label1.Text = "Before";
            // 
            // label2
            // 
            label2.AutoSize = true;
            label2.Font = new Font("맑은 고딕", 20.25F, FontStyle.Bold, GraphicsUnit.Point);
            label2.Location = new Point(700, 4);
            label2.Name = "label2";
            label2.Size = new Size(82, 37);
            label2.TabIndex = 4;
            label2.Text = "After";
            // 
            // BeforeAfterForm
            // 
            AutoScaleDimensions = new SizeF(7F, 15F);
            AutoScaleMode = AutoScaleMode.Font;
            ClientSize = new Size(984, 511);
            Controls.Add(label2);
            Controls.Add(label1);
            Controls.Add(button1);
            Controls.Add(panel2);
            Controls.Add(panel1);
            Name = "BeforeAfterForm";
            Text = "Before / After 비교";
            panel1.ResumeLayout(false);
            ((System.ComponentModel.ISupportInitialize)picBefore).EndInit();
            panel2.ResumeLayout(false);
            ((System.ComponentModel.ISupportInitialize)picAfter).EndInit();
            ResumeLayout(false);
            PerformLayout();
        }

        #endregion

        private Panel panel1;
        private PictureBox picBefore;
        private Panel panel2;
        private PictureBox picAfter;
        private Button button1;
        private Label label1;
        private Label label2;
    }
}