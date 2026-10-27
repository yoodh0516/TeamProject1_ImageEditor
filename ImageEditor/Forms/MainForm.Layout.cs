using ImageEditor.Controls;
using System.Drawing.Drawing2D;

namespace ImageEditor
{
    public partial class MainForm : Form
    {
        private Label? lblStatusMessage;
        private Label? lblStatusFileName;
        private Label? lblStatusImageSize;
        private Label? lblStatusZoom;
        private Panel? pnlCanvasEmpty;
        private Label? lblCanvasGuide;
        private Panel? toolPlaceholderPanel;
        private Label? lblToolPlaceholderStateTitle;
        private Label? lblToolPlaceholderStateDescription;
        private Label? lblToolPlaceholderFooter;
        private UserControl? comingSoonPanel;

        private Panel? pnlQuickBar;
        private TableLayoutPanel? workspaceLayout;
        private TableLayoutPanel? centerLayout;
        private Panel? pnlLayerHost;
        private Panel? pnlTimelineDrawer;
        private Button? btnQuickUndo;
        private Button? btnQuickRedo;
        private Button? btnQuickSave;
        private Button? btnQuickFit;
        private Button? btnQuickBeforeAfter;
        private Button? btnToggleTimeline;
        private Button? btnToggleWorkPanel;
        private Button? btnToggleDetailPanel;
        private bool timelineVisible;
        private bool workPanelVisible = true;
        private bool detailPanelVisible = true;
        private bool autoFitCanvas = true;
        private System.Windows.Forms.Timer? canvasResizeTimer;
        private ToolTip? editorToolTip;

        private const float WorkPanelWidth = 250F;
        private const float DetailPanelWidth = 310F;
        // 썸네일 카드(132px), 안쪽 여백과 가로 스크롤바 높이를 기준으로 맞춘다.
        // 기존 196px은 한 줄 타임라인 아래에 약 40px 이상의 빈 공간을 남겼다.
        private const float TimelineHeight = 210F;

        private static readonly Color UiAccent = Color.FromArgb(64, 101, 242);
        private static readonly Color UiAccentSoft = Color.FromArgb(235, 240, 255);
        private static readonly Color UiText = Color.FromArgb(30, 38, 52);
        private static readonly Color UiMuted = Color.FromArgb(110, 121, 139);
        private static readonly Color UiBorder = Color.FromArgb(220, 225, 233);
        private static readonly Color UiSurface = Color.White;
        private static readonly Color UiWorkspace = Color.FromArgb(232, 236, 243);
        private static readonly Color UiAppBackground = Color.FromArgb(244, 246, 250);

        /// <summary>
        /// 디자이너의 기존 기능 컨트롤을 재사용하면서
        /// 고정 레이어·가변 캔버스·접이식 타임라인·상세 설정 구조로 재배치한다.
        /// </summary>
        private void InitializeModernLayout()
        {
            SuspendLayout();

            Text = "ImageEditor · Social Content Studio";
            Font = new Font("맑은 고딕", 9F);
            BackColor = UiAppBackground;
            Rectangle workingArea = Screen.PrimaryScreen?.WorkingArea
                ?? new Rectangle(0, 0, 1440, 900);
            ClientSize = new Size(
                Math.Max(960, Math.Min(1440, workingArea.Width - 36)),
                Math.Max(600, Math.Min(860, workingArea.Height - 72)));
            MinimumSize = new Size(1100, 700);
            StartPosition = FormStartPosition.CenterScreen;
            DoubleBuffered = true;

            ConfigureMainMenu();
            ConfigureHeader();
            ConfigureQuickToolbar();
            ConfigureLeftPanel();
            ConfigureCanvasPanel();
            ConfigureTimelineDrawer();
            ConfigureToolOptionsPanel();
            ConfigureStatusBar();

            centerLayout = new TableLayoutPanel
            {
                Dock = DockStyle.Fill,
                BackColor = UiAppBackground,
                ColumnCount = 1,
                RowCount = 2,
                Margin = Padding.Empty,
                Padding = Padding.Empty
            };
            centerLayout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100F));
            centerLayout.RowStyles.Add(new RowStyle(SizeType.Percent, 100F));
            centerLayout.RowStyles.Add(new RowStyle(SizeType.Absolute, 0F));
            pnlCanvas.Margin = Padding.Empty;
            pnlTimelineDrawer!.Margin = Padding.Empty;
            centerLayout.Controls.Add(pnlCanvas, 0, 0);
            centerLayout.Controls.Add(pnlTimelineDrawer, 0, 1);

