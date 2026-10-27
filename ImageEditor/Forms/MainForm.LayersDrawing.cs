using ImageEditor.Core;
using System;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.IO;
using System.Windows.Forms;

namespace ImageEditor
{
    public partial class MainForm : Form
    {
        private enum ToolMode
        {
            None,
            Drawing
        }

        private ToolMode currentTool = ToolMode.None;


        // =====================================================
        // 그리기
        // =====================================================

        private bool isDrawing = false;
        private Point lastPoint;

        private Color drawingColor = Color.Black;
        private float drawingWidth = 5f;
        private float drawingOpacity = 1f;

        private bool eraserMode = false;


        // =====================================================
        // 그리기 UI
        // =====================================================

        private Button? btnPen;
        private Button? btnEraser;
        private Button? btnColor;

        private Label? colorLabel;
        private Label? sizeValueLabel;
        private Label? opacityValueLabel;

        private TrackBar? brushSizeBar;
        private TrackBar? brushOpacityBar;

        private Label? drawingSectionLabel;
        private Label? drawingColorValueLabel;
        private Button? btnClearDrawingLayer;
        private Button? btnQuickSize2;
        private Button? btnQuickSize5;
        private Button? btnQuickSize10;
        private Button? btnQuickSize20;
        private Button? btnQuickSize40;
        private Button? btnQuickSize80;
        private Point brushCursorPoint;
        private bool showBrushCursor = false;
        private readonly System.Windows.Forms.Timer drawingRenderTimer =
            new System.Windows.Forms.Timer { Interval = 33 };
        private bool drawingRenderQueued;


        // =====================================================
        // 레이어 UI
        // =====================================================

        private Panel? layerListPanel;

        private Button? btnAddLayer;
        private Button? btnAddImageLayer;
        private Button? btnDeleteLayer;
        private Button? btnDuplicateLayer;

        private Button? btnLayerUp;
        private Button? btnLayerDown;

        private Button? btnLayerVisible;
        private Button? btnLayerLock;

        private Button? btnRenameLayer;

        private TrackBar? layerOpacityBar;
        private Label? layerOpacityLabel;
        private Label? layerOpacityTitle;
        private bool updatingLayerOpacityUi;
        private bool layerOpacityEditActive;
        private bool layerOpacitySnapshotSaved;
        private int layerOpacityStartValue;
        private readonly System.Windows.Forms.Timer layerOpacityCommitTimer =
            new System.Windows.Forms.Timer { Interval = 250 };



        private void InitializeLayerDrawingFeatures()
        {
            InitializeCanvasEvents();
            picCanvas.SizeMode = PictureBoxSizeMode.StretchImage;
            drawingRenderTimer.Tick += DrawingRenderTimer_Tick;
            layerOpacityCommitTimer.Tick += LayerOpacityCommitTimer_Tick;

            menuDrawingTool.Click -= menuDrawingTool_Click;
            menuDrawingTool.Click += menuDrawingTool_Click;
            InitializeLayerUI();
        }

        private void StopLayerDrawingTools()
        {
            FlushDrawingRender();
            currentTool = ToolMode.None;
            StopDrawing();
        }

        private void QueueDrawingRender()
        {
            drawingRenderQueued = true;
            if (!drawingRenderTimer.Enabled)
                drawingRenderTimer.Start();
        }

        private void DrawingRenderTimer_Tick(object? sender, EventArgs e)
        {
            FlushDrawingRender();
        }

        private void FlushDrawingRender()
        {
            if (!drawingRenderQueued)
            {
                drawingRenderTimer.Stop();
                return;
            }

            drawingRenderQueued = false;
            drawingRenderTimer.Stop();
            editor.RenderLayers();
            RefreshCanvas();
        }
        private void menuDrawingTool_Click(
            object? sender,
            EventArgs e)
        {
            FinishAdjustmentFilterSessions(false);
            StopCommunitySession(false);
            CancelTemplateSession();
            StopMaskTool();
            CloseTextTool();
            cropTool?.Stop();
            aiObjectRemovalTool?.Stop();
            aiBackgroundRemovalTool?.Stop();
            currentTool =
                ToolMode.Drawing;

            StopDrawing();

            ShowDrawingPanel();

            pnlToolOptions.Visible =
                true;

            pnlToolOptions.BringToFront();
        }

        private void ClearToolPanel()
        {
            /*
             * resizeControl을 Dispose하면
             * 다시 사용할 수 없으므로
             * Controls.Clear()만 사용.
             */

            pnlToolOptions.Controls.Clear();
        }

