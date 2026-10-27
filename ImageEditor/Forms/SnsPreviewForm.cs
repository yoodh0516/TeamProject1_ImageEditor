using System;
using System.Drawing;
using System.Windows.Forms;
using ImageEditor.Controls;

namespace ImageEditor.Forms
{
    public partial class SnsPreviewForm : Form
    {
        private Bitmap sourceImage;

        public SnsPreviewForm(Bitmap image)
        {
            InitializeComponent();
            BuildModernLayout();

            sourceImage = new Bitmap(image);

            cmbPreset.Items.Add("Instagram 정사각형");
            cmbPreset.Items.Add("Instagram 세로형");
            cmbPreset.Items.Add("Instagram 스토리 / 릴스");
            cmbPreset.Items.Add("YouTube 썸네일");
            cmbPreset.Items.Add("웹 / 블로그 대표 이미지");

            cmbPreset.SelectedIndex = 0;
            EditorToolTipService.Attach(this);
        }

        private void BuildModernLayout()
        {
            SuspendLayout();
            Controls.Clear();

            Text = "결과 미리보기";
            StartPosition = FormStartPosition.CenterParent;
            BackColor = Color.FromArgb(244, 246, 250);
            Font = new Font("맑은 고딕", 9F);
            ClientSize = new Size(1060, 700);
            MinimumSize = new Size(820, 560);
            DoubleBuffered = true;

            Panel header = new Panel
            {
                Dock = DockStyle.Top,
                Height = 82,
                BackColor = Color.White,
                Padding = new Padding(24, 15, 24, 12)
            };
            label2.Text = "SNS 결과 미리보기";
            label2.Font = new Font("맑은 고딕", 17F, FontStyle.Bold);
            label2.ForeColor = ToolOptionStyle.Title;
            label2.Location = new Point(24, 13);
            label2.AutoSize = true;
            Label subtitle = new Label
            {
                AutoSize = true,
                Text = "플랫폼 규격을 선택해 게시물에서 보일 이미지 비율을 확인합니다.",
                Font = new Font("맑은 고딕", 9F),
                ForeColor = ToolOptionStyle.Description,
                Location = new Point(25, 48)
            };
            Panel headerBorder = new Panel
            {
                Dock = DockStyle.Bottom,
                Height = 1,
                BackColor = ToolOptionStyle.Border
            };
            header.Controls.Add(label2);
            header.Controls.Add(subtitle);
            header.Controls.Add(headerBorder);

            TableLayoutPanel content = new TableLayoutPanel
            {
                Dock = DockStyle.Fill,
                BackColor = Color.FromArgb(244, 246, 250),
                Padding = new Padding(20),
                ColumnCount = 2,
                RowCount = 1
            };
            content.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 286F));
            content.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100F));
            content.RowStyles.Add(new RowStyle(SizeType.Percent, 100F));

            Panel optionCard = new Panel
            {
                Dock = DockStyle.Fill,
                BackColor = Color.White,
                BorderStyle = BorderStyle.FixedSingle,
                Padding = new Padding(16),
                Margin = new Padding(0, 0, 12, 0)
            };
            label1.Text = "플랫폼 / 용도";
            label1.Font = new Font("맑은 고딕", 10F, FontStyle.Bold);
            label1.ForeColor = ToolOptionStyle.Title;
            label1.Location = new Point(16, 17);
            label1.AutoSize = true;
            Label selectorGuide = new Label
            {
                AutoSize = false,
                Text = "게시할 위치를 선택하면 권장 비율로 결과를 미리 보여줍니다.",
                Font = new Font("맑은 고딕", 8.5F),
                ForeColor = ToolOptionStyle.Description,
                Location = new Point(16, 46),
                Size = new Size(238, 38)
            };
            cmbPreset.Location = new Point(16, 92);
            cmbPreset.Width = 238;
            cmbPreset.Height = 31;
            cmbPreset.Font = new Font("맑은 고딕", 9.5F);

            Panel resultCard = new Panel
            {
                BackColor = ToolOptionStyle.AccentSoft,
                Location = new Point(16, 142),
                Size = new Size(238, 82),
                Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right,
                Padding = new Padding(12, 10, 12, 10)
            };
            Label resultTitle = new Label
            {
                Dock = DockStyle.Top,
                Height = 23,
                Text = "예상 결과 규격",
                Font = new Font("맑은 고딕", 8.5F),
                ForeColor = ToolOptionStyle.Description
            };
            lblInfo.Dock = DockStyle.Fill;
            lblInfo.AutoSize = false;
            lblInfo.TextAlign = ContentAlignment.MiddleLeft;
            lblInfo.Font = new Font("맑은 고딕", 11F, FontStyle.Bold);
            lblInfo.ForeColor = ToolOptionStyle.Accent;
            resultCard.Controls.Add(lblInfo);
            resultCard.Controls.Add(resultTitle);

            Panel guideCard = new Panel
            {
                BackColor = Color.FromArgb(248, 249, 252),
                BorderStyle = BorderStyle.FixedSingle,
                Location = new Point(16, 242),
                Size = new Size(238, 112),
                Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right,
                Padding = new Padding(12)
            };
            Label guide = new Label
            {
                Dock = DockStyle.Fill,
                AutoSize = false,
                Text = "미리보기 안내\r\n\r\n이미지는 중앙을 기준으로 잘라 표시됩니다. 이 화면에서는 실제 편집 결과가 변경되지 않습니다.",
                Font = new Font("맑은 고딕", 8.5F),
                ForeColor = ToolOptionStyle.Description
            };
            guideCard.Controls.Add(guide);
            optionCard.Resize += (_, _) =>
            {
                int width = Math.Max(1, optionCard.ClientSize.Width - 32);
                cmbPreset.Width = width;
                selectorGuide.Width = width;
                resultCard.Width = width;
                guideCard.Width = width;
            };
            optionCard.Controls.Add(label1);
            optionCard.Controls.Add(selectorGuide);
            optionCard.Controls.Add(cmbPreset);
            optionCard.Controls.Add(resultCard);
            optionCard.Controls.Add(guideCard);

            Panel previewCard = new Panel
            {
                Dock = DockStyle.Fill,
                BackColor = Color.White,
                BorderStyle = BorderStyle.FixedSingle,
                Padding = new Padding(12),
                Margin = new Padding(0)
            };
            Panel previewHeader = new Panel
            {
                Dock = DockStyle.Top,
                Height = 42,
                BackColor = Color.White
            };
            Label previewTitle = new Label
            {
                AutoSize = true,
                Text = "게시물에 표시될 이미지",
                Font = new Font("맑은 고딕", 10F, FontStyle.Bold),
                ForeColor = ToolOptionStyle.Title,
                Location = new Point(2, 8)
            };
            previewHeader.Controls.Add(previewTitle);
            Panel imageHost = new Panel
            {
                Dock = DockStyle.Fill,
                BackColor = Color.FromArgb(232, 236, 243),
                Padding = new Padding(16)
            };
            picPreview.Dock = DockStyle.Fill;
            picPreview.BackColor = Color.FromArgb(232, 236, 243);
            picPreview.BorderStyle = BorderStyle.None;
            picPreview.SizeMode = PictureBoxSizeMode.Zoom;
            imageHost.Controls.Add(picPreview);
            previewCard.Controls.Add(imageHost);
            previewCard.Controls.Add(previewHeader);

            content.Controls.Add(optionCard, 0, 0);
            content.Controls.Add(previewCard, 1, 0);

            Panel footer = new Panel
            {
                Dock = DockStyle.Bottom,
                Height = 66,
                BackColor = Color.White,
                Padding = new Padding(20, 13, 20, 13)
            };
            Label footerHint = new Label
            {
                AutoSize = true,
                Text = "실제 파일 저장은 메인 화면의 저장 기능을 이용해 주세요.",
                ForeColor = ToolOptionStyle.Description,
                Location = new Point(22, 23)
            };
            btnClose.Text = "닫기";
            btnClose.Size = new Size(96, 38);
            btnClose.Location = new Point(footer.ClientSize.Width - 116, 13);
            btnClose.Anchor = AnchorStyles.Top | AnchorStyles.Right;
            btnClose.BackColor = Color.White;
            btnClose.ForeColor = ToolOptionStyle.Title;
            btnClose.FlatStyle = FlatStyle.Flat;
            btnClose.FlatAppearance.BorderColor = ToolOptionStyle.Border;
            btnClose.UseVisualStyleBackColor = false;
            footer.Resize += (_, _) =>
                btnClose.Left = Math.Max(20, footer.ClientSize.Width - btnClose.Width - 20);
            footer.Controls.Add(footerHint);
            footer.Controls.Add(btnClose);

            Controls.Add(content);
            Controls.Add(footer);
            Controls.Add(header);
            ResumeLayout(true);
        }

        private void cmbPreset_SelectedIndexChanged(object sender, EventArgs e)
        {
            switch (cmbPreset.SelectedIndex)
            {
                case 0:
                    UpdatePreview(1, 1, "1080 × 1080 / 1:1");
                    break;

                case 1:
                    UpdatePreview(4, 5, "1080 × 1350 / 4:5");
                    break;

                case 2:
                    UpdatePreview(9, 16, "1080 × 1920 / 9:16");
                    break;

                case 3:
                    UpdatePreview(16, 9, "1280 × 720 / 16:9");
                    break;

                case 4:
                    UpdatePreview(191, 100, "1200 × 630 / 1.91:1");
                    break;
            }
        }

        private void UpdatePreview(
    int ratioWidth,
    int ratioHeight,
    string infoText)
        {
            if (sourceImage == null)
                return;

            Bitmap preview =
                CreatePreviewImage(
                    sourceImage,
                    ratioWidth,
                    ratioHeight
                );

            picPreview.Image?.Dispose();
            picPreview.Image = preview;

            lblInfo.Text = infoText;
        }

        private Bitmap CreatePreviewImage(
    Bitmap source,
    int ratioWidth,
    int ratioHeight)
        {
            double sourceRatio =
                (double)source.Width / source.Height;

            double targetRatio =
                (double)ratioWidth / ratioHeight;

            int cropWidth;
            int cropHeight;

            if (sourceRatio > targetRatio)
            {
                cropHeight = source.Height;
                cropWidth =
                    (int)(source.Height * targetRatio);
            }
            else
            {
                cropWidth = source.Width;
                cropHeight =
                    (int)(source.Width / targetRatio);
            }

            int x =
                (source.Width - cropWidth) / 2;

            int y =
                (source.Height - cropHeight) / 2;

            Rectangle cropArea =
                new Rectangle(
                    x,
                    y,
                    cropWidth,
                    cropHeight
                );

            Bitmap result =
                source.Clone(
                    cropArea,
                    source.PixelFormat
                );

            return result;
        }

        private void btnClose_Click(object sender, EventArgs e)
        {
            Close();
        }

        protected override void OnFormClosed(
    FormClosedEventArgs e)
        {
            picPreview.Image?.Dispose();
            sourceImage?.Dispose();

            base.OnFormClosed(e);
        }
    }

}
