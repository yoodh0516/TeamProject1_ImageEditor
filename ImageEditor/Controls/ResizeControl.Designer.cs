namespace ImageEditor.Controls
{
    partial class ResizeControl
    {
        /// <summary> 
        /// 필수 디자이너 변수입니다.
        /// </summary>
        private System.ComponentModel.IContainer components = null;

        /// <summary> 
        /// 사용 중인 모든 리소스를 정리합니다.
        /// </summary>
        /// <param name="disposing">관리되는 리소스를 삭제해야 하면 true이고, 그렇지 않으면 false입니다.</param>
        protected override void Dispose(bool disposing)
        {
            if (disposing && (components != null))
            {
                components.Dispose();
            }
            base.Dispose(disposing);
        }

        #region 구성 요소 디자이너에서 생성한 코드

        /// <summary> 
        /// 디자이너 지원에 필요한 메서드입니다. 
        /// 이 메서드의 내용을 코드 편집기로 수정하지 마세요.
        /// </summary>
        private void InitializeComponent()
        {
            components = new System.ComponentModel.Container();
            lblWidth = new Label();
            txtWidth = new TextBox();
            lblWidthUnit = new Label();
            lblHeightUnit = new Label();
            txtHeight = new TextBox();
            lblHeight = new Label();
            chkKeepRatio = new CheckBox();
            btnApply = new Button();
            btnReset = new Button();
            lblCurrentSize = new Label();
            toolTip1 = new ToolTip(components);
            SuspendLayout();
            // 
            // lblWidth
            // 
            lblWidth.AutoSize = true;
            lblWidth.Location = new Point(42, 147);
            lblWidth.Name = "lblWidth";
            lblWidth.Size = new Size(31, 15);
            lblWidth.TabIndex = 0;
            lblWidth.Text = "가로";
            // 
            // txtWidth
            // 
            txtWidth.Location = new Point(42, 175);
            txtWidth.Name = "txtWidth";
            txtWidth.Size = new Size(100, 23);
            txtWidth.TabIndex = 1;
            toolTip1.SetToolTip(txtWidth, "변경할 이미지의 가로 크기를 픽셀 단위로 입력합니다.");
            // 
            // lblWidthUnit
            // 
            lblWidthUnit.AutoSize = true;
            lblWidthUnit.Location = new Point(148, 178);
            lblWidthUnit.Name = "lblWidthUnit";
            lblWidthUnit.Size = new Size(20, 15);
            lblWidthUnit.TabIndex = 2;
            lblWidthUnit.Text = "px";
            // 
            // lblHeightUnit
            // 
            lblHeightUnit.AutoSize = true;
            lblHeightUnit.Location = new Point(148, 267);
            lblHeightUnit.Name = "lblHeightUnit";
            lblHeightUnit.Size = new Size(20, 15);
            lblHeightUnit.TabIndex = 5;
            lblHeightUnit.Text = "px";
            // 
            // txtHeight
            // 
            txtHeight.Location = new Point(42, 264);
            txtHeight.Name = "txtHeight";
            txtHeight.Size = new Size(100, 23);
            txtHeight.TabIndex = 4;
            toolTip1.SetToolTip(txtHeight, "변경할 이미지의 세로 크기를 픽셀 단위로 입력합니다.");
            // 
            // lblHeight
            // 
            lblHeight.AutoSize = true;
            lblHeight.Location = new Point(42, 236);
            lblHeight.Name = "lblHeight";
            lblHeight.Size = new Size(31, 15);
            lblHeight.TabIndex = 3;
            lblHeight.Text = "세로";
            // 
            // chkKeepRatio
            // 
            chkKeepRatio.AutoSize = true;
            chkKeepRatio.Checked = true;
            chkKeepRatio.CheckState = CheckState.Checked;
            chkKeepRatio.Location = new Point(42, 348);
            chkKeepRatio.Name = "chkKeepRatio";
            chkKeepRatio.Size = new Size(106, 19);
            chkKeepRatio.TabIndex = 6;
            chkKeepRatio.Text = "원본 비율 유지";
            toolTip1.SetToolTip(chkKeepRatio, "가로 또는 세로를 변경할 때 원본 비율을 자동으로 유지합니다.");
            chkKeepRatio.UseVisualStyleBackColor = true;
            // 
            // btnApply
            // 
            btnApply.Location = new Point(148, 451);
            btnApply.Name = "btnApply";
            btnApply.Size = new Size(75, 23);
            btnApply.TabIndex = 7;
            btnApply.Text = "적용";
            toolTip1.SetToolTip(btnApply, "입력한 크기로 이미지를 변경합니다.");
            btnApply.UseVisualStyleBackColor = true;
            btnApply.Click += btnApply_Click;
            // 
            // btnReset
            // 
            btnReset.Location = new Point(42, 451);
            btnReset.Name = "btnReset";
            btnReset.Size = new Size(75, 23);
            btnReset.TabIndex = 8;
            btnReset.Text = "초기화";
            btnReset.UseVisualStyleBackColor = true;
            btnReset.Click += btnReset_Click;
            // 
            // lblCurrentSize
            // 
            lblCurrentSize.AutoSize = true;
            lblCurrentSize.Location = new Point(30, 83);
            lblCurrentSize.Name = "lblCurrentSize";
            lblCurrentSize.Size = new Size(75, 15);
            lblCurrentSize.TabIndex = 9;
            lblCurrentSize.Text = "현재 크기 : -";
            // 
            // toolTip1
            // 
            toolTip1.AutoPopDelay = 5000;
            toolTip1.InitialDelay = 300;
            toolTip1.ReshowDelay = 100;
            toolTip1.ShowAlways = true;
            EditorToolTipService.Configure(toolTip1);
            // 
            // ResizeControl
            // 
            AutoScaleDimensions = new SizeF(7F, 15F);
            AutoScaleMode = AutoScaleMode.Font;
            Controls.Add(lblCurrentSize);
            Controls.Add(btnReset);
            Controls.Add(btnApply);
            Controls.Add(chkKeepRatio);
            Controls.Add(lblHeightUnit);
            Controls.Add(txtHeight);
            Controls.Add(lblHeight);
            Controls.Add(lblWidthUnit);
            Controls.Add(txtWidth);
            Controls.Add(lblWidth);
            Name = "ResizeControl";
            Size = new Size(280, 518);
            ResumeLayout(false);
            PerformLayout();
        }

        #endregion

        private Label lblWidth;
        private TextBox txtWidth;
        private Label lblWidthUnit;
        private Label lblHeightUnit;
        private TextBox txtHeight;
        private Label lblHeight;
        private CheckBox chkKeepRatio;
        private Button btnApply;
        private Button btnReset;
        private Label lblCurrentSize;
        private ToolTip toolTip1;
    }
}
