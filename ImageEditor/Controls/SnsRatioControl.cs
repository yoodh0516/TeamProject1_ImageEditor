using System;
using System.Drawing;
using System.Windows.Forms;

namespace ImageEditor.Controls
{
    public partial class SnsRatioControl : UserControl
    {
        public event Action<int, int>? RatioRequested;
        public event Action<int, int>? PreviewRequested;
        public event Action? CancelRequested;
        public event Action? ResetRequested;

        private readonly Button btnCancel;
        private readonly Button btnReset;
        private readonly Label lblExpectedSize;
        private readonly Panel[] ratioCards;
        private bool resetting;

        public SnsRatioControl()
        {
            InitializeComponent();
            btnCancel = ToolOptionStyle.CreateSecondaryButton("취소");
            btnCancel.Click += (_, _) => CancelRequested?.Invoke();
            btnReset = ToolOptionStyle.CreateSecondaryButton("초기화");
            btnReset.Click += (_, _) => ResetSelection();
            lblExpectedSize = ToolOptionStyle.CreateLabel("비율을 선택하면 예상 결과 크기가 표시됩니다.", 9F, FontStyle.Bold, ToolOptionStyle.Accent);
            ratioCards = new Panel[5];
            BuildCardLayout();
            rdoSquare.CheckedChanged += Ratio_CheckedChanged;
            rdoPortrait.CheckedChanged += Ratio_CheckedChanged;
            rdoVertical.CheckedChanged += Ratio_CheckedChanged;
            rdoLandscape.CheckedChanged += Ratio_CheckedChanged;
            rdoWide.CheckedChanged += Ratio_CheckedChanged;
            btnApply.Enabled = false;
        }