        private void InitializeLayerUI()
        {
            if (tabLayer == null)
                return;

            tabLayer.SuspendLayout();
            tabLayer.Controls.Clear();
            tabLayer.BackColor = Color.FromArgb(245, 247, 250);

            // -------------------------------------------------
            // 상단 제목
            // -------------------------------------------------
            Label title = CreateLabel("레이어", 10, 8, 13, true);
            title.ForeColor = Color.FromArgb(35, 45, 60);
            tabLayer.Controls.Add(title);

            Label hint = CreateLabel("위쪽이 앞 / 아래쪽이 배경", 10, 31, 8.5f);
            hint.ForeColor = Color.FromArgb(110, 120, 135);
            tabLayer.Controls.Add(hint);

            // -------------------------------------------------
            // 레이어 목록
            // -------------------------------------------------
            layerListPanel = new Panel
            {
                Location = new Point(6, 58),
                Size = new Size(194, 190),
                BorderStyle = BorderStyle.FixedSingle,
                AutoScroll = true,
                BackColor = Color.White,
                Padding = new Padding(2)
            };
            tabLayer.Controls.Add(layerListPanel);

            // -------------------------------------------------
            // 1행 : 추가 / 이미지
            // -------------------------------------------------
            btnAddLayer = CreateLayerButton("+ 빈 레이어", 6, 254, 94);
            btnAddLayer.Click += BtnAddLayer_Click;
            tabLayer.Controls.Add(btnAddLayer);

            btnAddImageLayer = CreateLayerButton("+ 이미지", 104, 254, 96);
            btnAddImageLayer.Click += BtnAddImageLayer_Click;
            tabLayer.Controls.Add(btnAddImageLayer);

            // -------------------------------------------------
            // 2행 : 삭제 / 복제
            // -------------------------------------------------
            btnDeleteLayer = CreateLayerButton("삭제", 6, 291, 94);
            btnDeleteLayer.Click += BtnDeleteLayer_Click;
            tabLayer.Controls.Add(btnDeleteLayer);

            btnDuplicateLayer = CreateLayerButton("복제", 104, 291, 96);
            btnDuplicateLayer.Click += BtnDuplicateLayer_Click;
            tabLayer.Controls.Add(btnDuplicateLayer);

            // -------------------------------------------------
            // 3행 : 위 / 아래
            // -------------------------------------------------
            btnLayerUp = CreateLayerButton("▲  위로", 6, 328, 94);
            btnLayerUp.Click += BtnLayerUp_Click;
            tabLayer.Controls.Add(btnLayerUp);

            btnLayerDown = CreateLayerButton("▼  아래로", 104, 328, 96);
            btnLayerDown.Click += BtnLayerDown_Click;
            tabLayer.Controls.Add(btnLayerDown);

            // -------------------------------------------------
            // 4행 : 표시 / 잠금
            // -------------------------------------------------
            btnLayerVisible = CreateLayerButton("표시", 6, 365, 94);
            btnLayerVisible.Click += BtnLayerVisible_Click;
            tabLayer.Controls.Add(btnLayerVisible);

            btnLayerLock = CreateLayerButton("잠금", 104, 365, 96);
            btnLayerLock.Click += BtnLayerLock_Click;
            tabLayer.Controls.Add(btnLayerLock);

            // -------------------------------------------------
            // 이름 변경
            // -------------------------------------------------
            btnRenameLayer = CreateLayerButton("✎  레이어 이름 변경", 6, 402, 194);
            btnRenameLayer.Click += BtnRenameLayer_Click;
            tabLayer.Controls.Add(btnRenameLayer);

            // -------------------------------------------------
            // 불투명도
            // -------------------------------------------------
            layerOpacityTitle = CreateLabel("불투명도", 8, 442, 9, true);
            layerOpacityTitle.ForeColor = Color.FromArgb(55, 65, 80);
            tabLayer.Controls.Add(layerOpacityTitle);

            layerOpacityBar = new TrackBar
            {
                Minimum = 0,
                Maximum = 100,
                Value = 100,
                Location = new Point(5, 452),
                Size = new Size(155, 30),
                TickFrequency = 10,
                AutoSize = false
            };
            layerOpacityBar.ValueChanged += LayerOpacityBar_ValueChanged;
            layerOpacityBar.MouseDown += LayerOpacityBar_MouseDown;
            layerOpacityBar.MouseUp += LayerOpacityBar_MouseUp;
            layerOpacityBar.KeyDown += LayerOpacityBar_KeyDown;
            layerOpacityBar.KeyUp += LayerOpacityBar_KeyUp;
            layerOpacityBar.LostFocus += (_, _) => EndLayerOpacityEdit();
            tabLayer.Controls.Add(layerOpacityBar);

            layerOpacityLabel = CreateLabel("100%", 165, 455, 9, true);
            layerOpacityLabel.ForeColor = Color.FromArgb(55, 65, 80);
            tabLayer.Controls.Add(layerOpacityLabel);

            tabLayer.Resize -= TabLayer_Resize;
            tabLayer.Resize += TabLayer_Resize;
            layerListPanel.Resize += (_, _) => LayoutLayerRows();
            LayoutLayerPanelControls();
            tabLayer.ResumeLayout(true);
            RefreshLayerUI();
        }

        private void TabLayer_Resize(object? sender, EventArgs e)
        {
            LayoutLayerPanelControls();
            LayoutLayerRows();
        }

        private void LayoutLayerPanelControls()
        {
            if (layerListPanel == null || tabLayer.ClientSize.Width <= 0 ||
                tabLayer.ClientSize.Height <= 0)
            {
                return;
            }

            const int left = 8;
            const int right = 8;
            const int listTop = 58;
            const int rowHeight = 34;
            const int rowGap = 4;
            const int sectionGap = 8;
            const int bottomMargin = 10;
            int contentWidth = Math.Max(150, tabLayer.ClientSize.Width - left - right);

            // 아래 조작부는 필요한 높이만 확보하고, 나머지는 전부 레이어 목록에 준다.
            int controlsHeight = sectionGap +
                rowHeight * 4 + rowGap * 3 +
                6 + rowHeight +
                sectionGap + 48 + bottomMargin;
            int listHeight = Math.Max(150,
                tabLayer.ClientSize.Height - listTop - controlsHeight);

            layerListPanel.SetBounds(left, listTop, contentWidth, listHeight);

            int y = layerListPanel.Bottom + sectionGap;
            const int pairGap = 6;
            int firstWidth = Math.Max(70, (contentWidth - pairGap) / 2);
            int secondWidth = Math.Max(70, contentWidth - pairGap - firstWidth);

            SetLayerButtonBounds(btnAddLayer, left, y, firstWidth, rowHeight);
            SetLayerButtonBounds(btnAddImageLayer, left + firstWidth + pairGap, y, secondWidth, rowHeight);
            y += rowHeight + rowGap;
            SetLayerButtonBounds(btnDeleteLayer, left, y, firstWidth, rowHeight);
            SetLayerButtonBounds(btnDuplicateLayer, left + firstWidth + pairGap, y, secondWidth, rowHeight);
            y += rowHeight + rowGap;
            SetLayerButtonBounds(btnLayerUp, left, y, firstWidth, rowHeight);
            SetLayerButtonBounds(btnLayerDown, left + firstWidth + pairGap, y, secondWidth, rowHeight);
            y += rowHeight + rowGap;
            SetLayerButtonBounds(btnLayerVisible, left, y, firstWidth, rowHeight);
            SetLayerButtonBounds(btnLayerLock, left + firstWidth + pairGap, y, secondWidth, rowHeight);
            y += rowHeight + 6;
            SetLayerButtonBounds(btnRenameLayer, left, y, contentWidth, rowHeight);
            y += rowHeight + sectionGap;

            if (layerOpacityTitle != null)
                layerOpacityTitle.Location = new Point(left + 2, y);

            int sliderTop = y + 18;
            int valueWidth = 45;
            if (layerOpacityBar != null)
                layerOpacityBar.SetBounds(left, sliderTop, Math.Max(70, contentWidth - valueWidth), 30);
            if (layerOpacityLabel != null)
                layerOpacityLabel.SetBounds(
                    left + contentWidth - valueWidth,
                    sliderTop + 3,
                    valueWidth,
                    22);
        }

        private static void SetLayerButtonBounds(
            Button? button,
            int x,
            int y,
            int width,
            int height)
        {
            button?.SetBounds(x, y, width, height);
        }

        private void LayoutLayerRows()
        {
            if (layerListPanel == null)
                return;

            int rowWidth = Math.Max(120,
                layerListPanel.ClientSize.Width - layerListPanel.Padding.Horizontal -
                SystemInformation.VerticalScrollBarWidth - 5);

            foreach (Control control in layerListPanel.Controls)
            {
                if (control is not Panel row || row.Name != "layerRow")
                    continue;

                row.Width = rowWidth;
                if (row.Controls["layerAccent"] is Control accent)
                    accent.Height = Math.Max(1, row.ClientSize.Height - 2);
                if (row.Controls["layerName"] is Control name)
                    name.Width = Math.Max(24, rowWidth - name.Left - 30);
                if (row.Controls["layerInfo"] is Control info)
                    info.Width = Math.Max(24, rowWidth - info.Left - 30);
                if (row.Controls["layerLock"] is Control lockLabel)
                    lockLabel.Left = Math.Max(0, rowWidth - lockLabel.Width - 5);
            }
        }

