using System;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;
using ImageEditor.Core;
using ImageEditor.Controls;
using ImageEditor.Services;

namespace ImageEditor.Tools
{
    /// <summary>
    /// AI background removal UI tool.
    /// The AI inference itself is performed by the project's local AI service.
    /// </summary>
    public sealed class AIBackgroundRemovalTool : IDisposable
    {
        private readonly PictureBox pictureBox;
        private readonly EditorState state;
        private readonly Control optionHost;
        private readonly AIBackgroundRemovalService service;

        private Panel? panel;
        private Label? titleLabel;
        private Label? guideLabel;
        private Label? statusLabel;
        private Button? applyButton;
        private Button? cancelButton;
        private ProgressBar? progressBar;
        private Bitmap? previewImage;
        private bool isBusy;
        private CancellationTokenSource? removalCancellation;

        public event Action<Bitmap>? ImageReady;
        public event Action? Cancelled;

        public AIBackgroundRemovalTool(
            PictureBox pictureBox,
            EditorState state,
            Control optionHost)
        {
            this.pictureBox = pictureBox ?? throw new ArgumentNullException(nameof(pictureBox));
            this.state = state ?? throw new ArgumentNullException(nameof(state));
            this.optionHost = optionHost ?? throw new ArgumentNullException(nameof(optionHost));
            service = new AIBackgroundRemovalService();
        }

        public void Start()
        {
            if (state.CurrentImage == null)
            {
                MessageBox.Show(
                    "먼저 이미지를 불러와 주세요.",
                    "AI 배경 제거",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Information);
                return;
            }

            StopPreviewOnly();
            BuildOptions();
        }

        private void BuildOptions()
        {
            optionHost.Controls.Clear();

            panel = new Panel
            {
                Dock = DockStyle.Fill,
                AutoScroll = true,
                Padding = new Padding(14),
                BackColor = Color.White
            };

            titleLabel = new Label
            {
                AutoSize = false,
                Location = new Point(14, 12),
                Size = new Size(250, 30),
                Font = new Font("맑은 고딕", 12F, FontStyle.Bold),
                Text = "✨ AI 배경 제거"
            };

            guideLabel = new Label
            {
                AutoSize = false,
                Location = new Point(14, 48),
                Size = new Size(250, 72),
                Font = new Font("맑은 고딕", 9.5F),
                ForeColor = Color.DimGray,
                Text = "사진의 인물이나 주요 대상을 로컬 AI가 자동으로 인식하여 배경을 투명하게 제거합니다.\r\n\r\nAPI 비용 없이 PC의 CPU에서 실행됩니다."
            };

            applyButton = new Button
            {
                Location = new Point(14, 135),
                Size = new Size(250, 42),
                Text = "✨ AI로 배경 제거",
                Font = new Font("맑은 고딕", 10F, FontStyle.Bold),
                UseVisualStyleBackColor = true
            };
            applyButton.Click += ApplyButton_Click;

            cancelButton = new Button
            {
                Location = new Point(14, 185),
                Size = new Size(250, 36),
                Text = "취소",
                UseVisualStyleBackColor = true
            };
            cancelButton.Click += CancelButton_Click;

            statusLabel = new Label
            {
                AutoSize = false,
                Location = new Point(14, 250),
                Size = new Size(250, 55),
                Font = new Font("맑은 고딕", 8.5F),
                ForeColor = Color.Gray,
                Text = "※ 최초 실행은 Python과 AI 모델 준비로 시간이 걸릴 수 있습니다.",
                TextAlign = ContentAlignment.TopLeft
            };

            progressBar = new ProgressBar
            {
                Location = new Point(14, 315),
                Size = new Size(250, 12),
                Style = ProgressBarStyle.Marquee,
                MarqueeAnimationSpeed = 25,
                Visible = false
            };

            panel.Controls.Add(titleLabel);
            panel.Controls.Add(guideLabel);
            panel.Controls.Add(applyButton);
            panel.Controls.Add(cancelButton);
            panel.Controls.Add(statusLabel);
            panel.Controls.Add(progressBar);

            optionHost.Controls.Add(panel);
            BuildCardLayout();
            panel.BringToFront();
        }

