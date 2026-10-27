using ImageEditor.Controls;
using ImageEditor.Forms;
using ImageEditor.ImageProcessing;
using ImageEditor.Models.AI;
using ImageEditor.Services;
using ImageEditor.Tools;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace ImageEditor
{
    public partial class MainForm : Form
    {
        private AIObjectRemovalTool? aiObjectRemovalTool;
        private Label? lblMusicRecommendation;
        private MusicRecommendationControl? musicRecommendationControl;
        private CancellationTokenSource? musicRecommendationCancellation;
        private bool musicRecommendationBusy;
        private AiCaptionRecommendationControl? aiCaptionRecommendationControl;
        private CancellationTokenSource? aiCaptionRecommendationCancellation;
        private bool aiCaptionRecommendationBusy;
        private ToolStripMenuItem? menuAIPointMode;
        private ToolStripMenuItem? menuAIBoxMode;
        private ToolStripMenuItem? menuAIBackgroundRemoval;
        private AIBackgroundRemovalTool? aiBackgroundRemovalTool;
        private ToolStripMenuItem? menuAIPhotoRestoration;
        private PhotoRestorationControl? photoRestorationControl;
        private PhotoRestorationService? photoRestorationService;
        private CancellationTokenSource? photoRestorationCancellation;
        private bool photoRestorationBusy;
        private ToolStripMenuItem? menuAIQualityEnhancement;
        private ToolStripMenuItem? menuAIPortraitEnhance;
        private ToolStripMenuItem? menuAIRealEsrgan;
        private ToolStripMenuItem? menuAIAnimeVideo;
        private bool aiImageProcessingBusy;
        private CancellationTokenSource? portraitEnhanceCancellation;
        private CancellationTokenSource? qualityEnhancementCancellation;
        private AiQualityEnhancementControl? aiQualityEnhancementControl;
        private PortraitEnhancementControl? portraitEnhancementControl;
        // Keep one ONNX model loaded while it is selected. Recreating a session
        // for every click was a major source of repeated disk I/O and memory churn.
        private RealEsrganModel? realEsrganModel;
        private AnimeVideoModel? animeVideoModel;
        private const string AiLoginRequiredMessage =
            "AI 기능은 로그인 후 사용할 수 있습니다.";

        private bool EnsureAiFeatureAccess(bool showMessage = true)
        {
            if (CurrentUser.UserNo > 0)
                return true;

            if (showMessage)
            {
                MessageBox.Show(
                    AiLoginRequiredMessage,
                    "로그인 필요",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Information);
            }

            return false;
        }

        private void UpdateAiFeatureAvailability(bool hasImage)
        {
            bool loggedIn = CurrentUser.UserNo > 0;
            bool enabled = loggedIn && hasImage;

            // 메뉴 자체와 모든 하위 진입점을 함께 막아 PerformClick 같은
            // 내부 경로에서도 비회원이 AI 기능을 실행하지 못하게 한다.
            menuAIRemoveObject.Enabled = enabled;
            menuAIRemoveObject.ToolTipText = loggedIn
                ? hasImage
                    ? "AI 이미지 편집과 추천 기능을 표시합니다."
                    : "이미지를 불러오면 AI 기능을 사용할 수 있습니다."
                : AiLoginRequiredMessage;

            foreach (ToolStripItem item in menuAIRemoveObject.DropDownItems)
            {
                if (item is not ToolStripSeparator)
                    item.Enabled = enabled;
            }

            if (menuAIQualityEnhancement != null)
                menuAIQualityEnhancement.Enabled = enabled && !aiImageProcessingBusy;
            if (menuAIPortraitEnhance != null)
                menuAIPortraitEnhance.Enabled = enabled && !aiImageProcessingBusy;

            communityFilterControl?.SetAutoFitAvailable(loggedIn);

            if (!loggedIn &&
                ((photoRestorationControl != null && pnlToolOptions.Controls.Contains(photoRestorationControl)) ||
                 (aiQualityEnhancementControl != null && pnlToolOptions.Controls.Contains(aiQualityEnhancementControl)) ||
                 (portraitEnhancementControl != null && pnlToolOptions.Controls.Contains(portraitEnhancementControl)) ||
                 (musicRecommendationControl != null && pnlToolOptions.Controls.Contains(musicRecommendationControl)) ||
                 (aiCaptionRecommendationControl != null && pnlToolOptions.Controls.Contains(aiCaptionRecommendationControl)) ||
                 (aiSocialPostControl != null && pnlToolOptions.Controls.Contains(aiSocialPostControl))))
            {
                ShowToolPlaceholder();
            }

            UpdateToolPlaceholderAccountState();
        }

        private void InitializeAIFeatures()
        {
            aiObjectRemovalTool = new AIObjectRemovalTool(picCanvas, editor.State, pnlToolOptions);
            aiObjectRemovalTool.ImageChanged += AIObjectRemovalTool_ImageChanged;
            aiObjectRemovalTool.ImageReady += AIObjectRemovalTool_ImageReady;

            aiBackgroundRemovalTool = new AIBackgroundRemovalTool(
                picCanvas,
                editor.State,
                pnlToolOptions);
            aiBackgroundRemovalTool.ImageReady +=
                AIBackgroundRemovalTool_ImageReady;

            // 선택 방식은 오른쪽 상세 설정에서 바꿀 수 있으므로 상단 메뉴에는
            // 물체 제거 진입점만 둔다. 기본 진입 모드는 포인터 방식이다.
            menuAIPointMode = new ToolStripMenuItem("포인터 모드");
            menuAIBoxMode = new ToolStripMenuItem("드래그 모드");
            menuAIPointMode.Click += menuAIPointMode_Click;
            menuAIBoxMode.Click += menuAIBoxMode_Click;

            toolStripMenuItem23.DropDownItems.Clear();
            toolStripMenuItem23.Click += menuAIPointMode_Click;

            photoRestorationService = new PhotoRestorationService();
            photoRestorationControl = new PhotoRestorationControl();
            photoRestorationControl.RestoreRequested +=
                PhotoRestorationControl_RestoreRequested;
            photoRestorationControl.CancelRequested +=
                PhotoRestorationControl_CancelRequested;

            menuAIPhotoRestoration = new ToolStripMenuItem(
                "AI 오래된 사진 복원")
            {
                ToolTipText =
                    "Replicate 클라우드 GPU로 손상과 흑백 색상을 고품질 복원합니다."
            };
            menuAIPhotoRestoration.Click += MenuAIPhotoRestoration_Click;

            menuAIBackgroundRemoval = new ToolStripMenuItem(
                "AI 배경 제거")
            {
                ToolTipText =
                    "로컬 CPU AI가 주요 대상을 인식해 배경을 투명하게 제거합니다."
            };
            menuAIBackgroundRemoval.Click += MenuAIBackgroundRemoval_Click;

            menuAIQualityEnhancement = new ToolStripMenuItem(
                "AI 화질 개선")
            {
                ToolTipText =
                    "로컬 ONNX AI 모델로 이미지를 4배 업스케일하여 화질을 개선합니다."
            };
            // 모델 선택은 오른쪽 상세 설정 패널로 통일한다.
            // 상단 메뉴는 패널을 여는 단일 진입점만 제공한다.
            menuAIQualityEnhancement.Click += MenuAIQualityOptions_Click;

            aiQualityEnhancementControl = new AiQualityEnhancementControl();
            aiQualityEnhancementControl.RunRequested +=
                AiQualityEnhancementControl_RunRequested;
            aiQualityEnhancementControl.CancelRequested +=
                AiQualityEnhancementControl_CancelRequested;
            aiQualityEnhancementControl.CloseRequested +=
                AiQualityEnhancementControl_CloseRequested;

            menuAIPortraitEnhance = new ToolStripMenuItem(
                "AI 인물 사진 자동보정")
            {
                ToolTipText =
                    "Cloudflare AI로 인물 사진을 자연스럽게 보정합니다."
            };
            menuAIPortraitEnhance.Click += MenuAIPortraitEnhance_Click;

            portraitEnhancementControl = new PortraitEnhancementControl();
            portraitEnhancementControl.RunRequested +=
                PortraitEnhancementControl_RunRequested;
            portraitEnhancementControl.CancelRequested +=
                PortraitEnhancementControl_CancelRequested;
            portraitEnhancementControl.CloseRequested +=
                PortraitEnhancementControl_CloseRequested;

            // AI 편집 기능을 위쪽에 모으고 추천 기능과 구분한다.
            menuAIRemoveObject.DropDownItems.Insert(1, menuAIBackgroundRemoval);
            menuAIRemoveObject.DropDownItems.Insert(2, menuAIPhotoRestoration);
            if (menuAIQualityEnhancement != null)
                menuAIRemoveObject.DropDownItems.Insert(3, menuAIQualityEnhancement);
            menuAIRemoveObject.DropDownItems.Insert(4, menuAIPortraitEnhance);
            menuAIRemoveObject.DropDownItems.Insert(5, new ToolStripSeparator());

            menuAIMusicRecommend.Click -= menuMusicRecommend_Click;
            menuAIMusicRecommend.Click += menuMusicRecommend_Click;

            musicRecommendationControl = new MusicRecommendationControl();
            musicRecommendationControl.RunRequested +=
                MusicRecommendationControl_RunRequested;
            musicRecommendationControl.CancelRequested +=
                MusicRecommendationControl_CancelRequested;
            musicRecommendationControl.CloseRequested +=
                MusicRecommendationControl_CloseRequested;

            aiCaptionRecommendationControl = new AiCaptionRecommendationControl();
            aiCaptionRecommendationControl.RunRequested +=
                AiCaptionRecommendationControl_RunRequested;
            aiCaptionRecommendationControl.CancelRequested +=
                AiCaptionRecommendationControl_CancelRequested;
            aiCaptionRecommendationControl.CloseRequested +=
                AiCaptionRecommendationControl_CloseRequested;
            aiCaptionRecommendationControl.StartEditingRequested +=
                AiCaptionRecommendationControl_StartEditingRequested;

            menuAICaption.Click -= MenuAICaption_Click;
            menuAICaption.Click += MenuAICaption_Click;
            menuAITextPosition.Click -= MenuAITextPosition_Click;
            menuAITextPosition.Click += MenuAITextPosition_Click;
            // 글씨 위치 추천은 유행어·밈 추천 패널의 선택 후 동작으로 통합했다.
            // Designer와 기존 이벤트는 보존하고, 중복되는 상단 메뉴만 숨긴다.
            menuAITextPosition.Visible = false;

            InitializeAiSocialPostFeature();

            lblMusicRecommendation = new Label
            {
                AutoSize = true,
                Font = new Font("맑은 고딕", 11F, FontStyle.Bold),
                ForeColor = Color.DeepPink,
                BackColor = Color.White,
                Location = new Point(20, ClientSize.Height - 60),
                Anchor = AnchorStyles.Bottom | AnchorStyles.Left,
                Visible = false
            };
            Controls.Add(lblMusicRecommendation);
            lblMusicRecommendation.BringToFront();
        }

        private void StopInteractiveTools()
        {
            // 도구 전환은 아직 적용하지 않은 미리보기 결과를 확정하지 않는다.
            // 각 세션의 명시적 적용 버튼만 실제 편집 상태를 변경한다.
            FinishAdjustmentFilterSessions(false);
            StopCommunitySession(false);
            CancelTemplateSession();
            StopMaskTool();
            CloseTextTool();
            cropTool?.Stop();
            aiObjectRemovalTool?.Stop();
            aiBackgroundRemovalTool?.Stop();
            photoRestorationCancellation?.Cancel();
            portraitEnhanceCancellation?.Cancel();
            qualityEnhancementCancellation?.Cancel();
            musicRecommendationCancellation?.Cancel();
            aiCaptionRecommendationCancellation?.Cancel();
            aiSocialPostCancellation?.Cancel();
            EndResizeSession(true);
            EndSnsRatioSession(true);
            StopLayerDrawingTools();
        }

        private void menuAIPointMode_Click(object? sender, EventArgs e)
        {
            if (!EnsureAiFeatureAccess()) return;
            if (editor.State.CurrentImage == null) return;
            FinishAdjustmentFilterSessions(false);
            StopCommunitySession(false);
            CancelTemplateSession();
            StopMaskTool();
            CloseTextTool();
            cropTool?.Stop();
            aiBackgroundRemovalTool?.Stop();
            StopLayerDrawingTools();
            aiObjectRemovalTool?.StartPointMode();
        }

        private void MenuAICaption_Click(object? sender, EventArgs e)
        {
            if (!EnsureAiFeatureAccess()) return;
            ShowAiCaptionRecommendationOptions();
        }

        private void MenuAITextPosition_Click(object? sender, EventArgs e)
        {
            if (!EnsureAiFeatureAccess()) return;
            ShowAiCaptionRecommendationOptions();
        }

        private void menuAIBoxMode_Click(object? sender, EventArgs e)
        {
            if (!EnsureAiFeatureAccess()) return;
            if (editor.State.CurrentImage == null) return;
            FinishAdjustmentFilterSessions(false);
            StopCommunitySession(false);
            CancelTemplateSession();
            StopMaskTool();
            CloseTextTool();
            cropTool?.Stop();
            aiBackgroundRemovalTool?.Stop();
            StopLayerDrawingTools();
            aiObjectRemovalTool?.StartBoxMode();
        }

        private void AIObjectRemovalTool_ImageChanged()
        {
            editor.InitializeLayers();
            RefreshAfterImageChange();
            RefreshLayerUI();
            UpdateUndoRedoMenuState();
        }

        private void AIObjectRemovalTool_ImageReady(Bitmap resultImage)
        {
            if (CurrentUser.UserNo <= 0 || editor.State.CurrentImage == null)
            {
                resultImage.Dispose();
                return;
            }

            editor.PushUndoSnapshot();
            editor.State.ReplaceCurrentImage(resultImage);
            AIObjectRemovalTool_ImageChanged();
        }

        private void MenuAIBackgroundRemoval_Click(object? sender, EventArgs e)
        {
            if (!EnsureAiFeatureAccess()) return;
            StopInteractiveTools();

            if (editor.State.CurrentImage == null)
            {
                MessageBox.Show(
                    "먼저 이미지를 불러와 주세요.",
                    "AI 배경 제거",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Information);
                return;
            }

            aiBackgroundRemovalTool?.Start();
        }

        private void AIBackgroundRemovalTool_ImageReady(Bitmap result)
        {
            if (CurrentUser.UserNo <= 0 || editor.State.CurrentImage == null)
            {
                result.Dispose();
                return;
            }

            editor.PushUndoSnapshot();
            editor.State.ReplaceCurrentImage(result);
            editor.InitializeLayers();
            RefreshAfterImageChange();
            UpdateUndoRedoMenuState();

            MessageBox.Show(
                "AI 배경 제거가 적용되었습니다.\r\n" +
                "투명 배경을 유지하려면 PNG 형식으로 저장하세요.",
                "AI 배경 제거 완료",
                MessageBoxButtons.OK,
                MessageBoxIcon.Information);
        }

        private void MenuAIPhotoRestoration_Click(object? sender, EventArgs e)
        {
            if (!EnsureAiFeatureAccess()) return;
            StopInteractiveTools();

            if (editor.State.CurrentImage == null)
            {
                MessageBox.Show("먼저 이미지를 불러와 주세요.");
                return;
            }

            if (photoRestorationControl == null)
                return;

            photoRestorationControl.PrepareForImage(
                editor.State.CurrentImage.Width,
                editor.State.CurrentImage.Height);
            ShowToolControl(photoRestorationControl);
        }

        private async void PhotoRestorationControl_RestoreRequested(
            object? sender,
            EventArgs e)
        {
            if (!EnsureAiFeatureAccess())
                return;

            if (photoRestorationBusy ||
                photoRestorationService == null ||
                photoRestorationControl == null ||
                editor.State.CurrentImage == null)
            {
                return;
            }

            photoRestorationBusy = true;
            photoRestorationControl.SetBusy(true);
            photoRestorationControl.SetStatus("복원 준비 중...");
            photoRestorationCancellation?.Dispose();
            photoRestorationCancellation = new CancellationTokenSource();
            CancellationToken cancellationToken =
                photoRestorationCancellation.Token;

            Bitmap sourceStateImage = editor.State.CurrentImage;
            using Bitmap beforeImage = new Bitmap(sourceStateImage);
            Bitmap? restoredImage = null;

            try
            {
                Progress<string> progress = new Progress<string>(message =>
                    photoRestorationControl.SetStatus(message));

                restoredImage = await photoRestorationService.RestoreOldPhotoAsync(
                    beforeImage,
                    progress,
                    cancellationToken);

                if (CurrentUser.UserNo <= 0 ||
                    !ReferenceEquals(editor.State.CurrentImage, sourceStateImage))
                {
                    restoredImage.Dispose();
                    restoredImage = null;
                    photoRestorationControl.SetStatus(
                        "복원 중 다른 편집이 적용되어 결과를 반영하지 않았습니다.");
                    return;
                }

                string restorationMethod =
                    photoRestorationService.LastRestorationMethodName;
                photoRestorationControl.SetStatus(
                    restorationMethod + " 결과를 확인해 주세요.",
                    photoRestorationService.UsedLocalFallback);

                using PhotoRestorationPreviewForm previewForm =
                    new PhotoRestorationPreviewForm(
                        beforeImage,
                        restoredImage,
                        restorationMethod,
                        AiResultPreviewKind.PhotoRestoration);

                if (previewForm.ShowDialog(this) != DialogResult.OK)
                {
                    photoRestorationControl.SetStatus(
                        "복원 결과 적용을 취소했습니다.");
                    return;
                }

                if (CurrentUser.UserNo <= 0 ||
                    !ReferenceEquals(editor.State.CurrentImage, sourceStateImage))
                {
                    restoredImage.Dispose();
                    restoredImage = null;
                    photoRestorationControl.SetStatus(
                        "현재 이미지가 변경되어 복원 결과를 폐기했습니다.");
                    return;
                }

                editor.PushUndoSnapshot();
                editor.State.ReplaceCurrentImage(restoredImage);
                restoredImage = null;

                editor.InitializeLayers();
                RefreshAfterImageChange();
                UpdateUndoRedoMenuState();

                photoRestorationControl.SetStatus(
                    restorationMethod + " 결과를 적용했습니다.");
            }
            catch (OperationCanceledException)
            {
                photoRestorationControl.SetStatus("AI 사진 복원을 취소했습니다.");
            }
            catch (Exception ex)
            {
                photoRestorationControl.SetStatus("AI 사진 복원에 실패했습니다.", true);
                MessageBox.Show(
                    ex.Message,
                    "AI 사진 복원",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Error);
            }
            finally
            {
                restoredImage?.Dispose();
                photoRestorationCancellation?.Dispose();
                photoRestorationCancellation = null;
                photoRestorationBusy = false;
                photoRestorationControl.SetBusy(false);
            }
        }

        private void PhotoRestorationControl_CancelRequested(
            object? sender,
            EventArgs e)
        {
            if (!photoRestorationBusy)
                return;

            photoRestorationControl?.SetStatus("복원 요청을 취소하는 중...");
            photoRestorationCancellation?.Cancel();
        }

        private async void MenuAIRealEsrgan_Click(object? sender, EventArgs e)
        {
            if (!EnsureAiFeatureAccess()) return;
            await StartAiQualityEnhancementAsync(AiQualityModel.RealEsrgan);
        }

        private async void MenuAIAnimeVideo_Click(object? sender, EventArgs e)
        {
            if (!EnsureAiFeatureAccess()) return;
            await StartAiQualityEnhancementAsync(AiQualityModel.AnimeVideo);
        }

        private void MenuAIQualityOptions_Click(object? sender, EventArgs e)
        {
            if (!EnsureAiFeatureAccess()) return;
            if (editor.State.CurrentImage == null)
            {
                MessageBox.Show(
                    "먼저 작업할 이미지를 열어주세요.",
                    "AI 화질 개선",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Information);
                return;
            }

            if (aiQualityEnhancementControl == null || aiImageProcessingBusy)
                return;

            StopInteractiveTools();
            aiQualityEnhancementControl.PrepareForImage(editor.State.CurrentImage.Size);
            ShowToolControl(aiQualityEnhancementControl);
        }

        private async void AiQualityEnhancementControl_RunRequested(
            object? sender,
            EventArgs e)
        {
            if (!EnsureAiFeatureAccess())
                return;

            if (aiQualityEnhancementControl == null)
                return;

            await StartAiQualityEnhancementAsync(
                aiQualityEnhancementControl.SelectedModel);
        }

        private void AiQualityEnhancementControl_CancelRequested(
            object? sender,
            EventArgs e)
        {
            qualityEnhancementCancellation?.Cancel();
            aiQualityEnhancementControl?.SetStatus("AI 화질 개선을 취소하는 중...");
        }

        private void AiQualityEnhancementControl_CloseRequested(
            object? sender,
            EventArgs e)
        {
            if (aiQualityEnhancementControl != null &&
                pnlToolOptions.Controls.Contains(aiQualityEnhancementControl))
            {
                pnlToolOptions.Controls.Remove(aiQualityEnhancementControl);
                RefreshCanvas();
            }
        }

        private async Task StartAiQualityEnhancementAsync(AiQualityModel model)
        {
            if (!EnsureAiFeatureAccess())
                return;

            if (model == AiQualityModel.RealEsrgan)
            {
                animeVideoModel?.Dispose();
                animeVideoModel = null;
                realEsrganModel ??= new RealEsrganModel();
                await ExecuteAIQualityEnhancement(realEsrganModel);
                return;
            }

            realEsrganModel?.Dispose();
            realEsrganModel = null;
            animeVideoModel ??= new AnimeVideoModel();
            await ExecuteAIQualityEnhancement(animeVideoModel);
        }

        private void MenuAIPortraitEnhance_Click(object? sender, EventArgs e)
        {
            if (!EnsureAiFeatureAccess()) return;
            if (editor.State.CurrentImage == null)
            {
                MessageBox.Show(
                    "먼저 작업할 이미지를 열어주세요.",
                    "AI 인물 사진 자동보정",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Information);
                return;
            }

            if (portraitEnhancementControl == null || aiImageProcessingBusy)
                return;

            StopInteractiveTools();
            portraitEnhancementControl.PrepareForImage(editor.State.CurrentImage.Size);
            ShowToolControl(portraitEnhancementControl);
        }

        private async void PortraitEnhancementControl_RunRequested(
            object? sender,
            EventArgs e)
        {
            if (!EnsureAiFeatureAccess())
                return;

            PortraitEnhancementStyle style = portraitEnhancementControl?.SelectedStyle ??
                PortraitEnhancementStyle.Natural;
            await ExecutePortraitEnhancementAsync(style);
        }

        private void PortraitEnhancementControl_CancelRequested(
            object? sender,
            EventArgs e)
        {
            portraitEnhanceCancellation?.Cancel();
            portraitEnhancementControl?.SetStatus("AI 자동보정을 취소하는 중...");
        }

        private void PortraitEnhancementControl_CloseRequested(
            object? sender,
            EventArgs e)
        {
            if (portraitEnhancementControl != null &&
                pnlToolOptions.Controls.Contains(portraitEnhancementControl))
            {
                pnlToolOptions.Controls.Remove(portraitEnhancementControl);
                RefreshCanvas();
            }
        }

        private async Task ExecutePortraitEnhancementAsync(
            PortraitEnhancementStyle style)
        {
            if (!EnsureAiFeatureAccess())
                return;

            if (editor.State.CurrentImage == null)
                return;

            if (aiImageProcessingBusy)
                return;

            Bitmap sourceStateImage = editor.State.CurrentImage;
            Bitmap? finalImage = null;

            try
            {
                aiImageProcessingBusy = true;
                Cursor = Cursors.WaitCursor;
                SetAiImageProcessingMenuEnabled(false);
                portraitEnhancementControl?.SetBusy(true);
                portraitEnhancementControl?.SetStatus("Cloudflare AI에 보정 요청을 보내는 중...");
                portraitEnhanceCancellation?.Dispose();
                portraitEnhanceCancellation = new CancellationTokenSource();

                using Bitmap sourceImage = new Bitmap(sourceStateImage);
                finalImage = await CloudflarePortraitService.EnhanceAsync(
                    sourceImage,
                    GetPortraitEnhancementInstruction(style),
                    portraitEnhanceCancellation.Token);

                if (CurrentUser.UserNo <= 0 ||
                    !ReferenceEquals(editor.State.CurrentImage, sourceStateImage))
                {
                    finalImage.Dispose();
                    finalImage = null;
                    MessageBox.Show(
                        "자동보정 중 다른 편집이 적용되어 결과를 반영하지 않았습니다.",
                        "AI 인물 사진 자동보정",
                        MessageBoxButtons.OK,
                        MessageBoxIcon.Information);
                    return;
                }

                using (PhotoRestorationPreviewForm previewForm =
                    new PhotoRestorationPreviewForm(
                        sourceStateImage,
                        finalImage,
                        GetPortraitEnhancementStyleName(style),
                        AiResultPreviewKind.PortraitEnhancement))
                {
                    portraitEnhancementControl?.SetStatus("결과를 확인한 뒤 적용하세요.");
                    if (previewForm.ShowDialog(this) != DialogResult.OK)
                    {
                        finalImage.Dispose();
                        finalImage = null;
                        return;
                    }
                }

                if (CurrentUser.UserNo <= 0 ||
                    !ReferenceEquals(editor.State.CurrentImage, sourceStateImage))
                {
                    finalImage.Dispose();
                    finalImage = null;
                    return;
                }

                editor.PushUndoSnapshot();
                editor.State.ReplaceCurrentImage(finalImage);
                finalImage = null; // EditorState가 결과 Bitmap의 수명을 관리한다.

                editor.InitializeLayers();
                RefreshAfterImageChange();
                UpdateUndoRedoMenuState();

                MessageBox.Show(
                    "Cloudflare AI 인물 사진 자동보정이 완료되었습니다.",
                    "AI 인물 사진 자동보정 완료",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Information);
            }
            catch (OperationCanceledException)
            {
                finalImage?.Dispose();
                portraitEnhancementControl?.SetStatus("AI 자동보정을 취소했습니다.");
            }
            catch (Exception ex)
            {
                finalImage?.Dispose();
                portraitEnhancementControl?.SetStatus("AI 자동보정에 실패했습니다.", true);
                MessageBox.Show(
                    "인물 사진 자동보정 중 오류가 발생했습니다.\r\n" + ex.Message,
                    "AI 인물 사진 자동보정",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Error);
            }
            finally
            {
                portraitEnhanceCancellation?.Dispose();
                portraitEnhanceCancellation = null;
                portraitEnhancementControl?.SetBusy(false);
                SetAiImageProcessingMenuEnabled(true);
                aiImageProcessingBusy = false;
                Cursor = Cursors.Default;
            }
        }

        private static string GetPortraitEnhancementInstruction(
            PortraitEnhancementStyle style)
        {
            return style switch
            {
                PortraitEnhancementStyle.SkinDetail =>
                    "Prioritize subtle, natural skin-tone balancing and minor blemish refinement. Preserve visible pores and facial texture.",
                PortraitEnhancementStyle.ToneAndExposure =>
                    "Prioritize subtle exposure, white balance and color correction. Keep skin texture, facial details and all image content unchanged.",
                _ =>
                    "Use balanced, subtle portrait retouching across skin tone, exposure and color while preserving natural detail."
            };
        }

        private static string GetPortraitEnhancementStyleName(
            PortraitEnhancementStyle style)
        {
            return style switch
            {
                PortraitEnhancementStyle.SkinDetail => "피부결 중심 보정",
                PortraitEnhancementStyle.ToneAndExposure => "색감·노출 중심 보정",
                _ => "자연 보정"
            };
        }

        private async Task ExecuteAIQualityEnhancement(IAiUpscaler aiModel)
        {
            if (!EnsureAiFeatureAccess())
                return;

            if (editor.State.CurrentImage == null)
            {
                MessageBox.Show(
                    "먼저 작업할 이미지를 열어주세요.",
                    "AI 화질 개선",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Information);
                return;
            }

            if (aiImageProcessingBusy)
                return;

            StopInteractiveTools();

            // 화질 개선은 크기 조절 기능이 아니므로 현재 캔버스 상태를 그대로 보존한다.
            Bitmap sourceStateImage = editor.State.CurrentImage;
            Size originalImageSize = sourceStateImage.Size;
            float previousZoomRatio = editor.State.ZoomRatio;
            Point previousScrollPosition = new Point(
                -pnlCanvas.AutoScrollPosition.X,
                -pnlCanvas.AutoScrollPosition.Y);

            Bitmap? finalImage = null;
            CancellationTokenSource? activeCancellation = null;

            try
            {
                aiImageProcessingBusy = true;
                Cursor = Cursors.WaitCursor;
                SetAiImageProcessingMenuEnabled(false);
                aiQualityEnhancementControl?.SetBusy(true);
                aiQualityEnhancementControl?.SetStatus("AI 모델을 준비하는 중...");
                qualityEnhancementCancellation?.Dispose();
                activeCancellation = new CancellationTokenSource();
                qualityEnhancementCancellation = activeCancellation;

                // Real-ESRGAN 계열 모델은 내부적으로 x4 타일을 만들지만,
                // 업스케일러가 타일 단위로 원본 크기에 고품질 반영해 캔버스와
                // SNS 작업 크기를 유지한다.
                using Bitmap sourceImage = new Bitmap(sourceStateImage);
                Bitmap? enhancedImage = await Task.Run(
                    () => aiModel.Enhance(
                        sourceImage,
                        activeCancellation.Token),
                    activeCancellation.Token);

                try
                {
                    // 현재 업스케일러는 타일 단위로 원본 크기 결과를 반환한다.
                    // 다른 x4 모델을 추가하더라도 기존 축소 경로와 호환되도록 둔다.
                    if (enhancedImage.Size == originalImageSize)
                    {
                        finalImage = enhancedImage;
                        enhancedImage = null;
                    }
                    else
                    {
                        finalImage = ResizeEnhancedImageToOriginalSize(
                            enhancedImage,
                            originalImageSize);
                    }
                }
                finally
                {
                    enhancedImage?.Dispose();
                }
                aiQualityEnhancementControl?.SetStatus(
                    "화질 개선이 완료되었습니다. 결과를 확인하세요.");

                if (CurrentUser.UserNo <= 0 ||
                    !ReferenceEquals(editor.State.CurrentImage, sourceStateImage))
                {
                    finalImage.Dispose();
                    finalImage = null;
                    MessageBox.Show(
                        "화질 개선 중 다른 편집이 적용되어 결과를 반영하지 않았습니다.",
                        "AI 화질 개선",
                        MessageBoxButtons.OK,
                        MessageBoxIcon.Information);
                    return;
                }

                using (PhotoRestorationPreviewForm previewForm =
                    new PhotoRestorationPreviewForm(
                        sourceStateImage,
                        finalImage,
                        aiModel.ModelName + " 화질 개선",
                        AiResultPreviewKind.QualityEnhancement))
                {
                    if (previewForm.ShowDialog(this) != DialogResult.OK)
                    {
                        finalImage.Dispose();
                        finalImage = null;
                        return;
                    }
                }

                if (CurrentUser.UserNo <= 0 ||
                    !ReferenceEquals(editor.State.CurrentImage, sourceStateImage))
                {
                    finalImage.Dispose();
                    finalImage = null;
                    return;
                }

                // AI 추론 결과를 사용자가 확인하고 적용한 뒤에만 Undo 스냅샷을 남긴다.
                editor.PushUndoSnapshot();
                editor.State.ReplaceCurrentImage(finalImage);
                finalImage = null; // EditorState가 이미지 수명을 관리한다.

                // 기존 편집 흐름과 호환되도록 레이어를 현재 결과 기준으로 갱신한다.
                // 단, FitToPanel은 호출하지 않아 사용자가 보던 캔버스 확대율을 유지한다.
                editor.InitializeLayers();
                editor.State.SetZoom(previousZoomRatio);

                RefreshCanvas();
                RefreshLayerUI();
                RecordTimelineSnapshot();

                // AutoScrollPosition을 읽으면 음수 좌표가 반환되므로
                // 위에서 양수로 저장한 값을 그대로 다시 설정한다.
                pnlCanvas.AutoScrollPosition = previousScrollPosition;

                UpdateUndoRedoMenuState();

                MessageBox.Show(
                    $"[{aiModel.ModelName}] 화질 개선이 완료되었습니다!\r\n" +
                    $"이미지 크기 유지: {originalImageSize.Width} x {originalImageSize.Height}",
                    "AI 화질 개선 완료",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Information);
            }
            catch (OperationCanceledException)
            {
                finalImage?.Dispose();
                aiQualityEnhancementControl?.SetStatus("AI 화질 개선을 취소했습니다.");
            }
            catch (Exception ex)
            {
                finalImage?.Dispose();
                aiQualityEnhancementControl?.SetStatus(
                    "AI 화질 개선에 실패했습니다.", true);

                MessageBox.Show(
                    "AI 화질 개선 중 오류가 발생했습니다.\r\n" + ex.Message,
                    "AI 화질 개선",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Error);
            }
            finally
            {
                if (ReferenceEquals(
                        qualityEnhancementCancellation,
                        activeCancellation))
                {
                    qualityEnhancementCancellation?.Dispose();
                    qualityEnhancementCancellation = null;
                }
                aiQualityEnhancementControl?.SetBusy(false);
                SetAiImageProcessingMenuEnabled(true);
                aiImageProcessingBusy = false;
                Cursor = Cursors.Default;
            }
        }

        private void SetAiImageProcessingMenuEnabled(bool enabled)
        {
            bool canUse = enabled &&
                CurrentUser.UserNo > 0 &&
                editor.State.CurrentImage != null;
            if (menuAIQualityEnhancement != null)
                menuAIQualityEnhancement.Enabled = canUse;
            if (menuAIPortraitEnhance != null)
                menuAIPortraitEnhance.Enabled = canUse;
        }

        /// <summary>
        /// x4 AI 결과를 원래 이미지 크기로 고품질 축소한다.
        /// 최종 Width/Height를 유지하므로 캔버스 크기와 줌 기준이 바뀌지 않는다.
        /// </summary>
        private static Bitmap ResizeEnhancedImageToOriginalSize(
            Bitmap source,
            Size originalSize)
        {
            if (source.Width == originalSize.Width &&
                source.Height == originalSize.Height)
            {
                return new Bitmap(source);
            }

            Bitmap resized = new Bitmap(
                originalSize.Width,
                originalSize.Height,
                System.Drawing.Imaging.PixelFormat.Format32bppArgb);

            using (Graphics graphics = Graphics.FromImage(resized))
            {
                graphics.CompositingMode =
                    System.Drawing.Drawing2D.CompositingMode.SourceCopy;
                graphics.CompositingQuality =
                    System.Drawing.Drawing2D.CompositingQuality.HighQuality;
                graphics.InterpolationMode =
                    System.Drawing.Drawing2D.InterpolationMode.HighQualityBicubic;
                graphics.SmoothingMode =
                    System.Drawing.Drawing2D.SmoothingMode.HighQuality;
                graphics.PixelOffsetMode =
                    System.Drawing.Drawing2D.PixelOffsetMode.HighQuality;

                graphics.DrawImage(
                    source,
                    new Rectangle(0, 0, originalSize.Width, originalSize.Height),
                    0,
                    0,
                    source.Width,
                    source.Height,
                    GraphicsUnit.Pixel);
            }

            return resized;
        }

        private async void MusicRecommendationControl_RunRequested(
            object? sender,
            EventArgs e)
        {
            if (!EnsureAiFeatureAccess())
                return;

            await RequestMusicRecommendationAsync();
        }

        private void MusicRecommendationControl_CancelRequested(
            object? sender,
            EventArgs e)
        {
            musicRecommendationControl?.SetStatus("음악 추천을 취소하는 중...");
            musicRecommendationCancellation?.Cancel();
        }

        private void MusicRecommendationControl_CloseRequested(
            object? sender,
            EventArgs e)
        {
            if (musicRecommendationControl != null &&
                pnlToolOptions.Controls.Contains(musicRecommendationControl))
            {
                pnlToolOptions.Controls.Remove(musicRecommendationControl);
                RefreshCanvas();
            }
        }

        private async Task RequestMusicRecommendationAsync()
        {
            if (!EnsureAiFeatureAccess())
                return;

            if (musicRecommendationBusy || musicRecommendationControl == null ||
                editor.State.CurrentImage == null)
            {
                return;
            }

            musicRecommendationBusy = true;
            musicRecommendationControl.SetBusy(true);
            musicRecommendationControl.SetStatus("AI 서버를 확인하는 중...");
            musicRecommendationCancellation?.Dispose();
            musicRecommendationCancellation = new CancellationTokenSource();
            CancellationToken cancellationToken = musicRecommendationCancellation.Token;

            using Bitmap sourceImage = new Bitmap(editor.State.CurrentImage);
            if (lblMusicRecommendation != null)
            {
                lblMusicRecommendation.Text = "음악 분석 중... 잠시만 기다려주세요 🎧";
                lblMusicRecommendation.Visible = true;
            }
            try
            {
                await LocalAiServerManager.EnsureReadyAsync(
                    cancellationToken: cancellationToken);

                using MultipartFormDataContent content =
                    new MultipartFormDataContent();
                byte[] imageBytes = await Task.Run(() =>
                {
                    using MemoryStream stream = new MemoryStream();
                    sourceImage.Save(stream, System.Drawing.Imaging.ImageFormat.Png);
                    return stream.ToArray();
                }, cancellationToken);
                ByteArrayContent imageContent = new ByteArrayContent(imageBytes);
                imageContent.Headers.ContentType =
                    new MediaTypeHeaderValue("image/png");
                content.Add(imageContent, "file", "current.png");

                musicRecommendationControl.SetStatus("사진 분위기를 분석하는 중...");
                using HttpResponseMessage response = await LocalAiServerManager.HttpClient.PostAsync(
                    LocalAiServerManager.BaseUrl + "/analyze-mood", content, cancellationToken);
                if (!response.IsSuccessStatusCode)
                {
                    string errorText = await response.Content.ReadAsStringAsync();
                    string detail =
                        LocalAiServerManager.ExtractErrorDetail(errorText);
                    throw new InvalidOperationException(
                        string.IsNullOrWhiteSpace(detail)
                            ? $"음악 추천에 실패했습니다. ({(int)response.StatusCode})"
                            : detail);
                }

                string jsonResponse = await response.Content.ReadAsStringAsync();
                using JsonDocument document = JsonDocument.Parse(jsonResponse);
                if (!document.RootElement.TryGetProperty(
                        "recommendation",
                        out JsonElement recommendationElement))
                {
                    throw new InvalidOperationException(
                        "음악 추천 결과 형식이 올바르지 않습니다.");
                }

                string resultText = recommendationElement.GetString()?.Trim()
                    ?? string.Empty;
                if (string.IsNullOrWhiteSpace(resultText))
                    throw new InvalidOperationException("음악 추천 결과가 비어 있습니다.");

                if (cancellationToken.IsCancellationRequested ||
                    CurrentUser.UserNo <= 0)
                {
                    return;
                }

                musicRecommendationControl.SetResult(resultText);
                musicRecommendationControl.SetStatus("추천이 완료되었습니다.");
                if (lblMusicRecommendation != null)
                    lblMusicRecommendation.Text = resultText;
            }
            catch (OperationCanceledException)
            {
                musicRecommendationControl.SetStatus("AI 음악 추천을 취소했습니다.");
            }
            catch (Exception ex)
            {
                musicRecommendationControl.SetStatus(ex.Message, true);
                if (lblMusicRecommendation != null)
                    lblMusicRecommendation.Text = "추천 실패";
            }
            finally
            {
                musicRecommendationCancellation?.Dispose();
                musicRecommendationCancellation = null;
                musicRecommendationBusy = false;
                musicRecommendationControl.SetBusy(false);
            }
        }

        private void menuMusicRecommend_Click(object? sender, EventArgs e)
        {
            if (!EnsureAiFeatureAccess()) return;
            StopInteractiveTools();
            if (editor.State.CurrentImage == null)
            {
                MessageBox.Show("먼저 이미지를 불러와 주세요.");
                return;
            }

            if (musicRecommendationControl == null)
                return;

            musicRecommendationControl.PrepareForImage(editor.State.CurrentImage.Size);
            ShowToolControl(musicRecommendationControl);
        }

        private void ShowAiCaptionRecommendationOptions()
        {
            StopInteractiveTools();
            if (editor.State.CurrentImage == null)
            {
                MessageBox.Show("먼저 이미지를 불러와 주세요.");
                return;
            }

            if (aiCaptionRecommendationControl == null)
                return;

            aiCaptionRecommendationControl.PrepareForImage(editor.State.CurrentImage.Size);
            ShowToolControl(aiCaptionRecommendationControl);
        }

        private async void AiCaptionRecommendationControl_RunRequested(
            object? sender,
            EventArgs e)
        {
            if (!EnsureAiFeatureAccess())
                return;

            await RequestAiCaptionRecommendationsAsync();
        }

        private void AiCaptionRecommendationControl_CancelRequested(
            object? sender,
            EventArgs e)
        {
            aiCaptionRecommendationControl?.SetStatus("유행어·밈 추천을 취소하는 중...");
            aiCaptionRecommendationCancellation?.Cancel();
        }

        private void AiCaptionRecommendationControl_CloseRequested(
            object? sender,
            EventArgs e)
        {
            if (aiCaptionRecommendationControl != null &&
                pnlToolOptions.Controls.Contains(aiCaptionRecommendationControl))
            {
                pnlToolOptions.Controls.Remove(aiCaptionRecommendationControl);
                RefreshCanvas();
            }
        }

        private void AiCaptionRecommendationControl_StartEditingRequested(
            object? sender,
            AiCaptionSuggestionEventArgs e)
        {
            if (!EnsureAiFeatureAccess())
                return;

            if (editor.State.CurrentImage == null)
                return;

            // 추천 결과는 아직 편집 상태가 아닙니다. 현재 TextToolPanel의 임시
            // 미리보기로 넘긴 뒤, 사용자가 적용할 때만 레이어를 추가합니다.
            StopInteractiveTools();
            OpenTextTool(
                e.Suggestion.Text,
                e.Suggestion.Placement,
                e.Suggestion.Effect,
                e.Suggestion.AnchorX,
                e.Suggestion.AnchorY,
                e.Suggestion.Alignment,
                e.Suggestion.TextColor,
                e.Suggestion.FontSizeRatio,
                e.Suggestion.FontStyle,
                e.Suggestion.EffectColor,
                e.Suggestion.EffectIntensity);
        }

        private async Task RequestAiCaptionRecommendationsAsync()
        {
            if (!EnsureAiFeatureAccess())
                return;

            if (aiCaptionRecommendationBusy || aiCaptionRecommendationControl == null ||
                editor.State.CurrentImage == null)
            {
                return;
            }

            aiCaptionRecommendationBusy = true;
            aiCaptionRecommendationControl.SetBusy(true);
            aiCaptionRecommendationControl.SetStatus("AI 서버를 확인하는 중...");
            aiCaptionRecommendationCancellation?.Dispose();
            aiCaptionRecommendationCancellation = new CancellationTokenSource();
            CancellationToken cancellationToken = aiCaptionRecommendationCancellation.Token;

            using Bitmap sourceImage = new Bitmap(editor.State.CurrentImage);
            try
            {
                aiCaptionRecommendationControl.SetStatus("사진의 얼굴·사물·여백과 문구를 함께 분석하는 중...");
                IReadOnlyList<AiCaptionSuggestion> suggestions =
                    await AiCaptionService.RequestAsync(
                        sourceImage,
                        aiCaptionRecommendationControl.Platform,
                        aiCaptionRecommendationControl.RecommendationType,
                        cancellationToken);
                if (cancellationToken.IsCancellationRequested ||
                    CurrentUser.UserNo <= 0)
                    return;

                aiCaptionRecommendationControl.SetResults(suggestions);
                aiCaptionRecommendationControl.SetStatus(
                    "문구를 선택하면 캔버스에서 바로 수정할 수 있습니다.");
            }
            catch (OperationCanceledException)
            {
                aiCaptionRecommendationControl.SetStatus("유행어·밈 추천을 취소했습니다.");
            }
            catch (Exception ex)
            {
                aiCaptionRecommendationControl.SetStatus(ex.Message, true);
            }
            finally
            {
                aiCaptionRecommendationCancellation?.Dispose();
                aiCaptionRecommendationCancellation = null;
                aiCaptionRecommendationBusy = false;
                aiCaptionRecommendationControl.SetBusy(false);
            }
        }
    }
}
