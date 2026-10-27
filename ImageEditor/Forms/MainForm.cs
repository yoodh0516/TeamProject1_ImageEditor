using ImageEditor.Controls;
using ImageEditor.Core;
using ImageEditor.Forms;
using ImageEditor.ImageProcessing;
using ImageEditor.Tools;

namespace ImageEditor
{
    public partial class MainForm : Form
    {
        private EditorController editor;
        private ResizeControl resizeControl;
        private CropControl cropControl;
        private CropTool cropTool;
        private SnsRatioControl snsRatioControl;
        private SnsRatioTool snsRatioTool;
        private bool snsRatioSnapshotSaved;
        private Bitmap? snsRatioPreviewImage;
        private bool snsRatioSessionActive;
        private bool snsRatioAutoFitBeforeSession;
        private Bitmap? resizeBaseImage;
        private Bitmap? resizePreviewImage;
        private int resizePreviewVersion;
        private bool resizeSessionActive;
        private bool resizeAutoFitBeforeSession;
        private float resizePreviewZoom = 1F;

        private bool IsTemporaryCanvasPreviewActive =>
            resizeSessionActive ||
            snsRatioSessionActive ||
            (adjustmentSessionActive && adjustmentPreviewImage != null) ||
            (filterSessionActive && filterPreviewImage != null) ||
            templatePreviewImage != null ||
            (communitySessionActive && communityPreviewImage != null);

        public MainForm()
        {
            InitializeComponent();

            // 이미지나 자식 컨트롤 크기가 MainForm 자체 크기를 바꾸지 못하게 합니다.
            AutoSize = false;

            editor = new EditorController();

            pnlToolOptions.AutoScroll = true;
            tabLayer.AutoScroll = true;

            resizeControl = new ResizeControl();

            resizeControl.ResizeRequested += ResizeControl_ResizeRequested;
            resizeControl.PreviewRequested += ResizeControl_PreviewRequested;
            resizeControl.CancelRequested += ResizeControl_CancelRequested;
            // Crop
            cropControl = new CropControl();

            cropTool = new CropTool(picCanvas, editor.State, cropControl);

            // Crop 적용이 끝나면 화면 다시 표시
            cropTool.CropApplied += CropTool_CropApplied;
            cropTool.CropApplying += CropTool_CropApplying;
            cropControl.CancelRequested += CropControl_CancelRequested;


            snsRatioControl = new SnsRatioControl();

            snsRatioTool = new SnsRatioTool(editor.State);

            snsRatioControl.RatioRequested += SnsRatioControl_RatioRequested;
            snsRatioControl.PreviewRequested += SnsRatioControl_PreviewRequested;
            snsRatioControl.CancelRequested += SnsRatioControl_CancelRequested;
            snsRatioControl.ResetRequested += SnsRatioControl_ResetRequested;

            snsRatioTool.ImageChanged += SnsRatioTool_ImageChanged;

            InitializeLayerDrawingFeatures();
            InitializeAdjustmentFilterFeatures();
            InitializeAIFeatures();
            InitializeAccountTimelineFeatures();
            InitializeEditStyleFeatures();
            InitializeUndoRedoFeatures();
            InitializeTextFeatures();
            InitializeTemplateCommunityFeatures();
            InitializeMaskFeatures();
            InitializeModernLayout();
            UpdateEditorStatus();
        }

        private void SnsRatioTool_ImageChanged() { }

        // 참고 UI 레이아웃의 캔버스 안쪽 여백을 제외한 실제 표시 영역입니다.
        private Size GetCanvasViewportSize()
        {
            return new Size(
                Math.Max(1, pnlCanvas.ClientSize.Width - pnlCanvas.Padding.Horizontal),
                Math.Max(1, pnlCanvas.ClientSize.Height - pnlCanvas.Padding.Vertical));
        }

