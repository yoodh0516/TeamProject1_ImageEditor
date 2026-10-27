using ImageEditor.Core;
using System;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Drawing.Imaging;
using System.Windows.Forms;

namespace ImageEditor
{
    public partial class MainForm : Form
    {
        private UserControl? maskPanel;
        private ComboBox? maskShapeComboBox;
        private TrackBar? maskBrushBar;
        private Label? maskBrushLabel;

        private bool maskActive;
        private bool maskDrawing;
        private bool maskEraseMode;
        private float maskBrushSize = 40f;
        private Point maskStart;
        private Point maskLast;

        private void InitializeMaskFeatures()
        {
            menuMask.ToolTipText =
                "브러시·네모·동그라미로 마스크 영역을 지정해 효과를 적용합니다.";
            menuMask.Click -= MenuMask_Click;
            menuMask.Click += MenuMask_Click;

            picCanvas.MouseDown += MaskCanvas_MouseDown;
            picCanvas.MouseMove += MaskCanvas_MouseMove;
            picCanvas.MouseUp += MaskCanvas_MouseUp;
            picCanvas.Paint += MaskCanvas_Paint;
        }

        private void MenuMask_Click(object? sender, EventArgs e)
        {
            if (editor.State.CurrentImage == null)
            {
                MessageBox.Show("먼저 이미지를 불러와주세요.");
                return;
            }

            Layer? selectedLayer = editor.GetSelectedLayer();
            if (selectedLayer == null)
            {
                MessageBox.Show("먼저 마스크를 적용할 레이어를 선택해주세요.");
                return;
            }

            if (selectedLayer.Locked)
            {
                MessageBox.Show("잠긴 레이어에는 마스크를 적용할 수 없습니다.");
                return;
            }

            StopInteractiveTools();
            maskActive = true;
            maskDrawing = false;
            maskEraseMode = false;
            editor.InitializeSelectionMask();
            ShowMaskPanel();
            picCanvas.Cursor = Cursors.Cross;
            picCanvas.Invalidate();
        }