        private void RefreshLayerUI()
        {
            if (layerListPanel == null)
                return;

            layerListPanel.SuspendLayout();
            DisposeLayerRows();

            if (editor.State.Layers == null)
            {
                layerListPanel.ResumeLayout(true);
                return;
            }

            int rowY = 2;

            // 위 레이어부터 표시
            for (int i = editor.State.Layers.Count - 1; i >= 0; i--)
            {
                Layer layer = editor.State.Layers[i];
                int index = i;
                bool selected = editor.State.SelectedLayerIndex == index;

                Panel row = new Panel
                {
                    Name = "layerRow",
                    Location = new Point(2, rowY),
                    Size = new Size(184, 56),
                    Tag = index,
                    BackColor = selected
                        ? Color.FromArgb(224, 239, 255)
                        : Color.White,
                    BorderStyle = BorderStyle.FixedSingle,
                    Cursor = Cursors.Hand
                };

                // 선택 강조선
                if (selected)
                {
                    Panel accent = new Panel
                    {
                        Name = "layerAccent",
                        Location = new Point(0, 0),
                        Size = new Size(4, 54),
                        BackColor = Color.FromArgb(50, 125, 220),
                        Tag = index,
                        Cursor = Cursors.Hand
                    };
                    accent.Click += LayerRow_Click;
                    row.Controls.Add(accent);
                }

                // 표시 버튼
                Button visibleButton = new Button
                {
                    Text = layer.Visible ? "●" : "○",
                    Location = new Point(5, 12),
                    Size = new Size(28, 30),
                    Tag = index,
                    FlatStyle = FlatStyle.Flat,
                    BackColor = Color.Transparent,
                    ForeColor = layer.Visible
                        ? Color.FromArgb(45, 125, 80)
                        : Color.FromArgb(150, 155, 165),
                    Font = new Font("맑은 고딕", 10f, FontStyle.Bold),
                    Cursor = Cursors.Hand
                };
                visibleButton.FlatAppearance.BorderSize = 0;
                visibleButton.Click += LayerVisibleButton_Click;
                row.Controls.Add(visibleButton);

                // 썸네일
                PictureBox thumbnail = new PictureBox
                {
                    Name = "layerThumbnail",
                    Location = new Point(37, 7),
                    Size = new Size(42, 42),
                    SizeMode = PictureBoxSizeMode.Zoom,
                    BackColor = Color.FromArgb(235, 238, 242),
                    BorderStyle = BorderStyle.FixedSingle,
                    Tag = index,
                    Cursor = Cursors.Hand
                };

                try
                {
                    if (layer.Image != null)
                        thumbnail.Image = CreateLayerThumbnail(layer.Image);
                }
                catch
                {
                    thumbnail.Image = null;
                }

                thumbnail.Click += LayerRow_Click;
                row.Controls.Add(thumbnail);

                // 이름
                Label nameLabel = new Label
                {
                    Name = "layerName",
                    Text = string.IsNullOrWhiteSpace(layer.Name) ? "이름 없음" : layer.Name,
                    Location = new Point(87, 8),
                    Size = new Size(73, 20),
                    AutoEllipsis = true,
                    TextAlign = ContentAlignment.MiddleLeft,
                    ForeColor = Color.FromArgb(35, 42, 52),
                    Font = new Font("맑은 고딕", 9f, selected ? FontStyle.Bold : FontStyle.Regular),
                    Tag = index,
                    Cursor = Cursors.Hand
                };
                nameLabel.Click += LayerRow_Click;
                row.Controls.Add(nameLabel);

                // 보조 정보
                Label infoLabel = new Label
                {
                    Name = "layerInfo",
                    Text = index == 0 ? "배경" : (layer.Locked ? "잠금됨" : "레이어"),
                    Location = new Point(87, 31),
                    Size = new Size(73, 14),
                    ForeColor = Color.FromArgb(125, 135, 150),
                    Font = new Font("맑은 고딕", 7.5f),
                    Tag = index,
                    Cursor = Cursors.Hand
                };
                infoLabel.Click += LayerRow_Click;
                row.Controls.Add(infoLabel);

                // 잠금 표시
                Label lockLabel = new Label
                {
                    Name = "layerLock",
                    Text = layer.Locked ? "🔒" : "",
                    Location = new Point(160, 17),
                    Size = new Size(20, 22),
                    TextAlign = ContentAlignment.MiddleCenter,
                    ForeColor = Color.FromArgb(90, 100, 115),
                    Font = new Font("Segoe UI Emoji", 9f),
                    Tag = index,
                    Cursor = Cursors.Hand
                };
                lockLabel.Click += LayerRow_Click;
                row.Controls.Add(lockLabel);

                row.Click += LayerRow_Click;
                layerListPanel.Controls.Add(row);
                rowY += 58;
            }

            LayoutLayerRows();

            layerListPanel.ResumeLayout(true);

            Layer? selectedLayer = editor.GetSelectedLayer();
            if (selectedLayer == null)
                return;

            int opacity = Math.Max(0, Math.Min(100, (int)(selectedLayer.Opacity * 100f)));

            if (layerOpacityBar != null && layerOpacityBar.Value != opacity)
            {
                updatingLayerOpacityUi = true;
                try
                {
                    layerOpacityBar.Value = opacity;
                }
                finally
                {
                    updatingLayerOpacityUi = false;
                }
            }

            if (layerOpacityLabel != null)
                layerOpacityLabel.Text = opacity + "%";

            if (btnLayerVisible != null)
                btnLayerVisible.Text = selectedLayer.Visible ? "●  표시" : "○  숨김";

            if (btnLayerLock != null)
                btnLayerLock.Text = selectedLayer.Locked ? "🔓  잠금 해제" : "🔒  잠금";
        }

        private void DisposeLayerRows()
        {
            if (layerListPanel == null)
                return;

            for (int index = layerListPanel.Controls.Count - 1; index >= 0; index--)
            {
                Control row = layerListPanel.Controls[index];
                layerListPanel.Controls.RemoveAt(index);
                DisposeControlTree(row);
            }
        }

        private static void DisposeControlTree(Control control)
        {
            for (int index = control.Controls.Count - 1; index >= 0; index--)
            {
                Control child = control.Controls[index];
                control.Controls.RemoveAt(index);
                DisposeControlTree(child);
            }

            if (control is PictureBox pictureBox)
            {
                pictureBox.Image?.Dispose();
                pictureBox.Image = null;
            }

            control.Dispose();
        }

