namespace ImageEditor.Login
{
    partial class LoginForm
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
            tbID = new TextBox();
            tbPASS = new TextBox();
            btnLogin = new Button();
            lbFindID = new Label();
            lbjoinMembership = new Label();
            btnNotLogin = new Button();
            SuspendLayout();
            // 
            // lblTitle
            // 
            lblTitle.AutoSize = true;
            lblTitle.Font = new Font("맑은 고딕", 16F, FontStyle.Bold);
            lblTitle.Location = new Point(202, 48);
            lblTitle.Name = "lblTitle";
            lblTitle.Size = new Size(117, 30);
            lblTitle.TabIndex = 0;
            lblTitle.Text = "로그인";
            // 
            // tbID
            // 
            tbID.Location = new Point(150, 104);
            tbID.Name = "tbID";
            tbID.PlaceholderText = "아이디";
            tbID.Size = new Size(220, 23);
            tbID.TabIndex = 1;
            // 
            // tbPASS
            // 
            tbPASS.Location = new Point(150, 142);
            tbPASS.Name = "tbPASS";
            tbPASS.PlaceholderText = "비밀번호";
            tbPASS.Size = new Size(220, 23);
            tbPASS.TabIndex = 2;
            tbPASS.UseSystemPasswordChar = true;
            // 
            // btnLogin
            // 
            btnLogin.Location = new Point(150, 184);
            btnLogin.Name = "btnLogin";
            btnLogin.Size = new Size(220, 38);
            btnLogin.TabIndex = 3;
            btnLogin.Text = "로그인";
            btnLogin.UseVisualStyleBackColor = true;
            btnLogin.Click += btnLogin_Click;
            // 
            // lbFindID
            // 
            lbFindID.AutoSize = true;
            lbFindID.Cursor = Cursors.Hand;
            lbFindID.ForeColor = Color.RoyalBlue;
            lbFindID.Location = new Point(150, 242);
            lbFindID.Name = "lbFindID";
            lbFindID.Size = new Size(156, 15);
            lbFindID.TabIndex = 4;
            lbFindID.Text = "아이디 찾기 / 비밀번호 찾기";
            lbFindID.Click += lbFindID_Click;
            // 
            // lbjoinMembership
            // 
            lbjoinMembership.AutoSize = true;
            lbjoinMembership.Cursor = Cursors.Hand;
            lbjoinMembership.ForeColor = Color.RoyalBlue;
            lbjoinMembership.Location = new Point(315, 242);
            lbjoinMembership.Name = "lbjoinMembership";
            lbjoinMembership.Size = new Size(55, 15);
            lbjoinMembership.TabIndex = 5;
            lbjoinMembership.Text = "회원가입";
            lbjoinMembership.Click += lbjoinMembership_Click;
            // 
            // btnNotLogin
            // 
            btnNotLogin.DialogResult = DialogResult.Cancel;
            btnNotLogin.Location = new Point(376, 12);
            btnNotLogin.Name = "btnNotLogin";
            btnNotLogin.Size = new Size(132, 30);
            btnNotLogin.TabIndex = 6;
            btnNotLogin.Text = "로그인 없이 시작";
            btnNotLogin.UseVisualStyleBackColor = true;
            btnNotLogin.Click += btnNotLogin_Click;
            // 
            // LoginForm
            // 
            AcceptButton = btnLogin;
            AutoScaleDimensions = new SizeF(7F, 15F);
            AutoScaleMode = AutoScaleMode.Font;
            CancelButton = btnNotLogin;
            ClientSize = new Size(520, 300);
            Controls.Add(btnNotLogin);
            Controls.Add(lbjoinMembership);
            Controls.Add(lbFindID);
            Controls.Add(btnLogin);
            Controls.Add(tbPASS);
            Controls.Add(tbID);
            Controls.Add(lblTitle);
            FormBorderStyle = FormBorderStyle.FixedDialog;
            MaximizeBox = false;
            MinimizeBox = false;
            Name = "LoginForm";
            StartPosition = FormStartPosition.CenterParent;
            Text = "ImageEditor 로그인";
            ResumeLayout(false);
            PerformLayout();
        }

        private Label lblTitle = null!;
        private TextBox tbID = null!;
        private TextBox tbPASS = null!;
        private Button btnLogin = null!;
        private Label lbFindID = null!;
        private Label lbjoinMembership = null!;
        private Button btnNotLogin = null!;
    }
}
