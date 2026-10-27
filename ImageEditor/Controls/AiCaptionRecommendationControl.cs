using ImageEditor.Services;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Windows.Forms;

namespace ImageEditor.Controls
{
    /// <summary>유행어·밈 추천 결과를 현재 텍스트 도구의 임시 편집으로 넘기는 패널입니다.</summary>
    public sealed class AiCaptionRecommendationControl : UserControl
    {
        private readonly Label imageInfo;
        private readonly Label status;
        private readonly FlowLayoutPanel contentRoot;
        private readonly FlowLayoutPanel resultList;
        private readonly Button runButton;
        private readonly Button cancelButton;
        private readonly Button startEditingButton;
        private readonly ProgressBar progressBar;
        private readonly List<Button> suggestionButtons = new List<Button>();
        private AiCaptionSuggestion? selectedSuggestion;

        public event EventHandler? RunRequested;
        public event EventHandler? CancelRequested;
        public event EventHandler? CloseRequested;
        public event EventHandler<AiCaptionSuggestionEventArgs>? StartEditingRequested;

        public string RecommendationType => "둘 다";
        public string Platform => "Instagram";

        public AiCaptionRecommendationControl()
        {
            BackColor = ToolOptionStyle.PanelBackground;
            AutoScroll = false;

            contentRoot = AiQualityEnhancementControl.CreateRoot();
            contentRoot.Controls.Add(AiQualityEnhancementControl.CreateHeading(
                "유행어·밈 추천",
                "사진을 분석해 어울리는 유행어와 밈 4개를 자동 추천합니다."));

            Panel imageCard = ToolOptionStyle.CreateCard(64);
            Label imageTitle = ToolOptionStyle.CreateLabel(
                "분석할 이미지", 9F, FontStyle.Regular, ToolOptionStyle.Description);
            imageTitle.Location = new Point(14, 10);
            imageInfo = ToolOptionStyle.CreateLabel(
                "이미지를 불러와 주세요.", 10F, FontStyle.Bold, ToolOptionStyle.Accent);
            imageInfo.Location = new Point(14, 33);
            imageCard.Controls.Add(imageTitle);
            imageCard.Controls.Add(imageInfo);

            Panel resultsCard = ToolOptionStyle.CreateCard(244);
            Label resultTitle = ToolOptionStyle.CreateLabel(
                "추천 결과", 10F, FontStyle.Bold, ToolOptionStyle.Title);
            resultTitle.Location = new Point(14, 10);
            resultList = new FlowLayoutPanel
            {
                Location = new Point(14, 37),
                Size = new Size(240, 192),
                Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right,
                FlowDirection = FlowDirection.TopDown,
                WrapContents = false,
                AutoScroll = true,
                BackColor = Color.White,
                Margin = Padding.Empty,
                Padding = Padding.Empty
            };
            resultsCard.Resize += (_, _) =>
            {
                resultList.Width = Math.Max(1, resultsCard.ClientSize.Width - 28);
                UpdateResultButtonWidths();
            };
            resultsCard.Controls.Add(resultTitle);
            resultsCard.Controls.Add(resultList);

            startEditingButton = new Button
            {
                Text = "추천 위치로 편집 시작",
                Enabled = false,
                Height = 38,
                Margin = new Padding(0, 0, 0, 7)
            };
            ToolOptionStyle.StylePrimaryButton(startEditingButton);
            startEditingButton.Click += (_, _) =>
            {
                if (selectedSuggestion != null)
                {
                    StartEditingRequested?.Invoke(this,
                        new AiCaptionSuggestionEventArgs(selectedSuggestion));
                }
            };
            ShowEmptyResult();

            TableLayoutPanel actions = AiQualityEnhancementControl.CreateActions(
                out runButton, out cancelButton);
            runButton.Text = "추천 받기";
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
                "추천 받기를 누르면 사진에 맞춰 자동으로 분석합니다.");

            contentRoot.Controls.Add(imageCard);
            contentRoot.Controls.Add(resultsCard);
            contentRoot.Controls.Add(startEditingButton);
            contentRoot.Controls.Add(actions);
            contentRoot.Controls.Add(progressBar);
            contentRoot.Controls.Add(status);
            Controls.Add(contentRoot);
        }

