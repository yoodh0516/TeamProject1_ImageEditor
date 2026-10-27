using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using ImageEditor.Controls;

namespace ImageEditor.Forms
{
    public partial class BeforeAfterForm : Form
    {
        public BeforeAfterForm(
            Bitmap beforeImage,
            Bitmap afterImage)
        {
            InitializeComponent();
            BuildModernLayout();

            // Before = 처음 불러온 원본
            if (beforeImage != null)
            {
                picBefore.Image =
                    new Bitmap(beforeImage);
            }

            // After = 현재 편집 결과
            if (afterImage != null)
            {
                picAfter.Image =
                    new Bitmap(afterImage);
            }

            EditorToolTipService.Attach(this);
        }

        private void BuildModernLayout()
        {
            SuspendLayout();
            Controls.Clear();

            Text = "Before / After 비교";
            StartPosition = FormStartPosition.CenterParent;
            BackColor = Color.FromArgb(244, 246, 250);
            Font = new Font("맑은 고딕", 9F);
            ClientSize = new Size(1120, 700);
            MinimumSize = new Size(820, 520);
            DoubleBuffered = true;

            Panel header = new Panel
            {
                Dock = DockStyle.Top,
                Height = 82,
                BackColor = Color.White,
                Padding = new Padding(24, 15, 24, 12)
            };
            Label title = new Label
            {
                AutoSize = true,
                Text = "편집 전·후 비교",
                Font = new Font("맑은 고딕", 17F, FontStyle.Bold),
                ForeColor = ToolOptionStyle.Title,
                Location = new Point(24, 13)
            };
            Label description = new Label
            {
                AutoSize = true,
                Text = "처음 불러온 원본과 현재 편집 결과를 같은 크기로 비교합니다.",
                Font = new Font("맑은 고딕", 9F),
                ForeColor = ToolOptionStyle.Description,
                Location = new Point(25, 48)
            };
            Panel headerBorder = new Panel
            {
                Dock = DockStyle.Bottom,
                Height = 1,
                BackColor = ToolOptionStyle.Border
            };
            header.Controls.Add(title);
            header.Controls.Add(description);
            header.Controls.Add(headerBorder);

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

            Panel beforeCard = CreateImageCard(
                "BEFORE", "원본 이미지", "편집을 시작하기 전 상태", picBefore);
            Panel afterCard = CreateImageCard(
                "AFTER", "편집 결과", "현재 적용된 편집 상태", picAfter);
            beforeCard.Margin = new Padding(0, 0, 8, 0);
            afterCard.Margin = new Padding(8, 0, 0, 0);
            comparison.Controls.Add(beforeCard, 0, 0);
            comparison.Controls.Add(afterCard, 1, 0);

            Panel footer = new Panel
            {
                Dock = DockStyle.Bottom,
                Height = 66,
                BackColor = Color.White,
                Padding = new Padding(20, 13, 20, 13)
            };
            Label hint = new Label
            {
                AutoSize = true,
                Text = "비교 화면에서는 이미지가 변경되지 않습니다.",
                ForeColor = ToolOptionStyle.Description,
                Location = new Point(22, 23)
            };
            button1.Text = "닫기";
            button1.Size = new Size(96, 38);
            button1.Anchor = AnchorStyles.Top | AnchorStyles.Right;
            button1.Location = new Point(footer.ClientSize.Width - button1.Width - 20, 13);
            button1.BackColor = Color.White;
            button1.ForeColor = ToolOptionStyle.Title;
            button1.FlatStyle = FlatStyle.Flat;
            button1.FlatAppearance.BorderColor = ToolOptionStyle.Border;
            button1.UseVisualStyleBackColor = false;
            footer.Resize += (_, _) =>
                button1.Left = Math.Max(20, footer.ClientSize.Width - button1.Width - 20);
            footer.Controls.Add(hint);
            footer.Controls.Add(button1);

            Controls.Add(comparison);
            Controls.Add(footer);
            Controls.Add(header);
            ResumeLayout(true);
        }

        private static Panel CreateImageCard(
            string badgeText,
            string titleText,
            string descriptionText,
            PictureBox pictureBox)
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
                ForeColor = ToolOptionStyle.Accent,
                BackColor = ToolOptionStyle.AccentSoft,
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
            Label description = new Label
            {
                AutoSize = true,
                Text = descriptionText,
                Font = new Font("맑은 고딕", 8.5F),
                ForeColor = ToolOptionStyle.Description,
                Location = new Point(72, 27)
            };
            cardHeader.Controls.Add(badge);
            cardHeader.Controls.Add(title);
            cardHeader.Controls.Add(description);

            Panel imageHost = new Panel
            {
                Dock = DockStyle.Fill,
                BackColor = Color.FromArgb(238, 241, 247),
                Padding = new Padding(12)
            };
            pictureBox.Dock = DockStyle.Fill;
            pictureBox.BackColor = Color.FromArgb(238, 241, 247);
            pictureBox.BorderStyle = BorderStyle.None;
            pictureBox.SizeMode = PictureBoxSizeMode.Zoom;
            imageHost.Controls.Add(pictureBox);
            card.Controls.Add(imageHost);
            card.Controls.Add(cardHeader);
            return card;
        }

        private void button1_Click(object sender, EventArgs e)
        {
            Close();
        }

        // Form이 닫힐 때 복사해서 만든 이미지 정리
        protected override void OnFormClosed(FormClosedEventArgs e)
        {
            picBefore.Image?.Dispose();
            picAfter.Image?.Dispose();

            base.OnFormClosed(e);
        }
    }
}
