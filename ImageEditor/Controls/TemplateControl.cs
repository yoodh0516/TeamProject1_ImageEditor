using System;
using System.Windows.Forms;

namespace ImageEditor.Controls
{
    public partial class TemplateControl : UserControl
    {
        public event Action<string>? TemplateSelected;
        public event Action? ResetRequested;
        public event Action? ApplyRequested;
        public event Action? CancelRequested;
        private Button? applyButton;
        private Button? cancelButton;

        public TemplateControl()
        {
            InitializeComponent();
            ConfigureSessionButtons();
            Resize += (_, _) => UpdateResponsiveLayout();
            UpdateResponsiveLayout();
        }

        private void ConfigureSessionButtons()
        {
            btnTemplateReset.Location = new System.Drawing.Point(18, 463);
            btnTemplateReset.Size = new System.Drawing.Size(72, 38);
            btnTemplateReset.Text = "초기화";

            cancelButton = new Button { Location = new System.Drawing.Point(96, 463), Size = new System.Drawing.Size(72, 38), Text = "취소" };
            applyButton = new Button { Location = new System.Drawing.Point(174, 463), Size = new System.Drawing.Size(72, 38), Text = "적용", BackColor = System.Drawing.Color.FromArgb(98, 78, 210), ForeColor = System.Drawing.Color.White, FlatStyle = FlatStyle.Flat, Enabled = false };
            applyButton.FlatAppearance.BorderSize = 0;
            cancelButton.Click += (_, _) => CancelRequested?.Invoke();
            applyButton.Click += (_, _) => ApplyRequested?.Invoke();
            Controls.Add(cancelButton);
            Controls.Add(applyButton);
        }

        private void UpdateResponsiveLayout()
        {
            int contentWidth = Math.Max(180, ClientSize.Width - 24);
            const int columnGap = 10;
            int cardWidth = Math.Max(76, (contentWidth - columnGap) / 2);
            Button[] cards =
            {
                btnPolaroid, btnFilm, btnMagazine, btnPhotoCard,
                btnInstagram, btnBirthday, btnPostcard, btnMinimalPoster
            };

            for (int i = 0; i < cards.Length; i++)
            {
                int column = i % 2;
                int row = i / 2;
                cards[i].SetBounds(12 + column * (cardWidth + columnGap), 84 + row * 94, cardWidth, 82);
            }

            if (cancelButton == null || applyButton == null)
                return;

            const int actionGap = 6;
            int actionWidth = Math.Max(54, (contentWidth - actionGap * 2) / 3);
            label1.Left = 12;
            lblSubTitle.AutoSize = false;
            lblSubTitle.SetBounds(13, 49, contentWidth, 25);
            btnTemplateReset.SetBounds(12, 463, actionWidth, 38);
            cancelButton.SetBounds(12 + actionWidth + actionGap, 463, actionWidth, 38);
            applyButton.SetBounds(12 + (actionWidth + actionGap) * 2, 463, actionWidth, 38);
        }

        public void SetApplyEnabled(bool enabled)
        {
            if (applyButton != null) applyButton.Enabled = enabled;
        }

        private void btnPolaroid_Click(object sender, EventArgs e) => TemplateSelected?.Invoke("Polaroid");
        private void btnFilm_Click(object sender, EventArgs e) => TemplateSelected?.Invoke("Film");
        private void btnMagazine_Click(object sender, EventArgs e) => TemplateSelected?.Invoke("Magazine");
        private void btnPhotoCard_Click(object sender, EventArgs e) => TemplateSelected?.Invoke("PhotoCard");
        private void btnInstagram_Click(object sender, EventArgs e) => TemplateSelected?.Invoke("Instagram");
        private void btnBirthday_Click(object sender, EventArgs e) => TemplateSelected?.Invoke("Birthday");
        private void btnPostcard_Click(object sender, EventArgs e) => TemplateSelected?.Invoke("Postcard");
        private void btnMinimalPoster_Click(object sender, EventArgs e) => TemplateSelected?.Invoke("MinimalPoster");
        private void btnTemplateReset_Click(object sender, EventArgs e) => ResetRequested?.Invoke();
    }
}
