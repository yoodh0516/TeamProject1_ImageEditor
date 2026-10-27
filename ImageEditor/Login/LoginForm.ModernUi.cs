using System.Drawing;
using System.Windows.Forms;

namespace ImageEditor.Login
{
    public partial class LoginForm
    {
        private void InitializeModernUi()
        {
            AuthUiStyle.PrepareForm(this, new Size(720, 440), "ImageEditor · 로그인");
            TableLayoutPanel shell = AuthUiStyle.CreateSplitShell(240);
            shell.Controls.Add(AuthUiStyle.CreateBrandPanel(
                "당신의 편집을\r\n이어가세요",
                "로그인하고 프로젝트와 편집 기록을\r\n안전하게 관리하세요."), 0, 0);

            Panel content = AuthUiStyle.CreateContentPanel(new Padding(56, 28, 56, 26));
            TableLayoutPanel layout = new TableLayoutPanel
            {
                Dock = DockStyle.Fill,
                ColumnCount = 1,
                RowCount = 10,
                BackColor = Color.White
            };
            layout.RowStyles.Add(new RowStyle(SizeType.Absolute, 40F));
            layout.RowStyles.Add(new RowStyle(SizeType.Absolute, 34F));
            layout.RowStyles.Add(new RowStyle(SizeType.Absolute, 64F));
            layout.RowStyles.Add(new RowStyle(SizeType.Absolute, 64F));
            layout.RowStyles.Add(new RowStyle(SizeType.Absolute, 40F));
            layout.RowStyles.Add(new RowStyle(SizeType.Absolute, 34F));
            layout.RowStyles.Add(new RowStyle(SizeType.Percent, 100F));
            layout.RowStyles.Add(new RowStyle(SizeType.Absolute, 14F));
            layout.RowStyles.Add(new RowStyle(SizeType.Absolute, 38F));
            layout.RowStyles.Add(new RowStyle(SizeType.Absolute, 18F));

            lblTitle.Text = "로그인";
            lblTitle = AuthUiStyle.CreateTitle("로그인");
            Label description = AuthUiStyle.CreateDescription(
                "ImageEditor 계정으로 편집 작업을 이어가세요.");

            AuthUiStyle.StylePrimaryButton(btnLogin, "로그인");
            AuthUiStyle.StyleSecondaryButton(btnNotLogin, "비회원으로 시작");
            lbFindID.Text = "아이디 찾기 · 비밀번호 찾기";
            lbjoinMembership.Text = "회원가입";
            AuthUiStyle.StyleLink(lbFindID);
            AuthUiStyle.StyleLink(lbjoinMembership);
            lbjoinMembership.TextAlign = ContentAlignment.MiddleRight;

            TableLayoutPanel links = new TableLayoutPanel
            {
                Dock = DockStyle.Fill,
                ColumnCount = 2,
                RowCount = 1,
                Margin = Padding.Empty
            };
            links.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 65F));
            links.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 35F));
            links.Controls.Add(lbFindID, 0, 0);
            links.Controls.Add(lbjoinMembership, 1, 0);

            Label divider = new Label
            {
                Text = "────────  또는  ────────",
                Dock = DockStyle.Fill,
                ForeColor = AuthUiStyle.Border,
                TextAlign = ContentAlignment.MiddleCenter,
                Font = new Font("맑은 고딕", 8F)
            };
            Label guestNote = new Label
            {
                Text = "비회원은 개인 프로젝트 기록이 저장되지 않습니다.",
                Dock = DockStyle.Fill,
                ForeColor = AuthUiStyle.TextMuted,
                TextAlign = ContentAlignment.BottomCenter,
                Font = new Font("맑은 고딕", 8F)
            };

            layout.Controls.Add(lblTitle, 0, 0);
            layout.Controls.Add(description, 0, 1);
            layout.Controls.Add(AuthUiStyle.CreateField("아이디", tbID, "아이디 입력"), 0, 2);
            layout.Controls.Add(AuthUiStyle.CreateField("비밀번호", tbPASS, "비밀번호 입력"), 0, 3);
            layout.Controls.Add(btnLogin, 0, 4);
            layout.Controls.Add(links, 0, 5);
            layout.Controls.Add(divider, 0, 7);
            layout.Controls.Add(btnNotLogin, 0, 8);
            layout.Controls.Add(guestNote, 0, 9);
            content.Controls.Add(layout);
            shell.Controls.Add(content, 1, 0);
            Controls.Add(shell);
            ResumeLayout(true);
        }
    }
}
