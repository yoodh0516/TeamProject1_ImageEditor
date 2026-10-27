using System;
using System.Drawing;
using System.IO;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;
using ImageEditor.Core;
using ImageEditor.Controls;
using ImageEditor.Services;

namespace ImageEditor.Tools
{
    public class AIObjectRemovalTool
    {
        private readonly PictureBox pictureBox;
        private readonly EditorState state;
        private readonly Control optionHost;

        private bool isActive;
        private string currentMode = "POINT";
        private bool isDragging;

        private int startX;
        private int startY;
        private int currentX;
        private int currentY;

        private int targetX;
        private int targetY;
        private int boxX1;
        private int boxY1;
        private int boxX2;
        private int boxY2;

        private readonly Button btnErase;
        private readonly Button btnCancel;
        private readonly Label lblGuide;
        private Bitmap? previewImage;
        private bool finalPreviewReady;
        private CancellationTokenSource? requestCancellation;
        private int requestSequence;

        public event Action? ImageChanged;
        public event Action? ImageApplying;
        public event Action<Bitmap>? ImageReady;

        public AIObjectRemovalTool(
            PictureBox pictureBox,
            EditorState state,
            Control optionHost)
        {
            this.pictureBox = pictureBox;
            this.state = state;
            this.optionHost = optionHost;

            lblGuide = new Label
            {
                AutoSize = false,
                Location = new Point(15, 20),
                Size = new Size(250, 70),
                TextAlign = ContentAlignment.MiddleLeft
            };

            btnErase = new Button
            {
                Text = "이 영역 지우기",
                Location = new Point(65, 115),
                Size = new Size(150, 35),
                Visible = false
            };

            btnErase.Click += BtnErase_Click;

            btnCancel = new Button
            {
                Text = "취소",
                Location = new Point(65, 160),
                Size = new Size(150, 35),
                Visible = false
            };
            btnCancel.Click += (_, _) => Stop();

            pictureBox.MouseDown += PictureBox_MouseDown;
            pictureBox.MouseMove += PictureBox_MouseMove;
            pictureBox.MouseUp += PictureBox_MouseUp;
            pictureBox.MouseClick += PictureBox_MouseClick;
            pictureBox.Paint += PictureBox_Paint;
        }

        public void StartPointMode()
        {
            if (state.CurrentImage == null)
                return;

            isActive = true;
            currentMode = "POINT";
            isDragging = false;
            ClearPreview();

            ShowOptions(
                "포인트 모드\n지울 물체를 이미지에서 한 번 클릭하세요."
            );

            pictureBox.Cursor = Cursors.Cross;
        }

        public void StartBoxMode()
        {
            if (state.CurrentImage == null)
                return;

            isActive = true;
            currentMode = "BOX";
            isDragging = false;
            ClearPreview();

            ShowOptions(
                "드래그 모드\n지울 물체를 사각형으로 드래그해서 선택하세요."
            );

            pictureBox.Cursor = Cursors.Cross;
        }

        public void Stop()
        {
            requestCancellation?.Cancel();
            isActive = false;
            isDragging = false;
            btnErase.Visible = false;
            btnCancel.Visible = false;
            pictureBox.Cursor = Cursors.Default;
            ClearPreview();
            pictureBox.Invalidate();
        }

