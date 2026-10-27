using MySql.Data.MySqlClient;
using ImageEditor.Controls;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Drawing.Imaging;
using System.IO;
using System.Linq;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace ImageEditor.Save
{
    public partial class TimeLinePage : UserControl
    {
        private static readonly Color Accent = Color.FromArgb(64, 101, 242);
        private static readonly Color AccentSoft = Color.FromArgb(235, 240, 255);
        private static readonly Color TextPrimary = Color.FromArgb(30, 38, 52);
        private static readonly Color TextMuted = Color.FromArgb(110, 121, 139);
        private static readonly Color Border = Color.FromArgb(220, 225, 233);
        private static readonly Color CardBackground = Color.FromArgb(248, 250, 253);

        private static readonly string HistoryRootPath = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
            "ImageEditor",
            "HistoryImages");
        private readonly string historyRoot = HistoryRootPath;
        private readonly string projectCatalogPath = Path.Combine(
            HistoryRootPath,
            "projects.json");
        private readonly Task storageInitializationTask;
        private readonly SemaphoreSlim historyWriteGate = new(1, 1);
        private readonly SemaphoreSlim databaseWriteGate = new(1, 1);
        private static readonly SemaphoreSlim CatalogGate = new(1, 1);
        private readonly ToolTip historyToolTip = new ToolTip();

        private MainForm? mainForm;
        private string connectionAddress = string.Empty;
        private int projectId;
        private int selectedProjectId;
        private int stepIndex;
        private int projectGeneration;
        private string currentHistoryFolder = string.Empty;
        private bool acceptingSnapshots = true;
        private bool uiResourcesReleased;
        private bool projectViewVisible;
        private volatile bool databaseAvailableForTimeline = true;

        private Button? btnTimelineView;
        private Button? btnProjectView;
        private Button? btnDeleteProject;
        private Label? lblCurrentProject;
        private FlowLayoutPanel? flowLayoutPanelProjects;

        public TimeLinePage()
        {
            storageInitializationTask = Task.Run(InitializeHistoryStorage);
            InitializeComponent();
            EditorToolTipService.Configure(historyToolTip);
            ConfigureModernTimelineUi();
        }

        public TimeLinePage(MainForm mainForm, string connectionAddress)
        {
            storageInitializationTask = Task.Run(InitializeHistoryStorage);
            InitializeComponent();
            this.mainForm = mainForm;
            this.connectionAddress = connectionAddress;
            EditorToolTipService.Configure(historyToolTip);
            ConfigureModernTimelineUi();
        }

        public async Task StartNewProjectAsync(int userId)
        {
            await storageInitializationTask;
            if (uiResourcesReleased)
                return;

            acceptingSnapshots = false;
            await historyWriteGate.WaitAsync();
            try
            {
                projectGeneration++;
                ResetProjectState();
                ClearFlowPanel(flowLayoutPanelHistory);
                // 현재 통합 DB에는 Projects 테이블이 없을 수 있다.
                // 새 이미지 열기가 DB 연결 대기에 막히지 않도록 로컬 카탈로그를
                // 프로젝트의 기준으로 사용하고 EditHistory는 뒤에서 보조 기록한다.
                databaseAvailableForTimeline = true;

                if (userId <= 0)
                {
                    ShowTimelineView();
                    UpdateCurrentProjectLabel();
                    return;
                }

                projectId = await AllocateLocalProjectIdAsync(
                    userId,
                    includeDatabaseMaximum: false);

                currentHistoryFolder = GetProjectFolder(projectId);
                await Task.Run(() => Directory.CreateDirectory(currentHistoryFolder));
                ShowTimelineView();
                UpdateCurrentProjectLabel();
            }
            finally
            {
                historyWriteGate.Release();
                if (!uiResourcesReleased)
                    acceptingSnapshots = true;
            }
        }

        public void SQLInsert()
        {
            Bitmap? currentImage = mainForm?.CurrentEditorImage;
            if (currentImage == null || !acceptingSnapshots ||
                string.IsNullOrWhiteSpace(currentHistoryFolder))
            {
                return;
            }

            try
            {
                Bitmap snapshot = new Bitmap(currentImage);
                int savedStep = Interlocked.Increment(ref stepIndex);
                int savedProjectId = projectId;
                int savedGeneration = projectGeneration;
                string fileName =
                    $"{savedStep:D4}_{DateTime.Now:yyyyMMdd_HHmmss_fff}.png";
                string fullImagePath = Path.Combine(currentHistoryFolder, fileName);

                _ = SaveSnapshotAsync(
                    snapshot,
                    savedProjectId,
                    savedGeneration,
                    savedStep,
                    fileName,
                    DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss"),
                    fullImagePath);
            }
            catch (Exception ex)
            {
                Console.WriteLine("타임라인 저장 중 오류: " + ex.Message);
            }
        }

        public void ShowTimelineView()
        {
            if (flowLayoutPanelProjects == null)
                return;

            flowLayoutPanelHistory.Visible = true;
            flowLayoutPanelProjects.Visible = false;
            projectViewVisible = false;
            if (btnDeleteProject != null)
                btnDeleteProject.Visible = false;
            SetViewButtonState(true);
            UpdateCurrentProjectLabel();
        }

        public async Task ShowProjectViewAsync(int userId)
        {
            await storageInitializationTask;
            if (flowLayoutPanelProjects == null)
                return;

            flowLayoutPanelHistory.Visible = false;
            flowLayoutPanelProjects.Visible = true;
            projectViewVisible = true;
            if (btnDeleteProject != null)
                btnDeleteProject.Visible = true;
            SetViewButtonState(false);
            await LoadProjectListAsync(userId);
        }

        public async Task LoadProjectListAsync(int userId)
        {
            await storageInitializationTask;
            if (flowLayoutPanelProjects == null || uiResourcesReleased)
                return;

            ClearFlowPanel(flowLayoutPanelProjects);
            selectedProjectId = 0;
            UpdateDeleteButton();

            if (userId <= 0)
            {
                SetStatus("로그인하면 프로젝트 기록을 확인할 수 있습니다.");
                return;
            }

            SetStatus("프로젝트 기록을 불러오는 중...");
            try
            {
                IReadOnlyList<ProjectSummary> projects =
                    await QueryProjectsAsync(userId);
                if (uiResourcesReleased)
                    return;

                foreach (ProjectSummary project in projects)
                    AddProjectCard(project);

                if (projectViewVisible)
                {
                    SetStatus(projects.Count == 0
                        ? "아직 저장된 프로젝트가 없습니다."
                        : $"프로젝트 {projects.Count}개");
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine("프로젝트 목록 로드 실패: " + ex.Message);
                if (projectViewVisible)
                {
                    SetStatus("프로젝트 DB를 확인할 수 없습니다. 현재 작업은 로컬에 기록됩니다.");
                }
            }
        }

        public async Task ClearSessionHistoryAsync()
        {
            await storageInitializationTask;
            if (uiResourcesReleased)
                return;

            acceptingSnapshots = false;
            await historyWriteGate.WaitAsync();
            try
            {
                int targetProjectId = projectId;
                string targetFolder = currentHistoryFolder;
                projectGeneration++;

                if (targetProjectId > 0)
                {
                    try
                    {
                        await DeleteProjectDatabaseRowsAsync(targetProjectId);
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine("로그아웃 타임라인 DB 정리 생략: " + ex.Message);
                    }
                }

                await DeleteFolderBestEffortAsync(targetFolder);
                if (targetProjectId > 0)
                    await RemoveCatalogProjectAsync(targetProjectId);
                ResetProjectState();
                ClearFlowPanel(flowLayoutPanelHistory);
                if (flowLayoutPanelProjects != null)
                    ClearFlowPanel(flowLayoutPanelProjects);
                ShowTimelineView();
            }
            finally
            {
                historyWriteGate.Release();
                if (!uiResourcesReleased)
                    acceptingSnapshots = true;
            }
        }

        // 이전 호출부 호환용이다. 프로젝트형 기록은 프로그램 종료 시 유지한다.
        public void ClearAllHistory()
        {
            ResetProjectState();
            ClearFlowPanel(flowLayoutPanelHistory);
            if (flowLayoutPanelProjects != null)
                ClearFlowPanel(flowLayoutPanelProjects);
        }

        public void ReleaseUiResources()
        {
            if (uiResourcesReleased)
                return;

            uiResourcesReleased = true;
            acceptingSnapshots = false;
            projectGeneration++;
            ClearFlowPanel(flowLayoutPanelHistory);
            if (flowLayoutPanelProjects != null)
                ClearFlowPanel(flowLayoutPanelProjects);
            historyToolTip.Dispose();
        }

        private void ConfigureModernTimelineUi()
        {
            Dock = DockStyle.Fill;
            BackColor = Color.White;

            flowLayoutPanelHistory.Dock = DockStyle.Fill;
            flowLayoutPanelHistory.FlowDirection = FlowDirection.LeftToRight;
            flowLayoutPanelHistory.WrapContents = false;
            flowLayoutPanelHistory.AutoScroll = true;
            flowLayoutPanelHistory.Padding = new Padding(6, 6, 6, 2);
            flowLayoutPanelHistory.BackColor = Color.White;

            flowLayoutPanelProjects = new FlowLayoutPanel
            {
                Dock = DockStyle.Fill,
                FlowDirection = FlowDirection.LeftToRight,
                WrapContents = false,
                AutoScroll = true,
                Padding = new Padding(6, 6, 6, 2),
                BackColor = Color.White,
                Visible = false
            };

            TableLayoutPanel header = new TableLayoutPanel
            {
                Dock = DockStyle.Fill,
                ColumnCount = 5,
                RowCount = 1,
                BackColor = Color.White,
                Margin = Padding.Empty,
                Padding = new Padding(10, 5, 10, 5)
            };
            // 글자 길이에 맞춰 열 폭을 계산하면 Windows 배율이 125% 이상이어도
            // 버튼 문구가 잘리지 않는다. 남은 폭만 상태 문구가 사용한다.
            header.ColumnStyles.Add(new ColumnStyle(SizeType.AutoSize));
            header.ColumnStyles.Add(new ColumnStyle(SizeType.AutoSize));
            header.ColumnStyles.Add(new ColumnStyle(SizeType.AutoSize));
            header.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100F));
            header.ColumnStyles.Add(new ColumnStyle(SizeType.AutoSize));

            Label title = new Label
            {
                Text = "편집 기록",
                Dock = DockStyle.Fill,
                Font = new Font("맑은 고딕", 10F, FontStyle.Bold),
                ForeColor = TextPrimary,
                TextAlign = ContentAlignment.MiddleLeft,
                AutoSize = true,
                MinimumSize = new Size(80, 0),
                Padding = new Padding(0, 0, 8, 0)
            };
            btnTimelineView = CreateHeaderButton("작업 타임라인");
            btnProjectView = CreateHeaderButton("프로젝트");
            btnDeleteProject = CreateHeaderButton("프로젝트 삭제");
            btnDeleteProject.ForeColor = Color.FromArgb(190, 45, 45);
            btnDeleteProject.Visible = false;
            btnDeleteProject.Enabled = false;
            lblCurrentProject = new Label
            {
                Dock = DockStyle.Fill,
                AutoEllipsis = true,
                Font = new Font("맑은 고딕", 8.5F),
                ForeColor = TextMuted,
                TextAlign = ContentAlignment.MiddleRight,
                Padding = new Padding(0, 0, 8, 0)
            };

            btnTimelineView.Click += (_, _) => ShowTimelineView();
            btnProjectView.Click += async (_, _) =>
                await ShowProjectViewAsync(CurrentUser.UserNo);
            btnDeleteProject.Click += async (_, _) =>
                await DeleteSelectedProjectAsync();

            header.Controls.Add(title, 0, 0);
            header.Controls.Add(btnTimelineView, 1, 0);
            header.Controls.Add(btnProjectView, 2, 0);
            header.Controls.Add(lblCurrentProject, 3, 0);
            header.Controls.Add(btnDeleteProject, 4, 0);

            Panel content = new Panel
            {
                Dock = DockStyle.Fill,
                BackColor = Color.White,
                Margin = Padding.Empty
            };
            content.Controls.Add(flowLayoutPanelHistory);
            content.Controls.Add(flowLayoutPanelProjects);

            TableLayoutPanel root = new TableLayoutPanel
            {
                Dock = DockStyle.Fill,
                ColumnCount = 1,
                RowCount = 2,
                BackColor = Color.White,
                Margin = Padding.Empty,
                Padding = Padding.Empty
            };
            root.RowStyles.Add(new RowStyle(SizeType.Absolute, 46F));
            root.RowStyles.Add(new RowStyle(SizeType.Percent, 100F));
            root.Controls.Add(header, 0, 0);
            root.Controls.Add(content, 0, 1);

            Controls.Clear();
            Controls.Add(root);
            ShowTimelineView();
        }

        private static Button CreateHeaderButton(string text)
        {
            Button button = new Button
            {
                Text = text,
                Dock = DockStyle.Fill,
                AutoSize = true,
                AutoSizeMode = AutoSizeMode.GrowAndShrink,
                MinimumSize = new Size(0, 32),
                Padding = new Padding(8, 2, 8, 2),
                Margin = new Padding(3, 2, 3, 2),
                FlatStyle = FlatStyle.Flat,
                BackColor = Color.White,
                ForeColor = TextMuted,
                Font = new Font("맑은 고딕", 8.5F, FontStyle.Bold),
                Cursor = Cursors.Hand
            };
            button.FlatAppearance.BorderColor = Border;
            return button;
        }

        private async Task SaveSnapshotAsync(
            Bitmap snapshot,
            int savedProjectId,
            int savedGeneration,
            int savedStep,
            string fileName,
            string createdAt,
            string fullImagePath)
        {
            Bitmap? thumbnailImage = null;
            try
            {
                await historyWriteGate.WaitAsync().ConfigureAwait(false);
                try
                {
                    if (!acceptingSnapshots || savedGeneration != projectGeneration)
                        return;

                    thumbnailImage = await Task.Run(() =>
                    {
                        Directory.CreateDirectory(Path.GetDirectoryName(fullImagePath)!);
                        snapshot.Save(fullImagePath, ImageFormat.Png);
                        return CreateThumbnail(snapshot, 160, 82);
                    }).ConfigureAwait(false);

                }
                finally
                {
                    historyWriteGate.Release();
                }

                _ = TryInsertDatabaseHistoryAsync(
                    savedProjectId,
                    savedStep,
                    fileName,
                    createdAt);

                if (!acceptingSnapshots || savedGeneration != projectGeneration ||
                    uiResourcesReleased || IsDisposed)
                {
                    return;
                }

                MainForm? uiOwner = mainForm;
                if (uiOwner == null || uiOwner.IsDisposed || !uiOwner.IsHandleCreated)
                    return;

                Bitmap uiThumbnail = thumbnailImage;
                thumbnailImage = null;
                try
                {
                    uiOwner.BeginInvoke(new Action(() =>
                    {
                        if (!acceptingSnapshots || savedGeneration != projectGeneration ||
                            uiResourcesReleased || IsDisposed)
                        {
                            uiThumbnail.Dispose();
                            return;
                        }

                        AddHistoryCard(fullImagePath, savedStep, uiThumbnail);
                        UpdateCurrentProjectLabel();
                    }));
                }
                catch
                {
                    uiThumbnail.Dispose();
                    throw;
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine("Timeline snapshot failed: " + ex.Message);
            }
            finally
            {
                thumbnailImage?.Dispose();
                snapshot.Dispose();
            }
        }

        private void AddHistoryCard(string imagePath, int savedStep, Bitmap thumbnailImage)
        {
            Panel card = new Panel
            {
                Width = 176,
                Height = 124,
                Margin = new Padding(5),
                BackColor = CardBackground,
                BorderStyle = BorderStyle.FixedSingle,
                Tag = imagePath,
                Cursor = Cursors.Hand
            };
            PictureBox thumbnail = new PictureBox
            {
                Width = 164,
                Height = 84,
                SizeMode = PictureBoxSizeMode.Zoom,
                Location = new Point(5, 5),
                BackColor = Color.White,
                Image = thumbnailImage,
                Tag = imagePath,
                Cursor = Cursors.Hand
            };
            Label label = new Label
            {
                Text = $"작업 {savedStep}",
                Location = new Point(7, 92),
                Size = new Size(162, 25),
                ForeColor = Accent,
                Font = new Font("맑은 고딕", 8F, FontStyle.Bold),
                TextAlign = ContentAlignment.MiddleLeft,
                AutoEllipsis = true,
                Tag = imagePath,
                Cursor = Cursors.Hand
            };

            thumbnail.Click += HistoryCard_Click;
            label.Click += HistoryCard_Click;
            card.Click += HistoryCard_Click;
            card.Controls.Add(thumbnail);
            card.Controls.Add(label);
            EditorToolTipService.Set(
                historyToolTip,
                thumbnail,
                $"작업 {savedStep} 상태로 복원");
            flowLayoutPanelHistory.Controls.Add(card);
            flowLayoutPanelHistory.ScrollControlIntoView(card);
        }

        private void AddProjectCard(ProjectSummary project)
        {
            if (flowLayoutPanelProjects == null)
                return;

            string imagePath = string.IsNullOrWhiteSpace(project.LastImageFile)
                ? string.Empty
                : Path.Combine(GetProjectFolder(project.ProjectId), project.LastImageFile);
            Panel card = new Panel
            {
                Width = 188,
                Height = 126,
                Margin = new Padding(5),
                BackColor = project.ProjectId == projectId ? AccentSoft : CardBackground,
                BorderStyle = BorderStyle.FixedSingle,
                Tag = project.ProjectId,
                Cursor = Cursors.Hand
            };
            PictureBox thumbnail = new PictureBox
            {
                Width = 176,
                Height = 82,
                Location = new Point(5, 5),
                SizeMode = PictureBoxSizeMode.Zoom,
                BackColor = Color.White,
                Tag = project.ProjectId,
                Cursor = Cursors.Hand
            };
            if (File.Exists(imagePath))
            {
                try
                {
                    using Bitmap image = LoadBitmapUnlocked(imagePath);
                    thumbnail.Image = CreateThumbnail(image, 176, 82);
                }
                catch (Exception ex)
                {
                    Console.WriteLine("프로젝트 썸네일 로드 생략: " + ex.Message);
                }
            }

            Label title = new Label
            {
                Text = $"프로젝트 #{project.ProjectId}",
                Location = new Point(7, 91),
                Size = new Size(116, 27),
                Font = new Font("맑은 고딕", 8F, FontStyle.Bold),
                ForeColor = TextPrimary,
                TextAlign = ContentAlignment.MiddleLeft,
                AutoEllipsis = true,
                Tag = project.ProjectId,
                Cursor = Cursors.Hand
            };
            Label count = new Label
            {
                Text = $"{project.StepCount}단계",
                Location = new Point(124, 91),
                Size = new Size(56, 27),
                Font = new Font("맑은 고딕", 8F),
                ForeColor = TextMuted,
                TextAlign = ContentAlignment.MiddleRight,
                Tag = project.ProjectId,
                Cursor = Cursors.Hand
            };

            card.Click += ProjectCard_Click;
            thumbnail.Click += ProjectCard_Click;
            title.Click += ProjectCard_Click;
            count.Click += ProjectCard_Click;
            card.DoubleClick += ProjectCard_DoubleClick;
            thumbnail.DoubleClick += ProjectCard_DoubleClick;
            title.DoubleClick += ProjectCard_DoubleClick;
            count.DoubleClick += ProjectCard_DoubleClick;
            card.Controls.Add(thumbnail);
            card.Controls.Add(title);
            card.Controls.Add(count);
            flowLayoutPanelProjects.Controls.Add(card);
        }

        private void HistoryCard_Click(object? sender, EventArgs e)
        {
            if (sender is not Control control || control.Tag is not string imagePath ||
                !File.Exists(imagePath))
            {
                MessageBox.Show("저장된 타임라인 이미지를 찾을 수 없습니다.");
                return;
            }

            try
            {
                Bitmap restoredImage = LoadBitmapUnlocked(imagePath);
                if (mainForm == null)
                {
                    restoredImage.Dispose();
                    return;
                }

                mainForm.RestoreHistoryImage(restoredImage);
                foreach (Control item in flowLayoutPanelHistory.Controls)
                    item.BackColor = CardBackground;
                Control selectedCard = control is Panel
                    ? control
                    : control.Parent ?? control;
                selectedCard.BackColor = AccentSoft;
            }
            catch (Exception ex)
            {
                MessageBox.Show("타임라인 이미지를 불러오지 못했습니다.\n" + ex.Message);
            }
        }

        private void ProjectCard_Click(object? sender, EventArgs e)
        {
            if (sender is not Control control || control.Tag is not int targetProjectId)
                return;

            // 한 번 클릭은 삭제 등 프로젝트 관리용 선택만 수행한다.
            // 프로젝트 전환은 더블클릭으로 분리해 선택 과정에서 저장 확인 창이
            // 반복해서 나타나지 않게 한다.
            selectedProjectId = targetProjectId;
            HighlightSelectedProject();
            UpdateDeleteButton();
            SetStatus($"프로젝트 #{targetProjectId} 선택 · 더블클릭하면 열립니다.");
        }

        private async void ProjectCard_DoubleClick(object? sender, EventArgs e)
        {
            if (sender is not Control control || control.Tag is not int targetProjectId)
                return;

            selectedProjectId = targetProjectId;
            HighlightSelectedProject();
            UpdateDeleteButton();

            if (mainForm != null && !mainForm.ConfirmTimelineProjectSwitch())
                return;

            await LoadProjectHistoryAsync(targetProjectId);
        }

        private async Task LoadProjectHistoryAsync(int targetProjectId)
        {
            acceptingSnapshots = false;
            await historyWriteGate.WaitAsync();
            try
            {
                SetStatus($"프로젝트 #{targetProjectId} 불러오는 중...");
                IReadOnlyList<HistoryEntry> entries =
                    await QueryProjectHistoryAsync(targetProjectId);
                string targetFolder = GetProjectFolder(targetProjectId);
                List<HistoryVisual> visuals = await Task.Run(() =>
                    LoadHistoryVisuals(entries, targetFolder));

                projectGeneration++;
                projectId = targetProjectId;
                selectedProjectId = targetProjectId;
                currentHistoryFolder = targetFolder;
                stepIndex = entries.Count == 0 ? 0 : entries[^1].Step;
                ClearFlowPanel(flowLayoutPanelHistory);
                foreach (HistoryVisual visual in visuals)
                {
                    AddHistoryCard(visual.ImagePath, visual.Step, visual.Thumbnail);
                    visual.Thumbnail = null!;
                }

                Bitmap? lastImage = visuals.Count == 0
                    ? null
                    : visuals[^1].FullImage;

                ShowTimelineView();
                if (lastImage != null)
                {
                    if (mainForm == null)
                        lastImage.Dispose();
                    else
                        mainForm.RestoreHistoryImage(lastImage);
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine("프로젝트 기록 로드 실패: " + ex.Message);
                MessageBox.Show(
                    "프로젝트 기록을 불러오지 못했습니다.\n" + ex.Message,
                    "프로젝트 기록",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Warning);
            }
            finally
            {
                historyWriteGate.Release();
                if (!uiResourcesReleased)
                    acceptingSnapshots = true;
            }
        }

        private static List<HistoryVisual> LoadHistoryVisuals(
            IReadOnlyList<HistoryEntry> entries,
            string folder)
        {
            List<HistoryVisual> result = new List<HistoryVisual>();
            try
            {
                foreach (HistoryEntry entry in entries)
                {
                    string path = Path.Combine(folder, entry.FileName);
                    if (!File.Exists(path))
                        continue;
                    Bitmap image = LoadBitmapUnlocked(path);
                    try
                    {
                        Bitmap thumbnail = CreateThumbnail(image, 160, 82);
                        if (result.Count > 0)
                        {
                            result[^1].FullImage?.Dispose();
                            result[^1].FullImage = null;
                        }
                        result.Add(new HistoryVisual(entry.Step, path, thumbnail, image));
                        image = null!;
                    }
                    finally
                    {
                        image?.Dispose();
                    }
                }
                return result;
            }
            catch
            {
                foreach (HistoryVisual visual in result)
                {
                    visual.Thumbnail?.Dispose();
                    visual.FullImage?.Dispose();
                }
                throw;
            }
        }

        private async Task DeleteSelectedProjectAsync()
        {
            int targetProjectId = selectedProjectId;
            if (targetProjectId <= 0)
                return;

            DialogResult result = MessageBox.Show(
                $"프로젝트 #{targetProjectId}과 모든 편집 기록을 삭제할까요?",
                "프로젝트 삭제",
                MessageBoxButtons.YesNo,
                MessageBoxIcon.Warning);
            if (result != DialogResult.Yes)
                return;

            bool deletingCurrentProject = projectId == targetProjectId;
            bool deletionSucceeded = false;
            acceptingSnapshots = false;
            await historyWriteGate.WaitAsync();
            try
            {
                try
                {
                    await DeleteProjectDatabaseRowsAsync(targetProjectId);
                }
                catch (Exception ex)
                {
                    Console.WriteLine("프로젝트 DB 삭제 생략: " + ex.Message);
                }
                await DeleteFolderBestEffortAsync(GetProjectFolder(targetProjectId));
                await RemoveCatalogProjectAsync(targetProjectId);
                if (deletingCurrentProject)
                {
                    projectGeneration++;
                    ResetProjectState();
                    ClearFlowPanel(flowLayoutPanelHistory);
                }

                selectedProjectId = 0;
                deletionSucceeded = true;
            }
            catch (Exception ex)
            {
                MessageBox.Show(
                    "프로젝트를 삭제하지 못했습니다.\n" + ex.Message,
                    "프로젝트 삭제",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Warning);
            }
            finally
            {
                historyWriteGate.Release();
                if (!uiResourcesReleased)
                    acceptingSnapshots = true;
            }

            if (deletionSucceeded && deletingCurrentProject &&
                mainForm?.CurrentEditorImage != null)
            {
                await StartNewProjectAsync(CurrentUser.UserNo);
                SQLInsert();
                await ShowProjectViewAsync(CurrentUser.UserNo);
            }
            else if (deletionSucceeded)
            {
                await LoadProjectListAsync(CurrentUser.UserNo);
            }
        }

        private async Task<int> InsertProjectAsync(int userId)
        {
            using MySqlConnection connection = new MySqlConnection(connectionAddress);
            await connection.OpenAsync();
            using (MySqlCommand tableCommand = new MySqlCommand(
                "SELECT COUNT(*) FROM INFORMATION_SCHEMA.TABLES " +
                "WHERE TABLE_SCHEMA = DATABASE() AND TABLE_NAME = 'Projects';",
                connection))
            {
                int tableCount = Convert.ToInt32(
                    await tableCommand.ExecuteScalarAsync());
                if (tableCount == 0)
                    return 0;
            }

            const string query = "INSERT INTO Projects (uid) VALUES (@uid);";
            using MySqlCommand command = new MySqlCommand(query, connection);
            command.Parameters.AddWithValue("@uid", userId);
            await command.ExecuteNonQueryAsync();
            long insertedId = command.LastInsertedId;
            return insertedId > 0 && insertedId <= int.MaxValue
                ? (int)insertedId
                : 0;
        }

        private async Task TryInsertDatabaseHistoryAsync(
            int targetProjectId,
            int step,
            string fileName,
            string createdAt)
        {
            if (targetProjectId <= 0 || string.IsNullOrWhiteSpace(connectionAddress) ||
                !databaseAvailableForTimeline)
                return;

            await databaseWriteGate.WaitAsync().ConfigureAwait(false);
            try
            {
                if (!databaseAvailableForTimeline)
                    return;
                using MySqlConnection connection = new MySqlConnection(connectionAddress);
                await connection.OpenAsync().ConfigureAwait(false);
                const string query = @"
INSERT INTO EditHistory (project_id, step_index, image_path, created_at)
VALUES (@projectId, @stepIndex, @imagePath, @createdAt);";
                using MySqlCommand command = new MySqlCommand(query, connection);
                command.Parameters.AddWithValue("@projectId", targetProjectId);
                command.Parameters.AddWithValue("@stepIndex", step);
                command.Parameters.AddWithValue("@imagePath", fileName);
                command.Parameters.AddWithValue("@createdAt", createdAt);
                await command.ExecuteNonQueryAsync().ConfigureAwait(false);
            }
            catch (Exception ex)
            {
                databaseAvailableForTimeline = false;
                Console.WriteLine("타임라인 DB 기록 생략: " + ex.Message);
            }
            finally
            {
                databaseWriteGate.Release();
            }
        }

        private async Task<IReadOnlyList<ProjectSummary>> QueryProjectsAsync(int userId)
        {
            IReadOnlyList<ProjectCatalogEntry> catalog =
                await ReadProjectCatalogAsync();
            return await Task.Run<IReadOnlyList<ProjectSummary>>(() =>
            {
                List<ProjectSummary> result = new List<ProjectSummary>();
                foreach (ProjectCatalogEntry entry in catalog
                    .Where(item => item.UserId == userId)
                    .OrderByDescending(item => item.ProjectId))
                {
                    string folder = GetProjectFolder(entry.ProjectId);
                    string[] files = Directory.Exists(folder)
                        ? Directory.GetFiles(folder, "*.png")
                            .OrderBy(ParseHistoryStep)
                            .ThenBy(path => path, StringComparer.OrdinalIgnoreCase)
                            .ToArray()
                        : Array.Empty<string>();
                    result.Add(new ProjectSummary(
                        entry.ProjectId,
                        files.Length == 0 ? null : Path.GetFileName(files[^1]),
                        files.Length));
                }
                return result;
            });
        }

        private async Task<IReadOnlyList<HistoryEntry>> QueryProjectHistoryAsync(
            int targetProjectId)
        {
            return await Task.Run<IReadOnlyList<HistoryEntry>>(() =>
            {
                string folder = GetProjectFolder(targetProjectId);
                if (!Directory.Exists(folder))
                    return Array.Empty<HistoryEntry>();

                List<HistoryEntry> result = new List<HistoryEntry>();
                foreach (string path in Directory.GetFiles(folder, "*.png")
                    .OrderBy(ParseHistoryStep)
                    .ThenBy(item => item, StringComparer.OrdinalIgnoreCase))
                {
                    string fileName = Path.GetFileName(path);
                    string stepText = fileName.Split('_')[0];
                    if (!int.TryParse(stepText, out int step))
                        continue;
                    result.Add(new HistoryEntry(step, fileName));
                }
                return result;
            });
        }

        private static int ParseHistoryStep(string path)
        {
            string stepText = Path.GetFileName(path).Split('_')[0];
            return int.TryParse(stepText, out int step) ? step : int.MaxValue;
        }

        private async Task DeleteProjectDatabaseRowsAsync(int targetProjectId)
        {
            if (targetProjectId <= 0 || string.IsNullOrWhiteSpace(connectionAddress))
                return;

            using MySqlConnection connection = new MySqlConnection(connectionAddress);
            await connection.OpenAsync();
            using MySqlTransaction transaction = await connection.BeginTransactionAsync();
            try
            {
                using MySqlCommand historyCommand = new MySqlCommand(
                    "DELETE FROM EditHistory WHERE project_id = @projectId;",
                    connection,
                    transaction);
                historyCommand.Parameters.AddWithValue("@projectId", targetProjectId);
                await historyCommand.ExecuteNonQueryAsync();

                using MySqlCommand tableCommand = new MySqlCommand(
                    "SELECT COUNT(*) FROM INFORMATION_SCHEMA.TABLES " +
                    "WHERE TABLE_SCHEMA = DATABASE() AND TABLE_NAME = 'Projects';",
                    connection,
                    transaction);
                int projectsTableCount = Convert.ToInt32(
                    await tableCommand.ExecuteScalarAsync());
                if (projectsTableCount > 0)
                {
                    using MySqlCommand projectCommand = new MySqlCommand(
                        "DELETE FROM Projects WHERE project_id = @projectId;",
                        connection,
                        transaction);
                    projectCommand.Parameters.AddWithValue("@projectId", targetProjectId);
                    await projectCommand.ExecuteNonQueryAsync();
                }
                await transaction.CommitAsync();
            }
            catch
            {
                await transaction.RollbackAsync();
                throw;
            }
        }

        private async Task<int> AllocateLocalProjectIdAsync(
            int userId,
            bool includeDatabaseMaximum)
        {
            await CatalogGate.WaitAsync();
            try
            {
                List<ProjectCatalogEntry> catalog = await LoadCatalogFileAsync();
                int maximumId = catalog.Count == 0
                    ? 0
                    : catalog.Max(item => item.ProjectId);
                if (Directory.Exists(historyRoot))
                {
                    foreach (string directory in Directory.GetDirectories(historyRoot))
                    {
                        if (int.TryParse(Path.GetFileName(directory), out int folderId))
                            maximumId = Math.Max(maximumId, folderId);
                    }
                }
                if (includeDatabaseMaximum)
                {
                    maximumId = Math.Max(
                        maximumId,
                        await QueryMaximumHistoryProjectIdAsync());
                }
                int nextId = maximumId == int.MaxValue ? 1 : maximumId + 1;
                while (catalog.Any(item => item.ProjectId == nextId))
                    nextId++;

                catalog.Add(new ProjectCatalogEntry
                {
                    ProjectId = nextId,
                    UserId = userId,
                    CreatedAt = DateTime.Now
                });
                await SaveCatalogFileAsync(catalog);
                return nextId;
            }
            finally
            {
                CatalogGate.Release();
            }
        }

        private async Task EnsureCatalogProjectAsync(int targetProjectId, int userId)
        {
            await CatalogGate.WaitAsync();
            try
            {
                List<ProjectCatalogEntry> catalog = await LoadCatalogFileAsync();
                if (catalog.Any(item => item.ProjectId == targetProjectId))
                    return;
                catalog.Add(new ProjectCatalogEntry
                {
                    ProjectId = targetProjectId,
                    UserId = userId,
                    CreatedAt = DateTime.Now
                });
                await SaveCatalogFileAsync(catalog);
            }
            finally
            {
                CatalogGate.Release();
            }
        }

        private async Task RemoveCatalogProjectAsync(int targetProjectId)
        {
            await CatalogGate.WaitAsync();
            try
            {
                List<ProjectCatalogEntry> catalog = await LoadCatalogFileAsync();
                if (catalog.RemoveAll(item => item.ProjectId == targetProjectId) > 0)
                    await SaveCatalogFileAsync(catalog);
            }
            finally
            {
                CatalogGate.Release();
            }
        }

        private async Task<IReadOnlyList<ProjectCatalogEntry>> ReadProjectCatalogAsync()
        {
            await CatalogGate.WaitAsync();
            try
            {
                return await LoadCatalogFileAsync();
            }
            finally
            {
                CatalogGate.Release();
            }
        }

        private async Task<List<ProjectCatalogEntry>> LoadCatalogFileAsync()
        {
            if (!File.Exists(projectCatalogPath))
                return new List<ProjectCatalogEntry>();

            try
            {
                await using FileStream stream = new FileStream(
                    projectCatalogPath,
                    FileMode.Open,
                    FileAccess.Read,
                    FileShare.Read);
                return await JsonSerializer.DeserializeAsync<List<ProjectCatalogEntry>>(stream)
                    ?? new List<ProjectCatalogEntry>();
            }
            catch (Exception ex)
            {
                Console.WriteLine("프로젝트 카탈로그 읽기 실패: " + ex.Message);
                return new List<ProjectCatalogEntry>();
            }
        }

        private async Task SaveCatalogFileAsync(List<ProjectCatalogEntry> catalog)
        {
            Directory.CreateDirectory(historyRoot);
            string temporaryPath = projectCatalogPath + ".tmp";
            await using (FileStream stream = new FileStream(
                temporaryPath,
                FileMode.Create,
                FileAccess.Write,
                FileShare.None))
            {
                await JsonSerializer.SerializeAsync(
                    stream,
                    catalog,
                    new JsonSerializerOptions { WriteIndented = true });
            }
            File.Move(temporaryPath, projectCatalogPath, true);
        }

        private static void InitializeHistoryStorage()
        {
            Directory.CreateDirectory(HistoryRootPath);
            string legacyRoot = Path.Combine(
                AppDomain.CurrentDomain.BaseDirectory,
                "HistoryImages");
            if (!Directory.Exists(legacyRoot) ||
                string.Equals(
                    Path.GetFullPath(legacyRoot),
                    Path.GetFullPath(HistoryRootPath),
                    StringComparison.OrdinalIgnoreCase))
            {
                return;
            }

            try
            {
                CopyMissingHistoryFiles(legacyRoot, HistoryRootPath);
            }
            catch (IOException ex)
            {
                System.Diagnostics.Trace.WriteLine(
                    "기존 타임라인 기록 복사 생략: " + ex.Message);
            }
            catch (UnauthorizedAccessException ex)
            {
                System.Diagnostics.Trace.WriteLine(
                    "기존 타임라인 기록 접근 생략: " + ex.Message);
            }
        }

        private static void CopyMissingHistoryFiles(string source, string destination)
        {
            Directory.CreateDirectory(destination);
            foreach (string file in Directory.EnumerateFiles(source))
            {
                string target = Path.Combine(destination, Path.GetFileName(file));
                if (!File.Exists(target))
                    File.Copy(file, target);
            }

            foreach (string directory in Directory.EnumerateDirectories(source))
            {
                CopyMissingHistoryFiles(
                    directory,
                    Path.Combine(destination, Path.GetFileName(directory)));
            }
        }

        private async Task<int> QueryMaximumHistoryProjectIdAsync()
        {
            if (string.IsNullOrWhiteSpace(connectionAddress))
                return 0;
            try
            {
                using MySqlConnection connection = new MySqlConnection(connectionAddress);
                await connection.OpenAsync();
                using MySqlCommand command = new MySqlCommand(
                    "SELECT COALESCE(MAX(project_id), 0) FROM EditHistory;",
                    connection);
                return Convert.ToInt32(await command.ExecuteScalarAsync());
            }
            catch (Exception ex)
            {
                Console.WriteLine("타임라인 프로젝트 번호 조회 생략: " + ex.Message);
                return 0;
            }
        }

        private void SetViewButtonState(bool timelineSelected)
        {
            if (btnTimelineView == null || btnProjectView == null)
                return;
            StyleViewButton(btnTimelineView, timelineSelected);
            StyleViewButton(btnProjectView, !timelineSelected);
        }

        private static void StyleViewButton(Button button, bool selected)
        {
            button.BackColor = selected ? AccentSoft : Color.White;
            button.ForeColor = selected ? Accent : TextMuted;
            button.FlatAppearance.BorderColor = selected ? Accent : Border;
        }

        private void UpdateCurrentProjectLabel()
        {
            SetStatus(projectId > 0
                ? $"프로젝트 #{projectId} · 작업 {stepIndex}단계"
                : stepIndex > 0
                    ? $"로컬 작업 · {stepIndex}단계"
                    : "이미지를 열면 새 프로젝트가 생성됩니다.");
        }

        private void SetStatus(string text)
        {
            if (lblCurrentProject != null)
                lblCurrentProject.Text = text;
        }

        private void HighlightSelectedProject()
        {
            if (flowLayoutPanelProjects == null)
                return;
            foreach (Control control in flowLayoutPanelProjects.Controls)
            {
                control.BackColor = control.Tag is int id && id == selectedProjectId
                    ? AccentSoft
                    : CardBackground;
            }
        }

        private void UpdateDeleteButton()
        {
            if (btnDeleteProject != null)
                btnDeleteProject.Enabled = selectedProjectId > 0;
        }

        private string GetProjectFolder(int targetProjectId) =>
            Path.Combine(historyRoot, targetProjectId.ToString());

        private void ResetProjectState()
        {
            projectId = 0;
            selectedProjectId = 0;
            stepIndex = 0;
            currentHistoryFolder = string.Empty;
            UpdateDeleteButton();
            UpdateCurrentProjectLabel();
        }

        private static async Task DeleteFolderBestEffortAsync(string folder)
        {
            if (string.IsNullOrWhiteSpace(folder) || !Directory.Exists(folder))
                return;
            await Task.Run(() => Directory.Delete(folder, true));
        }

        private static Bitmap LoadBitmapUnlocked(string path)
        {
            using FileStream stream = new FileStream(
                path,
                FileMode.Open,
                FileAccess.Read,
                FileShare.Read);
            using Image image = Image.FromStream(stream);
            return new Bitmap(image);
        }

        private static Bitmap CreateThumbnail(Image source, int maxWidth, int maxHeight)
        {
            float scale = Math.Min(
                (float)maxWidth / source.Width,
                (float)maxHeight / source.Height);
            int width = Math.Max(1, (int)Math.Round(source.Width * scale));
            int height = Math.Max(1, (int)Math.Round(source.Height * scale));
            Bitmap thumbnail = new Bitmap(width, height, PixelFormat.Format32bppArgb);
            using Graphics graphics = Graphics.FromImage(thumbnail);
            graphics.Clear(Color.White);
            graphics.InterpolationMode = InterpolationMode.HighQualityBicubic;
            graphics.SmoothingMode = SmoothingMode.HighQuality;
            graphics.PixelOffsetMode = PixelOffsetMode.HighQuality;
            graphics.DrawImage(source, new Rectangle(0, 0, width, height));
            return thumbnail;
        }

        private static void ClearFlowPanel(FlowLayoutPanel panel)
        {
            for (int index = panel.Controls.Count - 1; index >= 0; index--)
            {
                Control control = panel.Controls[index];
                panel.Controls.RemoveAt(index);
                DisposeImages(control);
                control.Dispose();
            }
        }

        private static void DisposeImages(Control control)
        {
            if (control is PictureBox pictureBox)
            {
                pictureBox.Image?.Dispose();
                pictureBox.Image = null;
            }
            foreach (Control child in control.Controls)
                DisposeImages(child);
        }

        private sealed class ProjectSummary
        {
            internal ProjectSummary(int projectId, string? lastImageFile, int stepCount)
            {
                ProjectId = projectId;
                LastImageFile = lastImageFile;
                StepCount = stepCount;
            }

            internal int ProjectId { get; }
            internal string? LastImageFile { get; }
            internal int StepCount { get; }
        }

        private sealed class ProjectCatalogEntry
        {
            public int ProjectId { get; set; }
            public int UserId { get; set; }
            public DateTime CreatedAt { get; set; }
        }

        private sealed class HistoryEntry
        {
            internal HistoryEntry(int step, string fileName)
            {
                Step = step;
                FileName = fileName;
            }

            internal int Step { get; }
            internal string FileName { get; }
        }

        private sealed class HistoryVisual
        {
            internal HistoryVisual(
                int step,
                string imagePath,
                Bitmap thumbnail,
                Bitmap? fullImage)
            {
                Step = step;
                ImagePath = imagePath;
                Thumbnail = thumbnail;
                FullImage = fullImage;
            }

            internal int Step { get; }
            internal string ImagePath { get; }
            internal Bitmap Thumbnail { get; set; }
            internal Bitmap? FullImage { get; set; }
        }
    }
}