        // Keep the existing AI request and preview flow, but present its controls
        // in the same card layout used by the other AI detail panels.
        private void BuildCardLayout()
        {
            if (panel == null || titleLabel == null || guideLabel == null ||
                statusLabel == null || applyButton == null || cancelButton == null ||
                progressBar == null)
            {
                return;
            }

            panel.Controls.Clear();
            panel.Padding = Padding.Empty;
            panel.BackColor = ToolOptionStyle.PanelBackground;

            FlowLayoutPanel root = AiQualityEnhancementControl.CreateRoot();

            Panel heading = new Panel { Height = 72, Margin = new Padding(0, 0, 0, 8) };
            titleLabel.Text = "AI 배경 제거";
            titleLabel.AutoSize = true;
            titleLabel.Location = Point.Empty;
            titleLabel.Font = new Font("맑은 고딕", 16F, FontStyle.Bold);
            titleLabel.ForeColor = ToolOptionStyle.Title;
            Label description = ToolOptionStyle.CreateWrappedLabel(
                "주요 대상을 남기고 배경을 투명하게 만듭니다.",
                9F, FontStyle.Regular, ToolOptionStyle.Description, 40);
            description.Location = new Point(1, 31);
            ToolOptionStyle.FillCardControl(heading, description, 1, 0);
            heading.Controls.Add(titleLabel);
            heading.Controls.Add(description);

            Panel guideCard = ToolOptionStyle.CreateCard(116);
            Label guideTitle = ToolOptionStyle.CreateLabel("처리 방식", 10F, FontStyle.Bold, ToolOptionStyle.Title);
            guideTitle.Location = new Point(14, 11);
            guideLabel.Text = "AI가 주요 피사체를 자동 인식합니다.\r\n결과는 먼저 미리보기로 표시되며, 적용을 눌러야\r\n실제 이미지에 반영됩니다.";
            guideLabel.AutoSize = false;
            guideLabel.Location = new Point(14, 37);
            guideLabel.Height = 70;
            guideLabel.Font = new Font("맑은 고딕", 9F);
            guideLabel.ForeColor = ToolOptionStyle.Description;
            ToolOptionStyle.FillCardControl(guideCard, guideLabel);
            guideCard.Controls.Add(guideTitle);
            guideCard.Controls.Add(guideLabel);

            Panel noticeCard = ToolOptionStyle.CreateCard(68);
            Label noticeTitle = ToolOptionStyle.CreateLabel("안내", 10F, FontStyle.Bold, ToolOptionStyle.Title);
            Label notice = ToolOptionStyle.CreateWrappedLabel(
                "투명 배경을 유지하려면 PNG 형식으로 저장하세요.",
                8.5F, FontStyle.Regular, ToolOptionStyle.Description, 28);
            noticeTitle.Location = new Point(14, 10);
            notice.Location = new Point(14, 34);
            ToolOptionStyle.FillCardControl(noticeCard, notice);
            noticeCard.Controls.Add(noticeTitle);
            noticeCard.Controls.Add(notice);

            TableLayoutPanel actions = new TableLayoutPanel
            {
                Height = 38,
                ColumnCount = 2,
                Margin = new Padding(0, 0, 0, 7)
            };
            actions.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 40F));
            actions.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 60F));
            cancelButton.Text = "취소";
            cancelButton.Dock = DockStyle.Fill;
            cancelButton.FlatStyle = FlatStyle.Flat;
            cancelButton.BackColor = Color.White;
            cancelButton.ForeColor = ToolOptionStyle.Title;
            cancelButton.FlatAppearance.BorderColor = ToolOptionStyle.Border;
            cancelButton.Margin = Padding.Empty;
            applyButton.Text = "AI 배경 제거 시작";
            applyButton.Dock = DockStyle.Fill;
            applyButton.Margin = new Padding(8, 0, 0, 0);
            ToolOptionStyle.StylePrimaryButton(applyButton);
            actions.Controls.Add(cancelButton, 0, 0);
            actions.Controls.Add(applyButton, 1, 0);

            statusLabel.Text = "실행하면 AI 서버와 모델을 준비합니다.";
            statusLabel.AutoSize = false;
            statusLabel.Height = 38;
            statusLabel.Location = Point.Empty;
            statusLabel.Font = new Font("맑은 고딕", 8.5F);
            statusLabel.ForeColor = ToolOptionStyle.Description;
            progressBar.Height = 5;
            progressBar.Location = Point.Empty;

            root.Controls.Add(heading);
            root.Controls.Add(guideCard);
            root.Controls.Add(noticeCard);
            root.Controls.Add(actions);
            root.Controls.Add(progressBar);
            root.Controls.Add(statusLabel);
            panel.Controls.Add(root);
        }

        private async void ApplyButton_Click(object? sender, EventArgs e)
        {
            if (isBusy || state.CurrentImage == null)
                return;

            isBusy = true;
            SetBusy(true);
            SetProcessingUiText();
            removalCancellation?.Dispose();
            removalCancellation = new CancellationTokenSource();
            CancellationToken cancellationToken = removalCancellation.Token;
            if (statusLabel != null)
                statusLabel.Text = "AI 서버를 확인하고 있습니다...\r\n처음 실행하면 모델 준비에 시간이 걸릴 수 있습니다.";

            try
            {
                using Bitmap source = new Bitmap(state.CurrentImage);

                Bitmap? result = await service.RemoveBackgroundAsync(
                    source,
                    cancellationToken);

                if (result == null)
                    throw new InvalidOperationException("AI 배경 제거 결과를 만들지 못했습니다.");

                ReplacePreview(result);
                SetPreviewUiText();

                if (statusLabel != null)
                    statusLabel.Text = "배경 제거가 완료되었습니다.\r\n아래 [적용하기]를 눌러 편집 이미지에 반영하세요.";

                if (applyButton != null)
                    applyButton.Text = "✓ 적용하기";

                if (guideLabel != null)
                    guideLabel.Text = "결과를 확인한 후 적용하거나 취소하세요.";

                if (applyButton != null)
                {
                    applyButton.Click -= ApplyButton_Click;
                    applyButton.Click += ApplyPreviewButton_Click;
                }
            }
            catch (OperationCanceledException)
            {
            }
            catch (Exception ex)
            {
                MessageBox.Show(
                    ex.Message,
                    "AI 배경 제거",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Error);
            }
            finally
            {
                removalCancellation?.Dispose();
                removalCancellation = null;
                isBusy = false;
                SetBusy(false);
            }
        }

        private void ApplyPreviewButton_Click(object? sender, EventArgs e)
        {
            if (previewImage == null)
                return;

            Bitmap result = new Bitmap(previewImage);
            ImageReady?.Invoke(result);

            previewImage.Dispose();
            previewImage = null;
            Stop();
        }

        private void SetProcessingUiText()
        {
            if (statusLabel != null)
                statusLabel.Text = "AI 서버와 모델을 준비하는 중입니다...";
        }

        private void SetPreviewUiText()
        {
            if (statusLabel != null)
                statusLabel.Text = "결과를 확인한 뒤 적용하거나 취소하세요.";
            if (guideLabel != null)
                guideLabel.Text = "배경 제거 결과가 미리보기로 표시됩니다.\r\n적용을 누르기 전까지 원본 이미지는 유지됩니다.";
            if (applyButton != null)
                applyButton.Text = "적용";
        }

        private void ReplacePreview(Bitmap result)
        {
            Image? oldDisplay = pictureBox.Image;

            previewImage?.Dispose();
            previewImage = result;

            pictureBox.Image = CreatePreviewBitmap(result);

            if (oldDisplay != null &&
                !ReferenceEquals(oldDisplay, state.CurrentImage))
            {
                oldDisplay.Dispose();
            }
        }

        private Bitmap CreatePreviewBitmap(Bitmap source)
        {
            Bitmap result = new Bitmap(source.Width, source.Height);

            using Graphics g = Graphics.FromImage(result);
            g.SmoothingMode = SmoothingMode.HighQuality;
            g.InterpolationMode = InterpolationMode.HighQualityBicubic;
            g.Clear(Color.White);

            // 투명 영역을 확인하기 쉽도록 체크무늬 배경을 표시한다.
            const int cell = 16;
            using Brush light = new SolidBrush(Color.FromArgb(235, 235, 235));
            using Brush dark = new SolidBrush(Color.FromArgb(210, 210, 210));

            for (int y = 0; y < result.Height; y += cell)
            {
                for (int x = 0; x < result.Width; x += cell)
                {
                    bool odd = ((x / cell) + (y / cell)) % 2 == 1;
                    g.FillRectangle(
                        odd ? dark : light,
                        x,
                        y,
                        Math.Min(cell, result.Width - x),
                        Math.Min(cell, result.Height - y));
                }
            }

            g.DrawImageUnscaled(source, 0, 0);
            return result;
        }

        private void SetBusy(bool busy)
        {
            if (applyButton != null)
                applyButton.Enabled = !busy;

            if (cancelButton != null)
                cancelButton.Enabled = true;

            if (progressBar != null)
                progressBar.Visible = busy;

            pictureBox.Cursor = busy ? Cursors.WaitCursor : Cursors.Default;
        }

        private void CancelButton_Click(object? sender, EventArgs e)
        {
            if (isBusy)
            {
                removalCancellation?.Cancel();
                return;
            }

            Cancelled?.Invoke();
            Stop();
        }

        public void Stop()
        {
            removalCancellation?.Cancel();
            StopPreviewOnly();
            optionHost.Controls.Clear();
            pictureBox.Cursor = Cursors.Default;
        }

        private void StopPreviewOnly()
        {
            bool hadPreview = previewImage != null;
            Image? displayedPreview = hadPreview ? pictureBox.Image : null;

            if (hadPreview)
                pictureBox.Image = null;

            previewImage?.Dispose();
            previewImage = null;

            if (displayedPreview != null &&
                !ReferenceEquals(displayedPreview, state.CurrentImage))
            {
                displayedPreview.Dispose();
            }

            if (hadPreview && state.CurrentImage != null)
            {
                Bitmap display = new Bitmap(state.CurrentImage);
                pictureBox.Image = display;
            }
        }

        public void Dispose()
        {
            Stop();
        }
    }
}
