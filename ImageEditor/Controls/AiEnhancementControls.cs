using System;
using System.Drawing;
using System.Windows.Forms;

namespace ImageEditor.Controls
{
    public enum AiQualityModel
    {
        RealEsrgan,
        AnimeVideo
    }

    public enum PortraitEnhancementStyle
    {
        Natural,
        SkinDetail,
        ToneAndExposure
    }

    public sealed class AiQualityEnhancementControl : UserControl
    {
        private readonly Label imageInfo;
        private readonly Label status;
        private readonly Button realEsrganButton;
        private readonly Button animeVideoButton;
        private readonly Button runButton;
        private readonly Button cancelButton;
        private readonly ProgressBar progressBar;

        public AiQualityModel SelectedModel { get; private set; } =
            AiQualityModel.RealEsrgan;

        public event EventHandler? RunRequested;
        public event EventHandler? CancelRequested;
        public event EventHandler? CloseRequested;

        public AiQualityEnhancementControl()
        {
            BackColor = ToolOptionStyle.PanelBackground;
            AutoScroll = true;

            FlowLayoutPanel root = CreateRoot();
            root.Controls.Add(CreateHeading(
                "AI 화질 개선",
                "원본 크기는 유지한 채 선명도와 세부 묘사를 보정합니다."));

            Panel imageCard = ToolOptionStyle.CreateCard(68);
            Label imageTitle = ToolOptionStyle.CreateLabel(
                "현재 이미지", 9F, FontStyle.Regular, ToolOptionStyle.Description);
            imageTitle.Location = new Point(14, 10);
            imageInfo = ToolOptionStyle.CreateLabel(
                "이미지를 불러와 주세요.", 10F, FontStyle.Bold, ToolOptionStyle.Accent);
            imageInfo.Location = new Point(14, 33);
            imageCard.Controls.Add(imageTitle);
            imageCard.Controls.Add(imageInfo);

            Panel modelCard = ToolOptionStyle.CreateCard(173);
            Label modelTitle = ToolOptionStyle.CreateLabel(
                "화질 개선 모델", 10F, FontStyle.Bold, ToolOptionStyle.Title);
            modelTitle.Location = new Point(14, 11);
            realEsrganButton = CreateModelButton(
                "Real-ESRGAN", "사진·풍경의 질감과 가장자리 보정", 41);
            animeVideoButton = CreateModelButton(
                "AnimeVideo", "일러스트·애니 이미지에 빠른 보정", 92);
            realEsrganButton.Click += (_, _) => SelectModel(AiQualityModel.RealEsrgan);
            animeVideoButton.Click += (_, _) => SelectModel(AiQualityModel.AnimeVideo);
            modelCard.Resize += (_, _) =>
            {
                int width = Math.Max(1, modelCard.ClientSize.Width - 28);
                realEsrganButton.Width = width;
                animeVideoButton.Width = width;
            };
            modelCard.Controls.Add(modelTitle);
            modelCard.Controls.Add(realEsrganButton);
            modelCard.Controls.Add(animeVideoButton);

            Panel optionCard = ToolOptionStyle.CreateCard(92);
            Label optionTitle = ToolOptionStyle.CreateLabel(
                "적용 방식", 10F, FontStyle.Bold, ToolOptionStyle.Title);
            optionTitle.Location = new Point(14, 11);
            Label optionText = ToolOptionStyle.CreateLabel(
                "• 원본 해상도와 캔버스 크기를 유지합니다.\n• 결과 확인 후 적용할 때만 실제 이미지가 바뀝니다.",
                8.5F,
                FontStyle.Regular,
                ToolOptionStyle.Description);
            optionText.Location = new Point(14, 35);
            optionText.AutoSize = false;
            optionText.Height = 46;
            ToolOptionStyle.FillCardControl(optionCard, optionText);
            optionCard.Controls.Add(optionTitle);
            optionCard.Controls.Add(optionText);

            Label note = ToolOptionStyle.CreateLabel(
                "고해상도 사진은 처리 시간이 길어질 수 있습니다.",
                8.5F,
                FontStyle.Regular,
                ToolOptionStyle.Description);
            note.AutoSize = false;
            note.Height = 38;
            note.Margin = new Padding(3, 0, 3, 6);

            TableLayoutPanel actions = CreateActions(out runButton, out cancelButton);
            runButton.Text = "AI 화질 개선 시작";
            runButton.Click += (_, _) => RunRequested?.Invoke(this, EventArgs.Empty);
            cancelButton.Click += (_, _) =>
            {
                if (runButton.Enabled)
                    CloseRequested?.Invoke(this, EventArgs.Empty);
                else
                    CancelRequested?.Invoke(this, EventArgs.Empty);
            };

            progressBar = CreateProgressBar();
            status = CreateStatusLabel("모델을 선택한 뒤 실행하세요.");

            root.Controls.Add(imageCard);
            root.Controls.Add(modelCard);
            root.Controls.Add(optionCard);
            root.Controls.Add(note);
            root.Controls.Add(actions);
            root.Controls.Add(progressBar);
            root.Controls.Add(status);
            Controls.Add(root);
            SelectModel(SelectedModel);
        }