        // SNS 비율 미리보기만 화면에 표시
        private void SnsRatioControl_PreviewRequested(int widthRatio, int heightRatio)
        {
            Bitmap? preview = snsRatioTool.CreatePreview(widthRatio, heightRatio);
            if (preview == null)
                return;

            snsRatioPreviewImage?.Dispose();
            snsRatioPreviewImage = preview;
            snsRatioControl.SetPreviewResultSize(preview.Size);
            float previewZoom = CanvasViewCalculator.GetFitZoom(
                snsRatioPreviewImage.Size,
                GetCanvasViewportSize());
            RefreshCanvas(snsRatioPreviewImage, previewZoom);
        }

        // 선택한 SNS 비율을 실제 이미지에 적용
        private void SnsRatioControl_RatioRequested(int widthRatio, int heightRatio)
        {
            if (editor.State.CurrentImage == null || snsRatioPreviewImage == null)
                return;

            // 비율 변환 전 상태를 Undo에 저장
            editor.PushUndoSnapshot();
            editor.State.ReplaceCurrentImage(new Bitmap(snsRatioPreviewImage));
            editor.InitializeLayers();
            RefreshAfterImageChange();
            EndSnsRatioSession(false);
        }

        private void SnsRatioControl_CancelRequested()
        {
            EndSnsRatioSession(true);
        }

        private void SnsRatioControl_ResetRequested()
        {
            snsRatioPreviewImage?.Dispose();
            snsRatioPreviewImage = null;
            RefreshCanvas();
        }

        private void EndSnsRatioSession(bool restoreCanvas = false)
        {
            bool restoreAutoFit = snsRatioSessionActive
                ? snsRatioAutoFitBeforeSession
                : autoFitCanvas;
            snsRatioSessionActive = false;
            autoFitCanvas = restoreAutoFit;

            snsRatioPreviewImage?.Dispose();
            snsRatioPreviewImage = null;
            snsRatioSnapshotSaved = false;
            snsRatioTool.EndSession();
            snsRatioControl.ResetSession();
            if (pnlToolOptions.Controls.Contains(snsRatioControl))
                pnlToolOptions.Controls.Remove(snsRatioControl);

            if (restoreCanvas)
            {
                if (autoFitCanvas && editor.State.CurrentImage != null)
                {
                    editor.FitToPanel(GetCanvasViewportSize());
                    ResetCanvasScroll();
                }
                RefreshCanvas();
            }
            UpdateUndoRedoMenuState();
        }