            workspaceLayout = new TableLayoutPanel
            {
                Dock = DockStyle.Fill,
                BackColor = UiAppBackground,
                ColumnCount = 3,
                RowCount = 1,
                Margin = Padding.Empty,
                Padding = new Padding(6, 0, 6, 6)
            };
            workspaceLayout.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, WorkPanelWidth));
            workspaceLayout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100F));
            workspaceLayout.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, DetailPanelWidth));
            workspaceLayout.RowStyles.Add(new RowStyle(SizeType.Percent, 100F));

            pnlLeft.Margin = new Padding(0, 0, 6, 0);
            centerLayout.Margin = Padding.Empty;
            pnlToolOptions.Margin = new Padding(6, 0, 0, 0);
            workspaceLayout.Controls.Add(pnlLeft, 0, 0);
            workspaceLayout.Controls.Add(centerLayout, 1, 0);
            workspaceLayout.Controls.Add(pnlToolOptions, 2, 0);

            TableLayoutPanel root = new TableLayoutPanel
            {
                Dock = DockStyle.Fill,
                BackColor = UiAppBackground,
                ColumnCount = 1,
                RowCount = 4,
                Margin = Padding.Empty,
                Padding = Padding.Empty
            };
            root.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100F));
            root.RowStyles.Add(new RowStyle(SizeType.Absolute, 56F));
            root.RowStyles.Add(new RowStyle(SizeType.Absolute, 48F));
            root.RowStyles.Add(new RowStyle(SizeType.Percent, 100F));
            root.RowStyles.Add(new RowStyle(SizeType.Absolute, 34F));
            root.Controls.Add(pnlHeader, 0, 0);
            root.Controls.Add(pnlQuickBar!, 0, 1);
            root.Controls.Add(workspaceLayout, 0, 2);
            root.Controls.Add(pnlBottom, 0, 3);

            Controls.Clear();
            Controls.Add(root);
            MainMenuStrip = menuStrip1;

            editorToolTip ??= EditorToolTipService.Attach(this);
            EditorToolTipService.ApplyMenuToolTips(menuStrip1.Items);

            ResumeLayout(true);
        }

        private void ConfigureHeader()
        {
            pnlHeader.Controls.Clear();
            pnlHeader.Dock = DockStyle.Fill;
            pnlHeader.BackColor = UiSurface;
            pnlHeader.Margin = new Padding(0, 0, 0, 1);
            pnlHeader.Padding = new Padding(14, 0, 12, 0);

            Panel brandPanel = new Panel
            {
                Dock = DockStyle.Fill,
                BackColor = UiSurface,
                Margin = Padding.Empty
            };
            Label logo = new Label
            {
                Text = "IE",
                Location = new Point(0, 11),
                Size = new Size(34, 34),
                TextAlign = ContentAlignment.MiddleCenter,
                Font = new Font("Segoe UI", 10F, FontStyle.Bold),
                ForeColor = Color.White,
                BackColor = UiAccent
            };
            Label brandTitle = new Label
            {
                Text = "IMAGE EDITOR",
                Location = new Point(45, 12),
                AutoSize = true,
                Font = new Font("Segoe UI", 10.5F, FontStyle.Bold),
                ForeColor = UiText
            };
            Label brandSubtitle = new Label
            {
                Text = "CONTENT STUDIO",
                Location = new Point(45, 31),
                AutoSize = true,
                Font = new Font("Segoe UI", 7.5F),
                ForeColor = UiMuted
            };
            brandPanel.Controls.Add(logo);
            brandPanel.Controls.Add(brandTitle);
            brandPanel.Controls.Add(brandSubtitle);

            Panel accountPanel = new Panel
            {
                Dock = DockStyle.Fill,
                BackColor = UiSurface,
                Padding = new Padding(8, 9, 4, 9),
                Margin = Padding.Empty
            };
            label1.AutoSize = false;
            label1.Dock = DockStyle.Fill;
            label1.TextAlign = ContentAlignment.MiddleRight;
            label1.Padding = new Padding(8, 0, 8, 0);
            label1.Font = new Font("맑은 고딕", 9F, FontStyle.Bold);
            label1.ForeColor = UiAccent;
            label1.BackColor = Color.FromArgb(247, 249, 255);
            accountPanel.Controls.Add(label1);

            TableLayoutPanel headerLayout = new TableLayoutPanel
            {
                Dock = DockStyle.Fill,
                BackColor = UiSurface,
                ColumnCount = 3,
                RowCount = 1,
                Margin = Padding.Empty,
                Padding = Padding.Empty
            };
            headerLayout.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 188F));
            headerLayout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100F));
            headerLayout.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 170F));
            headerLayout.RowStyles.Add(new RowStyle(SizeType.Percent, 100F));

            menuStrip1.Dock = DockStyle.None;
            menuStrip1.Anchor = AnchorStyles.Left;
            menuStrip1.Margin = Padding.Empty;
            headerLayout.Controls.Add(brandPanel, 0, 0);
            headerLayout.Controls.Add(menuStrip1, 1, 0);
            headerLayout.Controls.Add(accountPanel, 2, 0);
            pnlHeader.Controls.Add(headerLayout);
        }

        private void ConfigureMainMenu()
        {
            menuStrip1.SuspendLayout();
            menuStrip1.Items.Clear();

            // 기능을 너무 잘게 나누지 않고 작업 흐름 기준으로 묶는다.
            toolStripMenuItem4.Text = "보정·필터";
            toolStripMenuItem4.DropDownItems.Clear();
            toolStripMenuItem5.DropDownItems.Clear();
            toolStripMenuItem6.DropDownItems.Clear();
            toolStripMenuItem7.DropDownItems.Clear();

            toolStripMenuItem4.DropDownItems.Add(menuBrightness);
            toolStripMenuItem4.DropDownItems.Add(new ToolStripSeparator());
            toolStripMenuItem4.DropDownItems.Add(menuBasicFilter);
            toolStripMenuItem4.DropDownItems.Add(menuMyStyle);

            toolStripMenuItem5.Text = "꾸미기";
            toolStripMenuItem5.DropDownItems.Add(menuTemplate);
            toolStripMenuItem5.DropDownItems.Add(menuCommunityFilter);
            toolStripMenuItem5.DropDownItems.Add(menuAddText);
            toolStripMenuItem5.DropDownItems.Add(new ToolStripSeparator());
            toolStripMenuItem5.DropDownItems.Add(menuDrawingTool);
            toolStripMenuItem5.DropDownItems.Add(menuMask);

            menuAIRemoveObject.Text = "AI";
            // 최신 통합본에서 이미 구성한 AI 하위 메뉴와 이벤트를 보존한다.
            menuAICaption.Text = "유행어·밈 추천";
            menuAICaption.Visible = true;
            menuAICaption.Enabled = true;
            menuAITextPosition.Visible = false;

            toolStripMenuItem1.DropDownItems.Clear();
            toolStripMenuItem1.DropDownItems.Add(menuOpen);
            toolStripMenuItem1.DropDownItems.Add(new ToolStripSeparator());
            toolStripMenuItem1.DropDownItems.Add(menuSave);
            toolStripMenuItem1.DropDownItems.Add(menuSaveAs);
            toolStripMenuItem1.DropDownItems.Add(menuExport);
            toolStripMenuItem1.DropDownItems.Add(new ToolStripSeparator());
            toolStripMenuItem1.DropDownItems.Add(menuExit);

            // 내보내기는 SNS 규격 선택이 바로 보이도록 한 단계로 구성한다.
            // 기존 JPG/PNG 내보내기 코드는 보존하되 파일 메뉴에는 노출하지 않는다.
            menuExport.DropDownItems.Clear();
            menuExport.DropDownItems.Add(menuInstagramSquareExport);
            menuExport.DropDownItems.Add(menuInstagramPortraitExport);
            menuExport.DropDownItems.Add(menuVerticalExport);
            menuExport.DropDownItems.Add(menuYoutubeThumbnailExport);
            menuExport.DropDownItems.Add(menuWebThumbnailExport);
            menuExport.Text = "SNS용 내보내기";
            menuExport.ToolTipText = "선택한 SNS 또는 웹 규격에 맞춰 이미지를 저장합니다.";

            toolStripMenuItem2.DropDownItems.Clear();
            toolStripMenuItem2.DropDownItems.Add(menuUndo);
            toolStripMenuItem2.DropDownItems.Add(menuRedo);
            toolStripMenuItem2.DropDownItems.Add(new ToolStripSeparator());
            toolStripMenuItem2.DropDownItems.Add(menuResetOriginal);

            toolStripMenuItem3.DropDownItems.Clear();
            toolStripMenuItem3.DropDownItems.Add(menuResize);
            toolStripMenuItem3.DropDownItems.Add(menuCrop);
            toolStripMenuItem3.DropDownItems.Add(menuSnsRatio);
            toolStripMenuItem3.DropDownItems.Add(new ToolStripSeparator());
            toolStripMenuItem3.DropDownItems.Add(menuRotateLeft);
            toolStripMenuItem3.DropDownItems.Add(menuRotateRight);
            toolStripMenuItem3.DropDownItems.Add(new ToolStripSeparator());
            toolStripMenuItem3.DropDownItems.Add(menuFlipHorizontal);
            toolStripMenuItem3.DropDownItems.Add(menuFlipVertical);

            toolStripMenuItem9.DropDownItems.Clear();
            toolStripMenuItem9.DropDownItems.Add(menuZoom);
            toolStripMenuItem9.DropDownItems.Add(new ToolStripSeparator());
            toolStripMenuItem9.DropDownItems.Add(menuBeforeAfter);
            toolStripMenuItem9.DropDownItems.Add(menuPreview);

            menuStrip1.Items.AddRange(new ToolStripItem[]
            {
                toolStripMenuItem1,
                toolStripMenuItem2,
                toolStripMenuItem3,
                toolStripMenuItem4,
                toolStripMenuItem5,
                menuAIRemoveObject,
                toolStripMenuItem9
            });

            menuOpen.ShortcutKeys = Keys.Control | Keys.O;
            menuSave.ShortcutKeys = Keys.Control | Keys.S;
            menuSaveAs.ShortcutKeys = Keys.Control | Keys.Shift | Keys.S;
            menuZoomIn.ShortcutKeys = Keys.Control | Keys.Oemplus;
            menuZoomOut.ShortcutKeys = Keys.Control | Keys.OemMinus;
            menuExit.ShortcutKeys = Keys.Alt | Keys.F4;
            menuExit.Click -= MenuExit_Click;
            menuExit.Click += MenuExit_Click;

            menuStrip1.BackColor = UiSurface;
            menuStrip1.ForeColor = UiText;
            menuStrip1.Font = new Font("맑은 고딕", 9F);
            menuStrip1.Padding = Padding.Empty;
            foreach (ToolStripItem item in menuStrip1.Items)
            {
                item.Padding = new Padding(8, 7, 8, 7);
                item.Margin = Padding.Empty;
            }
            menuStrip1.Renderer = new EditorMenuRenderer();
            menuStrip1.ResumeLayout(true);
        }

        private void ConfigureQuickToolbar()
        {
            pnlQuickBar = new Panel
            {
                Dock = DockStyle.Fill,
                BackColor = UiAppBackground,
                Padding = new Padding(6, 5, 6, 5),
                Margin = Padding.Empty
            };

            FlowLayoutPanel buttons = new FlowLayoutPanel
            {
                Dock = DockStyle.Fill,
                FlowDirection = FlowDirection.LeftToRight,
                WrapContents = false,
                AutoScroll = true,
                BackColor = UiSurface,
                Padding = new Padding(8, 3, 8, 3),
                Margin = Padding.Empty
            };

            buttons.Controls.Add(CreateQuickButton("열기", () => menuOpen.PerformClick(), true, 64));
            btnQuickSave = CreateQuickButton("저장", () => menuSave.PerformClick(), false, 56);
            buttons.Controls.Add(btnQuickSave);
            buttons.Controls.Add(CreateQuickSeparator());

            btnQuickUndo = CreateQuickButton("취소", () => menuUndo.PerformClick(), false, 58);
            btnQuickRedo = CreateQuickButton("다시", () => menuRedo.PerformClick(), false, 58);
            buttons.Controls.Add(btnQuickUndo);
            buttons.Controls.Add(btnQuickRedo);
            buttons.Controls.Add(CreateQuickSeparator());

            btnToggleTimeline = CreateQuickButton("타임라인 열기", ToggleTimelinePanel, false, 100);
            buttons.Controls.Add(btnToggleTimeline);
            buttons.Controls.Add(CreateQuickSeparator());

            btnQuickFit = CreateQuickButton("화면 맞춤", () => menuFitToScreen.PerformClick(), false, 76);
            btnQuickBeforeAfter = CreateQuickButton("B/A 비교", () => menuBeforeAfter.PerformClick(), false, 72);
            buttons.Controls.Add(btnQuickFit);
            buttons.Controls.Add(btnQuickBeforeAfter);
            buttons.Controls.Add(CreateQuickSeparator());

            btnToggleWorkPanel = CreateQuickButton("레이어 숨김", ToggleWorkPanel, false, 88);
            btnToggleDetailPanel = CreateQuickButton("상세 숨김", ToggleDetailPanel, false, 88);
            buttons.Controls.Add(btnToggleWorkPanel);
            buttons.Controls.Add(btnToggleDetailPanel);
            pnlQuickBar.Controls.Add(buttons);
        }

        private ContextMenuStrip BuildQuickToolMenu()
        {
            ContextMenuStrip menu = new ContextMenuStrip
            {
                BackColor = Color.White,
                Font = new Font("맑은 고딕", 9F),
                Padding = new Padding(4),
                ShowImageMargin = false,
                ShowItemToolTips = true,
                Renderer = new ToolStripProfessionalRenderer(new EditorMenuColorTable())
            };

            ToolStripMenuItem imageGroup = new ToolStripMenuItem("이미지 편집");
            imageGroup.DropDownItems.Add(CreateActionMenuItem("크기 조절", () => menuResize.PerformClick()));
            imageGroup.DropDownItems.Add(CreateActionMenuItem("자르기", () => menuCrop.PerformClick()));
            imageGroup.DropDownItems.Add(CreateActionMenuItem("SNS 비율", () => menuSnsRatio.PerformClick()));
            imageGroup.DropDownItems.Add(new ToolStripSeparator());
            imageGroup.DropDownItems.Add(CreateActionMenuItem("왼쪽 90° 회전", () => menuRotateLeft.PerformClick()));
            imageGroup.DropDownItems.Add(CreateActionMenuItem("오른쪽 90° 회전", () => menuRotateRight.PerformClick()));

            ToolStripMenuItem colorGroup = new ToolStripMenuItem("보정·필터");
            colorGroup.DropDownItems.Add(CreateActionMenuItem("사진 보정", () => menuBrightness.PerformClick()));
            colorGroup.DropDownItems.Add(CreateActionMenuItem("기본 필터", () => menuBasicFilter.PerformClick()));
            colorGroup.DropDownItems.Add(CreateActionMenuItem("나만의 편집 스타일", () => menuMyStyle.PerformClick()));

            ToolStripMenuItem decorateGroup = new ToolStripMenuItem("꾸미기");
            decorateGroup.DropDownItems.Add(CreateActionMenuItem("템플릿", () => menuTemplate.PerformClick()));
            decorateGroup.DropDownItems.Add(CreateActionMenuItem("스티커", () => menuCommunityFilter.PerformClick()));
            decorateGroup.DropDownItems.Add(CreateActionMenuItem("텍스트 추가", () => menuAddText.PerformClick()));
            decorateGroup.DropDownItems.Add(CreateActionMenuItem("그리기", () => menuDrawingTool.PerformClick()));
            decorateGroup.DropDownItems.Add(CreateActionMenuItem("마스크", () => menuMask.PerformClick()));

            ToolStripMenuItem aiGroup = new ToolStripMenuItem("AI 기능");
            aiGroup.DropDownItems.Add(CreateActionMenuItem("AI 물체 제거", OpenAiRemovalMenu));
            aiGroup.DropDownItems.Add(CreateActionMenuItem("AI 배경 제거", () => menuAIBackgroundRemoval?.PerformClick()));
            aiGroup.DropDownItems.Add(CreateActionMenuItem("AI 오래된 사진 복원", () => menuAIPhotoRestoration?.PerformClick()));
            aiGroup.DropDownItems.Add(CreateActionMenuItem("AI 화질 개선", () => menuAIQualityEnhancement?.PerformClick()));
            aiGroup.DropDownItems.Add(CreateActionMenuItem("AI 인물 사진 자동보정", () => menuAIPortraitEnhance?.PerformClick()));
            aiGroup.DropDownItems.Add(CreateActionMenuItem("음악 추천", () => menuAIMusicRecommend.PerformClick()));
            aiGroup.DropDownItems.Add(new ToolStripSeparator());
            aiGroup.DropDownItems.Add(CreateActionMenuItem("유행어·밈 추천", () => menuAICaption.PerformClick()));

            menu.Items.Add(imageGroup);
            menu.Items.Add(colorGroup);
            menu.Items.Add(decorateGroup);
            menu.Items.Add(aiGroup);
            EditorToolTipService.ApplyMenuToolTips(menu.Items);
            return menu;
        }

        private static ToolStripMenuItem CreateActionMenuItem(string text, Action action)
        {
            ToolStripMenuItem item = new ToolStripMenuItem(text);
            item.Click += (_, _) => action();
            return item;
        }

        private static Control CreateQuickSeparator()
        {
            return new Label
            {
                Text = string.Empty,
                Width = 1,
                Height = 26,
                BackColor = Color.FromArgb(224, 228, 235),
                Margin = new Padding(5, 3, 5, 3)
            };
        }

        private static Button CreateQuickButton(
            string text,
            Action? action,
            bool primary,
            int width)
        {
            Button button = new Button
            {
                Text = text,
                Width = width,
                Height = 30,
                Margin = new Padding(2, 1, 2, 1),
                Padding = Padding.Empty,
                FlatStyle = FlatStyle.Flat,
                Font = new Font("맑은 고딕", 8.5F, FontStyle.Bold),
                BackColor = primary ? UiAccent : Color.White,
                ForeColor = primary ? Color.White : Color.FromArgb(61, 71, 88),
                Cursor = Cursors.Hand,
                UseVisualStyleBackColor = false
            };
            button.FlatAppearance.BorderSize = primary ? 0 : 1;
            button.FlatAppearance.BorderColor = Color.FromArgb(214, 220, 230);
            button.FlatAppearance.MouseOverBackColor = primary
                ? Color.FromArgb(52, 88, 221)
                : Color.FromArgb(244, 247, 255);
            if (action != null)
                button.Click += (_, _) => action();
            return button;
        }

        private void OpenAiRemovalMenu()
        {
            if (menuAIPointMode != null)
                menuAIPointMode.PerformClick();
            else
                toolStripMenuItem23.PerformClick();
        }

        private void ConfigureLeftPanel()
        {
            pnlLeft.Controls.Clear();
            pnlLeft.Dock = DockStyle.Fill;
            pnlLeft.BackColor = UiSurface;
            pnlLeft.Padding = Padding.Empty;

            // 최신 통합본은 tabLayer에 실제 레이어 도구를, tabTimeLine에 세션
            // 타임라인을 연결한다. 참고본처럼 Dispose하면 기능이 사라지므로,
            // 동일한 왼쪽 폭·배경 안에서 레이어 탭만 표시하도록 유지한다.
            tabControl1.SuspendLayout();
            tabControl1.Controls.Clear();
            tabControl1.Controls.Add(tabLayer);
            tabControl1.Dock = DockStyle.Fill;
            tabControl1.Appearance = TabAppearance.FlatButtons;
            tabControl1.ItemSize = new Size(0, 1);
            tabControl1.SizeMode = TabSizeMode.Fixed;
            tabControl1.SelectedTab = tabLayer;
            tabControl1.ResumeLayout(true);
            pnlLeft.Controls.Add(tabControl1);
        }

        private void ConfigureTimelineDrawer()
        {
            pnlTimelineDrawer = new Panel
            {
                Dock = DockStyle.Fill,
                BackColor = UiAppBackground,
                Padding = new Padding(0, 4, 0, 0),
                Visible = false
            };

            // 기존 TimeLinePage 인스턴스를 하단 서랍으로 옮긴다. 데이터와 이벤트는
            // 그대로 보존되며, 새 타임라인을 만들거나 DB 흐름을 바꾸지 않는다.
            while (tabTimeLine.Controls.Count > 0)
            {
                Control child = tabTimeLine.Controls[0];
                tabTimeLine.Controls.RemoveAt(0);
                child.Dock = DockStyle.Fill;
                child.BackColor = Color.White;
                pnlTimelineDrawer.Controls.Add(child);
            }
        }

        private void ConfigureCanvasPanel()
        {
            canvasResizeTimer = new System.Windows.Forms.Timer { Interval = 140 };
            canvasResizeTimer.Tick += (_, _) =>
            {
                canvasResizeTimer.Stop();
                if (!autoFitCanvas || IsTemporaryCanvasPreviewActive ||
                    IsDisposed || editor?.State.CurrentImage == null)
                    return;
                editor.FitToPanel(GetCanvasViewportSize());
                RefreshCanvas();
            };

            pnlCanvas.Dock = DockStyle.Fill;
            pnlCanvas.BackColor = UiWorkspace;
            pnlCanvas.BorderStyle = BorderStyle.None;
            pnlCanvas.Padding = new Padding(26);
            pnlCanvas.AutoScroll = true;
            pnlCanvas.Resize -= PnlCanvas_Resize;
            pnlCanvas.Resize += PnlCanvas_Resize;
            pnlCanvas.Paint -= PnlCanvas_Paint;
            pnlCanvas.Paint += PnlCanvas_Paint;

            picCanvas.Anchor = AnchorStyles.Top | AnchorStyles.Left;
            picCanvas.BackColor = Color.White;
            picCanvas.BorderStyle = BorderStyle.None;

            pnlCanvasEmpty = new Panel
            {
                Size = new Size(340, 188),
                BackColor = UiWorkspace
            };
            Label plus = new Label
            {
                Text = "+",
                Location = new Point(145, 0),
                Size = new Size(50, 44),
                TextAlign = ContentAlignment.MiddleCenter,
                Font = new Font("Segoe UI", 24F),
                ForeColor = UiAccent
            };
            Label emptyTitle = new Label
            {
                Text = "편집할 이미지를 열어주세요",
                Location = new Point(20, 48),
                Size = new Size(300, 28),
                TextAlign = ContentAlignment.MiddleCenter,
                Font = new Font("맑은 고딕", 11F, FontStyle.Bold),
                ForeColor = UiText
            };
            lblCanvasGuide = new Label
            {
                Text = "JPG, JPEG, PNG, BMP\r\nCtrl + O로도 빠르게 불러올 수 있습니다.",
                Location = new Point(20, 80),
                Size = new Size(300, 42),
                TextAlign = ContentAlignment.MiddleCenter,
                Font = new Font("맑은 고딕", 8.5F),
                ForeColor = UiMuted,
                BackColor = UiWorkspace
            };
            Button openButton = CreateQuickButton("이미지 선택", () => menuOpen.PerformClick(), true, 126);
            openButton.Location = new Point(107, 136);
            openButton.Height = 38;
            pnlCanvasEmpty.Controls.Add(openButton);
            pnlCanvasEmpty.Controls.Add(lblCanvasGuide);
            pnlCanvasEmpty.Controls.Add(emptyTitle);
            pnlCanvasEmpty.Controls.Add(plus);
            pnlCanvas.Controls.Add(pnlCanvasEmpty);
            CenterCanvasGuide();
        }

        private void PnlCanvas_Paint(object? sender, PaintEventArgs e)
        {
            if (picCanvas.Image == null || !picCanvas.Visible)
                return;

            Rectangle shadow = picCanvas.Bounds;
            shadow.Offset(5, 5);
            using SolidBrush shadowBrush = new SolidBrush(Color.FromArgb(24, 38, 48, 72));
            e.Graphics.FillRectangle(shadowBrush, shadow);

            Rectangle border = picCanvas.Bounds;
            border.Inflate(1, 1);
            using Pen borderPen = new Pen(Color.FromArgb(174, 181, 192));
            e.Graphics.DrawRectangle(borderPen, border);
        }

        private void ConfigureToolOptionsPanel()
        {
            pnlToolOptions.Dock = DockStyle.Fill;
            pnlToolOptions.BorderStyle = BorderStyle.None;
            PrepareToolOptionsHost();
            ShowToolPlaceholder();
        }

        private void PrepareToolOptionsHost()
        {
            pnlToolOptions.BackColor = UiSurface;
            pnlToolOptions.Padding = new Padding(10);
            pnlToolOptions.AutoScroll = true;
        }

        private void ShowToolPlaceholder()
        {
            PrepareToolOptionsHost();
            pnlToolOptions.Controls.Clear();

            if (toolPlaceholderPanel == null)
            {
                Panel empty = new Panel
                {
                    Dock = DockStyle.Fill,
                    BackColor = UiSurface,
                    Padding = new Padding(10)
                };
                TableLayoutPanel layout = new TableLayoutPanel
                {
                    Dock = DockStyle.Fill,
                    ColumnCount = 1,
                    RowCount = 8,
                    BackColor = UiSurface,
                    Margin = Padding.Empty,
                    Padding = Padding.Empty
                };
                layout.RowStyles.Add(new RowStyle(SizeType.Absolute, 34F));
                layout.RowStyles.Add(new RowStyle(SizeType.Absolute, 52F));
                layout.RowStyles.Add(new RowStyle(SizeType.Absolute, 14F));
                layout.RowStyles.Add(new RowStyle(SizeType.Absolute, 88F));
                layout.RowStyles.Add(new RowStyle(SizeType.Absolute, 12F));
                layout.RowStyles.Add(new RowStyle(SizeType.Absolute, 216F));
                layout.RowStyles.Add(new RowStyle(SizeType.Percent, 100F));
                layout.RowStyles.Add(new RowStyle(SizeType.Absolute, 42F));

                Label title = new Label
                {
                    Text = "상세 설정",
                    Dock = DockStyle.Fill,
                    Font = new Font("맑은 고딕", 14F, FontStyle.Bold),
                    ForeColor = UiText,
                    TextAlign = ContentAlignment.MiddleLeft
                };
                Label description = new Label
                {
                    Text = "편집 도구를 선택하면 필요한 옵션이\r\n이 영역에 표시됩니다.",
                    Dock = DockStyle.Fill,
                    Font = new Font("맑은 고딕", 8.5F),
                    ForeColor = UiMuted,
                    TextAlign = ContentAlignment.TopLeft
                };

                Panel stateCard = new Panel
                {
                    Dock = DockStyle.Fill,
                    BackColor = UiAccentSoft,
                    Padding = new Padding(14, 12, 12, 10),
                    Margin = Padding.Empty
                };
                Label stateTitle = new Label
                {
                    Text = "도구를 선택해 편집을 시작하세요",
                    Dock = DockStyle.Top,
                    Height = 28,
                    Font = new Font("맑은 고딕", 9F, FontStyle.Bold),
                    ForeColor = Color.FromArgb(61, 82, 185)
                };
                Label stateDescription = new Label
                {
                    Text = "상단 메뉴에서 원하는 기능을 선택할 수 있습니다.",
                    Dock = DockStyle.Fill,
                    Font = new Font("맑은 고딕", 8.25F),
                    ForeColor = Color.FromArgb(80, 96, 160),
                    TextAlign = ContentAlignment.TopLeft
                };
                stateCard.Controls.Add(stateDescription);
                stateCard.Controls.Add(stateTitle);

                Panel guideCard = new Panel
                {
                    Dock = DockStyle.Fill,
                    BackColor = Color.FromArgb(248, 249, 252),
                    Padding = new Padding(14, 12, 14, 10),
                    Margin = Padding.Empty
                };
                TableLayoutPanel guideLayout = new TableLayoutPanel
                {
                    Dock = DockStyle.Fill,
                    ColumnCount = 1,
                    RowCount = 4,
                    BackColor = Color.Transparent,
                    Margin = Padding.Empty,
                    Padding = Padding.Empty
                };
                guideLayout.RowStyles.Add(new RowStyle(SizeType.Absolute, 32F));
                guideLayout.RowStyles.Add(new RowStyle(SizeType.Percent, 33.33F));
                guideLayout.RowStyles.Add(new RowStyle(SizeType.Percent, 33.33F));
                guideLayout.RowStyles.Add(new RowStyle(SizeType.Percent, 33.34F));
                guideLayout.Controls.Add(new Label
                {
                    Text = "빠른 시작",
                    Dock = DockStyle.Fill,
                    Font = new Font("맑은 고딕", 9F, FontStyle.Bold),
                    ForeColor = UiText,
                    TextAlign = ContentAlignment.MiddleLeft
                }, 0, 0);
                guideLayout.Controls.Add(CreatePlaceholderStep(
                    "1", "이미지 열기", "편집할 사진을 불러옵니다."), 0, 1);
                guideLayout.Controls.Add(CreatePlaceholderStep(
                    "2", "도구 선택", "보정·필터·텍스트 기능을 선택합니다."), 0, 2);
                guideLayout.Controls.Add(CreatePlaceholderStep(
                    "3", "결과 적용", "미리보기를 확인한 뒤 적용합니다."), 0, 3);
                guideCard.Controls.Add(guideLayout);

                Label footer = new Label
                {
                    Text = "선택한 기능의 옵션만 표시되어 편집 화면을 방해하지 않습니다.",
                    Dock = DockStyle.Fill,
                    Font = new Font("맑은 고딕", 8F),
                    ForeColor = UiMuted,
                    TextAlign = ContentAlignment.BottomLeft
                };

                layout.Controls.Add(title, 0, 0);
                layout.Controls.Add(description, 0, 1);
                layout.Controls.Add(stateCard, 0, 3);
                layout.Controls.Add(guideCard, 0, 5);
                layout.Controls.Add(footer, 0, 7);
                empty.Controls.Add(layout);
                toolPlaceholderPanel = empty;
                lblToolPlaceholderStateTitle = stateTitle;
                lblToolPlaceholderStateDescription = stateDescription;
                lblToolPlaceholderFooter = footer;
            }

            UpdateToolPlaceholderAccountState();
            pnlToolOptions.Controls.Add(toolPlaceholderPanel);
        }

        private void UpdateToolPlaceholderAccountState()
        {
            if (lblToolPlaceholderStateTitle == null ||
                lblToolPlaceholderStateDescription == null ||
                lblToolPlaceholderFooter == null)
            {
                return;
            }

            if (CurrentUser.UserNo <= 0)
            {
                lblToolPlaceholderStateTitle.Text = "비회원으로 편집 중입니다";
                lblToolPlaceholderStateDescription.Text =
                    "일반 편집 기능은 사용할 수 있습니다.\r\n" +
                    "AI·개인 스타일·타임라인은 로그인 후 사용할 수 있습니다.";
                lblToolPlaceholderFooter.Text =
                    "오른쪽 위의 ‘로그인’을 선택하면 AI 기능이 활성화됩니다.";
                return;
            }

            lblToolPlaceholderStateTitle.Text = "도구를 선택해 편집을 시작하세요";
            lblToolPlaceholderStateDescription.Text =
                "상단 메뉴에서 원하는 기능을 선택할 수 있습니다.";
            lblToolPlaceholderFooter.Text =
                "선택한 기능의 옵션만 표시되어 편집 화면을 방해하지 않습니다.";
        }

        private static Control CreatePlaceholderStep(
            string number,
            string title,
            string description)
        {
            TableLayoutPanel row = new TableLayoutPanel
            {
                Dock = DockStyle.Fill,
                ColumnCount = 2,
                RowCount = 1,
                BackColor = Color.Transparent,
                Margin = Padding.Empty,
                Padding = Padding.Empty
            };
            row.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 34F));
            row.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100F));

            Label numberLabel = new Label
            {
                Text = number,
                Dock = DockStyle.Fill,
                Margin = new Padding(0, 5, 8, 5),
                BackColor = UiAccentSoft,
                ForeColor = UiAccent,
                Font = new Font("맑은 고딕", 8.5F, FontStyle.Bold),
                TextAlign = ContentAlignment.MiddleCenter
            };
            Label text = new Label
            {
                Text = title + "\r\n" + description,
                Dock = DockStyle.Fill,
                Margin = Padding.Empty,
                ForeColor = UiText,
                Font = new Font("맑은 고딕", 8.25F),
                TextAlign = ContentAlignment.MiddleLeft
            };
            row.Controls.Add(numberLabel, 0, 0);
            row.Controls.Add(text, 1, 0);
            return row;
        }

        private void ShowComingSoonPanel(string titleText, string descriptionText)
        {
            EnsureDetailPanelVisible();
            PrepareToolOptionsHost();
            if (comingSoonPanel != null)
            {
                pnlToolOptions.Controls.Remove(comingSoonPanel);
                comingSoonPanel.Dispose();
                comingSoonPanel = null;
            }
            pnlToolOptions.Controls.Clear();

            UserControl panel = new UserControl
            {
                Dock = DockStyle.Fill,
                BackColor = UiSurface,
                Padding = new Padding(14)
            };
            Label title = new Label
            {
                Text = titleText,
                Dock = DockStyle.Top,
                Height = 38,
                Font = new Font("맑은 고딕", 14F, FontStyle.Bold),
                ForeColor = UiText
            };
            Label badge = new Label
            {
                Text = "AI · COMING SOON",
                Dock = DockStyle.Top,
                Height = 30,
                TextAlign = ContentAlignment.MiddleLeft,
                Font = new Font("Segoe UI", 8F, FontStyle.Bold),
                ForeColor = UiAccent
            };
            Panel card = new Panel
            {
                Dock = DockStyle.Top,
                Height = 150,
                BackColor = UiAccentSoft,
                Padding = new Padding(16)
            };
            Label description = new Label
            {
                Text = descriptionText + "\r\n\r\n현재는 UI 자리와 메뉴만 준비되어 있으며,\r\n추후 AI 로직을 연결할 예정입니다.",
                Dock = DockStyle.Fill,
                Font = new Font("맑은 고딕", 8.5F),
                ForeColor = Color.FromArgb(70, 84, 148)
            };
            card.Controls.Add(description);
            panel.Controls.Add(card);
            panel.Controls.Add(badge);
            panel.Controls.Add(title);
            comingSoonPanel = panel;
            pnlToolOptions.Controls.Add(panel);
        }

        private void ConfigureStatusBar()
        {
            pnlBottom.Controls.Clear();
            pnlBottom.Dock = DockStyle.Fill;
            pnlBottom.BackColor = UiSurface;
            pnlBottom.Margin = new Padding(0, 1, 0, 0);
            pnlBottom.Padding = new Padding(14, 0, 14, 0);

            lblStatusMessage = CreateStatusLabel("●  준비됨", ContentAlignment.MiddleLeft);
            lblStatusMessage.ForeColor = Color.FromArgb(29, 157, 92);
            lblStatusFileName = CreateStatusLabel("파일 · 이미지 없음", ContentAlignment.MiddleLeft);
            lblStatusImageSize = CreateStatusLabel("-", ContentAlignment.MiddleCenter);
            lblStatusZoom = CreateStatusLabel("-", ContentAlignment.MiddleRight);

            TableLayoutPanel statusLayout = new TableLayoutPanel
            {
                Dock = DockStyle.Fill,
                ColumnCount = 4,
                RowCount = 1,
                BackColor = UiSurface,
                Margin = Padding.Empty
            };
            statusLayout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 24F));
            statusLayout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 42F));
            statusLayout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 20F));
            statusLayout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 14F));
            statusLayout.Controls.Add(lblStatusMessage, 0, 0);
            statusLayout.Controls.Add(lblStatusFileName, 1, 0);
            statusLayout.Controls.Add(lblStatusImageSize, 2, 0);
            statusLayout.Controls.Add(lblStatusZoom, 3, 0);
            pnlBottom.Controls.Add(statusLayout);
        }

        private static Label CreateStatusLabel(string text, ContentAlignment alignment)
        {
            return new Label
            {
                Text = text,
                Dock = DockStyle.Fill,
                TextAlign = alignment,
                AutoEllipsis = true,
                Font = new Font("맑은 고딕", 8F),
                ForeColor = Color.FromArgb(102, 112, 129),
                Margin = Padding.Empty
            };
        }

        private void UpdateEditorStatus(
            string? message = null,
            Bitmap? displayedImage = null,
            float? displayedZoom = null,
            bool updateCommands = true)
        {
            Bitmap? image = displayedImage ?? editor?.State.CurrentImage;
            if (lblStatusMessage != null)
            {
                if (message == null)
                {
                    lblStatusMessage.Text = "●  준비됨";
                    lblStatusMessage.ForeColor = Color.FromArgb(29, 157, 92);
                }
                else if (message.Contains("준비 중"))
                {
                    lblStatusMessage.Text = $"○  예정 · {message}";
                    lblStatusMessage.ForeColor = UiAccent;
                }
                else if (message.Contains("중..."))
                {
                    lblStatusMessage.Text = $"●  작업 중 · {message}";
                    lblStatusMessage.ForeColor = Color.FromArgb(219, 139, 32);
                }
                else if (message.Contains("실패"))
                {
                    lblStatusMessage.Text = $"●  확인 필요 · {message}";
                    lblStatusMessage.ForeColor = Color.FromArgb(194, 65, 65);
                }
                else
                {
                    lblStatusMessage.Text = $"●  완료 · {message}";
                    lblStatusMessage.ForeColor = Color.FromArgb(29, 157, 92);
                }
            }

            if (lblStatusFileName != null)
            {
                string fileName = image == null
                    ? "이미지 없음"
                    : string.IsNullOrWhiteSpace(editor.State.CurrentFilePath)
                        ? "이름 없는 이미지"
                        : Path.GetFileName(editor.State.CurrentFilePath);
                lblStatusFileName.Text = "파일 · " + fileName;
            }

            if (lblStatusImageSize != null)
                lblStatusImageSize.Text = image == null
                    ? "-"
                    : $"{image.Width:N0} × {image.Height:N0} px";

            if (lblStatusZoom != null)
                lblStatusZoom.Text = image == null
                    ? "-"
                    : $"확대 {(displayedZoom ?? editor.State.ZoomRatio) * 100:0}%";

            if (pnlCanvasEmpty != null)
            {
                pnlCanvasEmpty.Visible = image == null;
                if (image == null)
                    pnlCanvasEmpty.BringToFront();
            }

            if (updateCommands)
                UpdateCommandAvailability(image != null);
        }

        private void UpdateCommandAvailability(bool hasImage)
        {
            menuSave.Enabled = hasImage;
            menuSaveAs.Enabled = hasImage;
            menuExport.Enabled = hasImage;
            toolStripMenuItem2.Enabled = hasImage;
            toolStripMenuItem3.Enabled = hasImage;
            toolStripMenuItem4.Enabled = hasImage;
            toolStripMenuItem5.Enabled = hasImage;
            UpdateAiFeatureAvailability(hasImage);
            UpdateMyEditStyleAvailability(hasImage);
            UpdateTimelineAvailability();
            toolStripMenuItem9.Enabled = hasImage;

            if (btnQuickSave != null)
                btnQuickSave.Enabled = hasImage;
            if (btnQuickFit != null)
                btnQuickFit.Enabled = hasImage;
            if (btnQuickBeforeAfter != null)
                btnQuickBeforeAfter.Enabled = hasImage;

            Button?[] layerButtons =
            {
                btnAddLayer,
                btnAddImageLayer,
                btnDeleteLayer,
                btnDuplicateLayer,
                btnLayerUp,
                btnLayerDown,
                btnLayerVisible,
                btnLayerLock,
                btnRenameLayer
            };
            foreach (Button? button in layerButtons)
            {
                if (button != null)
                    button.Enabled = hasImage;
            }

            if (layerOpacityBar != null)
                layerOpacityBar.Enabled = hasImage;

            UpdateUndoRedoMenuState();
        }

        private void ToggleWorkPanel()
        {
            if (workspaceLayout == null)
                return;

            workPanelVisible = !workPanelVisible;
            pnlLeft.Visible = workPanelVisible;
            workspaceLayout.ColumnStyles[0].Width = workPanelVisible ? WorkPanelWidth : 0F;
            if (btnToggleWorkPanel != null)
                btnToggleWorkPanel.Text = workPanelVisible ? "레이어 숨김" : "레이어 표시";
            workspaceLayout.PerformLayout();
            FitCanvasAfterPanelToggle();
        }

        private void ToggleTimelinePanel()
        {
            if (CurrentUser.UserNo <= 0)
            {
                MessageBox.Show(
                    "편집 타임라인은 로그인 후 사용할 수 있습니다.",
                    "로그인 필요",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Information);
                return;
            }

            if (centerLayout == null || pnlTimelineDrawer == null)
                return;

            timelineVisible = !timelineVisible;
            pnlTimelineDrawer.Visible = timelineVisible;
            centerLayout.RowStyles[1].Height = timelineVisible ? TimelineHeight : 0F;
            if (btnToggleTimeline != null)
            {
                btnToggleTimeline.Text = timelineVisible ? "타임라인 닫기" : "타임라인 열기";
                btnToggleTimeline.BackColor = timelineVisible ? UiAccentSoft : Color.White;
                btnToggleTimeline.ForeColor = timelineVisible ? UiAccent : Color.FromArgb(61, 71, 88);
            }
            centerLayout.PerformLayout();
            FitCanvasAfterPanelToggle();
        }

        private void OpenTimelineDrawer()
        {
            if (CurrentUser.UserNo <= 0)
                return;

            if (!timelineVisible)
                ToggleTimelinePanel();
        }

        private void UpdateTimelineAvailability()
        {
            bool loggedIn = CurrentUser.UserNo > 0;
            if (btnToggleTimeline != null)
            {
                btnToggleTimeline.Enabled = loggedIn;
                btnToggleTimeline.AccessibleDescription = loggedIn
                    ? "현재 계정의 편집 타임라인을 열거나 닫습니다."
                    : "타임라인은 로그인 이후에 사용 가능합니다.";
                if (editorToolTip != null)
                {
                    editorToolTip.SetToolTip(
                        btnToggleTimeline,
                        loggedIn
                            ? "현재 계정의 편집 타임라인을 열거나 닫습니다."
                            : "타임라인은 로그인 이후에 사용 가능합니다.");
                }
            }

            if (loggedIn || !timelineVisible ||
                centerLayout == null || pnlTimelineDrawer == null)
            {
                return;
            }

            timelineVisible = false;
            pnlTimelineDrawer.Visible = false;
            centerLayout.RowStyles[1].Height = 0F;
            if (btnToggleTimeline != null)
            {
                btnToggleTimeline.Text = "타임라인 열기";
                btnToggleTimeline.BackColor = Color.White;
                btnToggleTimeline.ForeColor = Color.FromArgb(61, 71, 88);
            }
            centerLayout.PerformLayout();
            FitCanvasAfterPanelToggle();
        }

        private void ToggleDetailPanel()
        {
            if (workspaceLayout == null)
                return;

            detailPanelVisible = !detailPanelVisible;
            pnlToolOptions.Visible = detailPanelVisible;
            workspaceLayout.ColumnStyles[2].Width = detailPanelVisible ? DetailPanelWidth : 0F;
            if (btnToggleDetailPanel != null)
                btnToggleDetailPanel.Text = detailPanelVisible ? "상세 숨김" : "상세 표시";
            workspaceLayout.PerformLayout();
            FitCanvasAfterPanelToggle();
        }

        private void EnsureDetailPanelVisible()
        {
            if (!detailPanelVisible)
                ToggleDetailPanel();
        }

        private void FitCanvasAfterPanelToggle()
        {
            if (IsTemporaryCanvasPreviewActive)
            {
                autoFitCanvas = false;
                canvasResizeTimer?.Stop();
                return;
            }

            autoFitCanvas = true;
            BeginInvoke(new Action(() =>
            {
                if (IsDisposed || editor?.State.CurrentImage == null)
                    return;
                if (IsTemporaryCanvasPreviewActive)
                {
                    autoFitCanvas = false;
                    canvasResizeTimer?.Stop();
                    return;
                }
                canvasResizeTimer?.Stop();
                editor.FitToPanel(GetCanvasViewportSize());
                RefreshCanvas();
            }));
        }

        private void PnlCanvas_Resize(object? sender, EventArgs e)
        {
            CenterCanvasGuide();
            CenterCanvasImage();
            if (autoFitCanvas && editor?.State.CurrentImage != null)
            {
                canvasResizeTimer?.Stop();
                canvasResizeTimer?.Start();
            }
        }

        private void CenterCanvasGuide()
        {
            if (pnlCanvasEmpty == null)
                return;

            pnlCanvasEmpty.Location = new Point(
                Math.Max(0, (pnlCanvas.ClientSize.Width - pnlCanvasEmpty.Width) / 2),
                Math.Max(0, (pnlCanvas.ClientSize.Height - pnlCanvasEmpty.Height) / 2));
        }

        private void CenterCanvasImage()
        {
            if (picCanvas.Image == null)
                return;

            int viewportWidth = Math.Max(0, pnlCanvas.ClientSize.Width - pnlCanvas.Padding.Horizontal);
            int viewportHeight = Math.Max(0, pnlCanvas.ClientSize.Height - pnlCanvas.Padding.Vertical);
            int x = picCanvas.Width < viewportWidth
                ? pnlCanvas.Padding.Left + (viewportWidth - picCanvas.Width) / 2
                : pnlCanvas.Padding.Left;
            int y = picCanvas.Height < viewportHeight
                ? pnlCanvas.Padding.Top + (viewportHeight - picCanvas.Height) / 2
                : pnlCanvas.Padding.Top;
            picCanvas.Location = new Point(x, y);
            pnlCanvas.Invalidate();
        }

        private sealed class EditorMenuRenderer : ToolStripProfessionalRenderer
        {
            public EditorMenuRenderer()
                : base(new EditorMenuColorTable())
            {
                RoundedEdges = false;
            }

            protected override void OnRenderMenuItemBackground(ToolStripItemRenderEventArgs e)
            {
                if (e.ToolStrip is not MenuStrip)
                {
                    base.OnRenderMenuItemBackground(e);
                    return;
                }

                e.Graphics.FillRectangle(Brushes.White, e.Item.ContentRectangle);
                if (!e.Item.Selected && !e.Item.Pressed)
                    return;

                Rectangle background = new Rectangle(
                    3,
                    5,
                    Math.Max(1, e.Item.Width - 6),
                    Math.Max(1, e.Item.Height - 10));
                using SolidBrush soft = new SolidBrush(Color.FromArgb(241, 244, 255));
                e.Graphics.FillRectangle(soft, background);

                using SolidBrush accent = new SolidBrush(UiAccent);
                e.Graphics.FillRectangle(
                    accent,
                    new Rectangle(8, e.Item.Height - 3, Math.Max(1, e.Item.Width - 16), 2));
            }

            protected override void OnRenderToolStripBorder(ToolStripRenderEventArgs e)
            {
                if (e.ToolStrip is MenuStrip)
                    return;
                base.OnRenderToolStripBorder(e);
            }
        }

        private sealed class EditorMenuColorTable : ProfessionalColorTable
        {
            public override Color MenuItemSelected => Color.FromArgb(238, 242, 255);
            public override Color MenuItemBorder => Color.FromArgb(184, 196, 245);
            public override Color MenuItemSelectedGradientBegin => MenuItemSelected;
            public override Color MenuItemSelectedGradientEnd => MenuItemSelected;
            public override Color MenuItemPressedGradientBegin => Color.FromArgb(230, 235, 252);
            public override Color MenuItemPressedGradientEnd => Color.FromArgb(230, 235, 252);
            public override Color ToolStripDropDownBackground => Color.White;
            public override Color ImageMarginGradientBegin => Color.White;
            public override Color ImageMarginGradientMiddle => Color.White;
            public override Color ImageMarginGradientEnd => Color.White;
            public override Color SeparatorDark => Color.FromArgb(225, 229, 236);
            public override Color SeparatorLight => Color.FromArgb(245, 246, 249);
        }
    }
}
