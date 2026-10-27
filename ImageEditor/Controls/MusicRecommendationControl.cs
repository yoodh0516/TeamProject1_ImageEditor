using System;
using System.Drawing;
using System.Windows.Forms;

namespace ImageEditor.Controls
{
    /// <summary>
    /// Detail panel for the existing local AI music recommendation request.
    /// It owns only UI state; MainForm retains the server request implementation.
    /// </summary>
    public sealed class MusicRecommendationControl : UserControl
    {
        private readonly Label imageInfo;
        private readonly Label result;
        private readonly Label status;
        private readonly Button runButton;
        private readonly Button cancelButton;
        private readonly ProgressBar progressBar;

        public event EventHandler? RunRequested;
        public event EventHandler? CancelRequested;
        public event EventHandler? CloseRequested;

        public MusicRecommendationControl()
        {
            BackColor = ToolOptionStyle.PanelBackground;
            AutoScroll = true;

            FlowLayoutPanel root = AiQualityEnhancementControl.CreateRoot();
            root.Controls.Add(AiQualityEnhancementControl.CreateHeading(
                "AI 음악 추천",
                "사진의 분위기를 분석해 어울리는 음악을 추천합니다."));

            Panel imageCard = ToolOptionStyle.CreateCard(68);
            Label imageTitle = ToolOptionStyle.CreateLabel(
                "분석할 이미지", 9F, FontStyle.Regular, ToolOptionStyle.Description);
            imageTitle.Location = new Point(14, 10);
            imageInfo = ToolOptionStyle.CreateLabel(
                "이미지를 불러와 주세요.", 10F, FontStyle.Bold, ToolOptionStyle.Accent);
            imageInfo.Location = new Point(14, 33);
            imageCard.Controls.Add(imageTitle);
            imageCard.Controls.Add(imageInfo);

            Panel guideCard = ToolOptionStyle.CreateCard(94);
            Label guideTitle = ToolOptionStyle.CreateLabel(
                "추천 기준", 10F, FontStyle.Bold, ToolOptionStyle.Title);
            Label guide = ToolOptionStyle.CreateLabel(
                "색감, 밝기, 장면의 분위기를 분석합니다.\r\n이미지는 추천 분석에만 사용되며 편집 결과는 바뀌지 않습니다.",
                8.5F, FontStyle.Regular, ToolOptionStyle.Description);
            guideTitle.Location = new Point(14, 11);
            guide.Location = new Point(14, 36);
            guide.AutoSize = false;
            guide.Height = 48;
            ToolOptionStyle.FillCardControl(guideCard, guide);
            guideCard.Controls.Add(guideTitle);
            guideCard.Controls.Add(guide);

            Panel resultCard = ToolOptionStyle.CreateCard(118);
            Label resultTitle = ToolOptionStyle.CreateLabel(
                "추천 결과", 10F, FontStyle.Bold, ToolOptionStyle.Title);
            resultTitle.Location = new Point(14, 11);
            result = ToolOptionStyle.CreateLabel(
                "아직 추천 결과가 없습니다.", 9F, FontStyle.Regular, ToolOptionStyle.Description);
            result.AutoSize = false;
            result.Location = new Point(14, 37);
            result.Size = new Size(240, 68);
            ToolOptionStyle.FillCardControl(resultCard, result);
            resultCard.Controls.Add(resultTitle);
            resultCard.Controls.Add(result);

            TableLayoutPanel actions = AiQualityEnhancementControl.CreateActions(
                out runButton, out cancelButton);
            runButton.Text = "음악 추천 시작";
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
                "사진을 선택한 뒤 추천을 시작하세요.");

            root.Controls.Add(imageCard);
            root.Controls.Add(guideCard);
            root.Controls.Add(resultCard);
            root.Controls.Add(actions);
            root.Controls.Add(progressBar);
            root.Controls.Add(status);
            Controls.Add(root);
        }

        public void PrepareForImage(Size imageSize)
        {
            imageInfo.Text = $"{imageSize.Width:N0} × {imageSize.Height:N0} px";
            result.Text = "아직 추천 결과가 없습니다.";
            runButton.Enabled = true;
            SetStatus("사진을 선택한 뒤 추천을 시작하세요.");
        }

        public void SetBusy(bool busy)
        {
            runButton.Enabled = !busy;
            runButton.Text = busy ? "음악 분석 중..." : "음악 추천 시작";
            cancelButton.Text = busy ? "작업 취소" : "취소";
            progressBar.Visible = busy;
        }

        public void SetResult(string recommendation)
        {
            result.Text = recommendation;
            result.ForeColor = ToolOptionStyle.Accent;
        }

        public void SetStatus(string message, bool isError = false)
        {
            status.Text = message;
            status.ForeColor = isError
                ? Color.FromArgb(220, 38, 38)
                : ToolOptionStyle.Description;
        }
    }
}