        // 현재 이미지를 화면에 표시
        // 미리보기 또는 현재 이미지를 캔버스에 표시
        private void RefreshCanvas(Bitmap? previewImage = null, float? zoomOverride = null)
        {
            Bitmap? image = previewImage ?? editor.State.CurrentImage;

            if (image == null || image.Width <= 0 || image.Height <= 0)
                return;

            // 캔버스 테두리와 그림자는 PictureBox 바깥의 부모 패널에 그려진다.
            // 이미지 비율이 바뀔 때 이전 PictureBox 영역만 남지 않도록 장식 영역을
            // 먼저 보관했다가 새 영역과 함께 다시 그린다.
            Rectangle previousDecorationBounds = GetCanvasDecorationBounds(picCanvas.Bounds);

            float displayZoom = zoomOverride ?? editor.State.ZoomRatio;
            Size displaySize = CanvasViewCalculator.GetDisplaySize(
                image.Size,
                displayZoom);

            if (displaySize.Width <= 0 || displaySize.Height <= 0)
                return;

            // CurrentImage는 레이어 렌더링 때 교체되고 Dispose될 수 있으므로
            // PictureBox에는 화면 표시 전용 복사본을 연결한다.
            // 확대 배율이 100%를 넘을 때는 확대된 전체 크기의 중간 Bitmap을 만들지 않는다.
            // 원본 해상도 복사본을 PictureBox가 표시 크기에 맞춰 그리므로 편집 결과의
            // 픽셀 품질은 유지하면서 고배율 메모리 폭증을 막는다.
            Bitmap displayBitmap;
            if (displaySize.Width > image.Width || displaySize.Height > image.Height)
            {
                displayBitmap = new Bitmap(image);
            }
            else
            {
                displayBitmap = new Bitmap(
                    displaySize.Width,
                    displaySize.Height,
                    System.Drawing.Imaging.PixelFormat.Format32bppArgb);

                using Graphics graphics = Graphics.FromImage(displayBitmap);
                graphics.Clear(Color.Transparent);
                graphics.InterpolationMode =
                    System.Drawing.Drawing2D.InterpolationMode.HighQualityBicubic;
                graphics.SmoothingMode =
                    System.Drawing.Drawing2D.SmoothingMode.HighQuality;
                graphics.PixelOffsetMode =
                    System.Drawing.Drawing2D.PixelOffsetMode.HighQuality;

                graphics.DrawImage(
                    image,
                    new Rectangle(0, 0, displayBitmap.Width, displayBitmap.Height));
            }

            Image? oldDisplayImage = picCanvas.Image;
            picCanvas.Image = displayBitmap;
            if (oldDisplayImage != null &&
                !ReferenceEquals(oldDisplayImage, image) &&
                !ReferenceEquals(oldDisplayImage, editor.State.OriginalImage))
            {
                oldDisplayImage.Dispose();
            }

            picCanvas.Size = displaySize;
            Size viewportSize = GetCanvasViewportSize();
            Point centered = CanvasViewCalculator.GetCenterLocation(viewportSize, displaySize);
            picCanvas.Location = new Point(
                pnlCanvas.Padding.Left + centered.X,
                pnlCanvas.Padding.Top + centered.Y);
            picCanvas.Enabled = true;
            picCanvas.Visible = true;
            picCanvas.TabStop = false;
            picCanvas.BringToFront();

            pnlCanvas.AutoScrollMinSize = new Size(
                displaySize.Width + pnlCanvas.Padding.Horizontal,
                displaySize.Height + pnlCanvas.Padding.Vertical);

            Rectangle currentDecorationBounds = GetCanvasDecorationBounds(picCanvas.Bounds);
            pnlCanvas.Invalidate(
                Rectangle.Union(previousDecorationBounds, currentDecorationBounds));
            UpdateEditorStatus(
                displayedImage: image,
                displayedZoom: displayZoom,
                updateCommands: false);
        }

        private static Rectangle GetCanvasDecorationBounds(Rectangle canvasBounds)
        {
            // 현재 그림자는 오른쪽·아래로 5px, 테두리는 바깥으로 1px 그려진다.
            // DPI 보정과 안티앨리어싱 여유를 포함해 8px 확장한다.
            canvasBounds.Inflate(8, 8);
            return canvasBounds;
        }


        // 이미지 자체가 변경된 뒤 화면에 맞춰 다시 표시
        private void RefreshAfterImageChange(bool fitToPanel = true)
        {
            if (editor.State.CurrentImage == null)
                return;

            if (fitToPanel)
            {
                editor.FitToPanel(GetCanvasViewportSize());
                pnlCanvas.AutoScrollPosition = Point.Empty;
            }

            RefreshCanvas();
            RefreshLayerUI();
            RecordTimelineSnapshot();
            UpdateImageDependentControls();
        }

        // 이미지를 열기 전에는 실행할 수 없는 메뉴를 미리 비활성화해
        // 불필요한 안내창과 잘못된 작업 흐름을 줄인다.
        private void UpdateImageDependentControls()
        {
            bool hasImage = editor.State.CurrentImage != null;

            menuSave.Enabled = hasImage;
            menuSaveAs.Enabled = hasImage;
            menuExport.Enabled = hasImage;

            toolStripMenuItem2.Enabled = hasImage;
            toolStripMenuItem3.Enabled = hasImage;
            toolStripMenuItem4.Enabled = hasImage;
            toolStripMenuItem5.Enabled = hasImage;
            toolStripMenuItem6.Enabled = hasImage;
            toolStripMenuItem7.Enabled = hasImage;
            UpdateAiFeatureAvailability(hasImage);
            UpdateMyEditStyleAvailability(hasImage);
            toolStripMenuItem9.Enabled = hasImage;

            UpdateCommandAvailability(hasImage);
            UpdateUndoRedoMenuState();
        }


