using MySql.Data.MySqlClient;
using System;
using System.Windows.Forms;

namespace ImageEditor.Login
{
    public partial class ResetPasswordForm : Form
    {
        private readonly string connectionAddress;
        private readonly string userId;

        public ResetPasswordForm()
            : this(string.Empty, string.Empty)
        {
        }

        public ResetPasswordForm(string connectionAddress, string userId)
        {
            InitializeComponent();
            this.connectionAddress = connectionAddress ?? string.Empty;
            this.userId = userId ?? string.Empty;
            InitializeModernUi();
        }

        private void btnConfirm_Click(object? sender, EventArgs e)
        {
            string newPassword = tbResetPASS.Text;
            string confirmPassword = tbResetPASSCheck.Text;

            if (newPassword.Length == 0 || confirmPassword.Length == 0)
            {
                MessageBox.Show("새 비밀번호를 두 번 입력해주세요.");
                return;
            }

            if (!string.Equals(newPassword, confirmPassword, StringComparison.Ordinal))
            {
                MessageBox.Show("비밀번호가 일치하지 않습니다.");
                tbResetPASSCheck.Clear();
                tbResetPASSCheck.Focus();
                return;
            }

            try
            {
                using MySqlConnection mysql = new MySqlConnection(connectionAddress);
                mysql.Open();

                const string query =
                    "UPDATE userinfo SET Password = @Password WHERE ID = @ID";

                using MySqlCommand command = new MySqlCommand(query, mysql);
                command.Parameters.AddWithValue("@Password", PasswordSecurity.Hash(newPassword));
                command.Parameters.AddWithValue("@ID", userId);

                if (command.ExecuteNonQuery() != 1)
                {
                    MessageBox.Show("비밀번호를 찾을 계정을 확인하지 못했습니다.");
                    return;
                }

                MessageBox.Show("비밀번호 찾기가 완료되었습니다.");
                DialogResult = DialogResult.OK;
                Close();
            }
            catch (Exception ex)
            {
                MessageBox.Show(
                    "비밀번호 찾기 중 DB 오류가 발생했습니다.\n\n" + ex.Message,
                    "비밀번호 찾기",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Warning);
            }
        }
    }
}
