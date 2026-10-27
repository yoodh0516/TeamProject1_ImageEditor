using System.Drawing;
using System.Windows.Forms;

namespace ImageEditor.Login
{
    public partial class FindIDForm
    {
        private void InitializeModernUi()
        {
            AuthUiStyle.PrepareForm(this, new Size(720, 480), "ImageEditor · 계정 찾기");
            TableLayoutPanel shell = AuthUiStyle.CreateSplitShell(240);
            shell.Controls.Add(AuthUiStyle.CreateBrandPanel(
                "계정 정보를\r\n다시 확인하세요",
                "가입할 때 입력한 정보로 아이디를 찾거나\r\n비밀번호를 찾을 수 있습니다."), 0, 0);

            Panel content = AuthUiStyle.CreateContentPanel(new Padding(56, 28, 56, 26));
            TableLayoutPanel layout = new TableLayoutPanel
            {
                Dock = DockStyle.Fill,
                ColumnCount = 1,
                RowCount = 5,
                BackColor = Color.White
            };
            layout.RowStyles.Add(new RowStyle(SizeType.Absolute, 40F));
            layout.RowStyles.Add(new RowStyle(SizeType.Absolute, 34F));
            layout.RowStyles.Add(new RowStyle(SizeType.Absolute, 40F));
            layout.RowStyles.Add(new RowStyle(SizeType.Absolute, 10F));
            layout.RowStyles.Add(new RowStyle(SizeType.Percent, 100F));

            Label title = AuthUiStyle.CreateTitle("계정 찾기");
            Label description = AuthUiStyle.CreateDescription(
                "필요한 계정 도움 방식을 선택해 주세요.");

            TableLayoutPanel tabs = new TableLayoutPanel
            {
                Dock = DockStyle.Fill,
                ColumnCount = 2,
                RowCount = 1,
                Margin = Padding.Empty
            };
            tabs.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 50F));
            tabs.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 50F));
            btnPanelFIndID.Text = "아이디 찾기";
            btnPanelFIndPass.Text = "비밀번호 찾기";
            btnPanelFIndID.Dock = DockStyle.Fill;
            btnPanelFIndPass.Dock = DockStyle.Fill;
            btnPanelFIndID.Margin = new Padding(0, 0, 4, 0);
            btnPanelFIndPass.Margin = new Padding(4, 0, 0, 0);
            tabs.Controls.Add(btnPanelFIndID, 0, 0);
            tabs.Controls.Add(btnPanelFIndPass, 1, 0);

            ConfigureFindIdPanel();
            ConfigureFindPasswordPanel();
            Panel pages = new Panel { Dock = DockStyle.Fill, BackColor = Color.White };
            pages.Controls.Add(panelFindPass);
            pages.Controls.Add(panelFindID);

            layout.Controls.Add(title, 0, 0);
            layout.Controls.Add(description, 0, 1);
            layout.Controls.Add(tabs, 0, 2);
            layout.Controls.Add(pages, 0, 4);
            content.Controls.Add(layout);
            shell.Controls.Add(content, 1, 0);
            Controls.Add(shell);

            panelFindID.VisibleChanged += (_, _) => UpdateRecoveryTabStyle();
            panelFindPass.VisibleChanged += (_, _) => UpdateRecoveryTabStyle();
            UpdateRecoveryTabStyle();
            ResumeLayout(true);
        }

        private void ConfigureFindIdPanel()
        {
            panelFindID.Controls.Clear();
            panelFindID.Dock = DockStyle.Fill;
            panelFindID.BackColor = Color.White;
            panelFindID.Padding = new Padding(0, 8, 0, 0);

            TableLayoutPanel fields = new TableLayoutPanel
            {
                Dock = DockStyle.Top,
                Height = 176,
                ColumnCount = 1,
                RowCount = 4
            };
            fields.RowStyles.Add(new RowStyle(SizeType.Absolute, 64F));
            fields.RowStyles.Add(new RowStyle(SizeType.Absolute, 64F));
            fields.RowStyles.Add(new RowStyle(SizeType.Absolute, 10F));
            fields.RowStyles.Add(new RowStyle(SizeType.Absolute, 38F));
            label1.Text = "이름";
            label2.Text = "이메일";
            AuthUiStyle.StylePrimaryButton(btnFindID, "아이디 찾기");
            fields.Controls.Add(AuthUiStyle.CreateField(label1, tbFindIDName, "회원 이름"), 0, 0);
            fields.Controls.Add(AuthUiStyle.CreateField(label2, tbFindIDEmaile, "example@email.com"), 0, 1);
            fields.Controls.Add(AuthUiStyle.CreateBackActionRow(this, btnFindID), 0, 3);
            panelFindID.Controls.Add(fields);
        }

        private void ConfigureFindPasswordPanel()
        {
            panelFindPass.Controls.Clear();
            panelFindPass.Dock = DockStyle.Fill;
            panelFindPass.BackColor = Color.White;
            panelFindPass.Padding = new Padding(0, 8, 0, 0);

            TableLayoutPanel fields = new TableLayoutPanel
            {
                Dock = DockStyle.Top,
                Height = 240,
                ColumnCount = 1,
                RowCount = 5
            };
            fields.RowStyles.Add(new RowStyle(SizeType.Absolute, 64F));
            fields.RowStyles.Add(new RowStyle(SizeType.Absolute, 64F));
            fields.RowStyles.Add(new RowStyle(SizeType.Absolute, 64F));
            fields.RowStyles.Add(new RowStyle(SizeType.Absolute, 10F));
            fields.RowStyles.Add(new RowStyle(SizeType.Absolute, 38F));
            label5.Text = "아이디";
            label3.Text = "이름";
            label4.Text = "이메일";
            AuthUiStyle.StylePrimaryButton(btnFindPass, "본인 확인");
            fields.Controls.Add(AuthUiStyle.CreateField(label5, tbFindPassID, "아이디"), 0, 0);
            fields.Controls.Add(AuthUiStyle.CreateField(label3, tbFindPassName, "회원 이름"), 0, 1);
            fields.Controls.Add(AuthUiStyle.CreateField(label4, tbFindPassEmail, "example@email.com"), 0, 2);
            fields.Controls.Add(AuthUiStyle.CreateBackActionRow(this, btnFindPass), 0, 4);
            panelFindPass.Controls.Add(fields);
        }

        private void UpdateRecoveryTabStyle()
        {
            if (btnPanelFIndID == null || btnPanelFIndPass == null)
                return;
            AuthUiStyle.SetSegmentButton(btnPanelFIndID, panelFindID.Visible);
            AuthUiStyle.SetSegmentButton(btnPanelFIndPass, panelFindPass.Visible);
        }
    }
}
