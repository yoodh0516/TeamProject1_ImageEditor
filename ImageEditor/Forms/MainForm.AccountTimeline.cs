using ImageEditor.Login;
using ImageEditor.Save;
using ImageEditor.Controls;
using MySql.Data.MySqlClient;
using System.Drawing;
using System.Windows.Forms;

namespace ImageEditor
{
    public partial class MainForm : Form
    {
        private TimeLinePage? timeLinePage;
        private bool restoringHistory;
        private bool initialLoginShown;
        private ContextMenuStrip? accountMenu;
        private ToolStripMenuItem? accountIdentityItem;

        private readonly string dbServer =
            Environment.GetEnvironmentVariable("IMAGEEDITOR_DB_SERVER") ?? "localhost";
        private readonly int dbPort = GetDatabasePort();
        private readonly string dbName =
            Environment.GetEnvironmentVariable("IMAGEEDITOR_DB_NAME") ?? "imageeditor";
        private readonly string dbUser =
            Environment.GetEnvironmentVariable("IMAGEEDITOR_DB_USER") ?? "root";
        private readonly string dbPassword =
            Environment.GetEnvironmentVariable("IMAGEEDITOR_DB_PASSWORD") ?? "1111";
        private string connectionAddress = string.Empty;

        // TimeLinePage가 현재 캔버스 이미지를 저장할 때 사용한다.
        public PictureBox pictureBox => picCanvas;
        public Bitmap? CurrentEditorImage => editor.State.CurrentImage;

        private void InitializeAccountTimelineFeatures()
        {
            connectionAddress = new MySqlConnectionStringBuilder
            {
                Server = dbServer,
                Port = (uint)dbPort,
                Database = dbName,
                UserID = dbUser,
                Password = dbPassword,
                ConnectionTimeout = 5
            }.ConnectionString;

            timeLinePage = new TimeLinePage(this, connectionAddress);
            tabTimeLine.Controls.Clear();
            tabTimeLine.Controls.Add(timeLinePage);

            // 기존 MainForm의 로그인 Label을 그대로 재사용한다.
            label1.Text = "로그인";
            label1.Cursor = Cursors.Hand;
            label1.Click += LoginLabel_Click;
            InitializeAccountMenu();

            Shown += MainForm_Shown;
            FormClosing += MainForm_AccountTimeline_FormClosing;
        }

        private static int GetDatabasePort()
        {
            string? value =
                Environment.GetEnvironmentVariable("IMAGEEDITOR_DB_PORT");

            return int.TryParse(value, out int port) &&
                   port > 0 && port <= 65535
                ? port
                : 3306;
        }

        private void MainForm_Shown(object? sender, EventArgs e)
        {
            if (initialLoginShown)
                return;

            initialLoginShown = true;
            ShowLoginDialog();
        }

        private void LoginLabel_Click(object? sender, EventArgs e)
        {
            if (CurrentUser.UserNo > 0)
            {
                ShowAccountMenu();
                return;
            }

            ShowLoginDialog();
        }

        private void InitializeAccountMenu()
        {
            accountMenu = new ContextMenuStrip
            {
                ShowImageMargin = false,
                ShowCheckMargin = false,
                ShowItemToolTips = true,
                Font = new Font("맑은 고딕", 9F)
            };

            accountIdentityItem = new ToolStripMenuItem
            {
                Enabled = false,
                AutoSize = false,
                Size = new Size(210, 44),
                TextAlign = ContentAlignment.MiddleLeft
            };
            ToolStripMenuItem myStyleItem = new ToolStripMenuItem("내 편집 스타일");
            ToolStripMenuItem timelineItem = new ToolStripMenuItem("프로젝트 타임라인");
            ToolStripMenuItem logoutItem = new ToolStripMenuItem("로그아웃")
            {
                ForeColor = Color.FromArgb(190, 45, 45)
            };

            myStyleItem.Click += (_, _) => menuMyStyle_Click(null, EventArgs.Empty);
            timelineItem.Click += (_, _) =>
            {
                timeLinePage?.ShowTimelineView();
                OpenTimelineDrawer();
            };
            logoutItem.Click += AccountLogoutItem_Click;

            accountMenu.Items.Add(accountIdentityItem);
            accountMenu.Items.Add(new ToolStripSeparator());
            accountMenu.Items.Add(myStyleItem);
            accountMenu.Items.Add(timelineItem);
            accountMenu.Items.Add(new ToolStripSeparator());
            accountMenu.Items.Add(logoutItem);
            EditorToolTipService.ApplyMenuToolTips(accountMenu.Items);
        }

        private void ShowAccountMenu()
        {
            if (accountMenu == null || CurrentUser.UserNo <= 0)
                return;

            accountIdentityItem!.Text = CurrentUser.UserName + "\r\n아이디: " + CurrentUser.UserID;
            // label1은 헤더 TableLayout 안으로 재배치되어 있으므로 Form 좌표를
            // 직접 계산하면 메뉴가 왼쪽에 뜬다. 컨트롤 기준으로 표시한다.
            accountMenu.Show(label1, new Point(
                Math.Max(0, label1.Width - accountMenu.Width),
                label1.Height + 4));
        }

