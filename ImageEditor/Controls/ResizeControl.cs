using System;
using System.Drawing;
using System.Windows.Forms;

namespace ImageEditor.Controls
{
    public partial class ResizeControl : UserControl
    {
        public event Action<int, int>? ResizeRequested;
        public event Action<int, int>? PreviewRequested;
        public event Action? CancelRequested;

        private int originalWidth;
        private int originalHeight;
        private double aspectRatio;
        private bool isUpdating;
        private readonly System.Windows.Forms.Timer previewTimer;
        private readonly Label lblExpectedSize;
        private readonly Button btnCancel;
        private readonly Button btnHalf;
        private readonly Button btnActual;
        private readonly Button btnDouble;
        private const int MaximumDimension = 32768;
        private const long MaximumPixelCount = 50_000_000;

        public ResizeControl()
        {
            InitializeComponent();
            // 숫자를 입력하는 도중의 중간값으로 큰 Bitmap을 반복 생성하지 않도록
            // 마지막 입력 뒤 잠시 기다렸다가 한 번만 미리보기를 만든다.
            previewTimer = new System.Windows.Forms.Timer { Interval = 500 };
            previewTimer.Tick += (_, _) => RaisePreview();

            lblExpectedSize = ToolOptionStyle.CreateLabel("변경 전", 10F, FontStyle.Bold, ToolOptionStyle.Accent);
            btnCancel = ToolOptionStyle.CreateSecondaryButton("취소");
            btnCancel.Click += (_, _) => CancelRequested?.Invoke();
            btnHalf = ToolOptionStyle.CreateSecondaryButton("50%");
            btnActual = ToolOptionStyle.CreateSecondaryButton("100%");
            btnDouble = ToolOptionStyle.CreateSecondaryButton("200%");
            btnHalf.Click += (_, _) => SetQuickSize(0.5);
            btnActual.Click += (_, _) => SetQuickSize(1.0);
            btnDouble.Click += (_, _) => SetQuickSize(2.0);

            BuildCardLayout();
            txtWidth.TextChanged += txtWidth_TextChanged;
            txtHeight.TextChanged += txtHeight_TextChanged;
            txtWidth.TextChanged += (_, _) => QueuePreview();
            txtHeight.TextChanged += (_, _) => QueuePreview();
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

            Panel heading = new Panel { Height = 72, Margin = new Padding(0, 0, 0, 8) };
            Label title = ToolOptionStyle.CreateLabel("크기 조절", 16F, FontStyle.Bold, ToolOptionStyle.Title);
            title.Location = new Point(0, 0);
            Label description = ToolOptionStyle.CreateWrappedLabel(
                "픽셀 기준으로 이미지 해상도를 변경합니다.",
                9F, FontStyle.Regular, ToolOptionStyle.Description, 40);
            description.Location = new Point(1, 30);
            ToolOptionStyle.FillCardControl(heading, description, 1, 0);
            heading.Controls.Add(title);
            heading.Controls.Add(description);

            Panel currentCard = ToolOptionStyle.CreateCard(78);
            lblCurrentSize.Location = new Point(14, 12);
            lblCurrentSize.Font = new Font("맑은 고딕", 9F, FontStyle.Regular);
            lblCurrentSize.ForeColor = ToolOptionStyle.Description;
            lblExpectedSize.Location = new Point(14, 36);
            lblCurrentSize.AutoSize = false;
            lblCurrentSize.Height = 22;
            lblExpectedSize.AutoSize = false;
            lblExpectedSize.Height = 24;
            ToolOptionStyle.FillCardControl(currentCard, lblCurrentSize);
            ToolOptionStyle.FillCardControl(currentCard, lblExpectedSize);
            currentCard.Controls.Add(lblCurrentSize);
            currentCard.Controls.Add(lblExpectedSize);

            Panel inputCard = ToolOptionStyle.CreateCard(175);
            Label inputTitle = ToolOptionStyle.CreateLabel("새 크기 입력", 10F, FontStyle.Bold, ToolOptionStyle.Title);
            inputTitle.Location = new Point(14, 12);
            lblWidth.Location = new Point(14, 47);
            txtWidth.Location = new Point(72, 43);
            txtWidth.Width = 132;
            lblWidthUnit.Location = new Point(212, 47);
            lblHeight.Location = new Point(14, 84);
            txtHeight.Location = new Point(72, 80);
            txtHeight.Width = 132;
            lblHeightUnit.Location = new Point(212, 84);
            chkKeepRatio.Location = new Point(14, 124);
            inputCard.Controls.AddRange(new Control[] { inputTitle, lblWidth, txtWidth, lblWidthUnit, lblHeight, txtHeight, lblHeightUnit, chkKeepRatio });

            Panel quickCard = ToolOptionStyle.CreateCard(84);
            Label quickTitle = ToolOptionStyle.CreateLabel("빠른 크기", 10F, FontStyle.Bold, ToolOptionStyle.Title);
            quickTitle.Location = new Point(14, 10);
            TableLayoutPanel quickButtons = new TableLayoutPanel { Location = new Point(14, 34), Height = 38, ColumnCount = 3, Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right };
            quickButtons.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 33.333F));
            quickButtons.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 33.333F));
            quickButtons.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 33.333F));
            quickButtons.Controls.Add(btnHalf, 0, 0);
            quickButtons.Controls.Add(btnActual, 1, 0);
            quickButtons.Controls.Add(btnDouble, 2, 0);
            quickCard.Controls.Add(quickTitle);
            quickCard.Controls.Add(quickButtons);
            quickCard.Resize += (_, _) => quickButtons.Width = Math.Max(1, quickCard.ClientSize.Width - 28);

            Label note = ToolOptionStyle.CreateLabel("입력 중에는 미리보기만 표시되며, 적용할 때만 실제 이미지가 변경됩니다.", 8.5F, FontStyle.Regular, ToolOptionStyle.Description);
            note.AutoSize = false;
            note.Height = 36;
            note.Margin = new Padding(4, 0, 4, 8);

            TableLayoutPanel actions = new TableLayoutPanel { Height = 38, ColumnCount = 3, Margin = new Padding(0, 0, 0, 12) };
            actions.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 33.333F));
            actions.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 33.333F));
            actions.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 33.333F));
            btnReset.Text = "초기화";
            btnReset.Height = 38;
            btnReset.FlatStyle = FlatStyle.Flat;
            btnReset.FlatAppearance.BorderColor = ToolOptionStyle.Border;
            ToolOptionStyle.StylePrimaryButton(btnApply);
            btnApply.Text = "적용";
            btnApply.Enabled = false;
            actions.Controls.Add(btnReset, 0, 0);
            actions.Controls.Add(btnCancel, 1, 0);
            actions.Controls.Add(btnApply, 2, 0);

            root.Controls.Add(heading);
            root.Controls.Add(currentCard);
            root.Controls.Add(inputCard);
            root.Controls.Add(quickCard);
            root.Controls.Add(note);
            root.Controls.Add(actions);
            Controls.Add(root);
        }

        public void SetImageSize(int width, int height)
        {
            originalWidth = width;
            originalHeight = height;
            aspectRatio = height == 0 ? 1 : (double)width / height;
            isUpdating = true;
            txtWidth.Text = width.ToString();
            txtHeight.Text = height.ToString();
            isUpdating = false;
            lblCurrentSize.Text = "현재 크기  " + width.ToString("N0") + " × " + height.ToString("N0") + " px";
            lblExpectedSize.ForeColor = ToolOptionStyle.Accent;
            lblExpectedSize.Text = "변경 전";
            btnApply.Enabled = false;
        }

        private void txtWidth_TextChanged(object sender, EventArgs e)
        {
            if (isUpdating || !chkKeepRatio.Checked ||
                !long.TryParse(txtWidth.Text, out long width) ||
                width <= 0 || width > MaximumDimension)
            {
                return;
            }

            double calculatedHeight = width / aspectRatio;
            if (calculatedHeight < 1 || calculatedHeight > MaximumDimension)
                return;

            isUpdating = true;
            txtHeight.Text = Math.Max(1, (int)Math.Round(calculatedHeight)).ToString();
            isUpdating = false;
        }

        private void txtHeight_TextChanged(object sender, EventArgs e)
        {
            if (isUpdating || !chkKeepRatio.Checked ||
                !long.TryParse(txtHeight.Text, out long height) ||
                height <= 0 || height > MaximumDimension)
            {
                return;
            }

            double calculatedWidth = height * aspectRatio;
            if (calculatedWidth < 1 || calculatedWidth > MaximumDimension)
                return;

            isUpdating = true;
            txtWidth.Text = Math.Max(1, (int)Math.Round(calculatedWidth)).ToString();
            isUpdating = false;
        }

        private void QueuePreview()
        {
            if (isUpdating) return;
            previewTimer.Stop();
            previewTimer.Start();
        }

        private void RaisePreview()
        {
            previewTimer.Stop();
            if (!TryGetSize(out int width, out int height, out string errorMessage))
            {
                btnApply.Enabled = false;
                lblExpectedSize.ForeColor = Color.Firebrick;
                lblExpectedSize.Text = errorMessage;
                return;
            }

            bool changed = width != originalWidth || height != originalHeight;
            btnApply.Enabled = changed;
            lblExpectedSize.ForeColor = ToolOptionStyle.Accent;
            lblExpectedSize.Text = changed ? "예상 결과  " + width.ToString("N0") + " × " + height.ToString("N0") + " px" : "변경 전";
            // 50%나 200% 미리보기 뒤 100%를 선택한 경우에도 원본 크기 화면으로
            // 되돌려야 한다. 실제 변경이 없으면 적용만 막고 미리보기는 갱신한다.
            PreviewRequested?.Invoke(width, height);
        }

        private bool TryGetSize(out int width, out int height, out string errorMessage)
        {
            width = 0;
            height = 0;
            errorMessage = string.Empty;

            if (!long.TryParse(txtWidth.Text, out long parsedWidth) ||
                !long.TryParse(txtHeight.Text, out long parsedHeight) ||
                parsedWidth <= 0 || parsedHeight <= 0)
            {
                errorMessage = "1 이상의 숫자를 입력해 주세요.";
                return false;
            }

            if (parsedWidth > MaximumDimension || parsedHeight > MaximumDimension)
            {
                errorMessage = $"가로·세로는 {MaximumDimension:N0}px 이하여야 합니다.";
                return false;
            }

            if (parsedWidth * parsedHeight > MaximumPixelCount)
            {
                errorMessage = "이미지가 너무 큽니다. 전체 5천만 픽셀 이하로 입력해 주세요.";
                return false;
            }

            width = (int)parsedWidth;
            height = (int)parsedHeight;
            return true;
        }

        private void SetQuickSize(double scale)
        {
            isUpdating = true;
            txtWidth.Text = Math.Max(1, (int)Math.Round(originalWidth * scale)).ToString();
            txtHeight.Text = Math.Max(1, (int)Math.Round(originalHeight * scale)).ToString();
            isUpdating = false;
            RaisePreview();
        }

        private void btnApply_Click(object sender, EventArgs e)
        {
            if (!TryGetSize(out int width, out int height, out string errorMessage))
            {
                btnApply.Enabled = false;
                lblExpectedSize.ForeColor = Color.Firebrick;
                lblExpectedSize.Text = errorMessage;
                return;
            }
            ResizeRequested?.Invoke(width, height);
        }

        public void SetPreviewError(string message)
        {
            btnApply.Enabled = false;
            lblExpectedSize.ForeColor = Color.Firebrick;
            lblExpectedSize.Text = message;
        }

        private void btnReset_Click(object sender, EventArgs e)
        {
            SetImageSize(originalWidth, originalHeight);
            PreviewRequested?.Invoke(originalWidth, originalHeight);
        }
    }
}