        public void PrepareForImage(Size imageSize)
        {
            imageInfo.Text = $"{imageSize.Width:N0} × {imageSize.Height:N0} px";
            runButton.Enabled = true;
            SetStatus("모델을 선택한 뒤 실행하세요.");
        }

        public void SetBusy(bool busy)
        {
            realEsrganButton.Enabled = !busy;
            animeVideoButton.Enabled = !busy;
            runButton.Enabled = !busy;
            runButton.Text = busy ? "AI 화질 개선 중..." : "AI 화질 개선 시작";
            cancelButton.Text = busy ? "작업 취소" : "취소";
            progressBar.Visible = busy;
        }

        public void SetStatus(string message, bool isError = false)
        {
            status.Text = message;
            status.ForeColor = isError
                ? Color.FromArgb(220, 38, 38)
                : ToolOptionStyle.Description;
        }

        private void SelectModel(AiQualityModel model)
        {
            SelectedModel = model;
            StyleModelButton(realEsrganButton, model == AiQualityModel.RealEsrgan);
            StyleModelButton(animeVideoButton, model == AiQualityModel.AnimeVideo);
        }

        internal static FlowLayoutPanel CreateRoot()
        {
            FlowLayoutPanel root = new FlowLayoutPanel
            {
                Dock = DockStyle.Fill,
                FlowDirection = FlowDirection.TopDown,
                WrapContents = false,
                AutoScroll = true,
                BackColor = ToolOptionStyle.PanelBackground,
                Padding = new Padding(12, 16, 12, 10)
            };
            ToolOptionStyle.ConfigureResponsiveFlow(root);
            return root;
        }

        internal static Panel CreateHeading(string titleText, string descriptionText)
        {
            Panel heading = new Panel { Height = 74, Margin = new Padding(0, 0, 0, 8) };
            Label title = ToolOptionStyle.CreateLabel(
                titleText, 16F, FontStyle.Bold, ToolOptionStyle.Title);
            Label description = ToolOptionStyle.CreateWrappedLabel(
                descriptionText, 9F, FontStyle.Regular, ToolOptionStyle.Description, 40);
            title.Location = Point.Empty;
            description.Location = new Point(1, 31);
            ToolOptionStyle.FillCardControl(heading, description, 1, 0);
            heading.Controls.Add(title);
            heading.Controls.Add(description);
            return heading;
        }

        internal static Button CreateModelButton(
            string title,
            string description,
            int top)
        {
            Button button = new Button
            {
                FlatStyle = FlatStyle.Flat,
                TextAlign = ContentAlignment.MiddleLeft,
                UseVisualStyleBackColor = false,
                Location = new Point(14, top),
                Height = 42,
                Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right,
                Text = title + Environment.NewLine + description,
                Font = new Font("맑은 고딕", 8.5F, FontStyle.Regular)
            };
            button.Resize += (_, _) => button.Padding = new Padding(10, 0, 6, 0);
            return button;
        }

