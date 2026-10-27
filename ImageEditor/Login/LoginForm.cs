using MySql.Data.MySqlClient;
using System;
using System.Windows.Forms;

namespace ImageEditor.Login
{
    public partial class LoginForm : Form
    {
        private readonly string connectionAddress;

        public LoginForm()
            : this(string.Empty)
        {
        }

        public LoginForm(string connectionAddress)
        {
            InitializeComponent();
            this.connectionAddress = connectionAddress ?? string.Empty;
            InitializeModernUi();
        }

        private void btnLogin_Click(object? sender, EventArgs e)
        {
            string inputId = tbID.Text.Trim();
            string inputPassword = tbPASS.Text;

            if (inputId.Length == 0 || inputPassword.Length == 0)
            {
                MessageBox.Show("아이디와 비밀번호를 모두 입력해주세요.");
                return;
            }

            try
            {
                using MySqlConnection mysql = new MySqlConnection(connectionAddress);
                mysql.Open();

                const string selectQuery =
                    "SELECT Uid, ID, Password, Name FROM userinfo WHERE ID = @ID LIMIT 1";

                int userNo;
                string userId;
                string userName;
                string storedPassword;

                using (MySqlCommand command = new MySqlCommand(selectQuery, mysql))
                {
                    command.Parameters.AddWithValue("@ID", inputId);

                    using MySqlDataReader reader = command.ExecuteReader();
                    if (!reader.Read())
                    {
                        ShowLoginFailed();
                        return;
                    }

                    userNo = Convert.ToInt32(reader["Uid"]);
                    userId = Convert.ToString(reader["ID"]) ?? string.Empty;
                    userName = Convert.ToString(reader["Name"]) ?? userId;
                    storedPassword = Convert.ToString(reader["Password"]) ?? string.Empty;
                }

                bool passwordMatches = PasswordSecurity.Verify(
                    inputPassword,
                    storedPassword,
                    out bool needsUpgrade);

                if (!passwordMatches)
                {
                    ShowLoginFailed();
                    tbPASS.SelectAll();
                    tbPASS.Focus();
                    return;
                }

                if (needsUpgrade)
                    TryUpgradeLegacyPassword(mysql, userNo, inputPassword);

                CurrentUser.UserNo = userNo;
                CurrentUser.UserID = userId;
                CurrentUser.UserName = userName;

                MessageBox.Show("로그인되었습니다.");
                DialogResult = DialogResult.OK;
                Close();
            }
            catch (Exception ex)
            {
                MessageBox.Show(
                    "로그인 DB에 연결하지 못했습니다.\n" +
                    "DB 설정과 MySQL 실행 상태를 확인해주세요.\n\n" + ex.Message,
                    "로그인 오류",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Warning);
            }
        }

        private static void TryUpgradeLegacyPassword(
            MySqlConnection mysql,
            int userNo,
            string password)
        {
            try
            {
                const string updateQuery =
                    "UPDATE userinfo SET Password = @Password WHERE Uid = @Uid";

                using MySqlCommand update = new MySqlCommand(updateQuery, mysql);
                update.Parameters.AddWithValue("@Password", PasswordSecurity.Hash(password));
                update.Parameters.AddWithValue("@Uid", userNo);
                update.ExecuteNonQuery();
            }
            catch
            {
                // 계정 로그인은 허용하되 다음 로그인 때 다시 이전을 시도한다.
            }
        }

        private static void ShowLoginFailed()
        {
            MessageBox.Show("아이디 또는 비밀번호가 일치하지 않습니다.");
        }

        private void lbjoinMembership_Click(object? sender, EventArgs e)
        {
            using JoinMembership form = new JoinMembership(connectionAddress);
            form.ShowDialog(this);
        }

        private void lbFindID_Click(object? sender, EventArgs e)
        {
            using FindIDForm form = new FindIDForm(connectionAddress);
            form.ShowDialog(this);
        }

        private void btnNotLogin_Click(object? sender, EventArgs e)
        {
            CurrentUser.Logout();
            DialogResult = DialogResult.Cancel;
            Close();
        }
    }
}
