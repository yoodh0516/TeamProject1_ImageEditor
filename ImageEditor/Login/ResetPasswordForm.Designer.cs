namespace ImageEditor.Login
{
    partial class ResetPasswordForm
    {
        private System.ComponentModel.IContainer? components = null;

        protected override void Dispose(bool disposing)
        {
            if (disposing && components != null)
                components.Dispose();

            base.Dispose(disposing);
        }

        private void InitializeComponent()
        {
            lblTitle = new Label();
            tbResetPASS = new TextBox();
            tbResetPASSCheck = new TextBox();
            btnConfirm = new Button();
            SuspendLayout();
            // 
            // lblTitle
            // 
            lblTitle.AutoSize = true;
            lblTitle.Font = new Font("맑은 고딕", 14F, FontStyle.Bold);
            lblTitle.Location = new Point(126, 28);
            lblTitle.Name = "lblTitle";
            lblTitle.Size = new Size(168, 25);
            lblTitle.TabIndex = 0;
            lblTitle.Text = "비밀번호 찾기";
            // 
            // tbResetPASS
            // 
            tbResetPASS.Location = new Point(85, 84);
            tbResetPASS.Name = "tbResetPASS";
            tbResetPASS.PlaceholderText = "새 비밀번호";
            tbResetPASS.Size = new Size(250, 23);
            tbResetPASS.TabIndex = 1;
            tbResetPASS.UseSystemPasswordChar = true;
            // 
            // tbResetPASSCheck
            // 
            tbResetPASSCheck.Location = new Point(85, 123);
            tbResetPASSCheck.Name = "tbResetPASSCheck";
            tbResetPASSCheck.PlaceholderText = "새 비밀번호 확인";
            tbResetPASSCheck.Size = new Size(250, 23);
            tbResetPASSCheck.TabIndex = 2;
            tbResetPASSCheck.UseSystemPasswordChar = true;
            // 
            // btnConfirm
            // 
            btnConfirm.Location = new Point(85, 168);
            btnConfirm.Name = "btnConfirm";
            btnConfirm.Size = new Size(250, 36);
            btnConfirm.TabIndex = 3;
            btnConfirm.Text = "확인";
            btnConfirm.UseVisualStyleBackColor = true;
            btnConfirm.Click += btnConfirm_Click;
            // 
            // ResetPasswordForm
            // 
            AcceptButton = btnConfirm;
            AutoScaleDimensions = new SizeF(7F, 15F);
            AutoScaleMode = AutoScaleMode.Font;
            ClientSize = new Size(420, 235);
            Controls.Add(btnConfirm);
            Controls.Add(tbResetPASSCheck);
            Controls.Add(tbResetPASS);
            Controls.Add(lblTitle);
            FormBorderStyle = FormBorderStyle.FixedDialog;
            MaximizeBox = false;
            MinimizeBox = false;
            Name = "ResetPasswordForm";
            StartPosition = FormStartPosition.CenterParent;
            Text = "비밀번호 찾기";
            ResumeLayout(false);
            PerformLayout();
        }

        private Label lblTitle = null!;
        private TextBox tbResetPASS = null!;
        private TextBox tbResetPASSCheck = null!;
        private Button btnConfirm = null!;
    }
}