        private void ShowMaskPanel()
        {
            maskPanel?.Dispose();
            maskPanel = new UserControl
            {
                BackColor = Color.FromArgb(248, 249, 252),
                Dock = DockStyle.Fill
            };

            FlowLayoutPanel layout = new FlowLayoutPanel
            {
                Dock = DockStyle.Fill,
                FlowDirection = FlowDirection.TopDown,
                WrapContents = false,
                AutoScroll = true,
                Padding = new Padding(12)
            };

            layout.Controls.Add(CreateMaskLabel("마스크", 14f, true));
            layout.Controls.Add(CreateMaskLabel(
                "1. 영역을 칠하거나 도형으로 지정\n2. 지정한 마스크 영역에 기능 적용",
                9f,
                false));

            Button undoButton = CreateMaskButton("⟲ 이전으로 되돌리기", 226);
            undoButton.Margin = new Padding(3, 8, 3, 4);
            undoButton.Click += (_, _) =>
            {
                if (!editor.Undo())
                    return;

                editor.InitializeSelectionMask();
                RefreshCanvas();
                RefreshLayerUI();
                UpdateUndoRedoMenuState();
            };
            layout.Controls.Add(undoButton);

            maskShapeComboBox = new ComboBox
            {
                Name = "cmbMaskShape",
                DropDownStyle = ComboBoxStyle.DropDownList,
                Width = 226,
                Margin = new Padding(3, 10, 3, 6)
            };
            maskShapeComboBox.Items.AddRange(new object[]
            {
                "브러시",
                "네모",
                "동그라미"
            });
            maskShapeComboBox.SelectedIndex = 0;
            layout.Controls.Add(maskShapeComboBox);

            FlowLayoutPanel modeRow = new FlowLayoutPanel
            {
                Width = 230,
                Height = 42,
                WrapContents = false,
                Margin = new Padding(0, 2, 0, 4)
            };

            Button selectButton = CreateMaskButton("선택 추가", 108);
            Button eraseButton = CreateMaskButton("선택 지우기", 108);
            selectButton.BackColor = Color.FromArgb(220, 230, 255);

            selectButton.Click += (_, _) =>
            {
                maskEraseMode = false;
                selectButton.BackColor = Color.FromArgb(220, 230, 255);
                eraseButton.BackColor = Color.White;
            };

            eraseButton.Click += (_, _) =>
            {
                maskEraseMode = true;
                selectButton.BackColor = Color.White;
                eraseButton.BackColor = Color.FromArgb(255, 225, 225);
            };

            modeRow.Controls.Add(selectButton);
            modeRow.Controls.Add(eraseButton);
            layout.Controls.Add(modeRow);

            maskBrushLabel = CreateMaskLabel(
                $"브러시 크기 : {(int)maskBrushSize}px",
                9f,
                true);
            layout.Controls.Add(maskBrushLabel);

            maskBrushBar = new TrackBar
            {
                Name = "trkMaskBrushSize",
                Minimum = 1,
                Maximum = 150,
                Value = (int)maskBrushSize,
                TickFrequency = 15,
                Width = 226,
                Height = 42,
                Margin = new Padding(0)
            };
            maskBrushBar.ValueChanged += (_, _) =>
            {
                maskBrushSize = maskBrushBar.Value;
                if (maskBrushLabel != null)
                    maskBrushLabel.Text =
                        $"브러시 크기 : {(int)maskBrushSize}px";
            };
            layout.Controls.Add(maskBrushBar);

            layout.Controls.Add(CreateMaskLabel("마스크 영역에 기능 적용", 10f, true));

            Button eraseEffect = CreateMaskButton("선택 영역 지우기 (투명)", 226);
            Button grayscaleEffect = CreateMaskButton("선택 영역 흑백", 226);
            Button blurEffect = CreateMaskButton("선택 영역 흐림", 226);
            Button invertEffect = CreateMaskButton("선택 영역 색상 반전", 226);
            Button swapEffect = CreateMaskButton("효과 영역 뒤집기", 226);

            eraseEffect.Click += (_, _) => ApplyMaskEffect("Erase");
            grayscaleEffect.Click += (_, _) => ApplyMaskEffect("Grayscale");
            blurEffect.Click += (_, _) => ApplyMaskEffect("Blur");
            invertEffect.Click += (_, _) => ApplyMaskEffect("ColorInvert");
            swapEffect.Click += (_, _) => SwapMaskEffect();

            layout.Controls.Add(eraseEffect);
            layout.Controls.Add(grayscaleEffect);
            layout.Controls.Add(blurEffect);
            layout.Controls.Add(invertEffect);
            layout.Controls.Add(swapEffect);

            FlowLayoutPanel utilityRow = new FlowLayoutPanel
            {
                Width = 230,
                Height = 42,
                WrapContents = false,
                Margin = new Padding(0, 6, 0, 6)
            };

            Button invertSelection = CreateMaskButton("선택 반전", 108);
            Button clearSelection = CreateMaskButton("선택 초기화", 108);
            invertSelection.Click += (_, _) =>
            {
                editor.InvertSelection();
                picCanvas.Invalidate();
            };
            clearSelection.Click += (_, _) =>
            {
                editor.ClearSelection();
                picCanvas.Invalidate();
            };

            utilityRow.Controls.Add(invertSelection);
            utilityRow.Controls.Add(clearSelection);
            layout.Controls.Add(utilityRow);

            void UpdateMaskLayout()
            {
                int contentWidth = Math.Max(
                    180,
                    layout.ClientSize.Width - layout.Padding.Horizontal -
                    ((layout.VerticalScroll.Visible ||
                      layout.DisplayRectangle.Height > layout.ClientSize.Height)
                        ? SystemInformation.VerticalScrollBarWidth
                        : 0) - 6);

                undoButton.Width = contentWidth;
                maskShapeComboBox.Width = contentWidth;
                maskBrushBar.Width = contentWidth;
                foreach (Button effectButton in new[]
                {
                    eraseEffect, grayscaleEffect, blurEffect, invertEffect, swapEffect
                })
                {
                    effectButton.Width = contentWidth;
                }

                foreach (Control child in layout.Controls)
                {
                    if (child is Label label)
                        label.MaximumSize = new Size(contentWidth, 0);
                }

                const int gap = 6;
                int halfWidth = Math.Max(72, (contentWidth - gap) / 2);
                modeRow.Width = contentWidth;
                selectButton.Width = halfWidth;
                eraseButton.Width = halfWidth;
                utilityRow.Width = contentWidth;
                invertSelection.Width = halfWidth;
                clearSelection.Width = halfWidth;
            }

            layout.SizeChanged += (_, _) => UpdateMaskLayout();

            maskPanel.Controls.Add(layout);
            UpdateMaskLayout();
            ShowToolControl(maskPanel);
        }

