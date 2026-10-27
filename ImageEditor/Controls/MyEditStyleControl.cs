using MySql.Data.MySqlClient;

namespace ImageEditor.Controls {
    public partial class MyEditStyleControl : UserControl {
        private readonly string connectionAddress;
        private bool schemaReady;

        public MyEditStyleControl()
            : this(string.Empty) {
        }

        public MyEditStyleControl(string connectionAddress) {
            InitializeComponent();
            Dock = DockStyle.Fill;
            this.connectionAddress = connectionAddress ?? string.Empty;
            BuildModernLayout();
        }

        private void BuildModernLayout() {
            SuspendLayout();
            Controls.Clear();
            BackColor = ToolOptionStyle.PanelBackground;
            Font = new Font("맑은 고딕", 9F);

            TableLayoutPanel root = new TableLayoutPanel {
                Dock = DockStyle.Fill,
                BackColor = ToolOptionStyle.PanelBackground,
                Padding = new Padding(12, 16, 12, 12),
                ColumnCount = 1,
                RowCount = 5
            };
            root.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100F));
            root.RowStyles.Add(new RowStyle(SizeType.Absolute, 76F));
            root.RowStyles.Add(new RowStyle(SizeType.Absolute, 52F));
            root.RowStyles.Add(new RowStyle(SizeType.Percent, 100F));
            root.RowStyles.Add(new RowStyle(SizeType.Absolute, 94F));
            root.RowStyles.Add(new RowStyle(SizeType.Absolute, 46F));

            Panel heading = new Panel { Dock = DockStyle.Fill, Margin = new Padding(0, 0, 0, 8) };
            lblTitle.Font = new Font("맑은 고딕", 16F, FontStyle.Bold);
            lblTitle.ForeColor = ToolOptionStyle.Title;
            lblTitle.Location = new Point(0, 0);
            lblGuide.Font = new Font("맑은 고딕", 9F);
            lblGuide.ForeColor = ToolOptionStyle.Description;
            lblGuide.AutoSize = false;
            lblGuide.Location = new Point(1, 32);
            lblGuide.Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right;
            lblGuide.Size = new Size(Math.Max(1, heading.ClientSize.Width - 2), 38);
            heading.Resize += (_, _) => lblGuide.Width = Math.Max(1, heading.ClientSize.Width - 2);
            heading.Controls.Add(lblTitle);
            heading.Controls.Add(lblGuide);

            Panel statusCard = new Panel {
                Dock = DockStyle.Fill,
                BackColor = ToolOptionStyle.AccentSoft,
                Margin = new Padding(0, 0, 0, 10),
                Padding = new Padding(14, 9, 14, 9)
            };
            lblStatus.Dock = DockStyle.Fill;
            lblStatus.Font = new Font("맑은 고딕", 9F, FontStyle.Bold);
            lblStatus.ForeColor = ToolOptionStyle.Accent;
            lblStatus.TextAlign = ContentAlignment.MiddleLeft;
            statusCard.Controls.Add(lblStatus);

            Panel listCard = new Panel {
                Dock = DockStyle.Fill,
                BackColor = Color.White,
                BorderStyle = BorderStyle.FixedSingle,
                Margin = new Padding(0, 0, 0, 10),
                Padding = new Padding(12, 10, 12, 12)
            };
            Label listTitle = ToolOptionStyle.CreateLabel(
                "저장된 스타일", 10F, FontStyle.Bold, ToolOptionStyle.Title);
            listTitle.Dock = DockStyle.Top;
            listTitle.Height = 30;
            lbMyEditStyle.Dock = DockStyle.Fill;
            lbMyEditStyle.BorderStyle = BorderStyle.None;
            lbMyEditStyle.BackColor = Color.White;
            lbMyEditStyle.ForeColor = ToolOptionStyle.Title;
            lbMyEditStyle.Font = new Font("맑은 고딕", 9.5F);
            lbMyEditStyle.IntegralHeight = false;
            lbMyEditStyle.DrawMode = DrawMode.OwnerDrawFixed;
            lbMyEditStyle.ItemHeight = 48;
            lbMyEditStyle.DrawItem += DrawStyleItem;
            listCard.Controls.Add(lbMyEditStyle);
            listCard.Controls.Add(listTitle);