        private static Bitmap CreateLayerThumbnail(Bitmap source)
        {
            const int thumbnailSize = 34;
            Bitmap thumbnail = new Bitmap(
                thumbnailSize,
                thumbnailSize,
                System.Drawing.Imaging.PixelFormat.Format32bppArgb);

            using Graphics graphics = Graphics.FromImage(thumbnail);
            graphics.Clear(Color.Transparent);
            graphics.InterpolationMode = InterpolationMode.HighQualityBicubic;
            graphics.SmoothingMode = SmoothingMode.HighQuality;

            float scale = Math.Min(
                (float)thumbnailSize / source.Width,
                (float)thumbnailSize / source.Height);
            int width = Math.Max(1, (int)Math.Round(source.Width * scale));
            int height = Math.Max(1, (int)Math.Round(source.Height * scale));
            int x = (thumbnailSize - width) / 2;
            int y = (thumbnailSize - height) / 2;
            graphics.DrawImage(source, new Rectangle(x, y, width, height));
            return thumbnail;
        }

        private void LayerRow_Click(
            object? sender,
            EventArgs e)
        {
            if (sender is not Control control)
                return;


            if (control.Tag is not int index)
                return;


            editor.SelectLayer(index);

            RefreshLayerUI();
            RefreshCanvas();
        }

        private void LayerVisibleButton_Click(
            object? sender,
            EventArgs e)
        {
            if (sender is not Button button)
                return;


            if (button.Tag is not int index)
                return;


            editor.SelectLayer(index);

            editor.PushUndoSnapshot();
            editor.ToggleLayerVisible();

            RefreshLayerUI();
            RefreshCanvas();
            RecordTimelineSnapshot();
        }

        private void BtnAddLayer_Click(
            object? sender,
            EventArgs e)
        {
            editor.PushUndoSnapshot();
            editor.AddLayer();

            RefreshLayerUI();
            RefreshCanvas();
            RecordTimelineSnapshot();
            UpdateUndoRedoMenuState();
        }

        private void BtnAddImageLayer_Click(
            object? sender,
            EventArgs e)
        {
            if (editor.State.CurrentImage == null)
            {
                MessageBox.Show("먼저 이미지를 열어주세요.");
                return;
            }

            Layer? selected = editor.GetSelectedLayer();
            if (selected != null && selected.Locked)
            {
                MessageBox.Show("잠긴 레이어가 선택되어 있습니다. 다른 레이어를 선택해주세요.");
                return;
            }

            using OpenFileDialog dialog = new OpenFileDialog();
            dialog.Title = "레이어로 추가할 이미지 선택";
            dialog.Filter =
                "이미지 파일|*.jpg;*.jpeg;*.png;*.bmp;*.gif;*.tif;*.tiff|모든 파일|*.*";
            dialog.Multiselect = false;

            if (dialog.ShowDialog(this) != DialogResult.OK)
                return;

            try
            {
                using Bitmap source = new Bitmap(dialog.FileName);
                string name = Path.GetFileNameWithoutExtension(dialog.FileName);

                editor.PushUndoSnapshot();
                if (!editor.AddImageLayer(source, name))
                {
                    MessageBox.Show("이미지 레이어를 추가하지 못했습니다.");
                    return;
                }
                RefreshLayerUI();
                RefreshCanvas();
                RecordTimelineSnapshot();
                UpdateUndoRedoMenuState();
            }
            catch (Exception ex)
            {
                MessageBox.Show(
                    "이미지를 레이어로 추가하는 중 오류가 발생했습니다.\n\n" + ex.Message,
                    "이미지 추가 오류",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Error);
            }
        }


        private void BtnDeleteLayer_Click(
            object? sender,
            EventArgs e)
        {
            editor.PushUndoSnapshot();
            editor.DeleteLayer();

            RefreshLayerUI();
            RefreshCanvas();
            RecordTimelineSnapshot();
            UpdateUndoRedoMenuState();
        }

        private void BtnDuplicateLayer_Click(
            object? sender,
            EventArgs e)
        {
            editor.PushUndoSnapshot();
            editor.DuplicateLayer();

            RefreshLayerUI();
            RefreshCanvas();
            RecordTimelineSnapshot();
            UpdateUndoRedoMenuState();
        }

        private void BtnLayerUp_Click(
            object? sender,
            EventArgs e)
        {
            editor.PushUndoSnapshot();
            editor.MoveLayerUp();

            RefreshLayerUI();
            RefreshCanvas();
            RecordTimelineSnapshot();
            UpdateUndoRedoMenuState();
        }

        private void BtnLayerDown_Click(
            object? sender,
            EventArgs e)
        {
            editor.PushUndoSnapshot();
            editor.MoveLayerDown();

            RefreshLayerUI();
            RefreshCanvas();
            RecordTimelineSnapshot();
            UpdateUndoRedoMenuState();
        }

        private void BtnLayerVisible_Click(
            object? sender,
            EventArgs e)
        {
            editor.PushUndoSnapshot();
            editor.ToggleLayerVisible();

            RefreshLayerUI();
            RefreshCanvas();
            RecordTimelineSnapshot();
            UpdateUndoRedoMenuState();
        }

        private void BtnLayerLock_Click(
            object? sender,
            EventArgs e)
        {
            editor.PushUndoSnapshot();
            editor.ToggleLayerLock();

            RefreshLayerUI();
            UpdateUndoRedoMenuState();
        }

        private void BtnRenameLayer_Click(
            object? sender,
            EventArgs e)
        {
            Layer? layer =
                editor.GetSelectedLayer();

            if (layer == null)
                return;


            using Form dialog =
                new Form();

            dialog.Text =
                "레이어 이름 변경";

            dialog.Width =
                300;

            dialog.Height =
                150;

            dialog.StartPosition =
                FormStartPosition.CenterParent;


            Label label =
                new Label();

            label.Text =
                "레이어 이름";

            label.Location =
                new Point(
                    10,
                    15);

            label.AutoSize =
                true;

            dialog.Controls.Add(
                label);


            TextBox textBox =
                new TextBox();

            textBox.Text =
                layer.Name;

            textBox.Location =
                new Point(
                    10,
                    40);

            textBox.Width =
                260;

            dialog.Controls.Add(
                textBox);


            Button okButton =
                new Button();

            okButton.Text =
                "확인";

            okButton.Location =
                new Point(
                    110,
                    75);

            okButton.DialogResult =
                DialogResult.OK;

            dialog.Controls.Add(
                okButton);

            dialog.AcceptButton =
                okButton;


            if (
                dialog.ShowDialog(this)
                == DialogResult.OK)
            {
                if (
                    !string.IsNullOrWhiteSpace(
                        textBox.Text))
                {
                    editor.PushUndoSnapshot();
                    editor.RenameLayer(
                        textBox.Text);

                    RefreshLayerUI();
                    RefreshCanvas();
                    UpdateUndoRedoMenuState();
                }
            }
        }

        private void LayerOpacityBar_ValueChanged(
            object? sender,
            EventArgs e)
        {
            if (layerOpacityBar == null)
                return;

            if (updatingLayerOpacityUi)
                return;

            BeginLayerOpacityEdit();
            if (!layerOpacitySnapshotSaved)
            {
                editor.PushUndoSnapshot();
                layerOpacitySnapshotSaved = true;
            }


            float opacity =
                layerOpacityBar.Value /
                100f;


            editor.SetLayerOpacity(
                opacity);


            if (layerOpacityLabel != null)
            {
                layerOpacityLabel.Text =
                    layerOpacityBar.Value + "%";
            }


            RefreshCanvas();
            layerOpacityCommitTimer.Stop();
            layerOpacityCommitTimer.Start();
        }

