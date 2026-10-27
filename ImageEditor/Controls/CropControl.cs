using System;
using System.Drawing;
using System.Windows.Forms;

namespace ImageEditor.Controls
{
    public partial class CropControl : UserControl
    {
        public event Action? ApplyRequested;
        public event Action? ResetRequested;
        public event Action? UndoRequested;
        public event Action? CancelRequested;

        private readonly Button btnCancel;
        private readonly Label lblSelectionBounds;

        public CropControl()
        {
            InitializeComponent();
            btnCancel = ToolOptionStyle.CreateSecondaryButton("취소");
            btnCancel.Click += (_, _) => CancelRequested?.Invoke();
            lblSelectionBounds = ToolOptionStyle.CreateLabel("아직 선택된 영역이 없습니다.", 10F, FontStyle.Bold, ToolOptionStyle.Accent);
            BuildCardLayout();
            SetSelectionBounds(Rectangle.Empty);
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
            Label title = ToolOptionStyle.CreateLabel("이미지 자르기", 16F, FontStyle.Bold, ToolOptionStyle.Title);
            title.Location = Point.Empty;
            Label description = ToolOptionStyle.CreateWrappedLabel(
                "필요한 영역만 남겨 사진의 구도를 정리합니다.",
                9F, FontStyle.Regular, ToolOptionStyle.Description, 40);
            description.Location = new Point(1, 30);
            ToolOptionStyle.FillCardControl(heading, description, 1, 0);
            heading.Controls.Add(title);
            heading.Controls.Add(description);

            Panel guideCard = ToolOptionStyle.CreateCard(112);
            Label guideTitle = ToolOptionStyle.CreateLabel("사용 방법", 10F, FontStyle.Bold, ToolOptionStyle.Title);
            guideTitle.Location = new Point(14, 12);
            Label guide = ToolOptionStyle.CreateLabel("1. 캔버스에서 남길 영역을 드래그하세요.\n2. 선택 정보를 확인한 뒤 적용하세요.", 9F, FontStyle.Regular, ToolOptionStyle.Description);
            guide.Location = new Point(14, 38);
            guide.AutoSize = false;
            guide.Height = 54;
            ToolOptionStyle.FillCardControl(guideCard, guide);
            guideCard.Controls.Add(guideTitle);
            guideCard.Controls.Add(guide);

            Panel selectionCard = ToolOptionStyle.CreateCard(108);
            Label selectionTitle = ToolOptionStyle.CreateLabel("선택 영역", 10F, FontStyle.Bold, ToolOptionStyle.Title);
            selectionTitle.Location = new Point(14, 12);
            lblSelectionBounds.Location = new Point(14, 40);
            lblSelectionSize.Location = new Point(14, 68);
            lblSelectionSize.ForeColor = ToolOptionStyle.Description;
            lblSelectionBounds.AutoSize = false;
            lblSelectionBounds.Height = 24;
            lblSelectionSize.AutoSize = false;
            lblSelectionSize.Height = 22;
            ToolOptionStyle.FillCardControl(selectionCard, lblSelectionBounds);
            ToolOptionStyle.FillCardControl(selectionCard, lblSelectionSize);
            selectionCard.Controls.Add(selectionTitle);
            selectionCard.Controls.Add(lblSelectionBounds);
            selectionCard.Controls.Add(lblSelectionSize);

            TableLayoutPanel actions = new TableLayoutPanel { Height = 38, ColumnCount = 3, Margin = new Padding(0, 0, 0, 8) };
            actions.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 33.333F));
            actions.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 33.333F));
            actions.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 33.333F));
            btnReset.Text = "다시 선택";
            btnReset.Height = 38;
            btnReset.FlatStyle = FlatStyle.Flat;
            btnReset.FlatAppearance.BorderColor = ToolOptionStyle.Border;
            ToolOptionStyle.StylePrimaryButton(btnApply);
            btnApply.Text = "적용";
            actions.Controls.Add(btnReset, 0, 0);
            actions.Controls.Add(btnCancel, 1, 0);
            actions.Controls.Add(btnApply, 2, 0);

            btnUndoCrop.Text = "방금 적용한 자르기 되돌리기";
            btnUndoCrop.Height = 36;
            btnUndoCrop.FlatStyle = FlatStyle.Flat;
            btnUndoCrop.FlatAppearance.BorderColor = ToolOptionStyle.Border;
            btnUndoCrop.Margin = new Padding(0, 0, 0, 8);
            Label tip = ToolOptionStyle.CreateLabel("Tip  드래그를 작게 시작하면 더 정밀하게 영역을 지정할 수 있습니다.", 8.5F, FontStyle.Regular, ToolOptionStyle.Description);
            tip.AutoSize = false;
            tip.Height = 34;
            tip.Margin = new Padding(4, 0, 4, 4);

            root.Controls.Add(heading);
            root.Controls.Add(guideCard);
            root.Controls.Add(selectionCard);
            root.Controls.Add(actions);
            root.Controls.Add(btnUndoCrop);
            root.Controls.Add(tip);
            Controls.Add(root);
        }

        public void SetSelectionSize(int width, int height)
        {
            SetSelectionBounds(width <= 0 || height <= 0 ? Rectangle.Empty : new Rectangle(0, 0, width, height));
        }

        public void SetSelectionBounds(Rectangle bounds)
        {
            bool hasSelection = bounds.Width > 0 && bounds.Height > 0;
            lblSelectionBounds.Text = hasSelection
                ? "X " + bounds.X + " · Y " + bounds.Y
                : "아직 선택된 영역이 없습니다.";
            lblSelectionSize.Text = hasSelection
                ? "가로 " + bounds.Width + " × 세로 " + bounds.Height + " px"
                : "선택 크기: -";
            btnApply.Enabled = hasSelection;
        }

        private void btnApply_Click(object sender, EventArgs e) => ApplyRequested?.Invoke();

        private void btnReset_Click(object sender, EventArgs e)
        {
            ResetRequested?.Invoke();
            SetSelectionBounds(Rectangle.Empty);
        }

        private void btnUndoCrop_Click(object sender, EventArgs e) => UndoRequested?.Invoke();
    }
}