        private void BuildCardLayout()
        {
            Controls.Clear();
            BackColor = ToolOptionStyle.PanelBackground;
            AutoScroll = true;
            FlowLayoutPanel root = new FlowLayoutPanel
            {
                Dock = DockStyle.Fill,
                FlowDirection = FlowDirection.TopDown,
                WrapContents = false,
                AutoScroll = true,
                Padding = new Padding(12, 16, 12, 10),
                BackColor = ToolOptionStyle.PanelBackground
            };
            ToolOptionStyle.ConfigureResponsiveFlow(root);

            Panel heading = new Panel { Height = 74, Margin = new Padding(0, 0, 0, 6) };
            Label title = ToolOptionStyle.CreateLabel("비율 변환", 16F, FontStyle.Bold, ToolOptionStyle.Title);
            Label description = ToolOptionStyle.CreateWrappedLabel(
                "SNS와 웹 규격에 맞게 이미지 비율을 변환합니다.",
                9F, FontStyle.Regular, ToolOptionStyle.Description, 40);
            title.Location = Point.Empty;
            description.Location = new Point(1, 30);
            ToolOptionStyle.FillCardControl(heading, description, 1, 0);
            heading.Controls.Add(title);
            heading.Controls.Add(description);
            root.Controls.Add(heading);
            Label ratioHeading = ToolOptionStyle.CreateLabel("사용할 비율", 10F, FontStyle.Bold, ToolOptionStyle.Title);
            ratioHeading.Margin = new Padding(2, 0, 2, 6);
            root.Controls.Add(ratioHeading);

            ratioCards[0] = CreateRatioCard(rdoSquare, "정사각형   1:1", "Instagram 피드 · 프로필");
            ratioCards[1] = CreateRatioCard(rdoPortrait, "세로 피드   4:5", "Instagram 세로형 게시물");
            ratioCards[2] = CreateRatioCard(rdoVertical, "세로 전체화면   9:16", "Story · Reels · TikTok");
            ratioCards[3] = CreateRatioCard(rdoLandscape, "가로 영상   16:9", "YouTube · 발표 자료");
            ratioCards[4] = CreateRatioCard(rdoWide, "와이드   1.91:1", "웹 배너 · 블로그 대표");
            foreach (Panel card in ratioCards) root.Controls.Add(card);

            Panel resultCard = ToolOptionStyle.CreateCard(68);
            lblExpectedSize.Location = new Point(14, 16);
            lblExpectedSize.AutoSize = false;
            lblExpectedSize.Height = 38;
            ToolOptionStyle.FillCardControl(resultCard, lblExpectedSize);
            resultCard.Controls.Add(lblExpectedSize);
            root.Controls.Add(resultCard);

            Label note = ToolOptionStyle.CreateLabel("현재 버전은 중앙 기준으로 자르며, 원본 이미지는 적용 전까지 유지됩니다.", 8.5F, FontStyle.Regular, ToolOptionStyle.Description);
            note.AutoSize = false;
            note.Height = 34;
            note.Margin = new Padding(4, 0, 4, 8);
            root.Controls.Add(note);

            TableLayoutPanel actions = new TableLayoutPanel { Height = 38, ColumnCount = 3, Margin = new Padding(0, 0, 0, 12) };
            actions.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 33.333F));
            actions.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 33.333F));
            actions.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 33.333F));
            ToolOptionStyle.StylePrimaryButton(btnApply);
            btnApply.Text = "적용";
            actions.Controls.Add(btnReset, 0, 0);
            actions.Controls.Add(btnCancel, 1, 0);
            actions.Controls.Add(btnApply, 2, 0);
            root.Controls.Add(actions);
            Controls.Add(root);
        }

        private Panel CreateRatioCard(RadioButton radio, string title, string description)
        {
            Panel card = ToolOptionStyle.CreateCard(55);
            card.Cursor = Cursors.Hand;
            radio.AutoSize = true;
            radio.Text = title;
            radio.Font = new Font("맑은 고딕", 9F, FontStyle.Bold);
            radio.Location = new Point(12, 8);
            Label platform = ToolOptionStyle.CreateLabel(description, 8.5F, FontStyle.Regular, ToolOptionStyle.Description);
            platform.Location = new Point(33, 29);
            card.Click += (_, _) => radio.Checked = true;
            platform.Click += (_, _) => radio.Checked = true;
            card.Controls.Add(radio);
            card.Controls.Add(platform);
            return card;
        }

        private void btnApply_Click(object sender, EventArgs e)
        {
            if (rdoSquare.Checked) RatioRequested?.Invoke(1, 1);
            else if (rdoPortrait.Checked) RatioRequested?.Invoke(4, 5);
            else if (rdoVertical.Checked) RatioRequested?.Invoke(9, 16);
            else if (rdoLandscape.Checked) RatioRequested?.Invoke(16, 9);
            else if (rdoWide.Checked) RatioRequested?.Invoke(191, 100);
        }

        private void RaisePreviewForCheckedRatio()
        {
            if (resetting) return;
            int width = 0;
            int height = 0;
            if (rdoSquare.Checked) { width = 1; height = 1; }
            else if (rdoPortrait.Checked) { width = 4; height = 5; }
            else if (rdoVertical.Checked) { width = 9; height = 16; }
            else if (rdoLandscape.Checked) { width = 16; height = 9; }
            else if (rdoWide.Checked) { width = 191; height = 100; }
            if (width == 0) return;
            PreviewRequested?.Invoke(width, height);
            btnApply.Enabled = true;
            UpdateCardSelection();
        }

        private void Ratio_CheckedChanged(object? sender, EventArgs e)
        {
            if (resetting || sender is not RadioButton selected || !selected.Checked)
                return;

            resetting = true;
            foreach (RadioButton radio in new[] { rdoSquare, rdoPortrait, rdoVertical, rdoLandscape, rdoWide })
            {
                if (!ReferenceEquals(radio, selected))
                    radio.Checked = false;
            }
            resetting = false;
            RaisePreviewForCheckedRatio();
        }

        private void UpdateCardSelection()
        {
            RadioButton[] radios = { rdoSquare, rdoPortrait, rdoVertical, rdoLandscape, rdoWide };
            for (int i = 0; i < ratioCards.Length; i++)
            {
                bool selected = radios[i].Checked;
                ratioCards[i].BackColor = selected ? ToolOptionStyle.AccentSoft : Color.White;
                ratioCards[i].BorderStyle = BorderStyle.FixedSingle;
                radios[i].ForeColor = selected ? ToolOptionStyle.Accent : ToolOptionStyle.Title;
            }
        }

        public void SetPreviewResultSize(Size size)
        {
            lblExpectedSize.Text = "예상 결과  " + size.Width.ToString("N0") + " × " + size.Height.ToString("N0") + " px";
        }

        private void ResetSelection()
        {
            resetting = true;
            rdoSquare.Checked = rdoPortrait.Checked = rdoVertical.Checked = rdoLandscape.Checked = rdoWide.Checked = false;
            resetting = false;
            btnApply.Enabled = false;
            lblExpectedSize.Text = "비율을 선택하면 예상 결과 크기가 표시됩니다.";
            UpdateCardSelection();
            ResetRequested?.Invoke();
        }

        public void ResetSession()
        {
            ResetSelection();
        }
    }
}
