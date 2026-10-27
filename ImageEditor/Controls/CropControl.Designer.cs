namespace ImageEditor.Controls
{
    partial class CropControl
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
            lblSelectionTitle = new Label();
            lblSelectionSize = new Label();
            btnApply = new Button();
            btnReset = new Button();
            btnUndoCrop = new Button();
            toolTip1 = new ToolTip(components);
            SuspendLayout();
            // 
            // lblSelectionTitle
            // 
            lblSelectionTitle.AutoSize = true;
            lblSelectionTitle.Location = new Point(54, 147);
            lblSelectionTitle.Name = "lblSelectionTitle";
            lblSelectionTitle.Size = new Size(55, 15);
            lblSelectionTitle.TabIndex = 0;
            lblSelectionTitle.Text = "선택영역";
            // 
            // lblSelectionSize
            // 
            lblSelectionSize.AutoSize = true;
            lblSelectionSize.Location = new Point(54, 202);
            lblSelectionSize.Name = "lblSelectionSize";
            lblSelectionSize.Size = new Size(71, 15);
            lblSelectionSize.TabIndex = 1;
            lblSelectionSize.Text = "선택 영역: -";
            // 
            // btnApply
            // 
            btnApply.Location = new Point(160, 375);
            btnApply.Name = "btnApply";
            btnApply.Size = new Size(94, 23);
            btnApply.TabIndex = 2;
            btnApply.Text = "적용";
            toolTip1.SetToolTip(btnApply, "선택한 영역만 남기고 이미지를 자릅니다.");
            btnApply.UseVisualStyleBackColor = true;
            btnApply.Click += btnApply_Click;
            // 
            // btnReset
            // 
            btnReset.Location = new Point(35, 375);
            btnReset.Name = "btnReset";
            btnReset.Size = new Size(90, 23);
            btnReset.TabIndex = 3;
            btnReset.Text = "다시 자르기";
            toolTip1.SetToolTip(btnReset, "현재 자르기 선택 영역을 초기화하고 다시 선택합니다.");
            btnReset.UseVisualStyleBackColor = true;
            btnReset.Click += btnReset_Click;
            // 
            // btnUndoCrop
            // 
            btnUndoCrop.Location = new Point(92, 414);
            btnUndoCrop.Name = "btnUndoCrop";
            btnUndoCrop.Size = new Size(92, 23);
            btnUndoCrop.TabIndex = 4;
            btnUndoCrop.Text = "이전으로";
            toolTip1.SetToolTip(btnUndoCrop, "방금 적용한 자르기 작업을 되돌립니다.");
            btnUndoCrop.UseVisualStyleBackColor = true;
            btnUndoCrop.Click += btnUndoCrop_Click;
            // 
            // toolTip1
            // 
            toolTip1.AutoPopDelay = 5000;
            toolTip1.InitialDelay = 300;
            toolTip1.ReshowDelay = 100;
            EditorToolTipService.Configure(toolTip1);
            // 
            // CropControl
            // 
            AutoScaleDimensions = new SizeF(7F, 15F);
            AutoScaleMode = AutoScaleMode.Font;
            Controls.Add(btnUndoCrop);
            Controls.Add(btnReset);
            Controls.Add(btnApply);
            Controls.Add(lblSelectionSize);
            Controls.Add(lblSelectionTitle);
            Name = "CropControl";
            Size = new Size(280, 518);
            ResumeLayout(false);
            PerformLayout();
        }

        #endregion

        private Label lblSelectionTitle;
        private Label lblSelectionSize;
        private Button btnApply;
        private Button btnReset;
        private Button btnUndoCrop;
        private ToolTip toolTip1;
    }
}