        // 오른쪽 상세설정 패널에 UserControl 표시
        private void ShowToolControl(UserControl control)
        {
            EnsureDetailPanelVisible();
            PrepareToolOptionsHost();
            pnlToolOptions.Controls.Clear();

            control.Dock = DockStyle.Fill;

            pnlToolOptions.Controls.Add(control);

            control.BringToFront();
        }


        // 이미지 열기
        private async void menuOpen_Click(object sender, EventArgs e)
        {
            FormWindowState previousWindowState = WindowState;
            Rectangle previousBounds = Bounds;

            StopInteractiveTools();
            if (!ConfirmDiscardUnsavedChanges())
                return;

            if (!editor.OpenImage())
                return;

            ResetLastAppliedAdjustmentStyle();

            // 이전 파일의 자르기 전 Bitmap이 새 이미지에서 복원되지 않도록 한다.
            cropTool?.ClearPreviousCrop();

            if (timeLinePage != null)
            {
                try
                {
                    await timeLinePage.StartNewProjectAsync(CurrentUser.UserNo);
                }
                catch (Exception ex)
                {
                    MessageBox.Show(
                        "이미지는 열었지만 타임라인 저장 폴더를 준비하지 못했습니다.\r\n" +
                        ex.Message,
                        "타임라인",
                        MessageBoxButtons.OK,
                        MessageBoxIcon.Warning);
                }
            }

            RefreshAfterImageChange();

            // 고해상도 이미지나 DPI 정보 때문에 이미지 열기 뒤 창이 커지는 현상을 막습니다.
            if (previousWindowState == FormWindowState.Normal)
            {
                WindowState = FormWindowState.Normal;
                Bounds = previousBounds;
            }
            else
            {
                WindowState = previousWindowState;
            }

            UpdateEditorStatus("이미지 열기 완료");
        }

        private bool ConfirmDiscardUnsavedChanges()
        {
            if (editor.State.CurrentImage == null || !editor.State.IsDirty)
                return true;

            DialogResult result = MessageBox.Show(
                "저장하지 않은 편집 내용이 있습니다.\r\n저장한 뒤 계속할까요?",
                "저장 확인",
                MessageBoxButtons.YesNoCancel,
                MessageBoxIcon.Warning);

            if (result == DialogResult.Yes)
                return editor.Save();

            return result == DialogResult.No;
        }


        // 저장
        private void menuSave_Click(object sender, EventArgs e)
        {
            StopInteractiveTools();
            if (editor.Save())
            {
                UpdateEditorStatus("저장 완료");
                MessageBox.Show("저장되었습니다.");
            }
        }


        // 다른 이름으로 저장
        private void menuSaveAs_Click(object sender, EventArgs e)
        {
            StopInteractiveTools();
            if (editor.SaveAs())
            {
                UpdateEditorStatus("다른 이름으로 저장 완료");
                MessageBox.Show("저장되었습니다.");
            }
        }


        // 오른쪽 회전
        // 회전 전 상태를 Undo에 저장
        private void menuRotateRight_Click(object sender, EventArgs e)
        {
            StopInteractiveTools();
            editor.PushUndoSnapshot();
            editor.RotateRight();

            RefreshAfterImageChange();
        }


        // 왼쪽 회전
        // 회전 전 상태를 Undo에 저장
        private void menuRotateLeft_Click(object sender, EventArgs e)
        {
            StopInteractiveTools();
            editor.PushUndoSnapshot();
            editor.RotateLeft();

            RefreshAfterImageChange();
        }


        // 크기 조절
        // 현재 이미지 복사본으로 크기 미리보기 준비
        private void menuResize_Click(object sender, EventArgs e)
        {
            StopInteractiveTools();
            if (editor.State.CurrentImage == null)
                return;

            resizeBaseImage?.Dispose();
            resizePreviewImage?.Dispose();
            resizeBaseImage = new Bitmap(editor.State.CurrentImage);
            resizePreviewImage = null;
            resizePreviewVersion++;
            resizeAutoFitBeforeSession = autoFitCanvas;
            resizePreviewZoom = editor.State.ZoomRatio;
            resizeSessionActive = true;
            autoFitCanvas = false;
            resizeControl.SetImageSize(editor.State.CurrentImage.Width, editor.State.CurrentImage.Height);

            ShowToolControl(resizeControl);
            autoFitCanvas = false;
        }