        private void LayerOpacityBar_MouseDown(object? sender, MouseEventArgs e)
        {
            if (e.Button == MouseButtons.Left)
                BeginLayerOpacityEdit();
        }

        private void LayerOpacityBar_MouseUp(object? sender, MouseEventArgs e)
        {
            if (e.Button != MouseButtons.Left)
                return;

            EndLayerOpacityEdit();
        }

        private void LayerOpacityBar_KeyDown(object? sender, KeyEventArgs e)
        {
            if (e.KeyCode is Keys.Left or Keys.Right or Keys.Up or Keys.Down or
                Keys.PageUp or Keys.PageDown or Keys.Home or Keys.End)
            {
                BeginLayerOpacityEdit();
            }
        }

        private void LayerOpacityBar_KeyUp(object? sender, KeyEventArgs e)
        {
            if (e.KeyCode is Keys.Left or Keys.Right or Keys.Up or Keys.Down or
                Keys.PageUp or Keys.PageDown or Keys.Home or Keys.End)
            {
                EndLayerOpacityEdit();
            }
        }

        private void BeginLayerOpacityEdit()
        {
            if (layerOpacityEditActive || updatingLayerOpacityUi ||
                layerOpacityBar == null || editor.GetSelectedLayer() == null)
            {
                return;
            }

            layerOpacityEditActive = true;
            layerOpacitySnapshotSaved = false;
            layerOpacityStartValue = layerOpacityBar.Value;
        }

        private void EndLayerOpacityEdit()
        {
            layerOpacityCommitTimer.Stop();
            if (!layerOpacityEditActive)
                return;

            layerOpacityEditActive = false;
            if (!layerOpacitySnapshotSaved)
                return;

            if (layerOpacityBar != null && layerOpacityBar.Value == layerOpacityStartValue)
                editor.DiscardLastUndoSnapshot();
            else
                RecordTimelineSnapshot();

            layerOpacitySnapshotSaved = false;
            UpdateUndoRedoMenuState();
        }

        private void LayerOpacityCommitTimer_Tick(object? sender, EventArgs e) =>
            EndLayerOpacityEdit();

        private Button CreateLayerButton(
            string text,
            int x,
            int y,
            int width)
        {
            Button button = new Button
            {
                Text = text,
                Location = new Point(x, y),
                Size = new Size(width, 34),
                Margin = new Padding(0),
                Padding = new Padding(2, 0, 2, 0),
                Font = new Font("맑은 고딕", 8.5f, FontStyle.Bold),
                TextAlign = ContentAlignment.MiddleCenter,
                FlatStyle = FlatStyle.Flat,
                BackColor = Color.FromArgb(255, 255, 255),
                ForeColor = Color.FromArgb(45, 55, 70),
                Cursor = Cursors.Hand,
                UseVisualStyleBackColor = false
            };

            button.FlatAppearance.BorderColor = Color.FromArgb(210, 216, 225);
            button.FlatAppearance.BorderSize = 1;
            button.MouseEnter += (_, _) =>
            {
                if (!button.IsDisposed)
                {
                    button.BackColor = Color.FromArgb(232, 242, 255);
                    button.FlatAppearance.BorderColor = Color.FromArgb(120, 165, 220);
                }
            };
            button.MouseLeave += (_, _) =>
            {
                if (!button.IsDisposed)
                {
                    button.BackColor = Color.FromArgb(255, 255, 255);
                    button.FlatAppearance.BorderColor = Color.FromArgb(210, 216, 225);
                }
            };

            return button;
        }

