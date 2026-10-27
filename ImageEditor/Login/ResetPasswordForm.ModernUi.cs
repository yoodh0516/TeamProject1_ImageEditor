using System.Drawing;
using System.Windows.Forms;

namespace ImageEditor.Login
{
    public partial class ResetPasswordForm
    {
        private void InitializeModernUi()
        {
            AuthUiStyle.PrepareForm(this, new Size(720, 440), "ImageEditor · 비밀번호 찾기");
            TableLayoutPanel shell = AuthUiStyle.CreateSplitShell(240);
            shell.Controls.Add(AuthUiStyle.CreateBrandPanel(
                "비밀번호\r\n찾기",
                "본인 확인이 완료됐습니다.\r\n사용할 비밀번호를 입력해 주세요."), 0, 0);

            Panel content = AuthUiStyle.CreateContentPanel(new Padding(56, 28, 56, 26));
            TableLayoutPanel layout = new TableLayoutPanel
            {
                Dock = DockStyle.Fill,
                ColumnCount = 1,
                RowCount = 7,
                BackColor = Color.White
            };
            layout.RowStyles.Add(new RowStyle(SizeType.Absolute, 40F));
            layout.RowStyles.Add(new RowStyle(SizeType.Absolute, 34F));
            layout.RowStyles.Add(new RowStyle(SizeType.Absolute, 64F));
            layout.RowStyles.Add(new RowStyle(SizeType.Absolute, 64F));
            layout.RowStyles.Add(new RowStyle(SizeType.Absolute, 10F));
            layout.RowStyles.Add(new RowStyle(SizeType.Absolute, 38F));
            layout.RowStyles.Add(new RowStyle(SizeType.Percent, 100F));

            lblTitle = AuthUiStyle.CreateTitle("비밀번호 찾기");
            Label description = AuthUiStyle.CreateDescription(
                "새로 사용할 비밀번호를 두 번 입력해 주세요.");
            AuthUiStyle.StylePrimaryButton(btnConfirm, "확인");
            Label hint = new Label
            {
                Text = "두 입력값이 일치해야 완료할 수 있습니다.",
                Dock = DockStyle.Fill,
                ForeColor = AuthUiStyle.TextMuted,
                Font = new Font("맑은 고딕", 8.5F),
                TextAlign = ContentAlignment.TopLeft,
                Padding = new Padding(0, 12, 0, 0)
            };

            layout.Controls.Add(lblTitle, 0, 0);
            layout.Controls.Add(description, 0, 1);
            layout.Controls.Add(AuthUiStyle.CreateField("새 비밀번호", tbResetPASS, "새 비밀번호"), 0, 2);
            layout.Controls.Add(AuthUiStyle.CreateField("비밀번호 확인", tbResetPASSCheck, "새 비밀번호 다시 입력"), 0, 3);
            layout.Controls.Add(AuthUiStyle.CreateBackActionRow(this, btnConfirm), 0, 5);
            layout.Controls.Add(hint, 0, 6);
            content.Controls.Add(layout);
            shell.Controls.Add(content, 1, 0);
            Controls.Add(shell);
            ResumeLayout(true);
        }
    }
}
