namespace ImageEditor.Controls
{
    partial class FilterControl
    {
        private System.ComponentModel.IContainer components = null;
        protected override void Dispose(bool disposing) { if (disposing && (components != null)) components.Dispose(); base.Dispose(disposing); }
        #region Windows Form Designer generated code
        private void InitializeComponent()
        {
            label1 = new Label();
            lblSubTitle = new Label();
            btnVivid = new Button();
            btnBright = new Button();
            btnCozy = new Button();
            btnElegant = new Button();
            btnBlackWhite = new Button();
            btnCool = new Button();
            SuspendLayout();
            // label1
            label1.AutoSize = true; label1.Font = new Font("맑은 고딕", 14F, FontStyle.Bold); label1.ForeColor = Color.FromArgb(38, 38, 46); label1.Location = new Point(18, 17); label1.Name = "label1"; label1.Size = new Size(45, 25); label1.TabIndex = 0; label1.Text = "필터";
            // lblSubTitle
            lblSubTitle.AutoSize = true; lblSubTitle.Font = new Font("맑은 고딕", 8.5F); lblSubTitle.ForeColor = Color.FromArgb(120, 120, 132); lblSubTitle.Location = new Point(19, 49); lblSubTitle.Name = "lblSubTitle"; lblSubTitle.Size = new Size(197, 15); lblSubTitle.TabIndex = 1; lblSubTitle.Text = "한 번의 클릭으로 분위기를 바꿔보세요";
            // btnVivid
            btnVivid.BackColor = Color.White; btnVivid.Cursor = Cursors.Hand; btnVivid.FlatAppearance.BorderColor = Color.FromArgb(224, 224, 232); btnVivid.FlatAppearance.MouseDownBackColor = Color.FromArgb(232, 227, 255); btnVivid.FlatAppearance.MouseOverBackColor = Color.FromArgb(244, 241, 255); btnVivid.FlatStyle = FlatStyle.Flat; btnVivid.Font = new Font("맑은 고딕", 9F, FontStyle.Bold); btnVivid.ForeColor = Color.FromArgb(70, 64, 90); btnVivid.Location = new Point(18, 84); btnVivid.Name = "btnVivid"; btnVivid.Size = new Size(108, 78); btnVivid.TabIndex = 2; btnVivid.Text = "✦\r\n화사하게"; btnVivid.UseVisualStyleBackColor = false; btnVivid.Click += btnVivid_Click;
            // btnBright
            btnBright.BackColor = Color.White; btnBright.Cursor = Cursors.Hand; btnBright.FlatAppearance.BorderColor = Color.FromArgb(224, 224, 232); btnBright.FlatAppearance.MouseDownBackColor = Color.FromArgb(232, 227, 255); btnBright.FlatAppearance.MouseOverBackColor = Color.FromArgb(244, 241, 255); btnBright.FlatStyle = FlatStyle.Flat; btnBright.Font = new Font("맑은 고딕", 9F, FontStyle.Bold); btnBright.ForeColor = Color.FromArgb(70, 64, 90); btnBright.Location = new Point(138, 84); btnBright.Name = "btnBright"; btnBright.Size = new Size(108, 78); btnBright.TabIndex = 3; btnBright.Text = "☀\r\n환하게"; btnBright.UseVisualStyleBackColor = false; btnBright.Click += btnBright_Click;
            // btnCozy
            btnCozy.BackColor = Color.White; btnCozy.Cursor = Cursors.Hand; btnCozy.FlatAppearance.BorderColor = Color.FromArgb(224, 224, 232); btnCozy.FlatAppearance.MouseDownBackColor = Color.FromArgb(232, 227, 255); btnCozy.FlatAppearance.MouseOverBackColor = Color.FromArgb(244, 241, 255); btnCozy.FlatStyle = FlatStyle.Flat; btnCozy.Font = new Font("맑은 고딕", 9F, FontStyle.Bold); btnCozy.ForeColor = Color.FromArgb(70, 64, 90); btnCozy.Location = new Point(18, 174); btnCozy.Name = "btnCozy"; btnCozy.Size = new Size(108, 78); btnCozy.TabIndex = 4; btnCozy.Text = "☕\r\n아늑하게"; btnCozy.UseVisualStyleBackColor = false; btnCozy.Click += btnCozy_Click;
            // btnElegant
            btnElegant.BackColor = Color.White; btnElegant.Cursor = Cursors.Hand; btnElegant.FlatAppearance.BorderColor = Color.FromArgb(224, 224, 232); btnElegant.FlatAppearance.MouseDownBackColor = Color.FromArgb(232, 227, 255); btnElegant.FlatAppearance.MouseOverBackColor = Color.FromArgb(244, 241, 255); btnElegant.FlatStyle = FlatStyle.Flat; btnElegant.Font = new Font("맑은 고딕", 9F, FontStyle.Bold); btnElegant.ForeColor = Color.FromArgb(70, 64, 90); btnElegant.Location = new Point(138, 174); btnElegant.Name = "btnElegant"; btnElegant.Size = new Size(108, 78); btnElegant.TabIndex = 5; btnElegant.Text = "◇\r\n우아하게"; btnElegant.UseVisualStyleBackColor = false; btnElegant.Click += btnElegant_Click;
            // btnBlackWhite
            btnBlackWhite.BackColor = Color.White; btnBlackWhite.Cursor = Cursors.Hand; btnBlackWhite.FlatAppearance.BorderColor = Color.FromArgb(224, 224, 232); btnBlackWhite.FlatAppearance.MouseDownBackColor = Color.FromArgb(232, 227, 255); btnBlackWhite.FlatAppearance.MouseOverBackColor = Color.FromArgb(244, 241, 255); btnBlackWhite.FlatStyle = FlatStyle.Flat; btnBlackWhite.Font = new Font("맑은 고딕", 9F, FontStyle.Bold); btnBlackWhite.ForeColor = Color.FromArgb(70, 64, 90); btnBlackWhite.Location = new Point(18, 264); btnBlackWhite.Name = "btnBlackWhite"; btnBlackWhite.Size = new Size(108, 78); btnBlackWhite.TabIndex = 6; btnBlackWhite.Text = "◐\r\n흑백"; btnBlackWhite.UseVisualStyleBackColor = false; btnBlackWhite.Click += btnBlackWhite_Click;
            // btnCool
            btnCool.BackColor = Color.White; btnCool.Cursor = Cursors.Hand; btnCool.FlatAppearance.BorderColor = Color.FromArgb(224, 224, 232); btnCool.FlatAppearance.MouseDownBackColor = Color.FromArgb(232, 227, 255); btnCool.FlatAppearance.MouseOverBackColor = Color.FromArgb(244, 241, 255); btnCool.FlatStyle = FlatStyle.Flat; btnCool.Font = new Font("맑은 고딕", 9F, FontStyle.Bold); btnCool.ForeColor = Color.FromArgb(70, 64, 90); btnCool.Location = new Point(138, 264); btnCool.Name = "btnCool"; btnCool.Size = new Size(108, 78); btnCool.TabIndex = 7; btnCool.Text = "❄\r\n시원하게"; btnCool.UseVisualStyleBackColor = false; btnCool.Click += btnCool_Click;
            // FilterControl
            AutoScaleDimensions = new SizeF(7F, 15F); AutoScaleMode = AutoScaleMode.Font; BackColor = Color.FromArgb(247, 247, 250); Controls.Add(btnCool); Controls.Add(btnBlackWhite); Controls.Add(btnElegant); Controls.Add(btnCozy); Controls.Add(btnBright); Controls.Add(btnVivid); Controls.Add(lblSubTitle); Controls.Add(label1); Name = "FilterControl"; Size = new Size(264, 479); ResumeLayout(false); PerformLayout();
        }
        #endregion
        private Button btnVivid, btnBright, btnCozy, btnElegant, btnBlackWhite, btnCool;
        private Label label1, lblSubTitle;
    }
}

