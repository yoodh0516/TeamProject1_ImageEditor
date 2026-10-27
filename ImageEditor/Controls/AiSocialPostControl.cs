using System;
using System.Drawing;
using System.Windows.Forms;

namespace ImageEditor.Controls
{
    /// <summary>
    /// Generates copy-ready SNS text without changing the editor image or history.
    /// </summary>
    public sealed class AiSocialPostControl : UserControl
    {
        private readonly Label imageInfo;
        private readonly ComboBox platformCombo;
        private readonly ComboBox toneCombo;
        private readonly ComboBox lengthCombo;
        private readonly RichTextBox postResult;
        private readonly TextBox hashtagResult;
        private readonly Button copyPostButton;
        private readonly Button copyHashtagsButton;
        private readonly Button runButton;
        private readonly Button cancelButton;
        private readonly ProgressBar progressBar;
        private readonly Label status;

        public event EventHandler? RunRequested;
        public event EventHandler? CancelRequested;
        public event EventHandler? CloseRequested;

        public string Platform => platformCombo.SelectedItem?.ToString() ?? "Instagram";
        public string Tone => toneCombo.SelectedItem?.ToString() ?? "감성적";
        public string PostLength => lengthCombo.SelectedItem?.ToString() ?? "보통";

        public AiSocialPostControl()
        {
            BackColor = ToolOptionStyle.PanelBackground;
            AutoScroll = false;

            FlowLayoutPanel root = AiQualityEnhancementControl.CreateRoot();
            root.Controls.Add(AiQualityEnhancementControl.CreateHeading(
                "게시글·해시태그 추천",
                "사진을 분석해 SNS용 게시글 2문장과 해시태그를 추천합니다."));

            imageInfo = new Label
            {
                AutoSize = false,
                Height = 24,
                Margin = new Padding(0, 0, 0, 8),
                Text = "현재 이미지 · 이미지를 불러와 주세요.",
                Font = new Font("맑은 고딕", 9F, FontStyle.Bold),
                ForeColor = ToolOptionStyle.Accent,
                TextAlign = ContentAlignment.MiddleLeft
            };

            Panel settingsCard = ToolOptionStyle.CreateCard(181);
            Label settingsTitle = ToolOptionStyle.CreateLabel(
                "추천 설정", 10F, FontStyle.Bold, ToolOptionStyle.Title);
            settingsTitle.Location = new Point(14, 10);
            platformCombo = CreateComboBox(new[] { "Instagram", "Threads", "Facebook", "X" });
            toneCombo = CreateComboBox(new[] { "감성적", "일상", "유머", "깔끔한", "힙한", "친근한" });
            lengthCombo = CreateComboBox(new[] { "짧게", "보통", "길게" });
            platformCombo.Name = "cmbSocialPlatform";
            toneCombo.Name = "cmbPostTone";
            lengthCombo.Name = "cmbPostLength";
            AddSettingRow(settingsCard, "SNS 플랫폼", platformCombo, 37);
            AddSettingRow(settingsCard, "게시글 분위기", toneCombo, 73);
            AddSettingRow(settingsCard, "게시글 길이", lengthCombo, 109);
            Label guide = ToolOptionStyle.CreateWrappedLabel(
                "※ 결과는 이미지에 적용되지 않습니다. 복사해 SNS에서 사용하세요.",
                8F, FontStyle.Regular, ToolOptionStyle.Description, 28);
            guide.Location = new Point(14, 145);
            ToolOptionStyle.FillCardControl(settingsCard, guide);
            settingsCard.Controls.Add(settingsTitle);
            settingsCard.Controls.Add(guide);
            platformCombo.SelectedIndex = 0;
            toneCombo.SelectedIndex = 0;
            lengthCombo.SelectedIndex = 1;

            Panel resultCard = ToolOptionStyle.CreateCard(278);
            Label resultTitle = ToolOptionStyle.CreateLabel(
                "추천 결과", 10F, FontStyle.Bold, ToolOptionStyle.Title);
            resultTitle.Location = new Point(14, 10);
            Label postTitle = ToolOptionStyle.CreateLabel(
                "게시글", 8.5F, FontStyle.Bold, ToolOptionStyle.Description);
            postTitle.Location = new Point(14, 37);
            postResult = new RichTextBox
            {
                Name = "txtPostResult",
                Location = new Point(14, 58),
                Height = 54,
                ReadOnly = true,
                BackColor = Color.White,
                BorderStyle = BorderStyle.FixedSingle,
                Font = new Font("맑은 고딕", 9F),
                Text = "아직 추천 결과가 없습니다.",
                ForeColor = ToolOptionStyle.Description,
                ScrollBars = RichTextBoxScrollBars.Vertical,
                Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right
            };
            copyPostButton = ToolOptionStyle.CreateSecondaryButton("게시글 복사");
            copyPostButton.Location = new Point(14, 118);
            copyPostButton.Height = 34;
            copyPostButton.Enabled = false;
            copyPostButton.Click += (_, _) => CopyResult(postResult.Text, "게시글");
            Label hashtagTitle = ToolOptionStyle.CreateLabel(
                "해시태그", 8.5F, FontStyle.Bold, ToolOptionStyle.Description);
            hashtagTitle.Location = new Point(14, 162);
            hashtagResult = new TextBox
            {
                Name = "txtHashtagResult",
                Location = new Point(14, 183),
                Height = 39,
                Multiline = true,
                ReadOnly = true,
                BackColor = Color.White,
                BorderStyle = BorderStyle.FixedSingle,
                Font = new Font("맑은 고딕", 8.5F),
                Text = "아직 추천 결과가 없습니다.",
                ForeColor = ToolOptionStyle.Description,
                ScrollBars = ScrollBars.Vertical,
                Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right
            };
            copyHashtagsButton = ToolOptionStyle.CreateSecondaryButton("해시태그 복사");
            copyHashtagsButton.Location = new Point(14, 228);
            copyHashtagsButton.Height = 34;
            copyHashtagsButton.Enabled = false;
            copyHashtagsButton.Click += (_, _) => CopyResult(hashtagResult.Text, "해시태그");
            resultCard.Resize += (_, _) =>
            {
                int width = Math.Max(1, resultCard.ClientSize.Width - 28);
                postResult.Width = width;
                copyPostButton.Width = width;
                hashtagResult.Width = width;
                copyHashtagsButton.Width = width;
            };
            resultCard.Controls.Add(resultTitle);
            resultCard.Controls.Add(postTitle);
            resultCard.Controls.Add(postResult);
            resultCard.Controls.Add(copyPostButton);
            resultCard.Controls.Add(hashtagTitle);
            resultCard.Controls.Add(hashtagResult);
            resultCard.Controls.Add(copyHashtagsButton);

            TableLayoutPanel actions = AiQualityEnhancementControl.CreateActions(
                out runButton, out cancelButton);
            runButton.Text = "AI 게시글 추천 시작";
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
                "사진과 추천 설정을 확인한 뒤 시작하세요.");

