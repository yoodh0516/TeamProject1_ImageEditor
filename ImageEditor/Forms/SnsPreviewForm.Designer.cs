namespace ImageEditor.Forms
{
    partial class SnsPreviewForm
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
            cmbPreset = new ComboBox();
            picPreview = new PictureBox();
            label1 = new Label();
            label2 = new Label();
            lblInfo = new Label();
            btnClose = new Button();
            ((System.ComponentModel.ISupportInitialize)picPreview).BeginInit();
            SuspendLayout();
            // 
            // cmbPreset
            // 
            cmbPreset.DropDownStyle = ComboBoxStyle.DropDownList;
            cmbPreset.FormattingEnabled = true;
            cmbPreset.Location = new Point(51, 112);
            cmbPreset.Name = "cmbPreset";
            cmbPreset.Size = new Size(336, 23);
            cmbPreset.TabIndex = 0;
            cmbPreset.SelectedIndexChanged += cmbPreset_SelectedIndexChanged;
            // 
            // picPreview
            // 
            picPreview.BorderStyle = BorderStyle.FixedSingle;
            picPreview.Location = new Point(51, 160);
            picPreview.Name = "picPreview";
            picPreview.Size = new Size(753, 402);
            picPreview.SizeMode = PictureBoxSizeMode.Zoom;
            picPreview.TabIndex = 1;
            picPreview.TabStop = false;
            // 
            // label1
            // 
            label1.AutoSize = true;
            label1.Location = new Point(51, 80);
            label1.Name = "label1";
            label1.Size = new Size(80, 15);
            label1.TabIndex = 2;
            label1.Text = "플랫폼 / 용도";
            // 
            // label2
            // 
            label2.AutoSize = true;
            label2.Location = new Point(51, 38);
            label2.Name = "label2";
            label2.Size = new Size(83, 15);
            label2.TabIndex = 3;
            label2.Text = "결과 미리보기";
            // 
            // lblInfo
            // 
            lblInfo.AutoSize = true;
            lblInfo.Location = new Point(51, 595);
            lblInfo.Name = "lblInfo";
            lblInfo.Size = new Size(111, 15);
            lblInfo.TabIndex = 4;
            lblInfo.Text = "1080 x 1350 / 4 :5";
            // 
            // btnClose
            // 
            btnClose.Location = new Point(708, 608);
            btnClose.Name = "btnClose";
            btnClose.Size = new Size(96, 41);
            btnClose.TabIndex = 5;
            btnClose.Text = "닫기";
            btnClose.UseVisualStyleBackColor = true;
            btnClose.Click += btnClose_Click;
            // 
            // SnsPreviewForm
            // 
            AutoScaleDimensions = new SizeF(7F, 15F);
            AutoScaleMode = AutoScaleMode.Font;
            ClientSize = new Size(884, 661);
            Controls.Add(btnClose);
            Controls.Add(lblInfo);
            Controls.Add(label2);
            Controls.Add(label1);
            Controls.Add(picPreview);
            Controls.Add(cmbPreset);
            Name = "SnsPreviewForm";
            StartPosition = FormStartPosition.CenterParent;
            Text = "SnsPreviewForm";
            ((System.ComponentModel.ISupportInitialize)picPreview).EndInit();
            ResumeLayout(false);
            PerformLayout();
        }

        #endregion

        private ComboBox cmbPreset;
        private PictureBox picPreview;
        private Label label1;
        private Label label2;
        private Label lblInfo;
        private Button btnClose;
    }
}