        private static Label CreateMaskLabel(string text, float size, bool bold)
        {
            return new Label
            {
                Text = text,
                AutoSize = true,
                MaximumSize = new Size(226, 0),
                Font = new Font(
                    "맑은 고딕",
                    size,
                    bold ? FontStyle.Bold : FontStyle.Regular),
                ForeColor = Color.FromArgb(45, 52, 68),
                Margin = new Padding(3, 4, 3, 4)
            };
        }

        private static Button CreateMaskButton(string text, int width)
        {
            Button button = new Button
            {
                Text = text,
                Width = width,
                Height = 34,
                BackColor = Color.White,
                FlatStyle = FlatStyle.Flat,
                Margin = new Padding(3)
            };
            button.FlatAppearance.BorderColor = Color.FromArgb(210, 215, 225);
            return button;
        }

        private string GetMaskShapeType()
        {
            return maskShapeComboBox?.SelectedIndex switch
            {
                1 => "Rectangle",
                2 => "Ellipse",
                _ => "Brush"
            };
        }

        private void MaskCanvas_MouseDown(object? sender, MouseEventArgs e)
        {
            if (!maskActive || e.Button != MouseButtons.Left)
                return;

            Point imagePoint = ConvertToImagePoint(e.Location);
            maskStart = imagePoint;
            maskLast = imagePoint;
            maskDrawing = true;
            picCanvas.Capture = true;

            if (GetMaskShapeType() == "Brush")
            {
                editor.DrawSelectionLine(
                    imagePoint,
                    imagePoint,
                    maskEraseMode,
                    maskBrushSize);
                picCanvas.Invalidate();
            }
        }

        private void MaskCanvas_MouseMove(object? sender, MouseEventArgs e)
        {
            if (!maskActive || !maskDrawing)
                return;

            if ((e.Button & MouseButtons.Left) != MouseButtons.Left)
                return;

            if (GetMaskShapeType() != "Brush")
            {
                maskLast = ConvertToImagePoint(e.Location);
                picCanvas.Invalidate();
                return;
            }

            Point imagePoint = ConvertToImagePoint(e.Location);
            editor.DrawSelectionLine(
                maskLast,
                imagePoint,
                maskEraseMode,
                maskBrushSize);
            maskLast = imagePoint;
            picCanvas.Invalidate();
        }

        private void MaskCanvas_MouseUp(object? sender, MouseEventArgs e)
        {
            if (!maskActive || !maskDrawing || e.Button != MouseButtons.Left)
                return;

            Point imagePoint = ConvertToImagePoint(e.Location);
            string shapeType = GetMaskShapeType();

            if (shapeType != "Brush")
            {
                Rectangle rectangle = NormalizeMaskRectangle(
                    maskStart,
                    imagePoint);
                editor.DrawSelectionShape(
                    rectangle,
                    shapeType,
                    maskEraseMode);
            }

            maskDrawing = false;
            picCanvas.Capture = false;
            picCanvas.Invalidate();
        }

