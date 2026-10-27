namespace ImageEditor.Controls
{
    partial class SnsRatioControl
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
            rdoSquare = new RadioButton();
            rdoPortrait = new RadioButton();
            rdoVertical = new RadioButton();
            rdoLandscape = new RadioButton();
            rdoWide = new RadioButton();
            btnApply = new Button();
            toolTip1 = new ToolTip(components);
            SuspendLayout();
            // 
            // rdoSquare
            // 
            rdoSquare.AutoSize = true;
            rdoSquare.Location = new Point(64, 127);
            rdoSquare.Name = "rdoSquare";
            rdoSquare.Size = new Size(102, 19);
            rdoSquare.TabIndex = 0;
            rdoSquare.TabStop = true;
            rdoSquare.Text = "정사각형 (1:1)";
            toolTip1.SetToolTip(rdoSquare, "인스타그램 정사각형 피드와 프로필 이미지 등에 적합한 1:1 비율입니다.");
            rdoSquare.UseVisualStyleBackColor = true;
            // 
            // rdoPortrait
            // 
            rdoPortrait.AutoSize = true;
            rdoPortrait.Location = new Point(64, 174);
            rdoPortrait.Name = "rdoPortrait";
            rdoPortrait.Size = new Size(90, 19);
            rdoPortrait.TabIndex = 0;
            rdoPortrait.TabStop = true;
            rdoPortrait.Text = "세로형 (4:5)\n";
            toolTip1.SetToolTip(rdoPortrait, "인스타그램 세로형 피드에 적합한 4:5 비율입니다.");
            rdoPortrait.UseVisualStyleBackColor = true;
            // 
            // rdoVertical
            // 
            rdoVertical.AutoSize = true;
            rdoVertical.Location = new Point(64, 226);
            rdoVertical.Name = "rdoVertical";
            rdoVertical.Size = new Size(137, 19);
            rdoVertical.TabIndex = 0;
            rdoVertical.TabStop = true;
            rdoVertical.Text = "세로 전체화면 (9:16)";
            toolTip1.SetToolTip(rdoVertical, "인스타그램 스토리·릴스 등 세로형 콘텐츠에 적합한 9:16 비율입니다.");
            rdoVertical.UseVisualStyleBackColor = true;
            // 
            // rdoLandscape
            // 
            rdoLandscape.AutoSize = true;
            rdoLandscape.Location = new Point(64, 273);
            rdoLandscape.Name = "rdoLandscape";
            rdoLandscape.Size = new Size(97, 19);
            rdoLandscape.TabIndex = 0;
            rdoLandscape.TabStop = true;
            rdoLandscape.Text = "가로형 (16:9)";
            toolTip1.SetToolTip(rdoLandscape, "유튜브 썸네일과 가로형 콘텐츠에 적합한 16:9 비율입니다.");
            rdoLandscape.UseVisualStyleBackColor = true;
            // 
            // rdoWide
            // 
            rdoWide.AutoSize = true;
            rdoWide.Location = new Point(64, 321);
            rdoWide.Name = "rdoWide";
            rdoWide.Size = new Size(119, 19);
            rdoWide.TabIndex = 0;
            rdoWide.TabStop = true;
            rdoWide.Text = "와이드형 (1.91:1)";
            toolTip1.SetToolTip(rdoWide, "웹이나 블로그 대표 이미지에 적합한 1.91:1 비율입니다.");
            rdoWide.UseVisualStyleBackColor = true;
            // 
            // btnApply
            // 
            btnApply.Location = new Point(153, 451);
            btnApply.Name = "btnApply";
            btnApply.Size = new Size(75, 23);
            btnApply.TabIndex = 2;
            btnApply.Text = "적용";
            toolTip1.SetToolTip(btnApply, "선택한 비율로 현재 이미지를 변경합니다.");
            btnApply.UseVisualStyleBackColor = true;
            btnApply.Click += btnApply_Click;
            // 
            // toolTip1
            // 
            toolTip1.AutoPopDelay = 5000;
            toolTip1.InitialDelay = 300;
            toolTip1.ReshowDelay = 100;
            EditorToolTipService.Configure(toolTip1);
            // 
            // SnsRatioControl
            // 
            AutoScaleDimensions = new SizeF(7F, 15F);
            AutoScaleMode = AutoScaleMode.Font;
            Controls.Add(btnApply);
            Controls.Add(rdoWide);
            Controls.Add(rdoLandscape);
            Controls.Add(rdoVertical);
            Controls.Add(rdoPortrait);
            Controls.Add(rdoSquare);
            Name = "SnsRatioControl";
            Size = new Size(280, 518);
            ResumeLayout(false);
            PerformLayout();
        }

        #endregion

        private RadioButton rdoSquare;
        private RadioButton rdoPortrait;
        private RadioButton rdoVertical;
        private RadioButton rdoLandscape;
        private RadioButton rdoWide;
        private Button btnApply;
        private ToolTip toolTip1;
    }
}