        private void ShowDrawingPanel()
        {
            currentTool = ToolMode.Drawing;
            StopDrawing();
            picCanvas.Enabled = true;
            picCanvas.Visible = true;
            picCanvas.Cursor = Cursors.Cross;
            ClearToolPanel();

            pnlToolOptions.BackColor = Color.FromArgb(248, 250, 253);
            pnlToolOptions.Padding = new Padding(0);
            int optionsWidth = Math.Max(280, pnlToolOptions.ClientSize.Width);
            int contentWidth = Math.Max(240, optionsWidth - 28);
            const int twoColumnGap = 10;
            int toolButtonWidth = Math.Max(100, (contentWidth - twoColumnGap) / 2);

            // =====================================================
            // 헤더
            // =====================================================
            Panel header = new Panel
            {
                Location = new Point(0, 0),
                Size = new Size(optionsWidth, 70),
                BackColor = Color.FromArgb(238, 244, 252)
            };

            Label title = CreateLabel("브러시", 14, 8, 16, true);
            title.ForeColor = Color.FromArgb(28, 43, 62);
            header.Controls.Add(title);

            Label subtitle = CreateLabel("레이어에 자유롭게 그리거나 지웁니다.", 14, 35, 8.5f);
            subtitle.ForeColor = Color.FromArgb(105, 118, 135);
            header.Controls.Add(subtitle);

            Label status = CreateLabel(eraserMode ? "지우개 사용 중" : "펜 사용 중", 14, 54, 8f, true);
            status.ForeColor = eraserMode ? Color.FromArgb(205, 82, 82) : Color.FromArgb(47, 116, 184);
            header.Controls.Add(status);

            pnlToolOptions.Controls.Add(header);

            // =====================================================
            // 도구 선택
            // =====================================================
            Label toolTitle = CreateDrawingSectionLabel("도구", 14, 80);
            pnlToolOptions.Controls.Add(toolTitle);

            btnPen = CreateDrawingToolButton("✎  펜", 14, 104, toolButtonWidth);
            btnPen.Name = "btnDrawingPen";
            btnPen.Click += BtnPen_Click;
            pnlToolOptions.Controls.Add(btnPen);

            btnEraser = CreateDrawingToolButton("⌫  지우개", 14 + toolButtonWidth + twoColumnGap, 104, toolButtonWidth);
            btnEraser.Name = "btnDrawingEraser";
            btnEraser.Click += BtnEraser_Click;
            pnlToolOptions.Controls.Add(btnEraser);

            UpdateDrawingToolButtons();

            // =====================================================
            // 색상
            // =====================================================
            Label colorTitle = CreateDrawingSectionLabel("색상", 14, 155);
            pnlToolOptions.Controls.Add(colorTitle);

            Panel colorCard = new Panel
            {
                Location = new Point(14, 178),
                Size = new Size(contentWidth, 58),
                BackColor = Color.White,
                BorderStyle = BorderStyle.FixedSingle
            };

            colorLabel = new Label
            {
                Text = "",
                Location = new Point(10, 8),
                Size = new Size(40, 40),
                BackColor = drawingColor,
                BorderStyle = BorderStyle.FixedSingle
            };
            colorCard.Controls.Add(colorLabel);

            btnColor = new Button
            {
                Name = "btnDrawingColor",
                Text = "색상 선택",
                Location = new Point(58, 8),
                Size = new Size(110, 40),
                FlatStyle = FlatStyle.Flat,
                BackColor = Color.FromArgb(247, 249, 252),
                ForeColor = Color.FromArgb(45, 55, 70),
                Font = new Font("맑은 고딕", 9f, FontStyle.Bold),
                Cursor = Cursors.Hand
            };
            btnColor.FlatAppearance.BorderColor = Color.FromArgb(210, 218, 228);
            btnColor.Click += BtnColor_Click;
            colorCard.Controls.Add(btnColor);

            drawingColorValueLabel = new Label
            {
                Text = ColorToHex(drawingColor),
                Location = new Point(Math.Max(176, contentWidth - 76), 20),
                Size = new Size(68, 18),
                TextAlign = ContentAlignment.MiddleRight,
                ForeColor = Color.FromArgb(100, 112, 128),
                Font = new Font("Consolas", 8.5f, FontStyle.Bold)
            };
            colorCard.Controls.Add(drawingColorValueLabel);

            pnlToolOptions.Controls.Add(colorCard);

            // 빠른 색상
            Color[] quickColors =
            {
                Color.Black, Color.White, Color.Red, Color.FromArgb(30, 120, 220),
                Color.FromArgb(40, 170, 100), Color.FromArgb(245, 150, 45), Color.FromArgb(150, 70, 190)
            };

            int colorX = 14;
            for (int i = 0; i < quickColors.Length; i++)
            {
                Button colorButton = new Button
                {
                    Name = "btnDrawingQuickColor",
                    Location = new Point(colorX, 242),
                    Size = new Size(27, 27),
                    BackColor = quickColors[i],
                    FlatStyle = FlatStyle.Flat,
                    Cursor = Cursors.Hand,
                    Tag = quickColors[i]
                };
                colorButton.FlatAppearance.BorderColor = quickColors[i] == Color.White
                    ? Color.FromArgb(190, 195, 205)
                    : quickColors[i];
                colorButton.Click += QuickColorButton_Click;
                pnlToolOptions.Controls.Add(colorButton);
                colorX += 34;
            }

            // =====================================================
            // 브러시 크기
            // =====================================================
            Label sizeTitle = CreateDrawingSectionLabel("브러시 크기", 14, 274);
            pnlToolOptions.Controls.Add(sizeTitle);

            sizeValueLabel = new Label
            {
                Text = $"{drawingWidth:0} px",
                Location = new Point(optionsWidth - 70, 274),
                Size = new Size(56, 20),
                TextAlign = ContentAlignment.MiddleRight,
                ForeColor = Color.FromArgb(55, 70, 90),
                Font = new Font("맑은 고딕", 9f, FontStyle.Bold)
            };
            pnlToolOptions.Controls.Add(sizeValueLabel);

            brushSizeBar = new TrackBar
            {
                Name = "trkDrawingBrushSize",
                Minimum = 1,
                Maximum = 100,
                Value = Math.Max(1, Math.Min(100, (int)drawingWidth)),
                Location = new Point(10, 297),
                Size = new Size(optionsWidth - 20, 35),
                TickFrequency = 10,
                AutoSize = false
            };
            brushSizeBar.ValueChanged += BrushSizeBar_ValueChanged;
            pnlToolOptions.Controls.Add(brushSizeBar);

            Label presetTitle = CreateLabel("빠른 크기", 14, 332, 8.5f);
            presetTitle.ForeColor = Color.FromArgb(105, 118, 135);
            pnlToolOptions.Controls.Add(presetTitle);

            int[] presets = { 2, 5, 10, 20, 40, 80 };
            Button[] presetButtons = new Button[6];
            int presetGap = 6;
            int presetWidth = Math.Max(30, (contentWidth - presetGap * 5) / presets.Length);
            for (int i = 0; i < presets.Length; i++)
            {
                Button b = new Button
                {
                    Name = "btnDrawingQuickSize",
                    Text = presets[i].ToString(),
                    Location = new Point(14 + i * (presetWidth + presetGap), 351),
                    Size = new Size(presetWidth, 28),
                    FlatStyle = FlatStyle.Flat,
                    BackColor = Color.White,
                    ForeColor = Color.FromArgb(60, 72, 88),
                    Font = new Font("맑은 고딕", 8f, FontStyle.Bold),
                    Tag = presets[i],
                    Cursor = Cursors.Hand
                };
                b.FlatAppearance.BorderColor = Color.FromArgb(210, 218, 228);
                b.Click += QuickBrushSize_Click;
                presetButtons[i] = b;
                pnlToolOptions.Controls.Add(b);
            }

            // =====================================================
            // 불투명도
            // =====================================================
            Label opacityTitle = CreateDrawingSectionLabel("불투명도", 14, 385);
            pnlToolOptions.Controls.Add(opacityTitle);

            opacityValueLabel = new Label
            {
                Text = $"{(int)(drawingOpacity * 100)}%",
                Location = new Point(optionsWidth - 70, 385),
                Size = new Size(56, 20),
                TextAlign = ContentAlignment.MiddleRight,
                ForeColor = Color.FromArgb(55, 70, 90),
                Font = new Font("맑은 고딕", 9f, FontStyle.Bold)
            };
            pnlToolOptions.Controls.Add(opacityValueLabel);

            brushOpacityBar = new TrackBar
            {
                Name = "trkDrawingOpacity",
                Minimum = 1,
                Maximum = 100,
                Value = Math.Max(1, Math.Min(100, (int)(drawingOpacity * 100))),
                Location = new Point(10, 408),
                Size = new Size(optionsWidth - 20, 35),
                TickFrequency = 10,
                AutoSize = false
            };
            brushOpacityBar.ValueChanged += BrushOpacityBar_ValueChanged;
            pnlToolOptions.Controls.Add(brushOpacityBar);

            // =====================================================
            // 레이어 작업
            // =====================================================
            Label actionTitle = CreateDrawingSectionLabel("레이어 작업", 14, 450);
            pnlToolOptions.Controls.Add(actionTitle);

            btnClearDrawingLayer = new Button
            {
                Name = "btnClearDrawingLayer",
                Text = "현재 레이어 전체 지우기",
                Location = new Point(14, 475),
                Size = new Size(contentWidth, 38),
                FlatStyle = FlatStyle.Flat,
                BackColor = Color.White,
                ForeColor = Color.FromArgb(190, 70, 70),
                Font = new Font("맑은 고딕", 9f, FontStyle.Bold),
                Cursor = Cursors.Hand
            };
            btnClearDrawingLayer.FlatAppearance.BorderColor = Color.FromArgb(225, 190, 190);
            btnClearDrawingLayer.Click += BtnClearDrawingLayer_Click;
            pnlToolOptions.Controls.Add(btnClearDrawingLayer);

            pnlToolOptions.Visible = true;
            pnlToolOptions.BringToFront();
        }

