using MySql.Data.MySqlClient;
using System;
using System.Windows.Forms;

namespace ImageEditor.Login
{
    public partial class FindIDForm : Form
    {
        private readonly string connectionAddress;

        public FindIDForm()
            : this(string.Empty)
        {
        }

        public FindIDForm(string connectionAddress)
        {
            InitializeComponent();
            this.connectionAddress = connectionAddress ?? string.Empty;
            InitializeModernUi();
        }

        private void FindIDForm_Load(object? sender, EventArgs e)
        {
            panelFindID.Visible = true;
            panelFindPass.Visible = false;
            UpdateRecoveryTabStyle();
        }

        private void btnPanelFIndID_Click(object? sender, EventArgs e)
        {
            panelFindID.Visible = true;
            panelFindPass.Visible = false;
            UpdateRecoveryTabStyle();
        }

        private void btnPanelFIndPass_Click(object? sender, EventArgs e)
        {
            panelFindID.Visible = false;
            panelFindPass.Visible = true;
            UpdateRecoveryTabStyle();
        }

        private void btnFindID_Click(object? sender, EventArgs e)
        {
            string name = tbFindIDName.Text.Trim();
            string email = tbFindIDEmaile.Text.Trim();

            if (name.Length == 0 || email.Length == 0)
            {
                MessageBox.Show("이름과 이메일을 모두 입력해주세요.");
                return;
            }

            try
            {
                using MySqlConnection mysql = new MySqlConnection(connectionAddress);
                mysql.Open();

                const string query =
                    "SELECT ID FROM userinfo WHERE Name = @Name AND Email = @Email LIMIT 1";

                using MySqlCommand command = new MySqlCommand(query, mysql);
                command.Parameters.AddWithValue("@Name", name);
                command.Parameters.AddWithValue("@Email", email);

                object? result = command.ExecuteScalar();
                if (result == null)
                {
                    MessageBox.Show("입력한 정보와 일치하는 계정이 없습니다.");
                    return;
                }

                MessageBox.Show($"회원님의 아이디는 {Convert.ToString(result)} 입니다.");
            }
            catch (Exception ex)
            {
                ShowDatabaseError(ex);
            }
        }

        private void btnFindPass_Click(object? sender, EventArgs e)
        {
            string id = tbFindPassID.Text.Trim();
            string name = tbFindPassName.Text.Trim();
            string email = tbFindPassEmail.Text.Trim();

            if (id.Length == 0 || name.Length == 0 || email.Length == 0)
            {
                MessageBox.Show("아이디, 이름, 이메일을 모두 입력해주세요.");
                return;
            }

            try
            {
                using MySqlConnection mysql = new MySqlConnection(connectionAddress);
                mysql.Open();

                const string query =
                    "SELECT Uid FROM userinfo " +
                    "WHERE ID = @ID AND Name = @Name AND Email = @Email LIMIT 1";

                using MySqlCommand command = new MySqlCommand(query, mysql);
                command.Parameters.AddWithValue("@ID", id);
                command.Parameters.AddWithValue("@Name", name);
                command.Parameters.AddWithValue("@Email", email);

                if (command.ExecuteScalar() == null)
                {
                    MessageBox.Show("입력한 정보와 일치하는 계정이 없습니다.");
                    return;
                }

                using ResetPasswordForm reset =
                    new ResetPasswordForm(connectionAddress, id);
                reset.ShowDialog(this);
            }
            catch (Exception ex)
            {
                ShowDatabaseError(ex);
            }
        }

        private static void ShowDatabaseError(Exception ex)
        {
            MessageBox.Show(
                "계정 조회 중 DB 오류가 발생했습니다.\n\n" + ex.Message,
                "계정 찾기",
                MessageBoxButtons.OK,
                MessageBoxIcon.Warning);
        }
    }
}
