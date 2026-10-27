using System;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Drawing.Imaging;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Threading;
using System.Threading.Tasks;

namespace ImageEditor.Services
{
    /// <summary>
    /// Old-photo restoration routed through the private localhost FastAPI server.
    /// The server uses Replicate first and its local CPU model as a safe fallback.
    /// </summary>
    public sealed class PhotoRestorationService
    {
        private const string RestoreEndpoint =
            LocalAiServerManager.BaseUrl + "/restore-photo";
        private const int MaximumUploadBytes = 24 * 1024 * 1024;

        public string LastRestorationMode { get; private set; } = "unknown";
        public string LastRestorationMethodName { get; private set; } = "AI 사진 복원";
        public bool UsedLocalFallback => LastRestorationMode == "local-fallback";

        public async Task<Bitmap> RestoreOldPhotoAsync(
            Bitmap sourceImage,
            IProgress<string>? progress = null,
            CancellationToken cancellationToken = default)
        {
            if (sourceImage == null)
                throw new ArgumentNullException(nameof(sourceImage));

            LastRestorationMode = "unknown";
            LastRestorationMethodName = "AI 사진 복원";

            await LocalAiServerManager.EnsureReadyAsync(
                progress,
                cancellationToken);

            cancellationToken.ThrowIfCancellationRequested();
            progress?.Report(
                "Replicate 클라우드 GPU에 고품질 사진 복원을 요청하는 중...");

            byte[] sourceBytes = await Task.Run(
                () => CreateUploadBytes(sourceImage),
                cancellationToken);

            using MultipartFormDataContent content = new MultipartFormDataContent();
            using ByteArrayContent imageContent = new ByteArrayContent(sourceBytes);

            imageContent.Headers.ContentType = new MediaTypeHeaderValue("image/jpeg");
            content.Add(imageContent, "file", "old-photo.jpg");
            content.Add(new StringContent("true"), "restore_damage");
            content.Add(new StringContent("true"), "colorize");
            content.Add(new StringContent("true"), "enhance_details");

            using HttpResponseMessage response = await LocalAiServerManager.HttpClient.PostAsync(
                RestoreEndpoint,
                content,
                cancellationToken);

            if (!response.IsSuccessStatusCode)
            {
                string responseText = await response.Content.ReadAsStringAsync(
                    cancellationToken);
                string detail = LocalAiServerManager.ExtractErrorDetail(responseText);
                throw new InvalidOperationException(
                    string.IsNullOrWhiteSpace(detail)
                        ? $"로컬 AI 사진 복원에 실패했습니다. ({(int)response.StatusCode})"
                        : detail);
            }

            LastRestorationMode = GetHeaderValue(
                response,
                "X-ImageEditor-Restoration-Mode");
            string fallbackReason = GetHeaderValue(
                response,
                "X-ImageEditor-Fallback-Reason");

            if (LastRestorationMode == "replicate")
            {
                LastRestorationMethodName = "Replicate 클라우드 GPU 고품질 복원";
                progress?.Report("Replicate 고품질 AI 복원이 완료되었습니다.");
            }
            else if (LastRestorationMode == "local-fallback")
            {
                LastRestorationMethodName = fallbackReason == "token-missing"
                    ? "로컬 CPU 대체 복원 (Replicate 토큰 없음)"
                    : "로컬 CPU 대체 복원 (Replicate 연결 실패)";
                progress?.Report(LastRestorationMethodName + " 처리가 완료되었습니다.");
            }
            else
            {
                LastRestorationMethodName = "AI 사진 복원";
            }

            progress?.Report("AI 복원 결과 이미지를 불러오는 중...");
            byte[] resultBytes = await response.Content.ReadAsByteArrayAsync(
                cancellationToken);
            return CreateBitmap(resultBytes);
        }

        private static string GetHeaderValue(
            HttpResponseMessage response,
            string headerName)
        {
            return response.Headers.TryGetValues(headerName, out var values)
                ? values.FirstOrDefault()?.Trim() ?? string.Empty
                : string.Empty;
        }