        // 좌우 반전
        private void menuFlipHorizontal_Click(object sender, EventArgs e)
        {
            StopInteractiveTools();
            editor.PushUndoSnapshot();
            editor.FlipHorizontal();

            RefreshAfterImageChange();
        }


        // 상하 반전
        private void menuFlipVertical_Click(object sender, EventArgs e)
        {
            StopInteractiveTools();
            editor.PushUndoSnapshot();
            editor.FlipVertical();

            RefreshAfterImageChange();
        }


        // 확대
        // 확대 전 화면 중심 위치 저장
        private void menuZoomIn_Click(object sender, EventArgs e)
        {
            StopInteractiveTools();
            autoFitCanvas = false;
            PointF imageCenter = GetViewportImageCenter();
            editor.ZoomIn();
            RefreshCanvas();
            RestoreViewportImageCenter(imageCenter);
        }


        // 축소
        // 축소 전 화면 중심 위치 저장
        private void menuZoomOut_Click(object sender, EventArgs e)
        {
            StopInteractiveTools();
            autoFitCanvas = false;
            PointF imageCenter = GetViewportImageCenter();
            editor.ZoomOut();
            RefreshCanvas();
            RestoreViewportImageCenter(imageCenter);
        }


        // 화면에 맞추기
        // 이미지를 작업 영역에 맞춰 표시
        private void menuFitToScreen_Click(object sender, EventArgs e)
        {
            StopInteractiveTools();
            editor.FitToPanel(GetCanvasViewportSize());
            autoFitCanvas = true;
            RefreshCanvas();
            ResetCanvasScroll();
        }

        // 확대 전후로 같은 이미지 지점이 작업 영역 가운데에 남도록 현재 뷰포트 중심을
        // 이미지 비율 좌표로 보관한다. AutoScrollPosition은 음수 값으로 반환된다.
        private PointF GetViewportImageCenter()
        {
            Size displaySize = picCanvas.Size;
            if (displaySize.Width <= 0 || displaySize.Height <= 0)
                return new PointF(0.5f, 0.5f);

            // 스크롤된 경우와 작은 이미지가 가운데 배치된 경우를 모두 반영하려면
            // 실제 화면에 그려진 PictureBox의 위치를 기준으로 계산해야 한다.
            Point canvasOrigin = pnlCanvas.PointToClient(picCanvas.PointToScreen(Point.Empty));
            float centerX = pnlCanvas.ClientSize.Width / 2f - canvasOrigin.X;
            float centerY = pnlCanvas.ClientSize.Height / 2f - canvasOrigin.Y;

            return new PointF(
                Math.Clamp(centerX / displaySize.Width, 0f, 1f),
                Math.Clamp(centerY / displaySize.Height, 0f, 1f));
        }

        private void RestoreViewportImageCenter(PointF imageCenter)
        {
            Size displaySize = picCanvas.Size;
            Size viewportSize = pnlCanvas.ClientSize;
            if (displaySize.Width <= viewportSize.Width && displaySize.Height <= viewportSize.Height)
            {
                ResetCanvasScroll();
                return;
            }

            int x = (int)Math.Round(imageCenter.X * displaySize.Width - viewportSize.Width / 2f);
            int y = (int)Math.Round(imageCenter.Y * displaySize.Height - viewportSize.Height / 2f);
            int maxX = Math.Max(0, displaySize.Width - viewportSize.Width);
            int maxY = Math.Max(0, displaySize.Height - viewportSize.Height);

            pnlCanvas.AutoScrollPosition = new Point(
                Math.Clamp(x, 0, maxX),
                Math.Clamp(y, 0, maxY));
        }

        private void ResetCanvasScroll()
        {
            pnlCanvas.AutoScrollPosition = Point.Empty;
        }