            TableLayoutPanel footer = new TableLayoutPanel
            {
                Dock = DockStyle.Fill,
                BackColor = ToolOptionStyle.PanelBackground,
                Padding = new Padding(12, 5, 12, 5),
                Margin = Padding.Empty,
                ColumnCount = 1,
                RowCount = 3
            };
            footer.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100F));
            footer.RowStyles.Add(new RowStyle(SizeType.Absolute, 39F));
            footer.RowStyles.Add(new RowStyle(SizeType.Absolute, 7F));
            footer.RowStyles.Add(new RowStyle(SizeType.Percent, 100F));
            actions.Dock = DockStyle.Fill;
            actions.Margin = Padding.Empty;
            progressBar.Dock = DockStyle.Fill;
            progressBar.Margin = new Padding(0, 1, 0, 1);
            status.Dock = DockStyle.Fill;
            status.Margin = Padding.Empty;
            footer.Controls.Add(actions, 0, 0);
            footer.Controls.Add(progressBar, 0, 1);
            footer.Controls.Add(status, 0, 2);

            TableLayoutPanel shell = new TableLayoutPanel
            {
                Dock = DockStyle.Fill,
                BackColor = ToolOptionStyle.PanelBackground,
                Margin = Padding.Empty,
                Padding = Padding.Empty,
                ColumnCount = 1,
                RowCount = 2
            };
            shell.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100F));
            shell.RowStyles.Add(new RowStyle(SizeType.Percent, 100F));
            shell.RowStyles.Add(new RowStyle(SizeType.Absolute, 86F));

            root.Controls.Add(imageInfo);
            root.Controls.Add(settingsCard);
            root.Controls.Add(resultCard);
            shell.Controls.Add(root, 0, 0);
            shell.Controls.Add(footer, 0, 1);
            Controls.Add(shell);
        }

        public void PrepareForImage(Size imageSize)
        {
            imageInfo.Text = $"현재 이미지 · {imageSize.Width:N0} × {imageSize.Height:N0} px";
            postResult.Text = "아직 추천 결과가 없습니다.";
            postResult.ForeColor = ToolOptionStyle.Description;
            hashtagResult.Text = "아직 추천 결과가 없습니다.";
            hashtagResult.ForeColor = ToolOptionStyle.Description;
            copyPostButton.Enabled = false;
            copyHashtagsButton.Enabled = false;
            runButton.Enabled = true;
            SetStatus("사진과 추천 설정을 확인한 뒤 시작하세요.");
        }

        public void SetBusy(bool busy)
        {
            runButton.Enabled = !busy;
            runButton.Text = busy ? "게시글 생성 중..." : "AI 게시글 추천 시작";
            cancelButton.Text = busy ? "작업 취소" : "취소";
            platformCombo.Enabled = !busy;
            toneCombo.Enabled = !busy;
            lengthCombo.Enabled = !busy;
            copyPostButton.Enabled = !busy && HasPostResult();
            copyHashtagsButton.Enabled = !busy && HasHashtagResult();
            progressBar.Visible = busy;
        }

        public void SetResult(string post, string hashtags)
        {
            postResult.Text = post.Trim();
            postResult.ForeColor = ToolOptionStyle.Title;
            hashtagResult.Text = hashtags.Trim();
            hashtagResult.ForeColor = ToolOptionStyle.Accent;
            copyPostButton.Enabled = HasPostResult();
            copyHashtagsButton.Enabled = HasHashtagResult();
        }

        public void SetStatus(string message, bool isError = false)
        {
            status.Text = message;
            status.ForeColor = isError
                ? Color.FromArgb(220, 38, 38)
                : ToolOptionStyle.Description;
        }

        private static ComboBox CreateComboBox(string[] items)
        {
            ComboBox combo = new ComboBox
            {
                DropDownStyle = ComboBoxStyle.DropDownList,
                Font = new Font("맑은 고딕", 9F),
                Height = 28,
                Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right
            };
            combo.Items.AddRange(items);
            return combo;
        }

        private static void AddSettingRow(
            Panel card,
            string labelText,
            ComboBox combo,
            int top)
        {
            Label label = ToolOptionStyle.CreateLabel(
                labelText, 8.5F, FontStyle.Regular, ToolOptionStyle.Description);
            label.Location = new Point(14, top + 5);
            label.AutoSize = false;
            label.Size = new Size(86, 22);
            combo.Location = new Point(103, top);
            card.Resize += (_, _) =>
                combo.Width = Math.Max(80, card.ClientSize.Width - combo.Left - 14);
            combo.Width = Math.Max(80, card.ClientSize.Width - combo.Left - 14);
            card.Controls.Add(label);
            card.Controls.Add(combo);
        }

        private bool HasPostResult() =>
            !string.IsNullOrWhiteSpace(postResult.Text) &&
            postResult.Text != "아직 추천 결과가 없습니다.";

        private bool HasHashtagResult() =>
            !string.IsNullOrWhiteSpace(hashtagResult.Text) &&
            hashtagResult.Text != "아직 추천 결과가 없습니다.";

        private void CopyResult(string text, string label)
        {
            if (string.IsNullOrWhiteSpace(text))
                return;

            try
            {
                Clipboard.SetText(text.Trim());
                SetStatus($"{label}를 클립보드에 복사했습니다.");
            }
            catch (Exception ex)
            {
                SetStatus($"{label} 복사에 실패했습니다: {ex.Message}", true);
            }
        }
    }
}