        private static byte[] CreateUploadBytes(Bitmap sourceImage)
        {
            byte[] imageBytes = EncodeJpeg(sourceImage, 92L);
            if (imageBytes.Length <= MaximumUploadBytes)
                return imageBytes;

            imageBytes = EncodeJpeg(sourceImage, 78L);
            if (imageBytes.Length <= MaximumUploadBytes)
                return imageBytes;

            using Bitmap resized = ResizeForUpload(sourceImage, 3000);
            imageBytes = EncodeJpeg(resized, 85L);

            if (imageBytes.Length > MaximumUploadBytes)
            {
                throw new InvalidOperationException(
                    "이미지 용량이 너무 큽니다. 긴 변을 3000px 이하로 줄인 뒤 다시 시도해 주세요.");
            }

            return imageBytes;
        }

        private static byte[] EncodeJpeg(Bitmap sourceImage, long quality)
        {
            using Bitmap rgbImage = new Bitmap(
                sourceImage.Width,
                sourceImage.Height,
                PixelFormat.Format24bppRgb);

            using (Graphics graphics = Graphics.FromImage(rgbImage))
            {
                graphics.Clear(Color.White);
                graphics.CompositingQuality = CompositingQuality.HighQuality;
                graphics.InterpolationMode = InterpolationMode.HighQualityBicubic;
                graphics.SmoothingMode = SmoothingMode.HighQuality;
                graphics.PixelOffsetMode = PixelOffsetMode.HighQuality;
                graphics.DrawImage(
                    sourceImage,
                    new Rectangle(0, 0, rgbImage.Width, rgbImage.Height));
            }

            ImageCodecInfo? jpegCodec = ImageCodecInfo.GetImageEncoders()
                .FirstOrDefault(codec => codec.FormatID == ImageFormat.Jpeg.Guid);

            using MemoryStream stream = new MemoryStream();
            if (jpegCodec == null)
            {
                rgbImage.Save(stream, ImageFormat.Jpeg);
            }
            else
            {
                using EncoderParameters parameters = new EncoderParameters(1);
                parameters.Param[0] = new EncoderParameter(
                    System.Drawing.Imaging.Encoder.Quality,
                    quality);
                rgbImage.Save(stream, jpegCodec, parameters);
            }

            return stream.ToArray();
        }

        private static Bitmap ResizeForUpload(Bitmap sourceImage, int maxSide)
        {
            int longestSide = Math.Max(sourceImage.Width, sourceImage.Height);
            if (longestSide <= maxSide)
                return new Bitmap(sourceImage);

            double ratio = (double)maxSide / longestSide;
            int width = Math.Max(1, (int)Math.Round(sourceImage.Width * ratio));
            int height = Math.Max(1, (int)Math.Round(sourceImage.Height * ratio));

            Bitmap resized = new Bitmap(width, height, PixelFormat.Format24bppRgb);
            using Graphics graphics = Graphics.FromImage(resized);
            graphics.Clear(Color.White);
            graphics.CompositingQuality = CompositingQuality.HighQuality;
            graphics.InterpolationMode = InterpolationMode.HighQualityBicubic;
            graphics.SmoothingMode = SmoothingMode.HighQuality;
            graphics.PixelOffsetMode = PixelOffsetMode.HighQuality;
            graphics.DrawImage(sourceImage, new Rectangle(0, 0, width, height));
            return resized;
        }

        private static Bitmap CreateBitmap(byte[] imageBytes)
        {
            if (imageBytes.Length == 0)
                throw new InvalidOperationException("AI 복원 결과 이미지가 비어 있습니다.");

            try
            {
                using MemoryStream stream = new MemoryStream(imageBytes);
                using Image image = Image.FromStream(stream, true, true);
                return new Bitmap(image);
            }
            catch (ArgumentException ex)
            {
                throw new InvalidOperationException(
                    "AI 복원 결과를 이미지로 읽을 수 없습니다.",
                    ex);
            }
        }
    }
}