        private async void AccountLogoutItem_Click(object? sender, EventArgs e)
        {
            if (CurrentUser.UserNo <= 0)
                return;

            DialogResult result = MessageBox.Show(
                "로그아웃하면 현재 작업 타임라인 기록이 삭제됩니다.\r\n캔버스의 이미지와 레이어는 유지됩니다. 계속할까요?",
                "로그아웃",
                MessageBoxButtons.YesNo,
                MessageBoxIcon.Question);
            if (result != DialogResult.Yes)
                return;

            CurrentUser.Logout();
            StopInteractiveTools();
            label1.Text = "로그인";
            label1.Font = new Font("맑은 고딕", 9F, FontStyle.Regular);
            label1.Cursor = Cursors.Hand;
            label1.Enabled = true;
            UpdateAiFeatureAvailability(editor.State.CurrentImage != null);
            UpdateMyEditStyleAvailability(editor.State.CurrentImage != null);
            UpdateTimelineAvailability();

            try
            {
                if (timeLinePage != null)
                    await timeLinePage.ClearSessionHistoryAsync();
            }
            catch (Exception ex)
            {
                MessageBox.Show(
                    "로그아웃은 완료되었지만 타임라인 정리 중 문제가 발생했습니다.\r\n" + ex.Message,
                    "타임라인 정리",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Warning);
            }
        }

        private void ShowLoginDialog()
        {
            try
            {
                using LoginForm loginForm = new LoginForm(connectionAddress);
                loginForm.ShowDialog(this);

                if (CurrentUser.UserNo > 0)
                    UpdateLoggedInUserLabel();
                else
                {
                    UpdateAiFeatureAvailability(editor.State.CurrentImage != null);
                    UpdateMyEditStyleAvailability(editor.State.CurrentImage != null);
                    UpdateTimelineAvailability();
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show(
                    "로그인 DB에 연결할 수 없습니다.\n" + ex.Message,
                    "로그인",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Warning);
            }
        }

        private void UpdateLoggedInUserLabel()
        {
            label1.Text = CurrentUser.UserName + "님 ▾";
            label1.Font = new Font("맑은 고딕", 9, FontStyle.Bold);
            label1.Cursor = Cursors.Hand;
            label1.Enabled = true;
            UpdateAiFeatureAvailability(editor.State.CurrentImage != null);
            UpdateMyEditStyleAvailability(editor.State.CurrentImage != null);
            UpdateTimelineAvailability();
        }

        private void MainForm_AccountTimeline_FormClosing(object? sender, FormClosingEventArgs e)
        {
            if (!ConfirmDiscardUnsavedChanges())
            {
                e.Cancel = true;
                return;
            }

            StopInteractiveTools();
            photoRestorationCancellation?.Cancel();
            portraitEnhanceCancellation?.Cancel();
            qualityEnhancementCancellation?.Cancel();
            aiSocialPostCancellation?.Cancel();
            communityFaceDetectionCancellation?.Cancel();
            realEsrganModel?.Dispose();
            realEsrganModel = null;
            animeVideoModel?.Dispose();
            animeVideoModel = null;
            drawingRenderTimer.Stop();
            drawingRenderTimer.Dispose();
            layerOpacityCommitTimer.Stop();
            layerOpacityCommitTimer.Dispose();
            communityRenderTimer.Stop();
            communityRenderTimer.Dispose();
            adjustmentPreviewTimer.Stop();
            adjustmentPreviewTimer.Dispose();
            filterPreviewTimer.Stop();
            filterPreviewTimer.Dispose();
            cropTool?.Dispose();
            aiObjectRemovalTool?.Stop();
            aiBackgroundRemovalTool?.Dispose();

            Image? displayImage = picCanvas.Image;
            picCanvas.Image = null;
            if (displayImage != null &&
                !ReferenceEquals(displayImage, editor.State.CurrentImage) &&
                !ReferenceEquals(displayImage, editor.State.OriginalImage))
            {
                displayImage.Dispose();
            }

            timeLinePage?.ReleaseUiResources();
            editor.Dispose();
        }

        // 이미지 자체가 변경된 작업이 끝났을 때 타임라인에 현재 상태를 남긴다.
        private void RecordTimelineSnapshot()
        {
            if (restoringHistory)
                return;

            if (editor.State.CurrentImage == null)
                return;

            if (CurrentUser.UserNo <= 0)
                return;

            timeLinePage?.SQLInsert();
        }

        // 타임라인 썸네일을 클릭했을 때 EditorState까지 정상적으로 되돌린다.
        public void RestoreHistoryImage(Bitmap image)
        {
            if (image == null)
                return;

            StopInteractiveTools();
            cropTool?.ClearPreviousCrop();

            restoringHistory = true;
            try
            {
                editor.State.ReplaceCurrentImage(image);
                editor.InitializeLayers();
                RefreshAfterImageChange();
            }
            finally
            {
                restoringHistory = false;
            }
        }

        public bool ConfirmTimelineProjectSwitch()
        {
            return ConfirmDiscardUnsavedChanges();
        }
    }

    public static class CurrentUser
    {
        public static int UserNo { get; set; } = -1;
        public static string UserID { get; set; } = string.Empty;
        public static string UserName { get; set; } = string.Empty;

        public static void Logout()
        {
            UserNo = -1;
            UserID = string.Empty;
            UserName = string.Empty;
        }
    }
}
