using System;
using System.Drawing;
using System.Windows.Forms;

namespace ImageEditor.Controls
{
    public sealed class PhotoRestorationControl : UserControl
    {
        private readonly Button btnRestore;
        private readonly Button btnCancel;
        private readonly Label lblStatus;
        private readonly ProgressBar progressBar;
        private readonly Label lblImageInfo;
        private readonly Label title;
        private readonly Label description;
        private readonly Panel damageCard;
        private readonly Panel colorCard;
        private readonly Label notice;
        private bool isBusy;

        public event EventHandler? RestoreRequested;
        public event EventHandler? CancelRequested;

        public PhotoRestorationControl()
        {
            BackColor = Color.FromArgb(248, 250, 252);
            Size = new Size(280, 518);
            AutoScroll = true;

            title = new Label
            {
                AutoSize = true,
                Font = new Font("맑은 고딕", 15F, FontStyle.Bold),
                ForeColor = Color.FromArgb(30, 41, 59),
                Location = new Point(20, 20),
                Text = "AI 오래된 사진 복원"
            };

            description = new Label
            {
                AutoSize = false,
                Font = new Font("맑은 고딕", 9F),
                ForeColor = Color.FromArgb(71, 85, 105),
                Location = new Point(20, 60),
                Size = new Size(238, 44),
                Text = "Replicate 클라우드 GPU로 사진 손상과\n흑백·세피아 색상을 고품질 복원합니다."
            };

            damageCard = CreateFeatureCard(
                new Point(20, 118),
                "Replicate 고품질 복원",
                "FLUX Kontext · 긁힘 · 손상 · 얼굴 보정");

            colorCard = CreateFeatureCard(
                new Point(20, 196),
                "안전한 로컬 대체 처리",
                "토큰·인터넷 오류 시 기존 CPU 복원 사용");

            lblImageInfo = new Label
            {
                AutoSize = false,
                Font = new Font("맑은 고딕", 9F),
                ForeColor = Color.FromArgb(71, 85, 105),
                BackColor = Color.White,
                BorderStyle = BorderStyle.FixedSingle,
                Location = new Point(20, 286),
                Padding = new Padding(10, 0, 0, 0),
                Size = new Size(238, 42),
                TextAlign = ContentAlignment.MiddleLeft,
                Text = "현재 이미지: -"
            };

            notice = new Label
            {
                AutoSize = false,
                Font = new Font("맑은 고딕", 8.5F),
                ForeColor = Color.FromArgb(180, 83, 9),
                BackColor = Color.FromArgb(255, 247, 237),
                Location = new Point(20, 342),
                Padding = new Padding(10, 7, 8, 6),
                Size = new Size(238, 82),
                Text = "※ Replicate는 실행할 때마다 크레딧을 사용합니다.\n   AI 추정 결과는 실제 모습과 다를 수 있습니다."
            };

            btnRestore = new Button
            {
                BackColor = Color.FromArgb(79, 70, 229),
                FlatStyle = FlatStyle.Flat,
                Font = new Font("맑은 고딕", 10F, FontStyle.Bold),
                ForeColor = Color.White,
                Location = new Point(20, 418),
                Size = new Size(238, 40),
                Text = "AI 복원 시작",
                UseVisualStyleBackColor = false
            };
            btnRestore.FlatAppearance.BorderSize = 0;
            btnRestore.Click += (_, _) => RestoreRequested?.Invoke(this, EventArgs.Empty);

            btnCancel = new Button
            {
                BackColor = Color.White,
                FlatStyle = FlatStyle.Flat,
                Font = new Font("맑은 고딕", 9F),
                ForeColor = Color.FromArgb(71, 85, 105),
                Location = new Point(182, 418),
                Size = new Size(76, 40),
                Text = "취소",
                UseVisualStyleBackColor = false,
                Visible = false
            };
            btnCancel.FlatAppearance.BorderColor = Color.FromArgb(203, 213, 225);
            btnCancel.Click += (_, _) => CancelRequested?.Invoke(this, EventArgs.Empty);

            progressBar = new ProgressBar
            {
                Location = new Point(20, 489),
                MarqueeAnimationSpeed = 25,
                Size = new Size(238, 5),
                Style = ProgressBarStyle.Marquee,
                Visible = false
            };

            lblStatus = new Label
            {
                AutoSize = false,
                Font = new Font("맑은 고딕", 8.5F),
                ForeColor = Color.FromArgb(71, 85, 105),
                Location = new Point(20, 501),
                Size = new Size(238, 26),
                TextAlign = ContentAlignment.TopCenter,
                Text = "2_set_replicate_token.bat로 토큰을 설정하세요."
            };

            Controls.Add(title);
            Controls.Add(description);
            Controls.Add(damageCard);
            Controls.Add(colorCard);
            Controls.Add(lblImageInfo);
            Controls.Add(notice);
            Controls.Add(btnRestore);
            Controls.Add(btnCancel);
            Controls.Add(progressBar);
            Controls.Add(lblStatus);
            Resize += (_, _) => UpdateResponsiveLayout();
            UpdateResponsiveLayout();
        }

