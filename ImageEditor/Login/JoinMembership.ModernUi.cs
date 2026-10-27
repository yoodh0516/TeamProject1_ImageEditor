using System.Drawing;
using System.Windows.Forms;

namespace ImageEditor.Login
{
    public partial class JoinMembership
    {
        private void InitializeModernUi()
        {
            AuthUiStyle.PrepareForm(this, new Size(720, 560), "ImageEditor · 회원가입");
            TableLayoutPanel shell = AuthUiStyle.CreateSplitShell(240);
            shell.Controls.Add(AuthUiStyle.CreateBrandPanel(
                "나만의 편집\r\n공간을 만드세요",
                "계정을 만들면 프로젝트와 편집 스타일을\r\n간편하게 보관할 수 있습니다."), 0, 0);

            Panel content = AuthUiStyle.CreateContentPanel(new Padding(56, 28, 56, 26));
            TableLayoutPanel layout = new TableLayoutPanel
            {
                Dock = DockStyle.Fill,
                ColumnCount = 1,
                RowCount = 9,
                BackColor = Color.White
            };
            layout.RowStyles.Add(new RowStyle(SizeType.Absolute, 40F));
            layout.RowStyles.Add(new RowStyle(SizeType.Absolute, 34F));
            for (int i = 0; i < 5; i++)
                layout.RowStyles.Add(new RowStyle(SizeType.Absolute, 64F));
            layout.RowStyles.Add(new RowStyle(SizeType.Percent, 100F));
            layout.RowStyles.Add(new RowStyle(SizeType.Absolute, 38F));

            lblTitle = AuthUiStyle.CreateTitle("회원가입");
            Label description = AuthUiStyle.CreateDescription(
                "필수 정보를 입력하고 ImageEditor를 시작하세요.");
            label1.Text = "아이디";
            label2.Text = "비밀번호";
            label3.Text = "비밀번호 확인";
            label4.Text = "이름";
            label5.Text = "이메일";
            AuthUiStyle.StylePrimaryButton(createMember, "회원가입 완료");

            layout.Controls.Add(lblTitle, 0, 0);
            layout.Controls.Add(description, 0, 1);
            layout.Controls.Add(AuthUiStyle.CreateField(label1, tbID, "사용할 아이디"), 0, 2);
            layout.Controls.Add(AuthUiStyle.CreateField(label2, tbPASS, "비밀번호"), 0, 3);
            layout.Controls.Add(AuthUiStyle.CreateField(label3, tbPASSCheck, "비밀번호 다시 입력"), 0, 4);
            layout.Controls.Add(AuthUiStyle.CreateField(label4, tbNAME, "이름"), 0, 5);
            layout.Controls.Add(AuthUiStyle.CreateField(label5, tbEMAIL, "example@email.com"), 0, 6);
            layout.Controls.Add(AuthUiStyle.CreateBackActionRow(this, createMember), 0, 8);
            content.Controls.Add(layout);
            shell.Controls.Add(content, 1, 0);
            Controls.Add(shell);
            ResumeLayout(true);
        }
    }
}
