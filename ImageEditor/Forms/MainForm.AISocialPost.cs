using ImageEditor.Controls;
using ImageEditor.Services;
using System;
using System.Drawing;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace ImageEditor
{
    public partial class MainForm : Form
    {
        private ToolStripMenuItem? menuAISocialPost;
        private AiSocialPostControl? aiSocialPostControl;
        private CancellationTokenSource? aiSocialPostCancellation;
        private bool aiSocialPostBusy;

        private void InitializeAiSocialPostFeature()
        {
            aiSocialPostControl = new AiSocialPostControl();
            aiSocialPostControl.RunRequested += AiSocialPostControl_RunRequested;
            aiSocialPostControl.CancelRequested += AiSocialPostControl_CancelRequested;
            aiSocialPostControl.CloseRequested += AiSocialPostControl_CloseRequested;

            menuAISocialPost = new ToolStripMenuItem("게시글·해시태그 추천")
            {
                ToolTipText =
                    "현재 사진을 분석해 SNS 게시글 2문장과 해시태그를 추천합니다."
            };
            menuAISocialPost.Click += MenuAISocialPost_Click;

            // 기존 자동 문구와 음악 추천은 그대로 두고 추천 기능의 마지막에 추가한다.
            menuAIRemoveObject.DropDownItems.Add(menuAISocialPost);
        }

        private void MenuAISocialPost_Click(object? sender, EventArgs e)
        {
            if (!EnsureAiFeatureAccess())
                return;

            StopInteractiveTools();
            if (editor.State.CurrentImage == null)
            {
                MessageBox.Show(
                    "먼저 이미지를 불러와 주세요.",
                    "게시글·해시태그 추천",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Information);
                return;
            }

            if (aiSocialPostControl == null)
                return;

            aiSocialPostControl.PrepareForImage(editor.State.CurrentImage.Size);
            ShowToolControl(aiSocialPostControl);
        }

        private async void AiSocialPostControl_RunRequested(
            object? sender,
            EventArgs e)
        {
            if (!EnsureAiFeatureAccess())
                return;

            await RequestAiSocialPostAsync();
        }

        private void AiSocialPostControl_CancelRequested(
            object? sender,
            EventArgs e)
        {
            aiSocialPostControl?.SetStatus("AI 게시글 추천을 취소하는 중...");
            aiSocialPostCancellation?.Cancel();
        }

        private void AiSocialPostControl_CloseRequested(
            object? sender,
            EventArgs e)
        {
            if (aiSocialPostControl != null &&
                pnlToolOptions.Controls.Contains(aiSocialPostControl))
            {
                pnlToolOptions.Controls.Remove(aiSocialPostControl);
                RefreshCanvas();
            }
        }

        private async Task RequestAiSocialPostAsync()
        {
            if (!EnsureAiFeatureAccess())
                return;

            if (aiSocialPostBusy || aiSocialPostControl == null ||
                editor.State.CurrentImage == null)
            {
                return;
            }

            aiSocialPostBusy = true;
            aiSocialPostControl.SetBusy(true);
            aiSocialPostControl.SetStatus("AI 서버를 확인하는 중...");
            aiSocialPostCancellation?.Dispose();
            aiSocialPostCancellation = new CancellationTokenSource();
            CancellationToken cancellationToken = aiSocialPostCancellation.Token;

            using Bitmap sourceImage = new Bitmap(editor.State.CurrentImage);
            try
            {
                aiSocialPostControl.SetStatus(
                    $"{aiSocialPostControl.Platform}용 게시글을 생성하는 중...");
                AiSocialPostResult result = await AiSocialPostService.RequestAsync(
                    sourceImage,
                    aiSocialPostControl.Platform,
                    aiSocialPostControl.Tone,
                    aiSocialPostControl.PostLength,
                    cancellationToken);
                if (cancellationToken.IsCancellationRequested ||
                    CurrentUser.UserNo <= 0)
                    return;

                aiSocialPostControl.SetResult(result.Post, result.Hashtags);
                aiSocialPostControl.SetStatus(
                    "추천이 완료되었습니다. 원하는 내용을 복사해 사용하세요.");
            }
            catch (OperationCanceledException)
            {
                aiSocialPostControl.SetStatus("AI 게시글 추천을 취소했습니다.");
            }
            catch (Exception ex)
            {
                aiSocialPostControl.SetStatus(ex.Message, true);
            }
            finally
            {
                aiSocialPostCancellation?.Dispose();
                aiSocialPostCancellation = null;
                aiSocialPostBusy = false;
                aiSocialPostControl.SetBusy(false);
            }
        }
    }
}
