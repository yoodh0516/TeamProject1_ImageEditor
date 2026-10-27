namespace ImageEditor.Login {
    partial class FindIDForm {
        /// <summary>
        /// Required designer variable.
        /// </summary>
        private System.ComponentModel.IContainer components = null;

        /// <summary>
        /// Clean up any resources being used.
        /// </summary>
        /// <param name="disposing">true if managed resources should be disposed; otherwise, false.</param>
        protected override void Dispose(bool disposing) {
            if (disposing && (components != null)) {
                components.Dispose();
            }
            base.Dispose(disposing);
        }

        #region Windows Form Designer generated code

        /// <summary>
        /// Required method for Designer support - do not modify
        /// the contents of this method with the code editor.
        /// </summary>
        private void InitializeComponent() {
            tbFindPassEmail = new TextBox();
            label4 = new Label();
            tbFindPassName = new TextBox();
            btnFindPass = new Button();
            label3 = new Label();
            tbFindIDEmaile = new TextBox();
            label2 = new Label();
            tbFindIDName = new TextBox();
            btnFindID = new Button();
            label1 = new Label();
            panelFindID = new Panel();
            panelFindPass = new Panel();
            label5 = new Label();
            tbFindPassID = new TextBox();
            btnPanelFIndPass = new Button();
            btnPanelFIndID = new Button();
            panelFindID.SuspendLayout();
            panelFindPass.SuspendLayout();
            SuspendLayout();
            // 
            // tbFindPassEmail
            // 
            tbFindPassEmail.Location = new Point(350, 229);
            tbFindPassEmail.Name = "tbFindPassEmail";
            tbFindPassEmail.Size = new Size(100, 23);
            tbFindPassEmail.TabIndex = 0;
            // 
            // label4
            // 
            label4.AutoSize = true;
            label4.Location = new Point(327, 201);
            label4.Name = "label4";
            label4.Size = new Size(147, 15);
            label4.TabIndex = 2;
            label4.Text = "이메일 주소를 입력하세요";
            // 
            // tbFindPassName
            // 
            tbFindPassName.Location = new Point(350, 101);
            tbFindPassName.Name = "tbFindPassName";
            tbFindPassName.Size = new Size(100, 23);
            tbFindPassName.TabIndex = 0;
            // 
            // btnFindPass
            // 
            btnFindPass.Location = new Point(363, 265);
            btnFindPass.Name = "btnFindPass";
            btnFindPass.Size = new Size(75, 23);
            btnFindPass.TabIndex = 3;
            btnFindPass.Text = "본인 확인";
            btnFindPass.UseVisualStyleBackColor = true;
            btnFindPass.Click += btnFindPass_Click;
            // 
            // label3
            // 
            label3.AutoSize = true;
            label3.Location = new Point(327, 73);
            label3.Name = "label3";
            label3.Size = new Size(147, 15);
            label3.TabIndex = 1;
            label3.Text = "본인의 이름을 입력하세요";
            // 
            // tbFindIDEmaile
            // 
            tbFindIDEmaile.Location = new Point(350, 206);
            tbFindIDEmaile.Name = "tbFindIDEmaile";
            tbFindIDEmaile.Size = new Size(100, 23);
            tbFindIDEmaile.TabIndex = 0;
            // 
            // label2
            // 
            label2.AutoSize = true;
            label2.Location = new Point(327, 169);
            label2.Name = "label2";
            label2.Size = new Size(147, 15);
            label2.TabIndex = 2;
            label2.Text = "이메일 주소를 입력하세요";
            // 
            // tbFindIDName
            // 
            tbFindIDName.Location = new Point(350, 124);
            tbFindIDName.Name = "tbFindIDName";
            tbFindIDName.Size = new Size(100, 23);
            tbFindIDName.TabIndex = 0;
            // 
            // btnFindID
            // 
            btnFindID.Location = new Point(363, 251);
            btnFindID.Name = "btnFindID";
            btnFindID.Size = new Size(75, 23);
            btnFindID.TabIndex = 3;
            btnFindID.Text = "ID 찾기";
            btnFindID.UseVisualStyleBackColor = true;
            btnFindID.Click += btnFindID_Click;
            // 
            // label1
            // 
            label1.AutoSize = true;
            label1.Location = new Point(327, 87);
            label1.Name = "label1";
            label1.Size = new Size(147, 15);
            label1.TabIndex = 1;
            label1.Text = "본인의 이름을 입력하세요";
            // 
            // panelFindID
            // 
            panelFindID.Controls.Add(label1);
            panelFindID.Controls.Add(btnFindID);
            panelFindID.Controls.Add(tbFindIDName);
            panelFindID.Controls.Add(label2);
            panelFindID.Controls.Add(tbFindIDEmaile);
            panelFindID.Location = new Point(0, 90);
            panelFindID.Name = "panelFindID";
            panelFindID.Size = new Size(800, 360);
            panelFindID.TabIndex = 4;
            // 
            // panelFindPass
            // 
            panelFindPass.Controls.Add(label5);
            panelFindPass.Controls.Add(tbFindPassID);
            panelFindPass.Controls.Add(label3);
            panelFindPass.Controls.Add(btnFindPass);
            panelFindPass.Controls.Add(tbFindPassName);
            panelFindPass.Controls.Add(label4);
            panelFindPass.Controls.Add(tbFindPassEmail);
            panelFindPass.Location = new Point(0, 90);
            panelFindPass.Name = "panelFindPass";
            panelFindPass.Size = new Size(800, 360);
            panelFindPass.TabIndex = 5;
            // 
            // label5
            // 
            label5.AutoSize = true;
            label5.Location = new Point(353, 137);
            label5.Name = "label5";
            label5.Size = new Size(95, 15);
            label5.TabIndex = 5;
            label5.Text = "ID를 입력하세요";
            // 
            // tbFindPassID
            // 
            tbFindPassID.Location = new Point(350, 165);
            tbFindPassID.Name = "tbFindPassID";
            tbFindPassID.Size = new Size(100, 23);
            tbFindPassID.TabIndex = 4;
            // 
            // btnPanelFIndPass
            // 
            btnPanelFIndPass.BackColor = Color.Transparent;
            btnPanelFIndPass.FlatStyle = FlatStyle.Flat;
            btnPanelFIndPass.Location = new Point(427, 37);
            btnPanelFIndPass.Margin = new Padding(0);
            btnPanelFIndPass.Name = "btnPanelFIndPass";
            btnPanelFIndPass.Size = new Size(129, 23);
            btnPanelFIndPass.TabIndex = 4;
            btnPanelFIndPass.Text = "비밀번호 찾기";
            btnPanelFIndPass.UseVisualStyleBackColor = false;
            btnPanelFIndPass.Click += btnPanelFIndPass_Click;
            // 
            // btnPanelFIndID
            // 
            btnPanelFIndID.BackColor = Color.Transparent;
            btnPanelFIndID.FlatStyle = FlatStyle.Flat;
            btnPanelFIndID.Location = new Point(245, 37);
            btnPanelFIndID.Margin = new Padding(0);
            btnPanelFIndID.Name = "btnPanelFIndID";
            btnPanelFIndID.Size = new Size(129, 23);
            btnPanelFIndID.TabIndex = 4;
            btnPanelFIndID.Text = "ID 찾기";
            btnPanelFIndID.UseVisualStyleBackColor = false;
            btnPanelFIndID.Click += btnPanelFIndID_Click;
            // 
            // FindIDForm
            // 
            AutoScaleDimensions = new SizeF(7F, 15F);
            AutoScaleMode = AutoScaleMode.Font;
            ClientSize = new Size(800, 450);
            Controls.Add(btnPanelFIndID);
            Controls.Add(btnPanelFIndPass);
            Controls.Add(panelFindPass);
            Controls.Add(panelFindID);
            FormBorderStyle = FormBorderStyle.FixedDialog;
            MaximizeBox = false;
            MinimizeBox = false;
            Name = "FindIDForm";
            StartPosition = FormStartPosition.CenterParent;
            Text = "계정 찾기";
            Load += FindIDForm_Load;
            panelFindID.ResumeLayout(false);
            panelFindID.PerformLayout();
            panelFindPass.ResumeLayout(false);
            panelFindPass.PerformLayout();
            ResumeLayout(false);
        }

        #endregion

        private TextBox tbFindPassEmail;
        private Label label4;
        private TextBox tbFindPassName;
        private Button btnFindPass;
        private Label label3;
        private TextBox tbFindIDEmaile;
        private Label label2;
        private TextBox tbFindIDName;
        private Button btnFindID;
        private Label label1;
        private Panel panelFindID;
        private Panel panelFindPass;
        private Button btnPanelFIndPass;
        private Button btnPanelFIndID;
        private Label label5;
        private TextBox tbFindPassID;
    }
}
