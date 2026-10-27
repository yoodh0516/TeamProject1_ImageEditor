using ImageEditor.Controls;
using ImageEditor.Core;
using ImageEditor.ImageProcessing;
using ImageEditor.Services;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Drawing.Imaging;
using System.Drawing.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace ImageEditor
{
    public partial class MainForm : Form
    {
        private TemplateControl? templateControl;
        private Bitmap? templateBaseImage;
        private Bitmap? templatePreviewImage;
        private bool templateSnapshotSaved;
private readonly List<Label> templateTextLabels = new();
private Label? draggingTemplateLabel;
private Point templateDragOffset;
private bool templateTextDirty;
private bool templateIsDragging;
private TextBox? templateInlineEditor;
private Label? templateEditingLabel;
private string? templateOriginalText;
private readonly System.Windows.Forms.Timer templateCaretBlinkTimer = new System.Windows.Forms.Timer { Interval = 500 };
private bool templateCaretVisible;

        private CommunityFilterControl? communityFilterControl;
        private Bitmap? communityBaseImage;
        private Bitmap? communityPreviewImage;
        private Image? communityOverlay;
        private Bitmap? communityOwnedOverlay;
        private float communityOverlayX;
        private float communityOverlayY;
        private float communityOverlayScale = 0.7f;
        private float communityOverlayRotation;
        private bool communityDragging;
        private bool communitySessionActive;
        private bool communitySessionDirty;
        private bool communitySnapshotSaved;
        private Point communityDragStart;
        private readonly System.Windows.Forms.Timer communityRenderTimer =
            new System.Windows.Forms.Timer { Interval = 33 };
        private bool communityRenderQueued;
        private CancellationTokenSource? communityFaceDetectionCancellation;
        private string? communitySelectedFilterName;
        private IReadOnlyList<CommunityFacePlacement>? communityDetectedFaces;

        private void InitializeTemplateCommunityFeatures()
        {
            templateControl = new TemplateControl { Dock = DockStyle.Fill };
            templateControl.TemplateSelected += TemplateControl_TemplateSelected;
            templateControl.ResetRequested += TemplateControl_ResetRequested;
            templateControl.ApplyRequested += TemplateControl_ApplyRequested;
            templateControl.CancelRequested += CancelTemplateSession;
            templateCaretBlinkTimer.Tick += TemplateCaretBlinkTimer_Tick;

            communityFilterControl = new CommunityFilterControl { Dock = DockStyle.Fill };
            communityFilterControl.FilterSelected += CommunityFilterControl_FilterSelected;
            communityFilterControl.ResetRequested += CommunityFilterControl_ResetRequested;
            communityFilterControl.StickerSizeChanged += CommunityFilterControl_SizeChanged;
            communityFilterControl.RotationChanged += CommunityFilterControl_RotationChanged;
            communityFilterControl.AutoFitChanged += CommunityFilterControl_AutoFitChanged;
            communityFilterControl.ApplyRequested += CommunityFilterControl_ApplyRequested;
            communityFilterControl.CancelRequested += CommunityFilterControl_CancelRequested;
            communityRenderTimer.Tick += CommunityRenderTimer_Tick;

            menuCommunityFilter.Click -= MenuCommunityFilter_Click;
            menuCommunityFilter.Click += MenuCommunityFilter_Click;

            picCanvas.MouseDown += CommunityCanvas_MouseDown;
            picCanvas.MouseMove += CommunityCanvas_MouseMove;
            picCanvas.MouseUp += CommunityCanvas_MouseUp;
        }

private void OpenTemplatePanel()
{
    if (editor.State.CurrentImage == null || templateControl == null)
    {
        MessageBox.Show("먼저 이미지를 불러와주세요.");
        return;
    }

    StopInteractiveTools();
    CancelTemplateSession();

    templateBaseImage = new Bitmap(editor.State.CurrentImage);
    templateSnapshotSaved = false;
    templateTextDirty = false;
    templateControl.SetApplyEnabled(false);
    ShowToolControl(templateControl);
}

private void TemplateControl_TemplateSelected(string templateName)
{
    if (templateBaseImage == null)
        return;

    ClearTemplateTextLayers();

    Bitmap result = templateName switch
    {
        "Polaroid" => ApplyPolaroidTemplate(templateBaseImage),
        "Film" => ApplyFilmTemplate(templateBaseImage),
        "Magazine" => ApplyMagazineTemplate(templateBaseImage),
        "PhotoCard" => ApplyPhotoCardTemplate(templateBaseImage),
        "Instagram" => ApplyInstagramTemplate(templateBaseImage),
        "Birthday" => ApplyBirthdayTemplate(templateBaseImage),
        "Postcard" => ApplyPostcardTemplate(templateBaseImage),
        "MinimalPoster" => ApplyMinimalPosterTemplate(templateBaseImage),
        _ => new Bitmap(templateBaseImage)
    };

    templatePreviewImage?.Dispose();
    templatePreviewImage = result;
    float previewZoom = CanvasViewCalculator.GetFitZoom(
        templatePreviewImage.Size,
        pnlCanvas.ClientSize);
    RefreshCanvas(templatePreviewImage, previewZoom);
    AddTemplateTextLayers(templateName, picCanvas.ClientSize);
    templateTextDirty = true;
    templateControl?.SetApplyEnabled(true);
}

private void TemplateControl_ResetRequested()
{
    if (templateBaseImage == null)
        return;

    ClearTemplateTextLayers();
    templatePreviewImage?.Dispose();
    templatePreviewImage = null;
    RefreshCanvas();
    templateTextDirty = false;
    templateControl?.SetApplyEnabled(false);
}

private void TemplateControl_ApplyRequested()
{
    if (templatePreviewImage == null)
        return;

    Bitmap committed = templateTextLabels.Count > 0
        ? RenderTemplateLabelsToBitmap(templatePreviewImage)
        : new Bitmap(templatePreviewImage);
    editor.PushUndoSnapshot();
    editor.State.ReplaceCurrentImage(committed);
    editor.InitializeLayers();
    RefreshAfterImageChange();
    EndTemplateSession();
}

private void EnsureTemplateUndoSnapshot()
{
    if (templateSnapshotSaved)
        return;

    editor.PushUndoSnapshot();
    templateSnapshotSaved = true;
}

private void FinishTemplateSession()
{
    if (templateTextLabels.Count > 0 && editor.State.CurrentImage != null)
    {
        Bitmap committed = RenderTemplateLabelsToBitmap(editor.State.CurrentImage);
        editor.State.ReplaceCurrentImage(committed);
        editor.InitializeLayers();

        if (templateTextDirty)
        {
            RefreshCanvas();
            RefreshLayerUI();
            RecordTimelineSnapshot();
            UpdateUndoRedoMenuState();
        }
    }

    ClearTemplateTextLayers();
    templateBaseImage?.Dispose();
    templateBaseImage = null;
    templateSnapshotSaved = false;
    templateTextDirty = false;
    templateIsDragging = false;
}

private void EndTemplateSession()
{
    ClearTemplateTextLayers();
    templateBaseImage?.Dispose();
    templateBaseImage = null;
    templatePreviewImage?.Dispose();
    templatePreviewImage = null;
    templateSnapshotSaved = false;
    templateTextDirty = false;
    templateIsDragging = false;
    templateControl?.SetApplyEnabled(false);
}

// 도구 전환/패널 닫기에서는 템플릿 선택 결과를 자동 확정하지 않는다.
// 현재 구현은 템플릿을 즉시 State에 렌더링하므로, 기준 스냅샷으로 복구한다.
private void CancelTemplateSession()
{
    if (templateBaseImage == null)
        return;

    EndTemplateSession();
    RefreshCanvas();
    RefreshLayerUI();
    UpdateUndoRedoMenuState();
}

private static Bitmap ApplyPolaroidTemplate(Bitmap original)
{
    int side = Math.Max(20, original.Width / 24);
    int top = side;
    int bottom = Math.Max(80, original.Height / 7);

    Bitmap result = new Bitmap(
        original.Width + side * 2,
        original.Height + top + bottom,
        PixelFormat.Format32bppArgb);

    using Graphics graphics = Graphics.FromImage(result);
    graphics.Clear(Color.White);
    graphics.DrawImage(original, side, top, original.Width, original.Height);
    return result;
}

private static Bitmap ApplyFilmTemplate(Bitmap original)
{
    int border = Math.Max(35, Math.Min(original.Width, original.Height) / 16);
    Bitmap result = new Bitmap(
        original.Width + border * 2,
        original.Height + border * 2,
        PixelFormat.Format32bppArgb);

    using Graphics graphics = Graphics.FromImage(result);
    graphics.Clear(Color.Black);
    graphics.DrawImage(original, border, border, original.Width, original.Height);

    using Pen perforationPen = new Pen(Color.White, Math.Max(1f, result.Width / 400f));
    int holeWidth = Math.Max(10, border / 3);
    int holeHeight = Math.Max(14, border / 2);
    int step = holeWidth + Math.Max(8, border / 6);
    for (int x = border / 4; x < result.Width - border / 4 - holeWidth; x += step)
    {
        graphics.DrawRectangle(perforationPen, x, Math.Max(6, border / 6), holeWidth, holeHeight);
        graphics.DrawRectangle(perforationPen, x, result.Height - Math.Max(6, border / 6) - holeHeight, holeWidth, holeHeight);
    }

    return result;
}

private static Bitmap ApplyMagazineTemplate(Bitmap original)
{
    Bitmap result = new Bitmap(original);
    using Graphics graphics = Graphics.FromImage(result);
    using SolidBrush shade = new SolidBrush(Color.FromArgb(80, 0, 0, 0));
    graphics.FillRectangle(shade, 0, 0, result.Width, Math.Min(125, result.Height));
    graphics.FillRectangle(shade, 0, Math.Max(0, result.Height - 85), result.Width, 85);
    return result;
}

private static Bitmap ApplyPhotoCardTemplate(Bitmap original)
{
    int border = Math.Max(25, original.Width / 30);
    int bottom = Math.Max(100, original.Height / 6);
    Bitmap result = new Bitmap(
        original.Width + border * 2,
        original.Height + border + bottom,
        PixelFormat.Format32bppArgb);

    using Graphics graphics = Graphics.FromImage(result);
    graphics.Clear(Color.FromArgb(245, 245, 245));
    graphics.DrawImage(original, border, border, original.Width, original.Height);
    return result;
}

private static Bitmap ApplyInstagramTemplate(Bitmap original)
{
    int size = Math.Max(original.Width, original.Height);
    int padding = Math.Max(32, size / 20);
    int header = Math.Max(45, size / 14);
    int footer = Math.Max(70, size / 10);
    Bitmap result = new Bitmap(size + padding * 2, size + padding * 2 + header + footer, PixelFormat.Format32bppArgb);

    using Graphics graphics = Graphics.FromImage(result);
    graphics.Clear(Color.White);

    Rectangle imageArea = new Rectangle(padding, padding + header, size, size);
    graphics.FillRectangle(Brushes.Gainsboro, imageArea);
    float scale = Math.Min((float)size / original.Width, (float)size / original.Height);
    int drawWidth = (int)(original.Width * scale);
    int drawHeight = (int)(original.Height * scale);
    int drawX = imageArea.X + (size - drawWidth) / 2;
    int drawY = imageArea.Y + (size - drawHeight) / 2;
    graphics.DrawImage(original, drawX, drawY, drawWidth, drawHeight);

    using Pen borderPen = new Pen(Color.FromArgb(225, 225, 230), 2);
    graphics.DrawRectangle(borderPen, imageArea);
    return result;
}

private static Bitmap ApplyBirthdayTemplate(Bitmap original)
{
    int border = Math.Max(40, original.Width / 20);
    int top = Math.Max(100, original.Height / 5);
    int bottom = Math.Max(80, original.Height / 8);
    Bitmap result = new Bitmap(original.Width + border * 2, original.Height + top + bottom, PixelFormat.Format32bppArgb);

    using Graphics graphics = Graphics.FromImage(result);
    graphics.Clear(Color.FromArgb(255, 245, 248));
    graphics.DrawImage(original, border, top, original.Width, original.Height);

    using Pen accent = new Pen(Color.FromArgb(230, 170, 190), 4);
    graphics.DrawRectangle(accent, 18, 18, result.Width - 36, result.Height - 36);
    using SolidBrush dot = new SolidBrush(Color.FromArgb(245, 190, 205));
    for (int x = 35; x < result.Width; x += 70)
    {
        graphics.FillEllipse(dot, x, Math.Max(40, top / 2), 10, 10);
    }

    return result;
}

private static Bitmap ApplyPostcardTemplate(Bitmap original)
{
    int border = Math.Max(30, original.Width / 25);
    int bottom = Math.Max(90, original.Height / 7);
    Bitmap result = new Bitmap(original.Width + border * 2, original.Height + border * 2 + bottom, PixelFormat.Format32bppArgb);

    using Graphics graphics = Graphics.FromImage(result);
    graphics.Clear(Color.FromArgb(249, 246, 238));
    graphics.DrawImage(original, border, border, original.Width, original.Height);
    using Pen line = new Pen(Color.FromArgb(175, 165, 145), 2);
    int lineY = original.Height + border + Math.Max(28, bottom / 3);
    graphics.DrawLine(line, result.Width / 2, lineY, result.Width / 2, result.Height - Math.Max(20, bottom / 5));
    return result;
}

private static Bitmap ApplyMinimalPosterTemplate(Bitmap original)
{
    int side = Math.Max(50, original.Width / 18);
    int top = Math.Max(40, original.Height / 16);
    int bottom = Math.Max(150, original.Height / 4);
    Bitmap result = new Bitmap(original.Width + side * 2, original.Height + top + bottom, PixelFormat.Format32bppArgb);

    using Graphics graphics = Graphics.FromImage(result);
    graphics.Clear(Color.White);
    graphics.DrawImage(original, side, top, original.Width, original.Height);
    using Pen line = new Pen(Color.Black, 3);
    graphics.DrawLine(line, side, original.Height + top + Math.Max(60, bottom / 2), side + Math.Min(180, original.Width / 2), original.Height + top + Math.Max(60, bottom / 2));
    return result;
}

private void AddTemplateTextLayers(string templateName, Size canvasSize)
{
    switch (templateName)
    {
        case "Polaroid":
            AddTemplateText("My Memory", new Point(60, canvasSize.Height - 90), 22, FontStyle.Bold, Color.Black);
            break;
        case "Film":
            AddTemplateText("35mm  •  01", new Point(60, 17), 15, FontStyle.Bold, Color.White);
            AddTemplateText("IMAGE EDITOR", new Point(60, canvasSize.Height - 45), 15, FontStyle.Bold, Color.White);
            break;
        case "Magazine":
            AddTemplateText("MOMENT", new Point(30, 25), 42, FontStyle.Bold, Color.White);
            AddTemplateText("YOUR DAILY PHOTO", new Point(35, 82), 15, FontStyle.Regular, Color.White);
            AddTemplateText("SPECIAL EDITION", new Point(35, canvasSize.Height - 58), 15, FontStyle.Regular, Color.White);
            break;
        case "PhotoCard":
            AddTemplateText("PHOTO CARD", new Point(35, canvasSize.Height - 105), 24, FontStyle.Bold, Color.Black);
            AddTemplateText(DateTime.Now.ToString("yyyy.MM.dd"), new Point(35, canvasSize.Height - 65), 13, FontStyle.Regular, Color.Gray);
            break;
        case "Instagram":
            AddTemplateText("@YOURNAME", new Point(45, 18), 16, FontStyle.Bold, Color.FromArgb(45, 45, 50));
            AddTemplateText("TRAVEL · AUGUST 2026", new Point(45, canvasSize.Height - 52), 15, FontStyle.Bold, Color.FromArgb(55, 55, 60));
            break;
        case "Birthday":
            AddTemplateText("HAPPY BIRTHDAY!", new Point(55, 43), 28, FontStyle.Bold, Color.FromArgb(180, 80, 115));
            AddTemplateText("Make a Wish ♥", new Point(60, canvasSize.Height - 72), 18, FontStyle.Italic, Color.FromArgb(155, 85, 110));
            break;
        case "Postcard":
            AddTemplateText("SUMMER TRIP", new Point(48, canvasSize.Height - 92), 22, FontStyle.Bold, Color.FromArgb(55, 55, 50));
            AddTemplateText("SEOUL  ·  2026", new Point(canvasSize.Width / 2 + 25, canvasSize.Height - 78), 14, FontStyle.Regular, Color.FromArgb(100, 95, 85));
            break;
        case "MinimalPoster":
            AddTemplateText("MOMENT", new Point(70, canvasSize.Height - 145), 30, FontStyle.Bold, Color.Black);
            AddTemplateText("Capture your day", new Point(70, canvasSize.Height - 72), 14, FontStyle.Regular, Color.FromArgb(90, 90, 90));
            break;
    }
}

private void AddTemplateText(string text, Point location, float fontSize, FontStyle style, Color color)
{
    Label label = new Label
    {
        AutoSize = true,
        Text = text,
        Location = location,
        BackColor = Color.Transparent,
        ForeColor = color,
        Font = new Font("맑은 고딕", fontSize, style),
        Cursor = Cursors.SizeAll,
        Padding = new Padding(3)
    };

    label.MouseDown += TemplateLabel_MouseDown;
    label.MouseMove += TemplateLabel_MouseMove;
    label.MouseUp += TemplateLabel_MouseUp;
    label.MouseWheel += TemplateLabel_MouseWheel;
    label.MouseDoubleClick += TemplateLabel_MouseDoubleClick;
    label.Paint += TemplateLabel_Paint;
    label.MouseEnter += (s, e) => label.Focus();

    picCanvas.Controls.Add(label);
    label.BringToFront();
    templateTextLabels.Add(label);
}

private void TemplateLabel_MouseDown(object? sender, MouseEventArgs e)
{
    if (e.Button != MouseButtons.Left || sender is not Label label || templateEditingLabel != null)
        return;

    draggingTemplateLabel = label;
    templateDragOffset = e.Location;
    templateIsDragging = false;
    label.Capture = true;
    label.BringToFront();
}

private void TemplateLabel_MouseMove(object? sender, MouseEventArgs e)
{
    if (draggingTemplateLabel == null || sender != draggingTemplateLabel)
        return;

    Point currentMouse = picCanvas.PointToClient(Control.MousePosition);
    int newX = currentMouse.X - templateDragOffset.X;
    int newY = currentMouse.Y - templateDragOffset.Y;

    newX = Math.Max(0, Math.Min(newX, Math.Max(0, picCanvas.ClientSize.Width - draggingTemplateLabel.Width)));
    newY = Math.Max(0, Math.Min(newY, Math.Max(0, picCanvas.ClientSize.Height - draggingTemplateLabel.Height)));

    if (!templateIsDragging && (Math.Abs(newX - draggingTemplateLabel.Left) >= 3 || Math.Abs(newY - draggingTemplateLabel.Top) >= 3))
    {
        templateIsDragging = true;
        draggingTemplateLabel.BorderStyle = BorderStyle.FixedSingle;
    }

    draggingTemplateLabel.Location = new Point(newX, newY);
}

private void TemplateLabel_MouseUp(object? sender, MouseEventArgs e)
{
    if (e.Button != MouseButtons.Left || sender is not Label label)
        return;

    label.Capture = false;
    if (templateIsDragging)
    {
        templateTextDirty = true;
    }

    label.BorderStyle = BorderStyle.None;
    draggingTemplateLabel = null;
    templateIsDragging = false;
}

private void TemplateLabel_MouseDoubleClick(object? sender, MouseEventArgs e)
{
    if (e.Button != MouseButtons.Left || sender is not Label label)
        return;

    BeginTemplateInlineEdit(label, e.Location);
}

private void BeginTemplateInlineEdit(Label label, Point clickLocation)
{
    EndTemplateInlineEdit(true);

    templateEditingLabel = label;
    templateOriginalText = label.Text;
    label.Cursor = Cursors.IBeam;

    // 사진 위의 흰 TextBox를 표시하지 않고, 실제 입력·한글 IME 처리는 작은 보조
    // TextBox가 담당한다. 보이는 텍스트와 커서는 Label이 직접 표시한다.
    templateInlineEditor = new TextBox
    {
        Location = new Point(0, 0),
        Size = new Size(1, 1),
        Multiline = true,
        BorderStyle = BorderStyle.None,
        BackColor = picCanvas.BackColor == Color.Transparent ? Color.White : picCanvas.BackColor,
        ForeColor = picCanvas.BackColor == Color.Transparent ? Color.White : picCanvas.BackColor,
        Font = new Font(label.Font.FontFamily, label.Font.Size, label.Font.Style),
        Text = label.Text
    };

    templateInlineEditor.TextChanged += (_, _) =>
    {
        if (templateEditingLabel == null || templateInlineEditor == null)
            return;

        templateEditingLabel.Text = templateInlineEditor.Text;
        templateTextDirty = true;
        templateEditingLabel.Invalidate();
    };
    templateInlineEditor.KeyDown += TemplateInlineEditor_KeyDown;
    templateInlineEditor.KeyUp += (_, _) => templateEditingLabel?.Invalidate();
    templateInlineEditor.LostFocus += (_, _) => EndTemplateInlineEdit(true);

    picCanvas.Controls.Add(templateInlineEditor);
    templateInlineEditor.BringToFront();
    templateInlineEditor.Focus();
    templateInlineEditor.SelectionStart = GetTemplateInsertionIndex(label, clickLocation);
    templateInlineEditor.SelectionLength = 0;
    templateCaretVisible = true;
    templateCaretBlinkTimer.Start();
    label.Invalidate();
}

private void TemplateInlineEditor_KeyDown(object? sender, KeyEventArgs e)
{
    if (e.KeyCode == Keys.Escape)
    {
        EndTemplateInlineEdit(false);
        e.SuppressKeyPress = true;
    }
    else if (e.KeyCode == Keys.Enter && e.Control)
    {
        EndTemplateInlineEdit(true);
        e.SuppressKeyPress = true;
    }
}

private void EndTemplateInlineEdit(bool keepText)
{
    if (templateInlineEditor == null || templateEditingLabel == null)
        return;

    TextBox editor = templateInlineEditor;
    Label label = templateEditingLabel;
    templateInlineEditor = null;
    templateEditingLabel = null;
    templateCaretBlinkTimer.Stop();
    templateCaretVisible = false;
    label.Text = keepText ? editor.Text : templateOriginalText ?? label.Text;
    label.Cursor = Cursors.SizeAll;
    templateOriginalText = null;
    picCanvas.Controls.Remove(editor);
    editor.Dispose();
    label.Invalidate();
}

private void TemplateCaretBlinkTimer_Tick(object? sender, EventArgs e)
{
    if (templateEditingLabel == null || templateInlineEditor == null)
    {
        templateCaretBlinkTimer.Stop();
        return;
    }

    templateCaretVisible = !templateCaretVisible;
    templateEditingLabel.Invalidate();
}

private static int GetTemplateInsertionIndex(Label label, Point clickLocation)
{
    using Graphics graphics = label.CreateGraphics();
    float localX = clickLocation.X - label.Padding.Left;
    int index = 0;
    float previousWidth = 0f;

    for (int i = 1; i <= label.Text.Length; i++)
    {
        float width = graphics.MeasureString(label.Text.Substring(0, i), label.Font).Width;
        if (localX < (previousWidth + width) / 2f)
            break;

        index = i;
        previousWidth = width;
    }

    return index;
}

private void TemplateLabel_Paint(object? sender, PaintEventArgs e)
{
    if (!templateCaretVisible || sender is not Label label || label != templateEditingLabel || templateInlineEditor == null)
        return;

    int caretIndex = Math.Max(0, Math.Min(label.Text.Length, templateInlineEditor.SelectionStart));
    string beforeCaret = label.Text.Substring(0, caretIndex);
    float x = label.Padding.Left + e.Graphics.MeasureString(beforeCaret, label.Font).Width;
    float top = label.Padding.Top + 2f;
    float height = Math.Max(16f, label.Font.GetHeight(e.Graphics));
    using Pen pen = new Pen(Color.FromArgb(30, 100, 220), 2f);
    e.Graphics.DrawLine(pen, x, top, x, top + height);
}

private void TemplateLabel_MouseWheel(object? sender, MouseEventArgs e)
{
    if (sender is not Label label)
        return;

    if ((ModifierKeys & Keys.Control) != Keys.Control)
        return;

    float newSize = e.Delta > 0 ? label.Font.Size + 2f : label.Font.Size - 2f;
    newSize = Math.Max(8f, Math.Min(100f, newSize));
    label.Font = new Font(label.Font.FontFamily, newSize, label.Font.Style);
    templateTextDirty = true;
}

private void ClearTemplateTextLayers()
{
    EndTemplateInlineEdit(false);

    foreach (Label label in templateTextLabels)
    {
        picCanvas.Controls.Remove(label);
        label.Dispose();
    }

    templateTextLabels.Clear();
    draggingTemplateLabel = null;
}

private Bitmap RenderTemplateLabelsToBitmap(Bitmap baseImage)
{
    Bitmap committed = new Bitmap(baseImage);
    if (picCanvas.ClientSize.Width <= 0 || picCanvas.ClientSize.Height <= 0)
        return committed;

    float scaleX = (float)committed.Width / picCanvas.ClientSize.Width;
    float scaleY = (float)committed.Height / picCanvas.ClientSize.Height;
    float fontScale = (scaleX + scaleY) / 2f;

    using Graphics graphics = Graphics.FromImage(committed);
    graphics.SmoothingMode = SmoothingMode.AntiAlias;
    graphics.InterpolationMode = InterpolationMode.HighQualityBicubic;
    graphics.TextRenderingHint = TextRenderingHint.AntiAliasGridFit;

    foreach (Label label in templateTextLabels)
    {
        using Font drawFont = new Font(label.Font.FontFamily, Math.Max(1f, label.Font.Size * fontScale), label.Font.Style);
        using SolidBrush brush = new SolidBrush(label.ForeColor);
        // Label의 화면상 텍스트 시작점은 Padding 안쪽이므로, 저장할 때도
        // 같은 여백을 반영해 미리보기와 저장 결과의 위치를 맞춘다.
        float x = (label.Left + label.Padding.Left) * scaleX;
        float y = (label.Top + label.Padding.Top) * scaleY;
        graphics.DrawString(label.Text, drawFont, brush, x, y);
    }

    return committed;
}

        private void MenuCommunityFilter_Click(object? sender, EventArgs e)
        {
            if (editor.State.CurrentImage == null || communityFilterControl == null)
            {
                MessageBox.Show("먼저 이미지를 불러와주세요.");
                return;
            }

            StopInteractiveTools();

            communityBaseImage?.Dispose();
            communityBaseImage = new Bitmap(editor.State.CurrentImage);
            DisposeOwnedCommunityOverlay();
            communityOverlay = null;
            communitySessionActive = true;
            communitySessionDirty = false;
            communitySnapshotSaved = false;
            communityDragging = false;
            communityOverlayScale = 0.7f;
            communityOverlayRotation = 0f;
            communitySelectedFilterName = null;
            communityDetectedFaces = null;
            communityFaceDetectionCancellation?.Cancel();
            communityFaceDetectionCancellation = null;
            communityPreviewImage?.Dispose();
            communityPreviewImage = null;
            communityFilterControl.SetApplyEnabled(false);
            communityFilterControl.SetOverlayTransformValues(0.7f, 0f);
            communityFilterControl.SetAutoFitBusy(
                false,
                CurrentUser.UserNo <= 0
                    ? "얼굴 자동 맞춤은 로그인 후 사용할 수 있습니다."
                    : communityFilterControl.AutoFitEnabled
                    ? "스티커 선택 시 얼굴에 자동으로 맞춥니다."
                    : "자동 맞춤 꺼짐 · 직접 배치할 수 있습니다.");

            ShowToolControl(communityFilterControl);
        }

        private async void CommunityFilterControl_FilterSelected(string filterName)
        {
            if (communityBaseImage == null || communityFilterControl == null)
                return;

            switch (filterName)
            {
                case "DogLens":
                    SetCommunityOverlay(communityFilterControl.DogImage);
                    SetupCommunityOverlay();
                    await AutoFitSelectedCommunityOverlayAsync(filterName);
                    return;

                case "FlowerCrown":
                    SetCommunityOverlay(communityFilterControl.FlowerImage);
                    SetupCommunityOverlay();
                    await AutoFitSelectedCommunityOverlayAsync(filterName);
                    return;

                case "CatFace":
                    SetCommunityOverlay(communityFilterControl.CatImage);
                    SetupCommunityOverlay();
                    await AutoFitSelectedCommunityOverlayAsync(filterName);
                    return;

                case "BunnyFace":
                    SetCommunityOverlay(communityFilterControl.BunnyImage);
                    SetupCommunityOverlay();
                    await AutoFitSelectedCommunityOverlayAsync(filterName);
                    return;

                case "PrincessCrown":
                    SetCommunityOverlay(communityFilterControl.PrincessCrownImage);
                    SetupCommunityOverlay();
                    await AutoFitSelectedCommunityOverlayAsync(filterName);
                    return;

                case "AngelHalo":
                    SetCommunityOverlay(communityFilterControl.AngelHaloImage);
                    SetupCommunityOverlay();
                    await AutoFitSelectedCommunityOverlayAsync(filterName);
                    return;

                case "DevilHorns":
                    SetCommunityOverlay(communityFilterControl.DevilHornsImage);
                    SetupCommunityOverlay();
                    await AutoFitSelectedCommunityOverlayAsync(filterName);
                    return;
            }

            communitySelectedFilterName = filterName;
            communityFaceDetectionCancellation?.Cancel();
            communityFaceDetectionCancellation = null;
            communityFilterControl.SetAutoFitBusy(false, "스티커 미리보기를 확인하세요.");
            DisposeOwnedCommunityOverlay();
            communityOverlay = null;

            Bitmap result = filterName switch
            {
                "BoldGlam" => BasicImageProcessor.Adjust(communityBaseImage, 10, 20, 20, 5),
                "CuteHeart" => ApplyCuteHeart(communityBaseImage),
                "SoftGlam" => BasicImageProcessor.Adjust(communityBaseImage, 10, -5, -5, 10),
                "RetroStory" => BasicImageProcessor.Adjust(communityBaseImage, 0, 10, -15, 25),
                "KBeauty" => BasicImageProcessor.Adjust(communityBaseImage, 15, -5, 10, 8),
                _ => new Bitmap(communityBaseImage)
            };

            communityPreviewImage?.Dispose();
            communityPreviewImage = result;
            communitySessionDirty = true;
            communityFilterControl.SetApplyEnabled(true);
            RefreshCanvas(communityPreviewImage);
        }

        private static Bitmap ApplyCuteHeart(Bitmap original)
        {
            Bitmap result = new Bitmap(original);
            using Graphics graphics = Graphics.FromImage(result);
            graphics.SmoothingMode = SmoothingMode.AntiAlias;

            float fontSize = Math.Max(24f, result.Width / 16f);
            using Font heartFont = new Font("Segoe UI Symbol", fontSize, FontStyle.Bold);
            using SolidBrush hotPink = new SolidBrush(Color.FromArgb(220, Color.HotPink));
            using SolidBrush pink = new SolidBrush(Color.FromArgb(190, Color.Pink));

            graphics.DrawString("♥", heartFont, hotPink, result.Width * 0.12f, result.Height * 0.16f);
            graphics.DrawString("♥", heartFont, hotPink, result.Width * 0.76f, result.Height * 0.22f);
            graphics.DrawString("♥", heartFont, pink, result.Width * 0.07f, result.Height * 0.58f);
            graphics.DrawString("♥", heartFont, pink, result.Width * 0.80f, result.Height * 0.62f);
            return result;
        }

        private void SetupCommunityOverlay()
        {
            if (communityBaseImage == null || communityOverlay == null)
                return;

            communityOverlayScale = 0.7f;
            communityOverlayRotation = 0f;

            SizeF overlaySize = GetCommunityOverlaySize();
            communityOverlayX = (communityBaseImage.Width - overlaySize.Width) / 2f;
            communityOverlayY = (communityBaseImage.Height - overlaySize.Height) / 2f;
            RenderCommunityOverlay();
        }

        private void SetCommunityOverlay(Image overlay)
        {
            DisposeOwnedCommunityOverlay();
            communityOverlay = overlay;
        }

        private void DisposeOwnedCommunityOverlay()
        {
            if (ReferenceEquals(communityOverlay, communityOwnedOverlay))
                communityOverlay = null;

            communityOwnedOverlay?.Dispose();
            communityOwnedOverlay = null;
        }

        private async Task AutoFitSelectedCommunityOverlayAsync(string filterName)
        {
            communitySelectedFilterName = filterName;
            if (communityFilterControl?.AutoFitEnabled == true)
            {
                await AutoFitCommunityOverlayAsync(filterName);
                return;
            }

            communityFaceDetectionCancellation?.Cancel();
            communityFilterControl?.SetAutoFitBusy(
                false,
                "자동 맞춤 꺼짐 · 드래그로 배치하세요.");
        }

        private async void CommunityFilterControl_AutoFitChanged(bool enabled)
        {
            if (communityFilterControl == null)
                return;

            if (!enabled)
            {
                communityFaceDetectionCancellation?.Cancel();
                communityFilterControl.SetAutoFitBusy(
                    false,
                    communityOverlay == null
                        ? "자동 맞춤 꺼짐 · 직접 배치할 수 있습니다."
                        : "자동 맞춤 꺼짐 · 드래그로 배치하세요.");
                return;
            }

            if (!communitySessionActive || communityOverlay == null ||
                string.IsNullOrEmpty(communitySelectedFilterName))
            {
                communityFilterControl.SetAutoFitStatus(
                    "스티커 선택 시 얼굴에 자동으로 맞춥니다.");
                return;
            }

            await AutoFitCommunityOverlayAsync(communitySelectedFilterName);
        }

        private async Task AutoFitCommunityOverlayAsync(string filterName)
        {
            if (!communitySessionActive || communityBaseImage == null ||
                communityOverlay == null || communityFilterControl == null ||
                !communityFilterControl.AutoFitEnabled)
            {
                return;
            }

            if (communityDetectedFaces != null)
            {
                if (communityDetectedFaces.Count == 0)
                {
                    communityFilterControl.SetAutoFitStatus(
                        "얼굴 없음 · 수동으로 위치를 조정하세요.",
                        true);
                    return;
                }

                ApplyCommunityFacePlacement(filterName, communityDetectedFaces[0]);
                communityFilterControl.SetAutoFitStatus(
                    communityDetectedFaces.Count > 1
                        ? $"얼굴 {communityDetectedFaces.Count}개 중 가장 큰 얼굴에 맞췄습니다."
                        : "얼굴에 맞췄습니다. 필요하면 조정하세요.");
                return;
            }

            communitySelectedFilterName = filterName;
            communityFaceDetectionCancellation?.Cancel();
            CancellationTokenSource requestCancellation =
                new CancellationTokenSource();
            communityFaceDetectionCancellation = requestCancellation;
            CancellationToken cancellationToken = requestCancellation.Token;
            string finalMessage = "얼굴에 맞췄습니다. 필요하면 조정하세요.";
            bool isError = false;

            communityFilterControl.SetAutoFitBusy(
                true,
                "얼굴 위치를 자동으로 맞추는 중...");

            using Bitmap sourceImage = new Bitmap(communityBaseImage);
            try
            {
                IReadOnlyList<CommunityFacePlacement> faces =
                    await CommunityFaceDetectionService.DetectAsync(
                        sourceImage,
                        cancellationToken);
                if (cancellationToken.IsCancellationRequested ||
                    !communitySessionActive ||
                    !communityFilterControl.AutoFitEnabled ||
                    communitySelectedFilterName != filterName)
                {
                    return;
                }

                communityDetectedFaces = faces;

                if (faces.Count == 0)
                {
                    finalMessage =
                        "얼굴 없음 · 수동으로 위치를 조정하세요.";
                    isError = true;
                    return;
                }

                ApplyCommunityFacePlacement(filterName, faces[0]);
                if (faces.Count > 1)
                {
                    finalMessage =
                        $"얼굴 {faces.Count}개 중 가장 큰 얼굴에 맞췄습니다.";
                }
            }
            catch (OperationCanceledException)
            {
                finalMessage = communityFilterControl?.AutoFitEnabled == false
                    ? "자동 맞춤 꺼짐 · 드래그로 배치하세요."
                    : "얼굴 자동 맞춤을 취소했습니다.";
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine(
                    $"Community face auto-fit failed: {ex}");
                finalMessage =
                    "자동 맞춤 실패 · 수동으로 조정하세요.";
                isError = true;
            }
            finally
            {
                if (ReferenceEquals(
                        communityFaceDetectionCancellation,
                        requestCancellation))
                {
                    communityFaceDetectionCancellation = null;
                    communityFilterControl?.SetAutoFitBusy(
                        false,
                        finalMessage,
                        isError);
                }

                requestCancellation.Dispose();
            }
        }

        private void ApplyCommunityFacePlacement(
            string filterName,
            CommunityFacePlacement face)
        {
            if (communityBaseImage == null || communityOverlay == null)
                return;

            if (communityFilterControl?.TryGetSplitFilterImages(
                    filterName,
                    out Image earImage,
                    out Image noseImage) == true)
            {
                ApplySplitCommunityFacePlacement(earImage, noseImage, face);
                return;
            }

            float widthFactor = filterName switch
            {
                "PrincessCrown" => 1.10f,
                "DevilHorns" => 1.15f,
                "AngelHalo" => 1.20f,
                "FlowerCrown" => 1.40f,
                _ => 1.40f
            };
            float desiredWidth = Math.Max(1f, face.Width * widthFactor);
            communityOverlayScale = Math.Max(
                0.05f,
                Math.Min(1.5f, desiredWidth / communityBaseImage.Width));
            communityOverlayRotation = Math.Max(
                -45f,
                Math.Min(45f, face.Rotation));

            SizeF overlaySize = GetCommunityOverlaySize();
            float faceCenterX = face.X + face.Width / 2f;

            if (filterName == "DogLens" || filterName == "CatFace" ||
                filterName == "BunnyFace")
            {
                float noseRatio = filterName switch
                {
                    "BunnyFace" => 0.65f,
                    _ => 0.76f
                };
                float overlayCenterX = faceCenterX * 0.45f + face.NoseX * 0.55f;
                communityOverlayX = overlayCenterX - overlaySize.Width / 2f;
                communityOverlayY = face.NoseY - overlaySize.Height * noseRatio;
            }
            else
            {
                float centerYOffset = filterName switch
                {
                    "AngelHalo" => -0.40f,
                    "PrincessCrown" => -0.30f,
                    "DevilHorns" => -0.30f,
                    _ => 0f
                };
                float overlayCenterY = face.Y + face.Height * centerYOffset;
                communityOverlayX = faceCenterX - overlaySize.Width / 2f;
                communityOverlayY = overlayCenterY - overlaySize.Height / 2f;
            }

            communityFilterControl?.SetOverlayTransformValues(
                communityOverlayScale,
                communityOverlayRotation);
            RenderCommunityOverlay();
        }

        private void ApplySplitCommunityFacePlacement(
            Image earImage,
            Image noseImage,
            CommunityFacePlacement face)
        {
            if (communityBaseImage == null)
                return;

            float maximumWidth = Math.Max(1f, communityBaseImage.Width * 3f);
            float earWidth = Math.Clamp(face.Width * 1.4f, 1f, maximumWidth);
            float earHeight = Math.Max(
                1f,
                earImage.Height * (earWidth / earImage.Width));
            float noseWidth = Math.Clamp(face.Width * 0.7f, 1f, maximumWidth);
            float noseHeight = Math.Max(
                1f,
                noseImage.Height * (noseWidth / noseImage.Width));

            float faceCenterX = face.X + face.Width / 2f;
            RectangleF earBounds = new RectangleF(
                faceCenterX - earWidth / 2f,
                face.Y - face.Height * 0.6f,
                earWidth,
                earHeight);
            RectangleF noseBounds = new RectangleF(
                face.NoseX - noseWidth / 2f,
                face.NoseY - noseHeight / 2f,
                noseWidth,
                noseHeight);

            float unionLeft = Math.Min(earBounds.Left, noseBounds.Left);
            float unionTop = Math.Min(earBounds.Top, noseBounds.Top);
            float unionRight = Math.Max(earBounds.Right, noseBounds.Right);
            float unionBottom = Math.Max(earBounds.Bottom, noseBounds.Bottom);
            int compositeWidth = Math.Max(
                1,
                (int)Math.Ceiling(unionRight - unionLeft));
            int compositeHeight = Math.Max(
                1,
                (int)Math.Ceiling(unionBottom - unionTop));

            Bitmap composite = new Bitmap(
                compositeWidth,
                compositeHeight,
                PixelFormat.Format32bppArgb);
            using (Graphics graphics = Graphics.FromImage(composite))
            {
                graphics.Clear(Color.Transparent);
                graphics.SmoothingMode = SmoothingMode.AntiAlias;
                graphics.InterpolationMode = InterpolationMode.HighQualityBicubic;
                graphics.PixelOffsetMode = PixelOffsetMode.HighQuality;
                graphics.DrawImage(
                    earImage,
                    earBounds.X - unionLeft,
                    earBounds.Y - unionTop,
                    earBounds.Width,
                    earBounds.Height);
                graphics.DrawImage(
                    noseImage,
                    noseBounds.X - unionLeft,
                    noseBounds.Y - unionTop,
                    noseBounds.Width,
                    noseBounds.Height);
            }

            DisposeOwnedCommunityOverlay();
            communityOwnedOverlay = composite;
            communityOverlay = composite;
            communityOverlayX = unionLeft;
            communityOverlayY = unionTop;
            communityOverlayScale = compositeWidth /
                (float)Math.Max(1, communityBaseImage.Width);
            communityOverlayRotation = Math.Clamp(face.Rotation, -45f, 45f);
            communityFilterControl?.SetOverlayTransformValues(
                communityOverlayScale,
                communityOverlayRotation);
            RenderCommunityOverlay();
        }

        private void QueueCommunityOverlayRender()
        {
            if (communityBaseImage == null || communityOverlay == null)
                return;

            communityRenderQueued = true;
            if (!communityRenderTimer.Enabled)
                communityRenderTimer.Start();
        }

        private void CommunityRenderTimer_Tick(object? sender, EventArgs e)
        {
            FlushCommunityOverlayRender();
        }

        private void FlushCommunityOverlayRender()
        {
            if (!communityRenderQueued)
            {
                communityRenderTimer.Stop();
                return;
            }

            communityRenderQueued = false;
            communityRenderTimer.Stop();
            RenderCommunityOverlay();
        }

        private SizeF GetCommunityOverlaySize()
        {
            if (communityBaseImage == null || communityOverlay == null)
                return SizeF.Empty;

            float width = Math.Max(1f, communityBaseImage.Width * communityOverlayScale);
            float height = Math.Max(1f, communityOverlay.Height * (width / communityOverlay.Width));
            return new SizeF(width, height);
        }

        private void RenderCommunityOverlay()
        {
            if (communityBaseImage == null || communityOverlay == null)
                return;

            Bitmap result = new Bitmap(
                communityBaseImage.Width,
                communityBaseImage.Height,
                PixelFormat.Format32bppArgb);

            using (Graphics graphics = Graphics.FromImage(result))
            {
                graphics.SmoothingMode = SmoothingMode.AntiAlias;
                graphics.InterpolationMode = InterpolationMode.HighQualityBicubic;
                graphics.DrawImageUnscaled(communityBaseImage, 0, 0);

                SizeF size = GetCommunityOverlaySize();
                float centerX = communityOverlayX + size.Width / 2f;
                float centerY = communityOverlayY + size.Height / 2f;

                GraphicsState state = graphics.Save();
                graphics.TranslateTransform(centerX, centerY);
                graphics.RotateTransform(communityOverlayRotation);
                graphics.DrawImage(
                    communityOverlay,
                    -size.Width / 2f,
                    -size.Height / 2f,
                    size.Width,
                    size.Height);
                graphics.Restore(state);
            }

            communityPreviewImage?.Dispose();
            communityPreviewImage = result;
            communitySessionDirty = true;
            communityFilterControl?.SetApplyEnabled(true);
            RefreshCanvas(communityPreviewImage);
        }

        private void CommunityFilterControl_SizeChanged(int value)
        {
            if (communityBaseImage == null || communityOverlay == null)
                return;

            SizeF oldSize = GetCommunityOverlaySize();
            float centerX = communityOverlayX + oldSize.Width / 2f;
            float centerY = communityOverlayY + oldSize.Height / 2f;

            communityOverlayScale = Math.Max(0.05f, value / 100f);
            SizeF newSize = GetCommunityOverlaySize();
            communityOverlayX = centerX - newSize.Width / 2f;
            communityOverlayY = centerY - newSize.Height / 2f;
            QueueCommunityOverlayRender();
        }

        private void CommunityFilterControl_RotationChanged(int value)
        {
            if (communityOverlay == null)
                return;

            communityOverlayRotation = value;
            QueueCommunityOverlayRender();
        }

        private void CommunityFilterControl_ResetRequested()
        {
            if (communityBaseImage == null)
                return;

            communityFaceDetectionCancellation?.Cancel();
            communityFaceDetectionCancellation = null;
            communitySelectedFilterName = null;
            communityDetectedFaces = null;
            DisposeOwnedCommunityOverlay();
            communityOverlay = null;
            communityRenderQueued = false;
            communityRenderTimer.Stop();
            communityDragging = false;
            communitySessionDirty = false;
            communityOverlayScale = 0.7f;
            communityOverlayRotation = 0f;
            communityPreviewImage?.Dispose();
            communityPreviewImage = null;
            RefreshCanvas();
            communityFilterControl?.SetApplyEnabled(false);
            communityFilterControl?.SetOverlayTransformValues(0.7f, 0f);
            communityFilterControl?.SetAutoFitBusy(
                false,
                communityFilterControl.AutoFitEnabled
                    ? "스티커 선택 시 얼굴에 자동으로 맞춥니다."
                    : "자동 맞춤 꺼짐 · 직접 배치할 수 있습니다.");
        }

        private void CommunityCanvas_MouseDown(object? sender, MouseEventArgs e)
        {
            if (!communitySessionActive || communityBaseImage == null || communityOverlay == null)
                return;

            if (e.Button != MouseButtons.Left)
                return;

            Point imagePoint = ConvertToImagePoint(e.Location);
            SizeF size = GetCommunityOverlaySize();
            RectangleF bounds = new RectangleF(
                communityOverlayX,
                communityOverlayY,
                size.Width,
                size.Height);

            if (!bounds.Contains(imagePoint))
                return;

            communityDragging = true;
            communityDragStart = e.Location;
            picCanvas.Cursor = Cursors.SizeAll;
        }

        private void CommunityCanvas_MouseMove(object? sender, MouseEventArgs e)
        {
            if (!communityDragging || communityBaseImage == null || communityOverlay == null)
                return;

            float scaleX = (float)communityBaseImage.Width / Math.Max(1, picCanvas.ClientSize.Width);
            float scaleY = (float)communityBaseImage.Height / Math.Max(1, picCanvas.ClientSize.Height);

            communityOverlayX += (e.X - communityDragStart.X) * scaleX;
            communityOverlayY += (e.Y - communityDragStart.Y) * scaleY;
            communityDragStart = e.Location;
            QueueCommunityOverlayRender();
        }

        private void CommunityCanvas_MouseUp(object? sender, MouseEventArgs e)
        {
            if (!communityDragging)
                return;

            communityDragging = false;
            picCanvas.Cursor = Cursors.Default;
            FlushCommunityOverlayRender();
        }

        private void EnsureCommunityUndoSnapshot()
        {
            if (communitySnapshotSaved)
                return;

            editor.PushUndoSnapshot();
            communitySnapshotSaved = true;
        }

        private void StopCommunitySession(bool commit)
        {
            if (!communitySessionActive)
                return;

            communityFaceDetectionCancellation?.Cancel();
            communityFaceDetectionCancellation = null;
            communitySelectedFilterName = null;
            communityDetectedFaces = null;
            FlushCommunityOverlayRender();
            communityDragging = false;
            picCanvas.Cursor = Cursors.Default;

            if (commit && communitySessionDirty && communityPreviewImage != null)
            {
                editor.PushUndoSnapshot();
                editor.State.ReplaceCurrentImage(new Bitmap(communityPreviewImage));
                editor.InitializeLayers();
                RefreshCanvas();
                RefreshLayerUI();
                RecordTimelineSnapshot();
            }

            communityBaseImage?.Dispose();
            communityBaseImage = null;
            communityPreviewImage?.Dispose();
            communityPreviewImage = null;
            DisposeOwnedCommunityOverlay();
            communityOverlay = null;
            communityRenderQueued = false;
            communityRenderTimer.Stop();
            communitySessionActive = false;
            communitySessionDirty = false;
            communitySnapshotSaved = false;
            communityFilterControl?.SetApplyEnabled(false);
            communityFilterControl?.SetAutoFitBusy(
                false,
                communityFilterControl.AutoFitEnabled
                    ? "스티커 선택 시 얼굴에 자동으로 맞춥니다."
                    : "자동 맞춤 꺼짐 · 직접 배치할 수 있습니다.");
            if (!commit)
                RefreshCanvas();
        }

        private void CommunityFilterControl_CancelRequested()
        {
            StopCommunitySession(false);
            if (pnlToolOptions.Controls.Contains(communityFilterControl))
                pnlToolOptions.Controls.Remove(communityFilterControl);
        }

        private void CommunityFilterControl_ApplyRequested()
        {
            StopCommunitySession(true);
            if (pnlToolOptions.Controls.Contains(communityFilterControl))
                pnlToolOptions.Controls.Remove(communityFilterControl);
        }
    }
}