        public void PrepareForImage(int width, int height)
        {
            lblImageInfo.Text = $"현재 이미지: {width:N0} × {height:N0} px";
            SetStatus("Replicate 우선 · 오류 시 로컬 CPU로 자동 대체합니다.");
        }

        public void SetBusy(bool busy)
        {
            isBusy = busy;
            btnRestore.Enabled = !busy;
            btnRestore.Text = busy ? "AI 복원 중..." : "AI 복원 시작";
            btnCancel.Visible = busy;
            btnCancel.Enabled = busy;
            progressBar.Visible = busy;
            UpdateResponsiveLayout();
        }

        private void UpdateResponsiveLayout()
        {
            const int left = 12;
            int contentWidth = Math.Max(190, ClientSize.Width - 24);
            title.Left = left;
            description.Left = left;
            description.Width = contentWidth;
            damageCard.Left = left;
            damageCard.Width = contentWidth;
            colorCard.Left = left;
            colorCard.Width = contentWidth;
            lblImageInfo.Left = left;
            lblImageInfo.Width = contentWidth;
            notice.Left = left;
            notice.Width = contentWidth;
            progressBar.Left = left;
            progressBar.Width = contentWidth;
            lblStatus.Left = left;
            lblStatus.Width = contentWidth;

            foreach (Panel card in new[] { damageCard, colorCard })
            {
                if (card.Controls.Count > 2)
                    card.Controls[2].Width = Math.Max(90, contentWidth - 68);
            }

            const int gap = 8;
            if (isBusy)
            {
                int availableWidth = Math.Max(1, contentWidth - gap);
                int cancelWidth = Math.Max(76, (int)Math.Round(availableWidth * 0.40));
                int restoreWidth = Math.Max(90, availableWidth - cancelWidth);
                btnCancel.SetBounds(left, 438, cancelWidth, 40);
                btnRestore.SetBounds(
                    left + cancelWidth + gap,
                    438,
                    restoreWidth,
                    40);
            }
            else
            {
                btnRestore.SetBounds(left, 438, contentWidth, 40);
                btnCancel.SetBounds(left, 438, 0, 40);
            }
        }

        public void SetStatus(string message, bool isError = false)
        {
            lblStatus.Text = message;
            lblStatus.ForeColor = isError
                ? Color.FromArgb(220, 38, 38)
                : Color.FromArgb(71, 85, 105);
        }

        private static Panel CreateFeatureCard(
            Point location,
            string title,
            string description)
        {
            Panel card = new Panel
            {
                BackColor = Color.White,
                BorderStyle = BorderStyle.FixedSingle,
                Location = location,
                Size = new Size(238, 72)
            };

            Label check = new Label
            {
                AutoSize = false,
                BackColor = Color.FromArgb(238, 242, 255),
                Font = new Font("맑은 고딕", 10F, FontStyle.Bold),
                ForeColor = Color.FromArgb(79, 70, 229),
                Location = new Point(10, 13),
                Size = new Size(36, 36),
                Text = "✓",
                TextAlign = ContentAlignment.MiddleCenter
            };

            Label titleLabel = new Label
            {
                AutoSize = true,
                Font = new Font("맑은 고딕", 9.5F, FontStyle.Bold),
                ForeColor = Color.FromArgb(30, 41, 59),
                Location = new Point(56, 10),
                Text = title
            };

            Label descriptionLabel = new Label
            {
                AutoSize = false,
                Font = new Font("맑은 고딕", 8F),
                ForeColor = Color.FromArgb(100, 116, 139),
                Location = new Point(56, 33),
                Size = new Size(170, 34),
                Text = description
            };

            card.Controls.Add(check);
            card.Controls.Add(titleLabel);
            card.Controls.Add(descriptionLabel);
            return card;
        }
    }
}