        private void ShowOptions(string guide)
        {
            optionHost.Controls.Clear();

            FlowLayoutPanel root = AiQualityEnhancementControl.CreateRoot();

            Panel heading = new Panel { Height = 72, Margin = new Padding(0, 0, 0, 8) };
            Label title = ToolOptionStyle.CreateLabel("AI 물체 제거", 16F, FontStyle.Bold, ToolOptionStyle.Title);
            Label description = ToolOptionStyle.CreateWrappedLabel(
                "사진에서 지우고 싶은 대상을 선택하세요.",
                9F, FontStyle.Regular, ToolOptionStyle.Description, 40);
            title.Location = Point.Empty;
            description.Location = new Point(1, 31);
            ToolOptionStyle.FillCardControl(heading, description, 1, 0);
            heading.Controls.Add(title);
            heading.Controls.Add(description);

            Panel guideCard = ToolOptionStyle.CreateCard(104);
            Label guideTitle = ToolOptionStyle.CreateLabel("선택 방법", 10F, FontStyle.Bold, ToolOptionStyle.Title);
            guideTitle.Location = new Point(14, 11);
            lblGuide.Text = currentMode == "POINT"
                ? "포인터 모드\r\n지울 물체 위를 한 번 클릭하세요."
                : "드래그 모드\r\n지울 물체를 감싸도록 영역을 드래그하세요.";
            lblGuide.AutoSize = false;
            lblGuide.Location = new Point(14, 37);
            lblGuide.Height = 55;
            lblGuide.Font = new Font("맑은 고딕", 9F);
            lblGuide.ForeColor = ToolOptionStyle.Description;
            ToolOptionStyle.FillCardControl(guideCard, lblGuide);
            guideCard.Controls.Add(guideTitle);
            guideCard.Controls.Add(lblGuide);

            Panel stateCard = ToolOptionStyle.CreateCard(66);
            Label stateTitle = ToolOptionStyle.CreateLabel("현재 모드", 9F, FontStyle.Regular, ToolOptionStyle.Description);
            Label stateValue = ToolOptionStyle.CreateLabel(currentMode == "POINT" ? "포인터로 대상 선택" : "영역을 드래그해 선택", 10F, FontStyle.Bold, ToolOptionStyle.Accent);
            stateTitle.Location = new Point(14, 10);
            stateValue.Location = new Point(14, 33);
            stateCard.Controls.Add(stateTitle);
            stateCard.Controls.Add(stateValue);

            Panel modeCard = ToolOptionStyle.CreateCard(108);
            Label modeTitle = ToolOptionStyle.CreateLabel("선택 방식 변경", 10F, FontStyle.Bold, ToolOptionStyle.Title);
            modeTitle.Location = new Point(14, 11);
            Button pointButton = new Button
            {
                Text = "포인터",
                FlatStyle = FlatStyle.Flat,
                UseVisualStyleBackColor = false,
                Location = new Point(14, 42),
                Size = new Size(108, 38)
            };
            Button boxButton = new Button
            {
                Text = "영역 드래그",
                FlatStyle = FlatStyle.Flat,
                UseVisualStyleBackColor = false,
                Location = new Point(128, 42),
                Size = new Size(108, 38)
            };
            AiQualityEnhancementControl.StyleModelButton(pointButton, currentMode == "POINT");
            AiQualityEnhancementControl.StyleModelButton(boxButton, currentMode == "BOX");
            pointButton.Click += (_, _) => StartPointMode();
            boxButton.Click += (_, _) => StartBoxMode();
            modeCard.Resize += (_, _) =>
            {
                int gap = 6;
                int width = Math.Max(1, (modeCard.ClientSize.Width - 28 - gap) / 2);
                pointButton.Width = width;
                boxButton.Left = pointButton.Right + gap;
                boxButton.Width = width;
            };
            modeCard.Controls.Add(modeTitle);
            modeCard.Controls.Add(pointButton);
            modeCard.Controls.Add(boxButton);

            TableLayoutPanel actions = new TableLayoutPanel { Height = 38, ColumnCount = 2, Margin = new Padding(0, 0, 0, 7) };
            actions.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 40F));
            actions.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 60F));
            btnCancel.Text = "취소";
            btnCancel.FlatStyle = FlatStyle.Flat;
            btnCancel.BackColor = Color.White;
            btnCancel.ForeColor = ToolOptionStyle.Title;
            btnCancel.FlatAppearance.BorderColor = ToolOptionStyle.Border;
            btnCancel.Dock = DockStyle.Fill;
            btnCancel.Margin = Padding.Empty;
            btnErase.Text = "선택 영역 지우기";
            ToolOptionStyle.StylePrimaryButton(btnErase);
            btnErase.Dock = DockStyle.Fill;
            btnErase.Margin = new Padding(8, 0, 0, 0);
            btnErase.Visible = false;
            finalPreviewReady = false;
            actions.Controls.Add(btnCancel, 0, 0);
            actions.Controls.Add(btnErase, 1, 0);

            Label note = ToolOptionStyle.CreateLabel("선택 후 AI 결과를 먼저 미리보기로 확인합니다.\r\n적용을 누르기 전에는 원본 이미지가 바뀌지 않습니다.", 8.5F, FontStyle.Regular, ToolOptionStyle.Description);
            note.AutoSize = false;
            note.Height = 38;

            root.Controls.Add(heading);
            root.Controls.Add(guideCard);
            root.Controls.Add(stateCard);
            root.Controls.Add(modeCard);
            root.Controls.Add(note);
            root.Controls.Add(actions);
            optionHost.Controls.Add(root);
            root.BringToFront();
        }

        private void PictureBox_MouseDown(object? sender, MouseEventArgs e)
        {
            if (!isActive || state.CurrentImage == null)
                return;

            if (finalPreviewReady)
                return;

            if (e.Button == MouseButtons.Left && currentMode == "BOX")
            {
                isDragging = true;
                startX = e.X;
                startY = e.Y;
                currentX = e.X;
                currentY = e.Y;
                btnErase.Visible = false;
                ClearPreview();
            }
        }

        private void PictureBox_MouseMove(object? sender, MouseEventArgs e)
        {
            if (!isActive || !isDragging || currentMode != "BOX")
                return;

            currentX = e.X;
            currentY = e.Y;
            pictureBox.Invalidate();
        }

        private async void PictureBox_MouseUp(object? sender, MouseEventArgs e)
        {
            if (!isActive || !isDragging || currentMode != "BOX")
                return;

            isDragging = false;

            GetRealImageCoordinates(
                Math.Min(startX, e.X),
                Math.Min(startY, e.Y),
                Math.Max(startX, e.X),
                Math.Max(startY, e.Y),
                out boxX1,
                out boxY1,
                out boxX2,
                out boxY2
            );

            if (Math.Abs(boxX2 - boxX1) < 5 ||
                Math.Abs(boxY2 - boxY1) < 5)
            {
                return;
            }

            await RequestAIProcess("preview-mask");
            pictureBox.Invalidate();
        }

        private async void PictureBox_MouseClick(object? sender, MouseEventArgs e)
        {
            if (!isActive || state.CurrentImage == null)
                return;

            if (finalPreviewReady)
                return;

            if (e.Button != MouseButtons.Left || currentMode != "POINT")
                return;

            GetRealImageCoordinate(e.X, e.Y, out targetX, out targetY);
            await RequestAIProcess("preview-mask");
        }

        private void PictureBox_Paint(object? sender, PaintEventArgs e)
        {
            if (!isActive || !isDragging || currentMode != "BOX")
                return;

            int x = Math.Min(startX, currentX);
            int y = Math.Min(startY, currentY);
            int width = Math.Abs(startX - currentX);
            int height = Math.Abs(startY - currentY);

            using Pen pen = new Pen(Color.Red, 2)
            {
                DashStyle = System.Drawing.Drawing2D.DashStyle.Dash
            };

            e.Graphics.DrawRectangle(pen, x, y, width, height);
        }

        // 현재 프로젝트는 PictureBox 자체 크기를 Zoom 비율에 따라 조절하고
        // StretchImage로 표시하므로 단순 비율 계산으로 실제 이미지 좌표를 구할 수 있다.
        private void GetRealImageCoordinate(
            int canvasX,
            int canvasY,
            out int imageX,
            out int imageY)
        {
            imageX = 0;
            imageY = 0;

            if (state.CurrentImage == null ||
                pictureBox.ClientSize.Width <= 0 ||
                pictureBox.ClientSize.Height <= 0)
            {
                return;
            }

            float scaleX =
                (float)state.CurrentImage.Width /
                pictureBox.ClientSize.Width;

            float scaleY =
                (float)state.CurrentImage.Height /
                pictureBox.ClientSize.Height;

            imageX = (int)(canvasX * scaleX);
            imageY = (int)(canvasY * scaleY);

            imageX = Math.Clamp(imageX, 0, state.CurrentImage.Width - 1);
            imageY = Math.Clamp(imageY, 0, state.CurrentImage.Height - 1);
        }

        private void GetRealImageCoordinates(
            int startCanvasX,
            int startCanvasY,
            int endCanvasX,
            int endCanvasY,
            out int imageX1,
            out int imageY1,
            out int imageX2,
            out int imageY2)
        {
            GetRealImageCoordinate(
                startCanvasX,
                startCanvasY,
                out imageX1,
                out imageY1
            );

            GetRealImageCoordinate(
                endCanvasX,
                endCanvasY,
                out imageX2,
                out imageY2
            );
        }

        private async void BtnErase_Click(object? sender, EventArgs e)
        {
            if (!isActive || state.CurrentImage == null)
                return;

            if (finalPreviewReady)
            {
                if (previewImage == null || ImageReady == null)
                    return;

                Bitmap imageToApply = new Bitmap(previewImage);
                ImageReady.Invoke(imageToApply);
                Stop();
                return;
            }

            btnErase.Enabled = false;
            btnErase.Text = "지우는 중...";

            await RequestAIProcess("remove-object");

            btnErase.Text = "이 영역 지우기";
            if (isActive)
            {
                btnErase.Enabled = true;
                if (finalPreviewReady)
                    btnErase.Text = "적용";
            }
        }

        private async Task RequestAIProcess(string endpoint)
        {
            if (state.CurrentImage == null)
                return;

            using Bitmap sourceImage = new Bitmap(state.CurrentImage);
            requestCancellation?.Cancel();
            requestCancellation?.Dispose();
            requestCancellation = new CancellationTokenSource();
            CancellationTokenSource activeCancellation = requestCancellation;
            CancellationToken cancellationToken = activeCancellation.Token;
            int requestId = Interlocked.Increment(ref requestSequence);

            try
            {
                Cursor.Current = Cursors.WaitCursor;
                await LocalAiServerManager.EnsureReadyAsync(
                    cancellationToken: cancellationToken);

                using MultipartFormDataContent content =
                    new MultipartFormDataContent();

                byte[] imageBytes = await Task.Run(() =>
                {
                    using MemoryStream ms = new MemoryStream();
                    sourceImage.Save(
                        ms,
                        System.Drawing.Imaging.ImageFormat.Png
                    );
                    return ms.ToArray();
                }, cancellationToken);
                ByteArrayContent imageContent =
                    new ByteArrayContent(imageBytes);
                imageContent.Headers.ContentType =
                    new MediaTypeHeaderValue("image/png");
                content.Add(imageContent, "file", "current.png");

                content.Add(new StringContent(currentMode), "mode");
                content.Add(new StringContent(targetX.ToString()), "x");
                content.Add(new StringContent(targetY.ToString()), "y");
                content.Add(new StringContent(boxX1.ToString()), "x1");
                content.Add(new StringContent(boxY1.ToString()), "y1");
                content.Add(new StringContent(boxX2.ToString()), "x2");
                content.Add(new StringContent(boxY2.ToString()), "y2");

                using HttpResponseMessage response = await LocalAiServerManager.HttpClient.PostAsync(
                    $"{LocalAiServerManager.BaseUrl}/{endpoint}",
                    content,
                    cancellationToken
                );

                if (!response.IsSuccessStatusCode)
                {
                    string responseText =
                        await response.Content.ReadAsStringAsync();
                    string detail =
                        LocalAiServerManager.ExtractErrorDetail(responseText);
                    throw new InvalidOperationException(
                        string.IsNullOrWhiteSpace(detail)
                            ? $"AI 물체 처리에 실패했습니다. ({(int)response.StatusCode})"
                            : detail);
                }

                byte[] resultBytes =
                    await response.Content.ReadAsByteArrayAsync(cancellationToken);

                using MemoryStream resultStream =
                    new MemoryStream(resultBytes);

                using Image tempImage =
                    Image.FromStream(resultStream);

                Bitmap resultImage =
                    new Bitmap(tempImage);

                if (requestId != Volatile.Read(ref requestSequence) || !isActive)
                {
                    resultImage.Dispose();
                    return;
                }

                if (endpoint == "preview-mask")
                {
                    Image? oldDisplayImage = pictureBox.Image;
                    Bitmap? oldPreviewImage = previewImage;
                    previewImage = resultImage;
                    pictureBox.Image = previewImage;

                    oldPreviewImage?.Dispose();
                    if (oldDisplayImage != null &&
                        !ReferenceEquals(oldDisplayImage, oldPreviewImage) &&
                        !ReferenceEquals(oldDisplayImage, state.CurrentImage) &&
                        !ReferenceEquals(oldDisplayImage, previewImage))
                    {
                        oldDisplayImage.Dispose();
                    }
                    btnErase.Visible = true;
                }
                else if (endpoint == "remove-object")
                {
                    Image? oldDisplayImage = pictureBox.Image;
                    Bitmap? oldPreviewImage = previewImage;
                    previewImage = resultImage;
                    pictureBox.Image = previewImage;

                    oldPreviewImage?.Dispose();
                    if (oldDisplayImage != null &&
                        !ReferenceEquals(oldDisplayImage, oldPreviewImage) &&
                        !ReferenceEquals(oldDisplayImage, state.CurrentImage) &&
                        !ReferenceEquals(oldDisplayImage, previewImage))
                    {
                        oldDisplayImage.Dispose();
                    }

                    finalPreviewReady = true;
                    btnErase.Text = "적용";
                    btnErase.Enabled = true;
                    btnErase.Visible = true;
                }
                else
                {
                    resultImage.Dispose();
                }
            }
            catch (OperationCanceledException)
            {
                // 새 선택이나 도구 종료로 취소된 이전 요청은 화면에 반영하지 않는다.
            }
            catch (Exception ex)
            {
                MessageBox.Show(
                    ex.Message,
                    "AI 물체 제거",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Error
                );
            }
            finally
            {
                if (ReferenceEquals(requestCancellation, activeCancellation))
                {
                    requestCancellation.Dispose();
                    requestCancellation = null;
                }
                Cursor.Current = Cursors.Default;
            }
        }

        private void ClearPreview()
        {
            finalPreviewReady = false;
            if (previewImage != null)
            {
                if (ReferenceEquals(pictureBox.Image, previewImage))
                    pictureBox.Image = null;

                previewImage.Dispose();
                previewImage = null;
                ShowCurrentImageCopy();
            }
            else if (state.CurrentImage != null)
            {
                ShowCurrentImageCopy();
            }
        }

        private void ShowCurrentImageCopy()
        {
            Image? oldDisplayImage = pictureBox.Image;
            pictureBox.Image = state.CurrentImage == null
                ? null
                : new Bitmap(state.CurrentImage);

            if (oldDisplayImage != null &&
                !ReferenceEquals(oldDisplayImage, state.CurrentImage) &&
                !ReferenceEquals(oldDisplayImage, previewImage))
            {
                oldDisplayImage.Dispose();
            }
        }
    }
}
