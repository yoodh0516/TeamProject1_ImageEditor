using ImageEditor.Controls;
using System;
using System.Drawing;
using System.Windows.Forms;

namespace ImageEditor.Forms
{
    public enum AiResultPreviewKind
    {
        PhotoRestoration,
        PortraitEnhancement,
        QualityEnhancement
    }

    public sealed class PhotoRestorationPreviewForm : Form
    {
        private readonly PictureBox picBefore;
        private readonly PictureBox picAfter;

        public PhotoRestorationPreviewForm(
            Bitmap beforeImage,
            Bitmap afterImage,
            string processName = "AI 사진 복원",
            AiResultPreviewKind previewKind = AiResultPreviewKind.PhotoRestoration)
        {
            PreviewProfile profile = PreviewProfile.Create(previewKind, processName);

            Text = profile.WindowTitle;
            StartPosition = FormStartPosition.CenterParent;
            FormBorderStyle = FormBorderStyle.Sizable;
            MaximizeBox = true;
            MinimizeBox = false;
            ShowInTaskbar = false;
            BackColor = Color.FromArgb(244, 246, 250);
            Font = new Font("맑은 고딕", 9F);
            ClientSize = new Size(1120, 720);
            MinimumSize = new Size(860, 560);
            DoubleBuffered = true;

            Panel header = CreateHeader(profile, beforeImage.Size, afterImage.Size);

            TableLayoutPanel comparison = new TableLayoutPanel
            {
                Dock = DockStyle.Fill,
                BackColor = Color.FromArgb(232, 236, 243),
                Padding = new Padding(20),
                ColumnCount = 2,
                RowCount = 1
            };
            comparison.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 50F));
            comparison.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 50F));
            comparison.RowStyles.Add(new RowStyle(SizeType.Percent, 100F));

            picBefore = CreatePictureBox(beforeImage);
            picAfter = CreatePictureBox(afterImage);
            Panel beforeCard = CreateImageCard(
                "원본", profile.BeforeTitle, beforeImage.Size, picBefore, ToolOptionStyle.Description);
            Panel afterCard = CreateImageCard(
                profile.ResultBadge, profile.AfterTitle, afterImage.Size, picAfter, profile.Accent);
            beforeCard.Margin = new Padding(0, 0, 8, 0);
            afterCard.Margin = new Padding(8, 0, 0, 0);
            comparison.Controls.Add(beforeCard, 0, 0);
            comparison.Controls.Add(afterCard, 1, 0);

            Panel footer = CreateFooter(profile, out Button btnCancel, out Button btnApply);
            AcceptButton = btnApply;
            CancelButton = btnCancel;

            Controls.Add(comparison);
            Controls.Add(footer);
            Controls.Add(header);
            EditorToolTipService.Attach(this);
        }

        protected override void OnFormClosed(FormClosedEventArgs e)
        {
            picBefore.Image?.Dispose();
            picAfter.Image?.Dispose();
            base.OnFormClosed(e);
        }

        private static Panel CreateHeader(
            PreviewProfile profile,
            Size beforeSize,
            Size afterSize)
        {
            Panel header = new Panel
            {
                Dock = DockStyle.Top,
                Height = 94,
                BackColor = Color.White,
                Padding = new Padding(24, 14, 24, 12)
            };
            Label featureBadge = new Label
            {
                AutoSize = false,
                Text = profile.FeatureBadge,
                TextAlign = ContentAlignment.MiddleCenter,
                Font = new Font("맑은 고딕", 8.5F, FontStyle.Bold),
                ForeColor = profile.Accent,
                BackColor = profile.AccentSoft,
                Location = new Point(24, 16),
                Size = new Size(86, 26)
            };
            Label title = new Label
            {
                AutoSize = true,
                Text = profile.Title,
                Font = new Font("맑은 고딕", 17F, FontStyle.Bold),
                ForeColor = ToolOptionStyle.Title,
                Location = new Point(122, 12)
            };
            Label description = new Label
            {
                AutoSize = true,
                Text = profile.Description,
                Font = new Font("맑은 고딕", 9F),
                ForeColor = ToolOptionStyle.Description,
                Location = new Point(25, 56)
            };
            Label sizeInfo = new Label
            {
                AutoSize = false,
                Text = $"{beforeSize.Width:N0} × {beforeSize.Height:N0} px   →   " +
                    $"{afterSize.Width:N0} × {afterSize.Height:N0} px",
                TextAlign = ContentAlignment.MiddleRight,
                Font = new Font("맑은 고딕", 9F, FontStyle.Bold),
                ForeColor = profile.Accent,
                Anchor = AnchorStyles.Top | AnchorStyles.Right,
                Size = new Size(330, 28),
                Location = new Point(header.ClientSize.Width - 354, 16)
            };
            Panel border = new Panel
            {
                Dock = DockStyle.Bottom,
                Height = 1,
                BackColor = ToolOptionStyle.Border
            };
            header.Resize += (_, _) =>
                sizeInfo.Left = Math.Max(430, header.ClientSize.Width - sizeInfo.Width - 24);
            header.Controls.Add(featureBadge);
            header.Controls.Add(title);
            header.Controls.Add(description);
            header.Controls.Add(sizeInfo);
            header.Controls.Add(border);
            return header;
        }

        private static Panel CreateImageCard(
            string badgeText,
            string titleText,
            Size imageSize,
            PictureBox pictureBox,
            Color badgeColor)
        {
            Panel card = new Panel
            {
                Dock = DockStyle.Fill,
                BackColor = Color.White,
                BorderStyle = BorderStyle.FixedSingle,
                Padding = new Padding(12)
            };
            Panel cardHeader = new Panel
            {
                Dock = DockStyle.Top,
                Height = 58,
                BackColor = Color.White
            };
            Label badge = new Label
            {
                AutoSize = false,
                Text = badgeText,
                TextAlign = ContentAlignment.MiddleCenter,
                Font = new Font("맑은 고딕", 8F, FontStyle.Bold),
                ForeColor = badgeColor,
                BackColor = badgeColor == ToolOptionStyle.Description
                    ? Color.FromArgb(241, 244, 248)
                    : Color.FromArgb(238, 242, 255),
                Location = new Point(0, 2),
                Size = new Size(62, 24)
            };
            Label title = new Label
            {
                AutoSize = true,
                Text = titleText,
                Font = new Font("맑은 고딕", 11F, FontStyle.Bold),
                ForeColor = ToolOptionStyle.Title,
                Location = new Point(72, 1)
            };
            Label resolution = new Label
            {
                AutoSize = true,
                Text = $"{imageSize.Width:N0} × {imageSize.Height:N0} px",
                Font = new Font("맑은 고딕", 8.5F),
                ForeColor = ToolOptionStyle.Description,
                Location = new Point(72, 28)
            };
            cardHeader.Controls.Add(badge);
            cardHeader.Controls.Add(title);
            cardHeader.Controls.Add(resolution);

            Panel imageHost = new Panel
            {
                Dock = DockStyle.Fill,
                BackColor = Color.FromArgb(238, 241, 247),
                Padding = new Padding(12)
            };
            imageHost.Controls.Add(pictureBox);
            card.Controls.Add(imageHost);
            card.Controls.Add(cardHeader);
            return card;
        }

        private static Panel CreateFooter(
            PreviewProfile profile,
            out Button btnCancel,
            out Button btnApply)
        {
            Panel footer = new Panel
            {
                Dock = DockStyle.Bottom,
                Height = 82,
                BackColor = Color.White,
                Padding = new Padding(20, 14, 20, 14)
            };
            Panel noticeCard = new Panel
            {
                BackColor = profile.AccentSoft,
                Location = new Point(20, 13),
                Size = new Size(650, 54),
                Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right,
                Padding = new Padding(12, 8, 12, 8)
            };
            Label notice = new Label
            {
                Dock = DockStyle.Fill,
                AutoSize = false,
                Text = profile.Notice,
                TextAlign = ContentAlignment.MiddleLeft,
                Font = new Font("맑은 고딕", 8.5F),
                ForeColor = ToolOptionStyle.Description
            };
            noticeCard.Controls.Add(notice);

            Button cancelButton = ToolOptionStyle.CreateSecondaryButton("취소");
            cancelButton.DialogResult = DialogResult.Cancel;
            cancelButton.Size = new Size(96, 40);
            cancelButton.Location = new Point(footer.ClientSize.Width - 236, 21);
            cancelButton.Anchor = AnchorStyles.Top | AnchorStyles.Right;

            Button applyButton = new Button
            {
                Text = profile.ApplyButtonText,
                DialogResult = DialogResult.OK,
                Size = new Size(124, 40),
                Location = new Point(footer.ClientSize.Width - 140, 21),
                Anchor = AnchorStyles.Top | AnchorStyles.Right,
                BackColor = profile.Accent,
                ForeColor = Color.White,
                FlatStyle = FlatStyle.Flat,
                Font = new Font("맑은 고딕", 9F, FontStyle.Bold),
                UseVisualStyleBackColor = false
            };
            applyButton.FlatAppearance.BorderSize = 0;
            footer.Resize += (_, _) =>
            {
                applyButton.Left = Math.Max(160, footer.ClientSize.Width - applyButton.Width - 20);
                cancelButton.Left = Math.Max(54, applyButton.Left - cancelButton.Width - 10);
                noticeCard.Width = Math.Max(120, cancelButton.Left - 34);
            };
            footer.Controls.Add(noticeCard);
            footer.Controls.Add(cancelButton);
            footer.Controls.Add(applyButton);
            btnCancel = cancelButton;
            btnApply = applyButton;
            return footer;
        }

        private static PictureBox CreatePictureBox(Bitmap image)
        {
            return new PictureBox
            {
                BackColor = Color.FromArgb(238, 241, 247),
                Dock = DockStyle.Fill,
                Image = new Bitmap(image),
                SizeMode = PictureBoxSizeMode.Zoom,
                TabStop = false
            };
        }

        private sealed class PreviewProfile
        {
            public string WindowTitle { get; init; } = string.Empty;
            public string FeatureBadge { get; init; } = string.Empty;
            public string ResultBadge { get; init; } = string.Empty;
            public string Title { get; init; } = string.Empty;
            public string Description { get; init; } = string.Empty;
            public string BeforeTitle { get; init; } = string.Empty;
            public string AfterTitle { get; init; } = string.Empty;
            public string Notice { get; init; } = string.Empty;
            public string ApplyButtonText { get; init; } = string.Empty;
            public Color Accent { get; init; }
            public Color AccentSoft { get; init; }

            public static PreviewProfile Create(
                AiResultPreviewKind kind,
                string processName)
            {
                return kind switch
                {
                    AiResultPreviewKind.PortraitEnhancement => new PreviewProfile
                    {
                        WindowTitle = "AI 인물 사진 자동보정 결과",
                        FeatureBadge = "인물 보정",
                        ResultBadge = "보정 후",
                        Title = "인물 자동보정 결과",
                        Description = processName + " 결과입니다. 얼굴과 색감을 확대해 확인한 뒤 적용하세요.",
                        BeforeTitle = "보정 전 이미지",
                        AfterTitle = "자동보정 결과",
                        Notice = "피부 질감, 얼굴 윤곽과 배경이 자연스럽게 유지됐는지 확인해 주세요. 취소하면 AI 결과는 폐기됩니다.",
                        ApplyButtonText = "보정 결과 적용",
                        Accent = Color.FromArgb(196, 72, 137),
                        AccentSoft = Color.FromArgb(253, 239, 247)
                    },
                    AiResultPreviewKind.QualityEnhancement => new PreviewProfile
                    {
                        WindowTitle = "AI 화질 개선 결과",
                        FeatureBadge = "화질 개선",
                        ResultBadge = "개선 후",
                        Title = "화질 개선 결과",
                        Description = processName + " 처리 결과입니다. 윤곽과 세부 표현을 비교해 주세요.",
                        BeforeTitle = "개선 전 이미지",
                        AfterTitle = "화질 개선 결과",
                        Notice = "두 이미지는 화면에 맞춰 같은 크기로 표시될 수 있습니다. 머리카락·윤곽·작은 글자 등 세부 선명도를 비교해 주세요.",
                        ApplyButtonText = "개선 결과 적용",
                        Accent = Color.FromArgb(15, 143, 122),
                        AccentSoft = Color.FromArgb(235, 249, 245)
                    },
                    _ => new PreviewProfile
                    {
                        WindowTitle = "AI 이미지 복원 결과",
                        FeatureBadge = "이미지 복원",
                        ResultBadge = "복원 후",
                        Title = "이미지 복원 결과",
                        Description = processName + " 처리 결과입니다. 손상 부위와 색상을 비교해 주세요.",
                        BeforeTitle = "복원 전 원본",
                        AfterTitle = "AI 복원 결과",
                        Notice = "AI가 추정해 복원한 색상과 세부 모습은 실제 원본과 다를 수 있습니다. 적용 전에 얼굴과 배경을 확인해 주세요.",
                        ApplyButtonText = "복원 결과 적용",
                        Accent = Color.FromArgb(64, 101, 242),
                        AccentSoft = Color.FromArgb(235, 240, 255)
                    }
                };
            }
        }
    }
}