        private Label CreateDrawingSectionLabel(string text, int x, int y)
        {
            Label label = CreateLabel(text, x, y, 9.5f, true);
            label.ForeColor = Color.FromArgb(55, 68, 85);
            return label;
        }

        private Button CreateDrawingToolButton(string text, int x, int y, int width)
        {
            Button button = new Button
            {
                Text = text,
                Location = new Point(x, y),
                Size = new Size(width, 38),
                FlatStyle = FlatStyle.Flat,
                BackColor = Color.White,
                ForeColor = Color.FromArgb(55, 65, 80),
                Font = new Font("맑은 고딕", 9f, FontStyle.Bold),
                Cursor = Cursors.Hand
            };
            button.FlatAppearance.BorderColor = Color.FromArgb(210, 218, 228);
            button.FlatAppearance.BorderSize = 1;
            return button;
        }

        private void UpdateDrawingToolButtons()
        {
            if (btnPen == null || btnEraser == null)
                return;

            btnPen.BackColor = eraserMode ? Color.White : Color.FromArgb(226, 239, 255);
            btnPen.ForeColor = eraserMode ? Color.FromArgb(70, 80, 95) : Color.FromArgb(30, 105, 190);
            btnPen.FlatAppearance.BorderColor = eraserMode
                ? Color.FromArgb(210, 218, 228)
                : Color.FromArgb(75, 145, 215);

            btnEraser.BackColor = eraserMode ? Color.FromArgb(255, 235, 235) : Color.White;
            btnEraser.ForeColor = eraserMode ? Color.FromArgb(190, 65, 65) : Color.FromArgb(70, 80, 95);
            btnEraser.FlatAppearance.BorderColor = eraserMode
                ? Color.FromArgb(220, 130, 130)
                : Color.FromArgb(210, 218, 228);
        }

        private string ColorToHex(Color color)
        {
            return $"#{color.R:X2}{color.G:X2}{color.B:X2}";
        }

        private void QuickColorButton_Click(object? sender, EventArgs e)
        {
            if (sender is not Button button || button.Tag is not Color color)
                return;

            drawingColor = color;

            if (colorLabel != null)
                colorLabel.BackColor = drawingColor;

            if (drawingColorValueLabel != null)
                drawingColorValueLabel.Text = ColorToHex(drawingColor);
        }

        private void QuickBrushSize_Click(object? sender, EventArgs e)
        {
            if (sender is not Button button || button.Tag is not int size)
                return;

            drawingWidth = size;

            if (brushSizeBar != null)
                brushSizeBar.Value = Math.Max(brushSizeBar.Minimum, Math.Min(brushSizeBar.Maximum, size));

            if (sizeValueLabel != null)
                sizeValueLabel.Text = $"{drawingWidth:0} px";

            picCanvas.Invalidate();
        }

        private void BtnClearDrawingLayer_Click(object? sender, EventArgs e)
        {
            Layer? layer = editor.GetSelectedLayer();
            if (layer == null)
                return;

            if (layer.Locked)
            {
                MessageBox.Show("잠긴 레이어는 지울 수 없습니다.", "레이어 잠금", MessageBoxButtons.OK, MessageBoxIcon.Information);
                return;
            }

            DialogResult result = MessageBox.Show(
                "현재 레이어의 그림을 모두 지울까요?",
                "현재 레이어 지우기",
                MessageBoxButtons.YesNo,
                MessageBoxIcon.Question);

            if (result != DialogResult.Yes)
                return;

            editor.PushUndoSnapshot();
            editor.ClearCurrentLayer();
            RefreshCanvas();
            RefreshLayerUI();
            RecordTimelineSnapshot();
            UpdateUndoRedoMenuState();
        }

        private void BtnPen_Click(
            object? sender,
            EventArgs e)
        {
            currentTool = ToolMode.Drawing;
            eraserMode = false;
            isDrawing = false;
            picCanvas.Capture = false;
            picCanvas.Cursor = Cursors.Cross;

            UpdateDrawingToolButtons();
            picCanvas.Focus();
            picCanvas.Invalidate();
        }

        private void BtnEraser_Click(
            object? sender,
            EventArgs e)
        {
            currentTool = ToolMode.Drawing;
            eraserMode = true;
            isDrawing = false;
            picCanvas.Capture = false;
            picCanvas.Cursor = Cursors.Cross;

            UpdateDrawingToolButtons();
            picCanvas.Focus();
            picCanvas.Invalidate();
        }

        private void BtnColor_Click(
            object? sender,
            EventArgs e)
        {
            using ColorDialog dialog =
                new ColorDialog();


            dialog.Color =
                drawingColor;


            if (
                dialog.ShowDialog()
                == DialogResult.OK)
            {
                drawingColor =
                    dialog.Color;


                if (colorLabel != null)
                    colorLabel.BackColor = drawingColor;

                if (drawingColorValueLabel != null)
                    drawingColorValueLabel.Text = ColorToHex(drawingColor);
            }
        }

        private void BrushSizeBar_ValueChanged(
            object? sender,
            EventArgs e)
        {
            if (brushSizeBar == null)
                return;


            drawingWidth =
                brushSizeBar.Value;


            if (sizeValueLabel != null)
            {
                sizeValueLabel.Text =
                    $"{drawingWidth:0} px";
                picCanvas.Invalidate();
            }
        }

        private void BrushOpacityBar_ValueChanged(
            object? sender,
            EventArgs e)
        {
            if (brushOpacityBar == null)
                return;


            drawingOpacity =
                brushOpacityBar.Value /
                100f;


            if (opacityValueLabel != null)
            {
                opacityValueLabel.Text =
                    brushOpacityBar.Value + "%";
            }
        }

        private void InitializeCanvasEvents()
        {
            picCanvas.MouseDown -=
                PicCanvas_MouseDown;

            picCanvas.MouseMove -=
                PicCanvas_MouseMove;

            picCanvas.MouseUp -=
                PicCanvas_MouseUp;

            picCanvas.MouseLeave -=
                PicCanvas_MouseLeave;

            picCanvas.Paint -=
                PicCanvas_Paint;


            picCanvas.MouseDown +=
                PicCanvas_MouseDown;

            picCanvas.MouseMove +=
                PicCanvas_MouseMove;

            picCanvas.MouseUp +=
                PicCanvas_MouseUp;

            picCanvas.MouseLeave +=
                PicCanvas_MouseLeave;

            picCanvas.Paint +=
                PicCanvas_Paint;
        }

