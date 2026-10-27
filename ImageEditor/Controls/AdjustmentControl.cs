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
    public partial class AdjustmentControl : UserControl
    {
        public event Action<int, int, int, int>? AdjustmentChanged;
        public event Action? ResetRequested;
        public event Action? ApplyRequested;
        public event Action? CancelRequested;
        private Button? applyButton;
        private Button? cancelButton;

        public AdjustmentControl()
        {
            InitializeComponent();
            AutoScroll = true;
            ConfigureSessionButtons();
            Resize += (_, _) => UpdateResponsiveLayout();
            UpdateResponsiveLayout();
        }

        private void ConfigureSessionButtons()
        {
            btnReset.Location = new Point(18, 421);
            btnReset.Size = new Size(72, 38);

            cancelButton = new Button
            {
                Location = new Point(96, 421),
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
                Location = new Point(174, 421),
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

            Controls.Add(cancelButton);
            Controls.Add(applyButton);
        }

        private void UpdateResponsiveLayout()
        {
            int contentWidth = Math.Max(180, ClientSize.Width - 24);
            Panel[] cards = { pnlBrightness, pnlContrast, pnlSaturation, pnlTemperature };
            TrackBar[] tracks = { trackBrightness, trackContrast, trackSaturation, trackTemperature };
            Label[] values = { lblBrightnessValue, lblContrastValue, lblSaturationValue, lblTemperatureValue };

            for (int i = 0; i < cards.Length; i++)
            {
                cards[i].Left = 12;
                cards[i].Width = contentWidth;
                tracks[i].Width = Math.Max(80, contentWidth - 16);
                values[i].Left = Math.Max(80, contentWidth - values[i].Width - 12);
            }

            if (cancelButton == null || applyButton == null)
                return;

            const int gap = 6;
            int buttonWidth = Math.Max(54, (contentWidth - gap * 2) / 3);
            lblTitle.Left = 12;
            lblSubTitle.AutoSize = false;
            lblSubTitle.SetBounds(13, 49, contentWidth, 25);
            btnReset.SetBounds(12, 421, buttonWidth, 38);
            cancelButton.SetBounds(12 + buttonWidth + gap, 421, buttonWidth, 38);
            applyButton.SetBounds(12 + (buttonWidth + gap) * 2, 421, buttonWidth, 38);
        }

        public void SetApplyEnabled(bool enabled)
        {
            if (applyButton != null)
                applyButton.Enabled = enabled;
        }

        //밝기 스크롤
        private void trackBrightness_Scroll(object sender, EventArgs e)
        {
            lblBrightnessValue.Text =
                trackBrightness.Value.ToString();
            AdjustmentChanged?.Invoke(
                trackBrightness.Value,
                trackContrast.Value,
                trackSaturation.Value,
                trackTemperature.Value
            );
        }

        //대비 스크롤
        private void trackContrast_Scroll(object sender, EventArgs e)
        {
            lblContrastValue.Text =
                trackContrast.Value.ToString();
            AdjustmentChanged?.Invoke(
            trackBrightness.Value,
            trackContrast.Value,
            trackSaturation.Value,
            trackTemperature.Value
        );
        }

        //채도 스크롤
        private void trackSaturation_Scroll(object sender, EventArgs e)
        {
            lblSaturationValue.Text =
                trackSaturation.Value.ToString();
            AdjustmentChanged?.Invoke(
            trackBrightness.Value,
            trackContrast.Value,
            trackSaturation.Value,
            trackTemperature.Value
        );
        }

        //색온도 스크롤
        private void trackTemperature_Scroll(object sender, EventArgs e)
        {
            lblTemperatureValue.Text =
                trackTemperature.Value.ToString();
            AdjustmentChanged?.Invoke(
            trackBrightness.Value,
            trackContrast.Value,
            trackSaturation.Value,
            trackTemperature.Value
        );
        }

        //초기화 버튼
        private void btnReset_Click(object sender, EventArgs e)
        {
            ResetSliders(false);
            ResetRequested?.Invoke();
        }

        //초기화
        public void ResetSliders(bool notify = true)
        {
            trackBrightness.Value = 0;
            trackContrast.Value = 0;
            trackSaturation.Value = 0;
            trackTemperature.Value = 0;

            lblBrightnessValue.Text = "0";
            lblContrastValue.Text = "0";
            lblSaturationValue.Text = "0";
            lblTemperatureValue.Text = "0";

            if (notify)
            {
                AdjustmentChanged?.Invoke(0, 0, 0, 0);
            }
        }
        public void SetAdjustmentValues(
            int brightness,
            int contrast,
            int saturation,
            int temperature)
        {
            trackBrightness.Value = brightness;
            trackContrast.Value = contrast;
            trackSaturation.Value = saturation;
            trackTemperature.Value = temperature;

            lblBrightnessValue.Text = brightness.ToString();
            lblContrastValue.Text = contrast.ToString();
            lblSaturationValue.Text = saturation.ToString();
            lblTemperatureValue.Text = temperature.ToString();
        }

        public EditStyle GetCurrentValues()
        {
            return new EditStyle
            {
                Brightness = trackBrightness.Value,
                Contrast = trackContrast.Value,
                Saturation = trackSaturation.Value,
                Temperature = trackTemperature.Value
            };
        }
    }   
}

