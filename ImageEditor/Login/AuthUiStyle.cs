using System;
using System.Drawing;
using System.Windows.Forms;
using ImageEditor.Controls;

namespace ImageEditor.Login
{
    internal static class AuthUiStyle
    {
        internal static readonly Color Accent = Color.FromArgb(64, 101, 242);
        internal static readonly Color AccentDark = Color.FromArgb(48, 78, 202);
        internal static readonly Color AccentSoft = Color.FromArgb(235, 240, 255);
        internal static readonly Color WindowBackground = Color.FromArgb(243, 246, 251);
        internal static readonly Color TextPrimary = Color.FromArgb(28, 36, 50);
        internal static readonly Color TextMuted = Color.FromArgb(105, 116, 135);
        internal static readonly Color Border = Color.FromArgb(216, 222, 232);

        internal static void PrepareForm(Form form, Size clientSize, string title)
        {
            form.SuspendLayout();
            form.Controls.Clear();
            form.AutoScaleMode = AutoScaleMode.Dpi;
            form.BackColor = WindowBackground;
            form.ClientSize = clientSize;
            form.Font = new Font("맑은 고딕", 9F);
            form.FormBorderStyle = FormBorderStyle.FixedDialog;
            form.MaximizeBox = false;
            form.MinimizeBox = false;
            form.StartPosition = FormStartPosition.CenterParent;
            form.Text = title;
            EditorToolTipService.Attach(form);
        }