        internal static void StyleModelButton(Button button, bool selected)
        {
            button.BackColor = selected ? ToolOptionStyle.AccentSoft : Color.White;
            button.ForeColor = selected ? ToolOptionStyle.Accent : ToolOptionStyle.Title;
            button.FlatAppearance.BorderColor = selected
                ? ToolOptionStyle.Accent
                : ToolOptionStyle.Border;
        }

        internal static TableLayoutPanel CreateActions(
            out Button runButton,
            out Button cancelButton)
        {
            TableLayoutPanel actions = new TableLayoutPanel
            {
                Height = 38,
                ColumnCount = 2,
                Margin = new Padding(0, 0, 0, 7)
            };
            actions.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 40F));
            actions.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 60F));
            cancelButton = ToolOptionStyle.CreateSecondaryButton("취소");
            runButton = new Button();
            ToolOptionStyle.StylePrimaryButton(runButton);
            cancelButton.Dock = DockStyle.Fill;
            cancelButton.Margin = Padding.Empty;
            runButton.Dock = DockStyle.Fill;
            runButton.Margin = new Padding(8, 0, 0, 0);
            actions.Controls.Add(cancelButton, 0, 0);
            actions.Controls.Add(runButton, 1, 0);
            return actions;
        }

        internal static ProgressBar CreateProgressBar() => new ProgressBar
        {
            Height = 5,
            Style = ProgressBarStyle.Marquee,
            MarqueeAnimationSpeed = 25,
            Visible = false,
            Margin = new Padding(0, 0, 0, 4)
        };

        internal static Label CreateStatusLabel(string text) => new Label
        {
            AutoSize = false,
            Height = 32,
            Text = text,
            Font = new Font("맑은 고딕", 8.5F),
            ForeColor = ToolOptionStyle.Description,
            TextAlign = ContentAlignment.TopCenter
        };

    }

    public sealed class PortraitEnhancementControl : UserControl
    {
        private readonly Label imageInfo;
        private readonly Label status;
        private readonly Button naturalButton;
        private readonly Button skinButton;
        private readonly Button toneButton;
        private readonly Button runButton;
        private readonly Button cancelButton;
        private readonly ProgressBar progressBar;

        public PortraitEnhancementStyle SelectedStyle { get; private set; } =
            PortraitEnhancementStyle.Natural;

        public event EventHandler? RunRequested;
        public event EventHandler? CancelRequested;
        public event EventHandler? CloseRequested;

        public PortraitEnhancementControl()
        {
            BackColor = ToolOptionStyle.PanelBackground;
            AutoScroll = true;

            FlowLayoutPanel root = AiQualityEnhancementControl.CreateRoot();
            root.Controls.Add(AiQualityEnhancementControl.CreateHeading(
                "AI 인물 사진 자동보정",
                "인물의 특징은 유지하고 필요한 부분만 자연스럽게 보정합니다."));

            Panel imageCard = ToolOptionStyle.CreateCard(68);
            Label imageTitle = ToolOptionStyle.CreateLabel(
                "현재 이미지", 9F, FontStyle.Regular, ToolOptionStyle.Description);
            imageTitle.Location = new Point(14, 10);
            imageInfo = ToolOptionStyle.CreateLabel(
                "이미지를 불러와 주세요.", 10F, FontStyle.Bold, ToolOptionStyle.Accent);
            imageInfo.Location = new Point(14, 33);
            imageCard.Controls.Add(imageTitle);
            imageCard.Controls.Add(imageInfo);

            Panel styleCard = ToolOptionStyle.CreateCard(222);
            Label styleTitle = ToolOptionStyle.CreateLabel(
                "보정 방향", 10F, FontStyle.Bold, ToolOptionStyle.Title);
            styleTitle.Location = new Point(14, 11);
            naturalButton = AiQualityEnhancementControl.CreateModelButton(
                "자연 보정", "피부·색감·노출을 균형 있게 미세 보정", 41);
            skinButton = AiQualityEnhancementControl.CreateModelButton(
                "피부결 중심", "잡티와 피부톤을 자연스럽게 정돈", 92);
            toneButton = AiQualityEnhancementControl.CreateModelButton(
                "색감·노출 중심", "밝기·화이트밸런스·색감을 부드럽게 보정", 143);
            naturalButton.Click += (_, _) => SelectStyle(PortraitEnhancementStyle.Natural);
            skinButton.Click += (_, _) => SelectStyle(PortraitEnhancementStyle.SkinDetail);
            toneButton.Click += (_, _) => SelectStyle(PortraitEnhancementStyle.ToneAndExposure);
            styleCard.Resize += (_, _) =>
            {
                int width = Math.Max(1, styleCard.ClientSize.Width - 28);
                naturalButton.Width = width;
                skinButton.Width = width;
                toneButton.Width = width;
            };
            styleCard.Controls.Add(styleTitle);
            styleCard.Controls.Add(naturalButton);
            styleCard.Controls.Add(skinButton);
            styleCard.Controls.Add(toneButton);

            Panel noticeCard = ToolOptionStyle.CreateCard(92);
            Label noticeTitle = ToolOptionStyle.CreateLabel(
                "안내", 10F, FontStyle.Bold, ToolOptionStyle.Title);
            noticeTitle.Location = new Point(14, 10);
            Label notice = ToolOptionStyle.CreateLabel(
                "AI 결과는 먼저 미리보기로 표시됩니다.\n적용 전에는 원본 편집 상태가 바뀌지 않습니다.",
                8.5F,
                FontStyle.Regular,
                ToolOptionStyle.Description);
            notice.Location = new Point(14, 34);
            notice.AutoSize = false;
            notice.Height = 52;
            ToolOptionStyle.FillCardControl(noticeCard, notice);
            noticeCard.Controls.Add(noticeTitle);
            noticeCard.Controls.Add(notice);

            TableLayoutPanel actions = AiQualityEnhancementControl.CreateActions(
                out runButton, out cancelButton);
            runButton.Text = "AI 자동보정 시작";
            runButton.Click += (_, _) => RunRequested?.Invoke(this, EventArgs.Empty);
            cancelButton.Click += (_, _) =>
            {
                if (runButton.Enabled)
                    CloseRequested?.Invoke(this, EventArgs.Empty);
                else
                    CancelRequested?.Invoke(this, EventArgs.Empty);
            };

            progressBar = AiQualityEnhancementControl.CreateProgressBar();
            status = AiQualityEnhancementControl.CreateStatusLabel(
                "보정 방향을 선택한 뒤 실행하세요.");

            root.Controls.Add(imageCard);
            root.Controls.Add(styleCard);
            root.Controls.Add(noticeCard);
            root.Controls.Add(actions);
            root.Controls.Add(progressBar);
            root.Controls.Add(status);
            Controls.Add(root);
            SelectStyle(SelectedStyle);
        }

        public void PrepareForImage(Size imageSize)
        {
            imageInfo.Text = $"{imageSize.Width:N0} × {imageSize.Height:N0} px";
            runButton.Enabled = true;
            SetStatus("보정 방향을 선택한 뒤 실행하세요.");
        }

        public void SetBusy(bool busy)
        {
            naturalButton.Enabled = !busy;
            skinButton.Enabled = !busy;
            toneButton.Enabled = !busy;
            runButton.Enabled = !busy;
            runButton.Text = busy ? "AI 자동보정 중..." : "AI 자동보정 시작";
            cancelButton.Text = busy ? "작업 취소" : "취소";
            progressBar.Visible = busy;
        }

        public void SetStatus(string message, bool isError = false)
        {
            status.Text = message;
            status.ForeColor = isError
                ? Color.FromArgb(220, 38, 38)
                : ToolOptionStyle.Description;
        }

        private void SelectStyle(PortraitEnhancementStyle style)
        {
            SelectedStyle = style;
            AiQualityEnhancementControl.StyleModelButton(
                naturalButton, style == PortraitEnhancementStyle.Natural);
            AiQualityEnhancementControl.StyleModelButton(
                skinButton, style == PortraitEnhancementStyle.SkinDetail);
            AiQualityEnhancementControl.StyleModelButton(
                toneButton, style == PortraitEnhancementStyle.ToneAndExposure);
        }
    }
}
