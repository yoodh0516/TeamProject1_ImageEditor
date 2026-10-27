namespace ImageEditor.Login
{
    partial class JoinMembership
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
            label1 = new Label();
            label2 = new Label();
            label3 = new Label();
            label4 = new Label();
            label5 = new Label();
            tbID = new TextBox();
            tbPASS = new TextBox();
            tbPASSCheck = new TextBox();
            tbNAME = new TextBox();
            tbEMAIL = new TextBox();
            createMember = new Button();
            SuspendLayout();
            // 
            // lblTitle
            // 
            lblTitle.AutoSize = true;
            lblTitle.Font = new Font("맑은 고딕", 15F, FontStyle.Bold);
            lblTitle.Location = new Point(207, 25);
            lblTitle.Name = "lblTitle";
            lblTitle.Size = new Size(103, 28);
            lblTitle.TabIndex = 0;
            lblTitle.Text = "회원가입";
            // 
            // label1
            // 
            label1.AutoSize = true;
            label1.Location = new Point(105, 84);
            label1.Name = "label1";
            label1.Size = new Size(43, 15);
            label1.TabIndex = 1;
            label1.Text = "아이디";
            // 
            // label2
            // 
            label2.AutoSize = true;
            label2.Location = new Point(93, 130);
            label2.Name = "label2";
            label2.Size = new Size(55, 15);
            label2.TabIndex = 2;
            label2.Text = "비밀번호";
            // 
            // label3
            // 
            label3.AutoSize = true;
            label3.Location = new Point(65, 176);
            label3.Name = "label3";
            label3.Size = new Size(83, 15);
            label3.TabIndex = 3;
            label3.Text = "비밀번호 확인";
            // 
            // label4
            // 
            label4.AutoSize = true;
            label4.Location = new Point(117, 222);
            label4.Name = "label4";
            label4.Size = new Size(31, 15);
            label4.TabIndex = 4;
            label4.Text = "이름";
            // 
            // label5
            // 
            label5.AutoSize = true;
            label5.Location = new Point(105, 268);
            label5.Name = "label5";
            label5.Size = new Size(43, 15);
            label5.TabIndex = 5;
            label5.Text = "이메일";
            // 
            // tbID
            // 
            tbID.Location = new Point(165, 80);
            tbID.MaxLength = 45;
            tbID.Name = "tbID";
            tbID.Size = new Size(250, 23);
            tbID.TabIndex = 6;
            // 
            // tbPASS
            // 
            tbPASS.Location = new Point(165, 126);
            tbPASS.Name = "tbPASS";
            tbPASS.MaxLength = 72;
            tbPASS.Size = new Size(250, 23);
            tbPASS.TabIndex = 7;
            tbPASS.UseSystemPasswordChar = true;
            // 
            // tbPASSCheck
            // 
            tbPASSCheck.Location = new Point(165, 172);
            tbPASSCheck.Name = "tbPASSCheck";
            tbPASSCheck.MaxLength = 72;
            tbPASSCheck.Size = new Size(250, 23);
            tbPASSCheck.TabIndex = 8;
            tbPASSCheck.UseSystemPasswordChar = true;
            // 
            // tbNAME
            // 
            tbNAME.Location = new Point(165, 218);
            tbNAME.MaxLength = 45;
            tbNAME.Name = "tbNAME";
            tbNAME.Size = new Size(250, 23);
            tbNAME.TabIndex = 9;
            // 
            // tbEMAIL
            // 
            tbEMAIL.Location = new Point(165, 264);
            tbEMAIL.MaxLength = 45;
            tbEMAIL.Name = "tbEMAIL";
            tbEMAIL.Size = new Size(250, 23);
            tbEMAIL.TabIndex = 10;
            // 
            // createMember
            // 
            createMember.Location = new Point(165, 315);
            createMember.Name = "createMember";
            createMember.Size = new Size(250, 38);
            createMember.TabIndex = 11;
            createMember.Text = "가입하기";
            createMember.UseVisualStyleBackColor = true;
            createMember.Click += createMember_Click;
            // 
            // JoinMembership
            // 
            AcceptButton = createMember;
            AutoScaleDimensions = new SizeF(7F, 15F);
            AutoScaleMode = AutoScaleMode.Font;
            ClientSize = new Size(520, 390);
            Controls.Add(createMember);
            Controls.Add(tbEMAIL);
            Controls.Add(tbNAME);
            Controls.Add(tbPASSCheck);
            Controls.Add(tbPASS);
            Controls.Add(tbID);
            Controls.Add(label5);
            Controls.Add(label4);
            Controls.Add(label3);
            Controls.Add(label2);
            Controls.Add(label1);
            Controls.Add(lblTitle);
            FormBorderStyle = FormBorderStyle.FixedDialog;
            MaximizeBox = false;
            MinimizeBox = false;
            Name = "JoinMembership";
            StartPosition = FormStartPosition.CenterParent;
            Text = "회원가입";
            ResumeLayout(false);
            PerformLayout();
        }

        private Label lblTitle = null!;
        private Label label1 = null!;
        private Label label2 = null!;
        private Label label3 = null!;
        private Label label4 = null!;
        private Label label5 = null!;
        private TextBox tbID = null!;
        private TextBox tbPASS = null!;
        private TextBox tbPASSCheck = null!;
        private TextBox tbNAME = null!;
        private TextBox tbEMAIL = null!;
        private Button createMember = null!;
    }
}