            Panel saveCard = new Panel {
                Dock = DockStyle.Fill,
                BackColor = Color.White,
                BorderStyle = BorderStyle.FixedSingle,
                Margin = new Padding(0, 0, 0, 10),
                Padding = new Padding(12, 9, 12, 10)
            };
            Label saveTitle = ToolOptionStyle.CreateLabel(
                "현재 보정값 저장", 9.5F, FontStyle.Bold, ToolOptionStyle.Title);
            saveTitle.Dock = DockStyle.Top;
            saveTitle.Height = 28;
            tbSaveMyEditStyleName.Dock = DockStyle.Bottom;
            tbSaveMyEditStyleName.Height = 30;
            tbSaveMyEditStyleName.Font = new Font("맑은 고딕", 9.5F);
            tbSaveMyEditStyleName.BorderStyle = BorderStyle.FixedSingle;
            saveCard.Controls.Add(tbSaveMyEditStyleName);
            saveCard.Controls.Add(saveTitle);

            TableLayoutPanel actions = new TableLayoutPanel {
                Dock = DockStyle.Fill,
                ColumnCount = 3,
                Margin = new Padding(0)
            };
            actions.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 36F));
            actions.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 40F));
            actions.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 24F));
            btnLoad.Text = "선택 적용";
            btnSaveMyEditStyle.Text = "스타일 저장";
            btnDelete.Text = "삭제";
            StylePrimaryButton(btnLoad);
            StyleSecondaryButton(btnSaveMyEditStyle);
            StyleSecondaryButton(btnDelete);
            btnDelete.ForeColor = Color.FromArgb(190, 54, 54);
            btnLoad.Margin = new Padding(0, 0, 5, 0);
            btnSaveMyEditStyle.Margin = new Padding(0, 0, 5, 0);
            btnDelete.Margin = new Padding(0);
            actions.Controls.Add(btnLoad, 0, 0);
            actions.Controls.Add(btnSaveMyEditStyle, 1, 0);
            actions.Controls.Add(btnDelete, 2, 0);

            root.Controls.Add(heading, 0, 0);
            root.Controls.Add(statusCard, 0, 1);
            root.Controls.Add(listCard, 0, 2);
            root.Controls.Add(saveCard, 0, 3);
            root.Controls.Add(actions, 0, 4);
            Controls.Add(root);
            ResumeLayout(true);
        }

        private void DrawStyleItem(object? sender, DrawItemEventArgs e) {
            e.DrawBackground();
            if (e.Index < 0 || e.Index >= lbMyEditStyle.Items.Count)
                return;

            bool selected = (e.State & DrawItemState.Selected) != 0;
            Color background = selected ? ToolOptionStyle.AccentSoft : Color.White;
            using SolidBrush backgroundBrush = new SolidBrush(background);
            e.Graphics.FillRectangle(backgroundBrush, e.Bounds);

            EditStyle? style = lbMyEditStyle.Items[e.Index] as EditStyle;
            string name = style?.PresetName ?? lbMyEditStyle.Items[e.Index]?.ToString() ?? string.Empty;
            string values = style == null
                ? string.Empty
                : $"밝기 {style.Brightness:+#;-#;0}  ·  대비 {style.Contrast:+#;-#;0}  ·  채도 {style.Saturation:+#;-#;0}  ·  색온도 {style.Temperature:+#;-#;0}";

            Rectangle nameBounds = new Rectangle(e.Bounds.X + 10, e.Bounds.Y + 6, e.Bounds.Width - 20, 19);
            Rectangle valueBounds = new Rectangle(e.Bounds.X + 10, e.Bounds.Y + 26, e.Bounds.Width - 20, 17);
            TextRenderer.DrawText(e.Graphics, name, lbMyEditStyle.Font, nameBounds,
                selected ? ToolOptionStyle.Accent : ToolOptionStyle.Title,
                TextFormatFlags.EndEllipsis | TextFormatFlags.VerticalCenter);
            using Font detailFont = new Font("맑은 고딕", 8F);
            TextRenderer.DrawText(e.Graphics, values, detailFont, valueBounds,
                ToolOptionStyle.Description,
                TextFormatFlags.EndEllipsis | TextFormatFlags.VerticalCenter);
            e.DrawFocusRectangle();
        }

        private static void StylePrimaryButton(Button button) {
            ToolOptionStyle.StylePrimaryButton(button);
            button.Dock = DockStyle.Fill;
            button.Font = new Font("맑은 고딕", 9F, FontStyle.Bold);
        }

        private static void StyleSecondaryButton(Button button) {
            button.Dock = DockStyle.Fill;
            button.BackColor = Color.White;
            button.ForeColor = ToolOptionStyle.Title;
            button.FlatStyle = FlatStyle.Flat;
            button.FlatAppearance.BorderColor = ToolOptionStyle.Border;
            button.UseVisualStyleBackColor = false;
            button.Font = new Font("맑은 고딕", 9F);
        }

        public Func<EditStyle?>? GetCurrentSettings { get; set; }
        public Action<EditStyle>? ApplySettings { get; set; }

        public void LoadPresetList() {
            lbMyEditStyle.Items.Clear();

            if (!EnsureLoggedIn())
                return;

            try {
                using MySqlConnection mysql = OpenConnection();
                EnsurePresetTable(mysql);

                const string query =
                    "SELECT StyleNo, PresetName, brightness, contrast, saturation, temperature " +
                    "FROM MyEditStyle WHERE uid = @uid ORDER BY StyleNo DESC";

                using MySqlCommand command = new MySqlCommand(query, mysql);
                command.Parameters.AddWithValue("@uid", CurrentUser.UserNo);

                using MySqlDataReader reader = command.ExecuteReader();
                while (reader.Read()) {
                    lbMyEditStyle.Items.Add(new EditStyle {
                        StyleNo = Convert.ToInt32(reader["StyleNo"]),
                        PresetName = Convert.ToString(reader["PresetName"]) ?? "이름 없는 스타일",
                        Brightness = ClampAdjustment(reader["brightness"]),
                        Contrast = ClampAdjustment(reader["contrast"]),
                        Saturation = ClampAdjustment(reader["saturation"]),
                        Temperature = ClampAdjustment(reader["temperature"])
                    });
                }

                lblStatus.Text = lbMyEditStyle.Items.Count == 0
                    ? "저장된 편집 스타일이 없습니다."
                    : $"저장된 스타일 {lbMyEditStyle.Items.Count}개";
            } catch (Exception ex) {
                ShowDatabaseError("편집 스타일 목록을 불러오지 못했습니다.", ex);
            }
        }

        private void btnSaveMyEditStyle_Click(object? sender, EventArgs e) {
            if (!EnsureLoggedIn())
                return;

            string presetName = tbSaveMyEditStyleName.Text.Trim();
            if (presetName.Length == 0) {
                MessageBox.Show("저장할 편집 스타일 이름을 입력해주세요.");
                tbSaveMyEditStyleName.Focus();
                return;
            }

            EditStyle? currentStyle = GetCurrentSettings?.Invoke();
            if (currentStyle == null) {
                MessageBox.Show("현재 보정 값을 가져올 수 없습니다.");
                return;
            }

            try {
                using MySqlConnection mysql = OpenConnection();
                EnsurePresetTable(mysql);

                const string query =
                    "INSERT INTO MyEditStyle " +
                    "(uid, PresetName, brightness, contrast, saturation, temperature) " +
                    "VALUES (@uid, @name, @brightness, @contrast, @saturation, @temperature)";

                using MySqlCommand command = new MySqlCommand(query, mysql);
                command.Parameters.AddWithValue("@uid", CurrentUser.UserNo);
                command.Parameters.AddWithValue("@name", presetName);
                command.Parameters.AddWithValue("@brightness", currentStyle.Brightness);
                command.Parameters.AddWithValue("@contrast", currentStyle.Contrast);
                command.Parameters.AddWithValue("@saturation", currentStyle.Saturation);
                command.Parameters.AddWithValue("@temperature", currentStyle.Temperature);

                command.ExecuteNonQuery();
                tbSaveMyEditStyleName.Clear();
                LoadPresetList();
                MessageBox.Show("현재 보정 값이 편집 스타일로 저장되었습니다.");
            } catch (Exception ex) {
                ShowDatabaseError("편집 스타일을 저장하지 못했습니다.", ex);
            }
        }

        private void btnLoad_Click(object? sender, EventArgs e) {
            if (lbMyEditStyle.SelectedItem is not EditStyle selectedStyle) {
                MessageBox.Show("불러올 편집 스타일을 목록에서 선택해주세요.");
                return;
            }

            ApplySettings?.Invoke(selectedStyle);
        }

        private void btnDelete_Click(object? sender, EventArgs e) {
            if (!EnsureLoggedIn())
                return;

            if (lbMyEditStyle.SelectedItem is not EditStyle selectedStyle) {
                MessageBox.Show("삭제할 편집 스타일을 목록에서 선택해주세요.");
                return;
            }

            DialogResult result = MessageBox.Show(
                $"[{selectedStyle.PresetName}] 편집 스타일을 삭제할까요?",
                "편집 스타일 삭제",
                MessageBoxButtons.YesNo,
                MessageBoxIcon.Question);

            if (result != DialogResult.Yes)
                return;

            try {
                using MySqlConnection mysql = OpenConnection();
                EnsurePresetTable(mysql);

                const string query =
                    "DELETE FROM MyEditStyle WHERE StyleNo = @styleNo AND uid = @uid";

                using MySqlCommand command = new MySqlCommand(query, mysql);
                command.Parameters.AddWithValue("@styleNo", selectedStyle.StyleNo);
                command.Parameters.AddWithValue("@uid", CurrentUser.UserNo);

                if (command.ExecuteNonQuery() == 0) {
                    MessageBox.Show("삭제할 편집 스타일을 찾지 못했습니다.");
                    return;
                }

                LoadPresetList();
            } catch (Exception ex) {
                ShowDatabaseError("편집 스타일을 삭제하지 못했습니다.", ex);
            }
        }

        private MySqlConnection OpenConnection() {
            if (string.IsNullOrWhiteSpace(connectionAddress))
                throw new InvalidOperationException("데이터베이스 연결 설정이 비어 있습니다.");

            MySqlConnection connection = new MySqlConnection(connectionAddress);
            connection.Open();
            return connection;
        }

        private void EnsurePresetTable(MySqlConnection mysql) {
            if (schemaReady)
                return;

            const string query = @"
CREATE TABLE IF NOT EXISTS MyEditStyle (
    StyleNo INT NOT NULL AUTO_INCREMENT,
    uid INT NOT NULL,
    PresetName VARCHAR(100) NOT NULL,
    brightness INT NOT NULL DEFAULT 0,
    contrast INT NOT NULL DEFAULT 0,
    saturation INT NOT NULL DEFAULT 0,
    temperature INT NOT NULL DEFAULT 0,
    PRIMARY KEY (StyleNo),
    INDEX IX_MyEditStyle_uid (uid)
)";

            using MySqlCommand command = new MySqlCommand(query, mysql);
            command.ExecuteNonQuery();
            schemaReady = true;
        }

        private static int ClampAdjustment(object value) {
            return Math.Clamp(Convert.ToInt32(value), -100, 100);
        }

        private static bool EnsureLoggedIn() {
            if (CurrentUser.UserNo > 0)
                return true;

            MessageBox.Show(
                "나만의 편집 스타일은 로그인 후 사용할 수 있습니다.",
                "로그인 필요",
                MessageBoxButtons.OK,
                MessageBoxIcon.Information);
            return false;
        }

        private void ShowDatabaseError(string message, Exception ex) {
            lblStatus.Text = "DB 연결을 확인해주세요.";
            MessageBox.Show(
                message + "\n\n" + ex.Message,
                "편집 스타일",
                MessageBoxButtons.OK,
                MessageBoxIcon.Warning);
        }
    }

    public sealed class EditStyle {
        public int StyleNo { get; set; }
        public string PresetName { get; set; } = string.Empty;
        public int Brightness { get; set; }
        public int Contrast { get; set; }
        public int Saturation { get; set; }
        public int Temperature { get; set; }

        public override string ToString() {
            return PresetName;
        }
    }
}