        public void PrepareForImage(Size imageSize)
        {
            imageInfo.Text = $"{imageSize.Width:N0} × {imageSize.Height:N0} px";
            runButton.Enabled = true;
            ShowEmptyResult();
            SetStatus("추천 받기를 누르면 사진에 맞춰 자동으로 분석합니다.");
            ResetScrollPosition();
        }

        public void SetBusy(bool busy)
        {
            runButton.Enabled = !busy;
            runButton.Text = busy ? "유행어·밈 생성 중..." : "추천 받기";
            cancelButton.Text = busy ? "작업 취소" : "취소";
            progressBar.Visible = busy;
            resultList.Enabled = !busy;
            startEditingButton.Enabled = !busy && selectedSuggestion != null;
        }

        public void SetStatus(string message, bool isError = false)
        {
            status.Text = message;
            status.ForeColor = isError
                ? Color.FromArgb(220, 38, 38)
                : ToolOptionStyle.Description;
        }

        public void SetResults(IReadOnlyList<AiCaptionSuggestion> suggestions)
        {
            resultList.SuspendLayout();
            ClearResults();
            foreach (AiCaptionSuggestion suggestion in suggestions)
            {
                Button button = new Button
                {
                    AutoSize = false,
                    Width = Math.Max(1, resultList.ClientSize.Width -
                        (resultList.VerticalScroll.Visible
                            ? SystemInformation.VerticalScrollBarWidth
                            : 0)),
                    Height = 42,
                    Margin = new Padding(0, 0, 0, 5),
                    Text = suggestion.Text.Trim(),
                    TextAlign = ContentAlignment.MiddleLeft,
                    FlatStyle = FlatStyle.Flat,
                    BackColor = Color.White,
                    ForeColor = ToolOptionStyle.Title,
                    UseVisualStyleBackColor = false,
                    Font = new Font("맑은 고딕", 8.5F)
                };
                button.FlatAppearance.BorderColor = ToolOptionStyle.Border;
                button.Click += (_, _) => SelectSuggestion(button, suggestion);
                suggestionButtons.Add(button);
                resultList.Controls.Add(button);
            }
            resultList.ResumeLayout();
            UpdateResultButtonWidths();
        }

        private void UpdateResultButtonWidths()
        {
            int width = Math.Max(1, resultList.ClientSize.Width -
                (resultList.VerticalScroll.Visible
                    ? SystemInformation.VerticalScrollBarWidth
                    : 0));
            foreach (Button button in suggestionButtons)
                button.Width = width;
        }

        private void ShowEmptyResult()
        {
            ClearResults();
            Label empty = ToolOptionStyle.CreateLabel(
                "아직 추천 결과가 없습니다.", 9F, FontStyle.Regular,
                ToolOptionStyle.Description);
            empty.Margin = new Padding(2, 8, 2, 2);
            resultList.Controls.Add(empty);
        }

        private void ClearResults()
        {
            selectedSuggestion = null;
            suggestionButtons.Clear();
            startEditingButton.Enabled = false;
            while (resultList.Controls.Count > 0)
            {
                Control control = resultList.Controls[0];
                resultList.Controls.RemoveAt(0);
                control.Dispose();
            }
        }

        private void SelectSuggestion(Button selectedButton, AiCaptionSuggestion suggestion)
        {
            selectedSuggestion = suggestion;
            foreach (Button button in suggestionButtons)
            {
                bool selected = ReferenceEquals(button, selectedButton);
                button.BackColor = selected ? ToolOptionStyle.AccentSoft : Color.White;
                button.ForeColor = selected ? ToolOptionStyle.Accent : ToolOptionStyle.Title;
                button.FlatAppearance.BorderColor = selected
                    ? ToolOptionStyle.Accent
                    : ToolOptionStyle.Border;
            }

            startEditingButton.Enabled = true;
            SetStatus("선택한 문구를 추천 위치에서 편집할 수 있습니다.");
        }

        private void ResetScrollPosition()
        {
            contentRoot.AutoScrollPosition = Point.Empty;
            if (!IsHandleCreated)
                return;

            BeginInvoke(new Action(() =>
            {
                if (!IsDisposed)
                    contentRoot.AutoScrollPosition = Point.Empty;
            }));
        }

    }

    public sealed class AiCaptionSuggestionEventArgs : EventArgs
    {
        public AiCaptionSuggestionEventArgs(AiCaptionSuggestion suggestion) =>
            Suggestion = suggestion;

        public AiCaptionSuggestion Suggestion { get; }
    }
}
