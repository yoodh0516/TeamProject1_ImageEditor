using MySql.Data.MySqlClient;
using System;
using System.Net.Mail;
using System.Text;
using System.Windows.Forms;

namespace ImageEditor.Login
{
    public partial class JoinMembership : Form
    {
        private readonly string connectionAddress;

        public JoinMembership()
            : this(string.Empty)
        {
        }

        public JoinMembership(string connectionAddress)
        {
            InitializeComponent();
            this.connectionAddress = connectionAddress ?? string.Empty;
            InitializeModernUi();
        }

        private void createMember_Click(object? sender, EventArgs e)
        {
            string id = tbID.Text.Trim();
            string password = tbPASS.Text;
            string name = tbNAME.Text.Trim();
            string email = tbEMAIL.Text.Trim();

            if (id.Length == 0 || password.Length == 0 ||
                name.Length == 0 || email.Length == 0)
            {
                MessageBox.Show("모든 항목을 입력해주세요.");
                return;
            }

            if (!string.Equals(password, tbPASSCheck.Text, StringComparison.Ordinal))
            {
                MessageBox.Show("비밀번호가 일치하지 않습니다.");
                tbPASSCheck.Clear();
                tbPASSCheck.Focus();
                return;
            }

            if (id.Length > 45 || name.Length > 45 || email.Length > 45)
            {
                MessageBox.Show("아이디, 이름과 이메일은 각각 45자 이내로 입력해주세요.");
                return;
            }

            if (Encoding.UTF8.GetByteCount(password) > 72)
            {
                MessageBox.Show("비밀번호가 너무 깁니다. 한글을 포함해 UTF-8 기준 72바이트 이내로 입력해주세요.");
                return;
            }

            try
            {
                MailAddress address = new MailAddress(email);
                if (!string.Equals(address.Address, email, StringComparison.OrdinalIgnoreCase))
                    throw new FormatException();
            }
            catch (FormatException)
            {
                MessageBox.Show("올바른 이메일 주소를 입력해주세요.");
                tbEMAIL.Focus();
                return;
            }

            try
            {
                using MySqlConnection mysql = new MySqlConnection(connectionAddress);
                mysql.Open();

                const string duplicateQuery =
                    "SELECT ID, Email FROM userinfo " +
                    "WHERE ID = @ID OR Email = @Email LIMIT 1";

                using (MySqlCommand duplicate = new MySqlCommand(duplicateQuery, mysql))
                {
                    duplicate.Parameters.AddWithValue("@ID", id);
                    duplicate.Parameters.AddWithValue("@Email", email);

                    using MySqlDataReader reader = duplicate.ExecuteReader();
                    if (reader.Read())
                    {
                        string existingId = Convert.ToString(reader["ID"]) ?? string.Empty;
                        MessageBox.Show(
                            string.Equals(existingId, id, StringComparison.OrdinalIgnoreCase)
                                ? "이미 존재하는 아이디입니다."
                                : "이미 존재하는 이메일입니다.");
                        return;
                    }
                }

                const string insertQuery =
                    "INSERT INTO userinfo (ID, Password, Name, Email) " +
                    "VALUES (@ID, @Password, @Name, @Email)";

                using MySqlCommand insert = new MySqlCommand(insertQuery, mysql);
                insert.Parameters.AddWithValue("@ID", id);
                insert.Parameters.AddWithValue("@Password", PasswordSecurity.Hash(password));
                insert.Parameters.AddWithValue("@Name", name);
                insert.Parameters.AddWithValue("@Email", email);

                if (insert.ExecuteNonQuery() != 1)
                {
                    MessageBox.Show("회원가입에 실패했습니다.");
                    return;
                }

                MessageBox.Show("회원가입이 완료되었습니다.");
                DialogResult = DialogResult.OK;
                Close();
            }
            catch (MySqlException ex)
            {
                string message = ex.Number == 1406
                    ? "입력한 정보가 데이터베이스 허용 길이를 초과했습니다."
                    : "회원가입 처리 중 데이터베이스에 연결하지 못했습니다.";
                MessageBox.Show(
                    message,
                    "회원가입",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Warning);
            }
            catch (Exception)
            {
                MessageBox.Show(
                    "회원가입 처리 중 오류가 발생했습니다. 입력 내용을 확인한 뒤 다시 시도해주세요.",
                    "회원가입",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Warning);
            }
        }
    }
}