        // ResizeControl 적용
        // 선택한 크기를 실제 이미지에 적용
        private void ResizeControl_ResizeRequested(int width, int height)
        {
            if (editor.State.CurrentImage == null)
                return;

            // 크기 변경 전 상태를 Undo에 저장
            editor.PushUndoSnapshot();
            if (resizePreviewImage != null &&
                resizePreviewImage.Width == width &&
                resizePreviewImage.Height == height)
            {
                editor.State.ReplaceCurrentImage(new Bitmap(resizePreviewImage));
                editor.InitializeLayers();
            }
            else
            {
                editor.Resize(width, height);
            }

            bool fitAfterApply = resizeSessionActive
                ? resizeAutoFitBeforeSession
                : autoFitCanvas;
            autoFitCanvas = fitAfterApply;
            RefreshAfterImageChange(fitAfterApply);
            EndResizeSession(false);
        }

        // 입력한 크기의 미리보기 생성
        private async void ResizeControl_PreviewRequested(int width, int height)
        {
            if (resizeBaseImage == null || width <= 0 || height <= 0)
                return;

            int version = ++resizePreviewVersion;
            Bitmap? preview;
            try
            {
                using Bitmap source = new Bitmap(resizeBaseImage);
                preview = await Task.Run(() => BasicImageProcessor.Resize(source, width, height));
            }
            catch (Exception ex) when (ex is ArgumentException ||
                                       ex is OutOfMemoryException ||
                                       ex is System.Runtime.InteropServices.ExternalException)
            {
                if (version == resizePreviewVersion &&
                    pnlToolOptions.Controls.Contains(resizeControl))
                {
                    resizeControl.SetPreviewError(
                        "해당 크기의 미리보기를 만들 수 없습니다. 더 작은 값을 입력해 주세요.");
                }
                return;
            }

            if (preview == null)
            {
                resizeControl.SetPreviewError("미리보기를 만들 수 없습니다.");
                return;
            }

            if (version != resizePreviewVersion || !pnlToolOptions.Controls.Contains(resizeControl))
            {
                preview.Dispose();
                return;
            }

            resizePreviewImage?.Dispose();
            resizePreviewImage = preview;
            // 매번 화면에 맞추면 50%와 200%가 같은 크기로 보여 크기 변경을
            // 알아보기 어렵다. 세션 시작 배율을 유지해 실제 픽셀 크기 차이를 표시한다.
            RefreshCanvas(resizePreviewImage, resizePreviewZoom);
        }

        private void ResizeControl_CancelRequested()
        {
            EndResizeSession(true);
        }

        private void EndResizeSession(bool restoreCanvas)
        {
            bool restoreAutoFit = resizeSessionActive
                ? resizeAutoFitBeforeSession
                : autoFitCanvas;
            resizeSessionActive = false;
            autoFitCanvas = restoreAutoFit;

            resizePreviewVersion++;
            resizePreviewImage?.Dispose();
            resizePreviewImage = null;
            resizeBaseImage?.Dispose();
            resizeBaseImage = null;
            if (pnlToolOptions.Controls.Contains(resizeControl))
                pnlToolOptions.Controls.Remove(resizeControl);
            if (restoreCanvas)
            {
                if (autoFitCanvas && editor.State.CurrentImage != null)
                {
                    editor.FitToPanel(GetCanvasViewportSize());
                    ResetCanvasScroll();
                }
                RefreshCanvas();
            }
            UpdateUndoRedoMenuState();
        }

        private void CropTool_CropApplied()
        {
            editor.InitializeLayers();
            RefreshAfterImageChange();
        }

        // 자르기 전 상태를 Undo에 저장
        private void CropTool_CropApplying()
        {
            editor.PushUndoSnapshot();
        }

        private void CropControl_CancelRequested()
        {
            cropTool.Stop();
            pnlToolOptions.Controls.Remove(cropControl);
            RefreshCanvas();
        }

        private void menuCrop_Click(object sender, EventArgs e)
        {
            FinishAdjustmentFilterSessions(false);
            StopCommunitySession(false);
            CancelTemplateSession();
            StopMaskTool();
            CloseTextTool();
            aiObjectRemovalTool?.Stop();
            aiBackgroundRemovalTool?.Stop();
            StopLayerDrawingTools();
            if (editor.State.CurrentImage == null)
                return;

            // 오른쪽에 Crop 상세설정 표시
            ShowToolControl(cropControl);

            // Crop 기능 시작
            cropTool.Start();
        }