        private void PicCanvas_MouseDown(
            object? sender,
            MouseEventArgs e)
        {
            if (e.Button != MouseButtons.Left)
                return;

            if (editor.State.CurrentImage == null)
                return;

            picCanvas.Focus();
            picCanvas.Capture = true;

            if (currentTool == ToolMode.Drawing)
            {
                Layer? layer = editor.GetSelectedLayer();

                if (layer == null || layer.Locked)
                {
                    picCanvas.Capture = false;
                    return;
                }

                Point imagePoint = ConvertToImagePoint(e.Location);
                editor.PushUndoSnapshot();
                lastPoint = imagePoint;
                isDrawing = true;

                Color drawColor = Color.FromArgb(
                    Math.Max(1, Math.Min(255, (int)(drawingOpacity * 255f))),
                    drawingColor);

                // 클릭 한 번만 해도 점이 찍힌다.
                editor.DrawLine(
                    imagePoint,
                    imagePoint,
                    drawColor,
                    drawingWidth,
                    eraserMode,
                    renderLayers: false);

                brushCursorPoint = e.Location;
                showBrushCursor = true;
                picCanvas.Invalidate();
                QueueDrawingRender();
                return;
            }

        }

        private void PicCanvas_MouseMove(
            object? sender,
            MouseEventArgs e)
        {
            if (editor.State.CurrentImage == null)
                return;

            if (currentTool == ToolMode.Drawing)
            {
                // MouseDown이 다른 컨트롤에서 시작된 경우에도
                // 왼쪽 버튼을 누른 채 캔버스 안으로 들어오면 그리기를 시작한다.
                if (!isDrawing && (e.Button & MouseButtons.Left) == MouseButtons.Left)
                {
                    Layer? fallbackLayer = editor.GetSelectedLayer();
                    if (fallbackLayer != null && !fallbackLayer.Locked)
                    {
                        editor.PushUndoSnapshot();
                        isDrawing = true;
                        picCanvas.Capture = true;
                        lastPoint = ConvertToImagePoint(e.Location);
                    }
                }

                if (!isDrawing)
                {
                    brushCursorPoint = e.Location;
                    showBrushCursor = true;
                    picCanvas.Invalidate();
                    return;
                }

                Layer? layer = editor.GetSelectedLayer();

                if (layer == null || layer.Locked)
                    return;

                Point imagePoint = ConvertToImagePoint(e.Location);

                Color drawColor = Color.FromArgb(
                    Math.Max(1, Math.Min(255, (int)(drawingOpacity * 255f))),
                    drawingColor);

                editor.DrawLine(
                    lastPoint,
                    imagePoint,
                    drawColor,
                    drawingWidth,
                    eraserMode,
                    renderLayers: false);

                lastPoint = imagePoint;
                brushCursorPoint = e.Location;
                showBrushCursor = true;
                picCanvas.Invalidate();
                QueueDrawingRender();
                return;
            }

        }

        private void PicCanvas_MouseLeave(
            object? sender,
            EventArgs e)
        {
            if (!isDrawing)
                showBrushCursor = false;

            picCanvas.Invalidate();
        }

        private void PicCanvas_MouseUp(
            object? sender,
            MouseEventArgs e)
        {
            if (e.Button != MouseButtons.Left)
                return;


            // =================================================
            // 그리기 종료
            // =================================================

            if (currentTool == ToolMode.Drawing)
            {
                isDrawing = false;
                picCanvas.Capture = false;
                FlushDrawingRender();
                RecordTimelineSnapshot();
                UpdateUndoRedoMenuState();
                return;
            }


        }

        private void PicCanvas_Paint(
            object? sender,
            PaintEventArgs e)
        {
            if (currentTool == ToolMode.Drawing && showBrushCursor)
            {
                float scaleX = editor.State.CurrentImage != null && editor.State.CurrentImage.Width > 0
                    ? (float)picCanvas.ClientSize.Width / editor.State.CurrentImage.Width
                    : 1f;
                float scaleY = editor.State.CurrentImage != null && editor.State.CurrentImage.Height > 0
                    ? (float)picCanvas.ClientSize.Height / editor.State.CurrentImage.Height
                    : 1f;

                float diameter = Math.Max(2f, drawingWidth * ((scaleX + scaleY) / 2f));
                float radius = diameter / 2f;

                using Pen cursorPen = new Pen(eraserMode ? Color.FromArgb(210, 70, 70) : Color.FromArgb(35, 105, 180), 1.5f);
                e.Graphics.DrawEllipse(
                    cursorPen,
                    brushCursorPoint.X - radius,
                    brushCursorPoint.Y - radius,
                    diameter,
                    diameter);
            }

        }

        private Rectangle ImageRectToCanvasRect(
            Rectangle imageRect)
        {
            if (editor.State.CurrentImage == null)
                return Rectangle.Empty;


            int imageWidth =
                editor.State.CurrentImage.Width;

            int imageHeight =
                editor.State.CurrentImage.Height;


            int canvasWidth =
                picCanvas.ClientSize.Width;

            int canvasHeight =
                picCanvas.ClientSize.Height;


            if (imageWidth <= 0 ||
                imageHeight <= 0 ||
                canvasWidth <= 0 ||
                canvasHeight <= 0)
            {
                return Rectangle.Empty;
            }


            float scaleX =
                (float)canvasWidth /
                imageWidth;

            float scaleY =
                (float)canvasHeight /
                imageHeight;


            return new Rectangle(
                (int)(imageRect.X * scaleX),
                (int)(imageRect.Y * scaleY),
                Math.Max(
                    1,
                    (int)(imageRect.Width * scaleX)),
                Math.Max(
                    1,
                    (int)(imageRect.Height * scaleY)));
        }

        private Point ConvertToImagePoint(
            Point point)
        {
            if (editor.State.CurrentImage == null)
                return Point.Empty;


            int imageWidth =
                editor.State.CurrentImage.Width;

            int imageHeight =
                editor.State.CurrentImage.Height;


            int canvasWidth =
                picCanvas.ClientSize.Width;

            int canvasHeight =
                picCanvas.ClientSize.Height;


            if (
                canvasWidth <= 0 ||
                canvasHeight <= 0)
            {
                return Point.Empty;
            }


            float scaleX =
                (float)imageWidth /
                canvasWidth;

            float scaleY =
                (float)imageHeight /
                canvasHeight;


            int x =
                (int)(
                    point.X *
                    scaleX);


            int y =
                (int)(
                    point.Y *
                    scaleY);


            x =
                Math.Max(
                    0,
                    Math.Min(
                        imageWidth - 1,
                        x));


            y =
                Math.Max(
                    0,
                    Math.Min(
                        imageHeight - 1,
                        y));


            return new Point(
                x,
                y);
        }

        private void StopDrawing()
        {
            isDrawing =
                false;
        }

        private Label CreateLabel(
            string text,
            int x,
            int y,
            float fontSize = 9f,
            bool bold = false)
        {
            Label label =
                new Label();

            label.Text =
                text;

            label.Location =
                new Point(
                    x,
                    y);

            label.AutoSize =
                true;

            label.Font =
                new Font(
                    "맑은 고딕",
                    fontSize,
                    bold
                        ? FontStyle.Bold
                        : FontStyle.Regular);

            return label;
        }
    }
}
