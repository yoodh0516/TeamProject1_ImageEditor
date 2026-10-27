using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace ImageEditor.Controls
{
    public partial class FilterControl : UserControl
    {
        public event Action<string>? FilterSelected;
        public event Action? ApplyRequested;
        public event Action? CancelRequested;
        public event Action? ResetRequested;
        private Button? applyButton;
        private Button? resetButton;
        private Button? cancelButton;

        public FilterControl()
        {
            InitializeComponent();
            AutoScroll = true;
            ConfigureSessionButtons();
            Resize += (_, _) => UpdateResponsiveLayout();
            UpdateResponsiveLayout();
        }

        private void ConfigureSessionButtons()
        {
            resetButton = new Button
            {
                Location = new Point(18, 380),
                Size = new Size(72, 38),
                Text = "초기화",
                FlatStyle = FlatStyle.Flat,
                BackColor = Color.White,
                Cursor = Cursors.Hand
            };
            resetButton.FlatAppearance.BorderColor = Color.FromArgb(218, 218, 225);
            resetButton.Click += (_, _) => ResetRequested?.Invoke();

            cancelButton = new Button
            {
                Location = new Point(96, 380),
                Size = new Size(72, 38),
                Text = "취소",
                FlatStyle = FlatStyle.Flat,
                BackColor = Color.White,
                Cursor = Cursors.Hand
            };
            cancelButton.FlatAppearance.BorderColor = Color.FromArgb(218, 218, 225);
            cancelButton.Click += (_, _) => CancelRequested?.Invoke();

            applyButton = new Button
            {
                Location = new Point(174, 380),
                Size = new Size(72, 38),
                Text = "적용",
                FlatStyle = FlatStyle.Flat,
                BackColor = Color.FromArgb(98, 78, 210),
                ForeColor = Color.White,
                Cursor = Cursors.Hand
            };
            applyButton.FlatAppearance.BorderSize = 0;
            applyButton.Enabled = false;
            applyButton.Click += (_, _) => ApplyRequested?.Invoke();

            Controls.Add(resetButton);
            Controls.Add(cancelButton);
            Controls.Add(applyButton);
        }

        private void UpdateResponsiveLayout()
        {
            int contentWidth = Math.Max(180, ClientSize.Width - 24);
            const int columnGap = 10;
            int cardWidth = Math.Max(76, (contentWidth - columnGap) / 2);
            Button[] cards = { btnVivid, btnBright, btnCozy, btnElegant, btnBlackWhite, btnCool };
            for (int i = 0; i < cards.Length; i++)
            {
                int column = i % 2;
                int row = i / 2;
                cards[i].SetBounds(12 + column * (cardWidth + columnGap), 84 + row * 90, cardWidth, 78);
            }

            if (resetButton == null || cancelButton == null || applyButton == null)
                return;

            const int actionGap = 6;
            int actionWidth = Math.Max(54, (contentWidth - actionGap * 2) / 3);
            label1.Left = 12;
            lblSubTitle.AutoSize = false;
            lblSubTitle.SetBounds(13, 49, contentWidth, 25);
            resetButton.SetBounds(12, 380, actionWidth, 38);
            cancelButton.SetBounds(12 + actionWidth + actionGap, 380, actionWidth, 38);
            applyButton.SetBounds(12 + (actionWidth + actionGap) * 2, 380, actionWidth, 38);
        }

        public void SetApplyEnabled(bool enabled)
        {
            if (applyButton != null)
                applyButton.Enabled = enabled;
        }

        private void btnVivid_Click(object sender, EventArgs e)
        {
            FilterSelected?.Invoke("화사하게");
        }

        private void btnBright_Click(object sender, EventArgs e)
        {
            FilterSelected?.Invoke("환하게");
        }

        private void btnCozy_Click(object sender, EventArgs e)
        {
            FilterSelected?.Invoke("아늑하게");
        }

        private void btnElegant_Click(object sender, EventArgs e)
        {
            FilterSelected?.Invoke("우아하게");
        }

        private void btnBlackWhite_Click(object sender, EventArgs e)
        {
            FilterSelected?.Invoke("흑백");
        }

        private void btnCool_Click(object sender, EventArgs e)
        {
            FilterSelected?.Invoke("시원하게");
        }
    }
}