        internal static TableLayoutPanel CreateSplitShell(int brandWidth)
        {
            TableLayoutPanel root = new TableLayoutPanel
            {
                Dock = DockStyle.Fill,
                ColumnCount = 2,
                RowCount = 1,
                BackColor = Color.White,
                Margin = Padding.Empty,
                Padding = Padding.Empty
            };
            root.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, brandWidth));
            root.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100F));
            root.RowStyles.Add(new RowStyle(SizeType.Percent, 100F));
            return root;
        }

        internal static Control CreateBrandPanel(string headline, string description)
        {
            Panel panel = new Panel
            {
                Dock = DockStyle.Fill,
                BackColor = AccentSoft,
                Padding = new Padding(24, 30, 22, 24),
                Margin = Padding.Empty
            };

            TableLayoutPanel layout = new TableLayoutPanel
            {
                Dock = DockStyle.Fill,
                ColumnCount = 1,
                RowCount = 6,
                BackColor = Color.Transparent,
                Margin = Padding.Empty
            };
            layout.RowStyles.Add(new RowStyle(SizeType.Absolute, 48F));
            layout.RowStyles.Add(new RowStyle(SizeType.Absolute, 58F));
            layout.RowStyles.Add(new RowStyle(SizeType.Absolute, 52F));
            layout.RowStyles.Add(new RowStyle(SizeType.Absolute, 62F));
            layout.RowStyles.Add(new RowStyle(SizeType.Percent, 100F));
            layout.RowStyles.Add(new RowStyle(SizeType.Absolute, 44F));

            TableLayoutPanel brand = new TableLayoutPanel
            {
                Dock = DockStyle.Fill,
                ColumnCount = 2,
                RowCount = 1,
                Margin = Padding.Empty
            };
            brand.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 44F));
            brand.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100F));

            Label mark = new Label
            {
                Text = "IE",
                Dock = DockStyle.Fill,
                Margin = new Padding(0, 0, 10, 8),
                BackColor = Accent,
                ForeColor = Color.White,
                Font = new Font("Segoe UI", 10F, FontStyle.Bold),
                TextAlign = ContentAlignment.MiddleCenter
            };
            Label brandName = new Label
            {
                Text = "IMAGE EDITOR\r\nCONTENT STUDIO",
                Dock = DockStyle.Fill,
                ForeColor = TextPrimary,
                Font = new Font("Segoe UI", 9F, FontStyle.Bold),
                TextAlign = ContentAlignment.MiddleLeft
            };
            brand.Controls.Add(mark, 0, 0);
            brand.Controls.Add(brandName, 1, 0);

            Label headlineLabel = new Label
            {
                Text = headline,
                Dock = DockStyle.Fill,
                Font = new Font("맑은 고딕", 14.5F, FontStyle.Bold),
                ForeColor = TextPrimary,
                TextAlign = ContentAlignment.BottomLeft
            };
            Label descriptionLabel = new Label
            {
                Text = description,
                Dock = DockStyle.Fill,
                Font = new Font("맑은 고딕", 8.25F),
                ForeColor = TextMuted,
                TextAlign = ContentAlignment.TopLeft
            };
            Label featureLabel = new Label
            {
                Text = "✓ 편집 기록과 프로젝트 저장\r\n✓ AI 편집 및 SNS 콘텐츠 도구",
                Dock = DockStyle.Fill,
                Font = new Font("맑은 고딕", 8.25F),
                ForeColor = AccentDark,
                TextAlign = ContentAlignment.MiddleLeft
            };
            Label footer = new Label
            {
                Text = "쉽고 빠른 SNS 이미지 편집",
                Dock = DockStyle.Fill,
                ForeColor = TextMuted,
                Font = new Font("맑은 고딕", 8F),
                TextAlign = ContentAlignment.BottomLeft
            };

            layout.Controls.Add(brand, 0, 0);
            layout.Controls.Add(headlineLabel, 0, 1);
            layout.Controls.Add(descriptionLabel, 0, 2);
            layout.Controls.Add(featureLabel, 0, 3);
            layout.Controls.Add(footer, 0, 5);
            panel.Controls.Add(layout);
            return panel;
        }

        internal static Panel CreateContentPanel(Padding padding)
        {
            return new Panel
            {
                Dock = DockStyle.Fill,
                BackColor = Color.White,
                Padding = padding,
                Margin = Padding.Empty,
                AutoScroll = true
            };
        }

        internal static Label CreateTitle(string text)
        {
            return new Label
            {
                Text = text,
                Dock = DockStyle.Fill,
                Font = new Font("맑은 고딕", 17F, FontStyle.Bold),
                ForeColor = TextPrimary,
                TextAlign = ContentAlignment.BottomLeft
            };
        }

        internal static Label CreateDescription(string text)
        {
            return new Label
            {
                Text = text,
                Dock = DockStyle.Fill,
                Font = new Font("맑은 고딕", 8.5F),
                ForeColor = TextMuted,
                TextAlign = ContentAlignment.TopLeft
            };
        }

        internal static Control CreateField(
            string labelText,
            TextBox textBox,
            string placeholder)
        {
            Label label = new Label
            {
                Text = labelText,
                Dock = DockStyle.Fill,
                Font = new Font("맑은 고딕", 9F, FontStyle.Bold),
                ForeColor = TextPrimary,
                TextAlign = ContentAlignment.MiddleLeft
            };
            return CreateField(label, textBox, placeholder);
        }

        internal static Control CreateField(
            Label label,
            TextBox textBox,
            string placeholder)
        {
            label.Dock = DockStyle.Fill;
            label.AutoSize = false;
            label.Font = new Font("맑은 고딕", 9F, FontStyle.Bold);
            label.ForeColor = TextPrimary;
            label.TextAlign = ContentAlignment.MiddleLeft;

            textBox.BorderStyle = BorderStyle.None;
            textBox.Dock = DockStyle.Fill;
            textBox.Font = new Font("맑은 고딕", 10F);
            textBox.BackColor = Color.White;
            textBox.PlaceholderText = placeholder;
            textBox.Margin = Padding.Empty;

            Panel inputHost = new Panel
            {
                Dock = DockStyle.Fill,
                BackColor = Color.White,
                BorderStyle = BorderStyle.FixedSingle,
                Padding = new Padding(11, 8, 11, 7),
                Margin = Padding.Empty
            };
            inputHost.Controls.Add(textBox);
            textBox.Enter += (_, _) => inputHost.BackColor = Color.FromArgb(249, 251, 255);
            textBox.Leave += (_, _) => inputHost.BackColor = Color.White;

            TableLayoutPanel field = new TableLayoutPanel
            {
                Dock = DockStyle.Fill,
                ColumnCount = 1,
                RowCount = 2,
                Margin = new Padding(0, 0, 0, 6)
            };
            field.RowStyles.Add(new RowStyle(SizeType.Absolute, 22F));
            field.RowStyles.Add(new RowStyle(SizeType.Absolute, 38F));
            field.Controls.Add(label, 0, 0);
            field.Controls.Add(inputHost, 0, 1);
            return field;
        }

        internal static void StylePrimaryButton(Button button, string text)
        {
            button.Text = text;
            button.Dock = DockStyle.Fill;
            button.FlatStyle = FlatStyle.Flat;
            button.FlatAppearance.BorderSize = 0;
            button.BackColor = Accent;
            button.ForeColor = Color.White;
            button.Font = new Font("맑은 고딕", 9.5F, FontStyle.Bold);
            button.Cursor = Cursors.Hand;
            button.UseVisualStyleBackColor = false;
            button.Margin = Padding.Empty;
            button.MouseEnter += (_, _) => button.BackColor = AccentDark;
            button.MouseLeave += (_, _) => button.BackColor = Accent;
        }

        internal static void StyleSecondaryButton(Button button, string text)
        {
            button.Text = text;
            button.Dock = DockStyle.Fill;
            button.FlatStyle = FlatStyle.Flat;
            button.FlatAppearance.BorderColor = Border;
            button.FlatAppearance.BorderSize = 1;
            button.BackColor = Color.White;
            button.ForeColor = TextPrimary;
            button.Font = new Font("맑은 고딕", 9F, FontStyle.Bold);
            button.Cursor = Cursors.Hand;
            button.UseVisualStyleBackColor = false;
            button.Margin = Padding.Empty;
        }

        internal static Control CreateBackActionRow(Form owner, Button primaryButton)
        {
            Button backButton = new Button
            {
                Name = "btnBack",
                CausesValidation = false,
                TabStop = true
            };
            StyleSecondaryButton(backButton, "← 뒤로가기");
            backButton.Click += (_, _) => owner.Close();

            TableLayoutPanel actions = new TableLayoutPanel
            {
                Dock = DockStyle.Fill,
                ColumnCount = 2,
                RowCount = 1,
                Margin = Padding.Empty,
                Padding = Padding.Empty
            };
            actions.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 35F));
            actions.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 65F));
            backButton.Margin = new Padding(0, 0, 5, 0);
            primaryButton.Margin = new Padding(5, 0, 0, 0);
            actions.Controls.Add(backButton, 0, 0);
            actions.Controls.Add(primaryButton, 1, 0);
            return actions;
        }

        internal static void StyleLink(Label label)
        {
            label.AutoSize = false;
            label.Dock = DockStyle.Fill;
            label.ForeColor = Accent;
            label.Font = new Font("맑은 고딕", 9F, FontStyle.Bold);
            label.Cursor = Cursors.Hand;
            label.TextAlign = ContentAlignment.MiddleLeft;
        }

        internal static void SetSegmentButton(Button button, bool selected)
        {
            button.FlatStyle = FlatStyle.Flat;
            button.FlatAppearance.BorderColor = selected ? Accent : Border;
            button.FlatAppearance.BorderSize = 1;
            button.BackColor = selected ? AccentSoft : Color.White;
            button.ForeColor = selected ? Accent : TextMuted;
            button.Font = new Font("맑은 고딕", 9F, FontStyle.Bold);
            button.Cursor = Cursors.Hand;
            button.UseVisualStyleBackColor = false;
        }
    }
}