        private void menuSnsRatio_Click(object sender, EventArgs e)
        {
            StopInteractiveTools();
            if (editor.State.CurrentImage == null)
                return;

            // SNS 비율 변환 기준 이미지 저장
            snsRatioAutoFitBeforeSession = autoFitCanvas;
            snsRatioSessionActive = true;
            autoFitCanvas = false;
            snsRatioTool.Start();
            snsRatioSnapshotSaved = false;

            ShowToolControl(snsRatioControl);
            autoFitCanvas = false;
        }

        // 원본과 현재 이미지를 비교 창에 표시
        private void menuBeforeAfter_Click(object sender, EventArgs e)
        {
            StopInteractiveTools();
            if (editor.State.OriginalImage == null || editor.State.CurrentImage == null)
                return;

            using BeforeAfterForm form = new BeforeAfterForm(
                editor.State.OriginalImage,
                editor.State.CurrentImage);
            form.ShowDialog(this);
        }

        private void menuJpeg100_Click(object sender, EventArgs e)
        {
            StopInteractiveTools();
            if (editor.ExportJpeg(100))
            {
                MessageBox.Show(
                    "JPG 최고 품질로 저장되었습니다."
                );
            }
        }

        private void menuJpeg85_Click(object sender, EventArgs e)
        {
            StopInteractiveTools();
            if (editor.ExportJpeg(85))
            {
                MessageBox.Show(
                    "JPG 높은 품질로 저장되었습니다."
                );
            }
        }

        private void menuJpeg60_Click(object sender, EventArgs e)
        {
            StopInteractiveTools();
            if (editor.ExportJpeg(60))
            {
                MessageBox.Show(
                    "JPG 용량 절약 설정으로 저장되었습니다."
                );
            }
        }

        private void menuExportPng_Click(object sender, EventArgs e)
        {
            StopInteractiveTools();
            if (editor.ExportPng())
            {
                MessageBox.Show(
                    "PNG로 저장되었습니다."
                );
            }
        }








        private void ExportSns(
    int width,
    int height,
    string fileName)
        {
            StopInteractiveTools();
            if (editor.State.CurrentImage == null)
            {
                MessageBox.Show(
                    "먼저 이미지를 불러와주세요."
                );

                return;
            }

            bool result =
                editor.ExportForSns(
                    width,
                    height,
                    90,
                    fileName
                );

            if (result)
            {
                MessageBox.Show(
                    $"{width} × {height} 크기로 저장되었습니다."
                );
            }
        }

        private void menuInstagramSquareExport_Click(object sender, EventArgs e)
        {
            ExportSns(
        1080,
        1080,
        "instagram_square.jpg"
    );
        }

        private void menuInstagramPortraitExport_Click(object sender, EventArgs e)
        {
            ExportSns(
        1080,
        1350,
        "instagram_portrait.jpg"
    );
        }

        private void menuVerticalExport_Click(object sender, EventArgs e)
        {
            ExportSns(
        1080,
        1920,
        "vertical_social.jpg"
    );
        }

        private void menuYoutubeThumbnailExport_Click(object sender, EventArgs e)
        {
            ExportSns(
        1280,
        720,
        "youtube_thumbnail.jpg"
    );
        }

        private void menuWebThumbnailExport_Click(object sender, EventArgs e)
        {
            ExportSns(
        1200,
        630,
        "web_thumbnail.jpg"
    );
        }

        // 현재 이미지 복사본으로 결과 미리보기 표시
        private void menuPreview_Click(object sender, EventArgs e)
        {
            StopInteractiveTools();
            if (editor.State.CurrentImage == null)
            {
                MessageBox.Show(
                    "먼저 이미지를 불러와주세요."
                );

                return;
            }

            using (SnsPreviewForm form =
                   new SnsPreviewForm(
                       editor.State.CurrentImage))
            {
                form.ShowDialog(this);
            }
        }
    }
}