        private static Rectangle NormalizeMaskRectangle(Point start, Point end)
        {
            int left = Math.Min(start.X, end.X);
            int top = Math.Min(start.Y, end.Y);
            int right = Math.Max(start.X, end.X);
            int bottom = Math.Max(start.Y, end.Y);
            return new Rectangle(left, top, right - left, bottom - top);
        }

        private void MaskCanvas_Paint(object? sender, PaintEventArgs e)
        {
            if (!maskActive || editor.SelectionMask == null)
                return;

            Rectangle canvasArea = ImageRectToCanvasRect(
                new Rectangle(
                    0,
                    0,
                    editor.SelectionMask.Width,
                    editor.SelectionMask.Height));

            if (canvasArea.Width <= 0 || canvasArea.Height <= 0)
                return;

            using ImageAttributes attributes = new ImageAttributes();
            ColorMatrix matrix = new ColorMatrix
            {
                Matrix00 = 0f,
                Matrix11 = 0f,
                Matrix22 = 0f,
                Matrix33 = 0.38f,
                Matrix40 = 0.55f,
                Matrix41 = 0.20f,
                Matrix42 = 0.90f
            };
            attributes.SetColorMatrix(matrix);

            e.Graphics.DrawImage(
                editor.SelectionMask,
                canvasArea,
                0,
                0,
                editor.SelectionMask.Width,
                editor.SelectionMask.Height,
                GraphicsUnit.Pixel,
                attributes);

            string shapeType = GetMaskShapeType();
            if (maskDrawing && shapeType != "Brush")
            {
                Rectangle preview = ImageRectToCanvasRect(
                    NormalizeMaskRectangle(maskStart, maskLast));
                if (preview.Width > 0 && preview.Height > 0)
                {
                    using Pen pen = new Pen(Color.Purple, 2f)
                    {
                        DashStyle = DashStyle.Dash
                    };

                    if (shapeType == "Ellipse")
                        e.Graphics.DrawEllipse(pen, preview);
                    else
                        e.Graphics.DrawRectangle(pen, preview);
                }
            }
        }

        private void ApplyMaskEffect(string effectType)
        {
            Layer? selectedLayer = editor.GetSelectedLayer();
            if (selectedLayer == null)
                return;

            if (selectedLayer.Name.Contains("그리기", StringComparison.OrdinalIgnoreCase))
            {
                DialogResult result = MessageBox.Show(
                    "현재 그리기 레이어가 선택되어 있습니다.\n배경 이미지에 효과를 적용할까요?",
                    "적용 레이어 확인",
                    MessageBoxButtons.YesNo,
                    MessageBoxIcon.Question);

                if (result != DialogResult.Yes)
                    return;

                editor.SelectLayer(0);
            }

            if (!editor.HasSelection())
            {
                MessageBox.Show("먼저 적용할 영역을 선택해주세요.");
                return;
            }

            editor.PushUndoSnapshot();

            if (!editor.ApplyEffectToSelection(effectType))
            {
                MessageBox.Show("선택한 레이어가 잠겨 있거나 효과를 적용할 수 없습니다.");
                return;
            }

            editor.ClearSelection();
            RefreshCanvas();
            RefreshLayerUI();
            RecordTimelineSnapshot();
            UpdateUndoRedoMenuState();
            picCanvas.Invalidate();
        }

        private void SwapMaskEffect()
        {
            if (!editor.CanSwapLastEffect)
            {
                MessageBox.Show("뒤집을 직전 마스크 작업이 없습니다.");
                return;
            }

            editor.PushUndoSnapshot();
            editor.SwapLastEffect();

            RefreshCanvas();
            RefreshLayerUI();
            RecordTimelineSnapshot();
            UpdateUndoRedoMenuState();
            picCanvas.Invalidate();
        }

        private void StopMaskTool()
        {
            if (!maskActive)
                return;

            maskActive = false;
            maskDrawing = false;
            picCanvas.Capture = false;
            picCanvas.Cursor = Cursors.Default;
            editor.ClearSelection();
            picCanvas.Invalidate();
        }
    }
}
