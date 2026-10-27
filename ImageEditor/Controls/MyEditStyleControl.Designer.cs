namespace ImageEditor.Controls
{
    partial class MyEditStyleControl
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
            lblGuide = new Label();
            lbMyEditStyle = new ListBox();
            lblStatus = new Label();
            tbSaveMyEditStyleName = new TextBox();
            btnLoad = new Button();
            btnSaveMyEditStyle = new Button();
            btnDelete = new Button();
            SuspendLayout();
            // 
            // lblTitle
            // 
            lblTitle.AutoSize = true;
            lblTitle.Font = new Font("맑은 고딕", 11F, FontStyle.Bold);
            lblTitle.Location = new Point(12, 12);
            lblTitle.Name = "lblTitle";
            lblTitle.Size = new Size(154, 20);
            lblTitle.TabIndex = 0;
            lblTitle.Text = "나만의 편집 스타일";
            // 
            // lblGuide
            // 
            lblGuide.Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right;
            lblGuide.ForeColor = Color.DimGray;
            lblGuide.Location = new Point(12, 38);
            lblGuide.Name = "lblGuide";
            lblGuide.Size = new Size(256, 34);
            lblGuide.TabIndex = 1;
            lblGuide.Text = "현재 밝기·대비·채도·색온도를 저장하고 다시 불러옵니다.";
            // 
            // lbMyEditStyle
            // 
            lbMyEditStyle.Anchor = AnchorStyles.Top | AnchorStyles.Bottom | AnchorStyles.Left | AnchorStyles.Right;
            lbMyEditStyle.FormattingEnabled = true;
            lbMyEditStyle.ItemHeight = 15;
            lbMyEditStyle.Location = new Point(12, 76);
            lbMyEditStyle.Name = "lbMyEditStyle";
            lbMyEditStyle.Size = new Size(256, 319);
            lbMyEditStyle.TabIndex = 2;
            lbMyEditStyle.DoubleClick += btnLoad_Click;
            // 
            // lblStatus
            // 
            lblStatus.Anchor = AnchorStyles.Bottom | AnchorStyles.Left | AnchorStyles.Right;
            lblStatus.ForeColor = Color.DimGray;
            lblStatus.Location = new Point(12, 401);
            lblStatus.Name = "lblStatus";
            lblStatus.Size = new Size(256, 18);
            lblStatus.TabIndex = 3;
            lblStatus.Text = "저장된 편집 스타일을 불러옵니다.";
            // 
            // tbSaveMyEditStyleName
            // 
            tbSaveMyEditStyleName.Anchor = AnchorStyles.Bottom | AnchorStyles.Left | AnchorStyles.Right;
            tbSaveMyEditStyleName.Location = new Point(12, 423);
            tbSaveMyEditStyleName.MaxLength = 100;
            tbSaveMyEditStyleName.Name = "tbSaveMyEditStyleName";
            tbSaveMyEditStyleName.PlaceholderText = "저장할 스타일 이름";
            tbSaveMyEditStyleName.Size = new Size(256, 23);
            tbSaveMyEditStyleName.TabIndex = 4;
            // 
            // btnLoad
            // 
            btnLoad.Anchor = AnchorStyles.Bottom | AnchorStyles.Left;
            btnLoad.Location = new Point(12, 454);
            btnLoad.Name = "btnLoad";
            btnLoad.Size = new Size(78, 30);
            btnLoad.TabIndex = 5;
            btnLoad.Text = "불러오기";
            btnLoad.UseVisualStyleBackColor = true;
            btnLoad.Click += btnLoad_Click;
            // 
            // btnSaveMyEditStyle
            // 
            btnSaveMyEditStyle.Anchor = AnchorStyles.Bottom | AnchorStyles.Left;
            btnSaveMyEditStyle.Location = new Point(96, 454);
            btnSaveMyEditStyle.Name = "btnSaveMyEditStyle";
            btnSaveMyEditStyle.Size = new Size(78, 30);
            btnSaveMyEditStyle.TabIndex = 6;
            btnSaveMyEditStyle.Text = "저장";
            btnSaveMyEditStyle.UseVisualStyleBackColor = true;
            btnSaveMyEditStyle.Click += btnSaveMyEditStyle_Click;
            // 
            // btnDelete
            // 
            btnDelete.Anchor = AnchorStyles.Bottom | AnchorStyles.Right;
            btnDelete.Location = new Point(190, 454);
            btnDelete.Name = "btnDelete";
            btnDelete.Size = new Size(78, 30);
            btnDelete.TabIndex = 7;
            btnDelete.Text = "삭제";
            btnDelete.UseVisualStyleBackColor = true;
            btnDelete.Click += btnDelete_Click;
            // 
            // MyEditStyleControl
            // 
            AutoScaleDimensions = new SizeF(7F, 15F);
            AutoScaleMode = AutoScaleMode.Font;
            Controls.Add(btnDelete);
            Controls.Add(btnSaveMyEditStyle);
            Controls.Add(btnLoad);
            Controls.Add(tbSaveMyEditStyleName);
            Controls.Add(lblStatus);
            Controls.Add(lbMyEditStyle);
            Controls.Add(lblGuide);
            Controls.Add(lblTitle);
            MinimumSize = new Size(220, 360);
            Name = "MyEditStyleControl";
            Size = new Size(280, 496);
            ResumeLayout(false);
            PerformLayout();
        }

        private Label lblTitle = null!;
        private Label lblGuide = null!;
        private ListBox lbMyEditStyle = null!;
        private Label lblStatus = null!;
        private TextBox tbSaveMyEditStyleName = null!;
        private Button btnLoad = null!;
        private Button btnSaveMyEditStyle = null!;
        private Button btnDelete = null!;
    }
}
