using System;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Threading;
using System.Threading.Tasks;

namespace ImageEditor.Services
{
    /// <summary>
    /// Sends an image only to the private localhost server. The U2NetP model is
    /// executed on the user's CPU; no paid API or API token is used.
    /// </summary>
    public sealed class AIBackgroundRemovalService
    {
        private const string Endpoint =
            LocalAiServerManager.BaseUrl + "/remove-background";

        public async Task<Bitmap> RemoveBackgroundAsync(
            Bitmap source,
            CancellationToken cancellationToken = default)
        {
            if (source == null)
                throw new ArgumentNullException(nameof(source));

            await LocalAiServerManager.EnsureReadyAsync(
                cancellationToken: cancellationToken);

            using MultipartFormDataContent content = new MultipartFormDataContent();
            byte[] inputBytes = await Task.Run(() =>
            {
                using MemoryStream stream = new MemoryStream();
                source.Save(stream, ImageFormat.Png);
                return stream.ToArray();
            }, cancellationToken).ConfigureAwait(false);

            using ByteArrayContent imageContent = new ByteArrayContent(inputBytes);
            imageContent.Headers.ContentType = new MediaTypeHeaderValue("image/png");
            content.Add(imageContent, "file", "background-removal-input.png");

            using HttpResponseMessage response = await LocalAiServerManager.HttpClient.PostAsync(
                Endpoint,
                content,
                cancellationToken);

            if (!response.IsSuccessStatusCode)
            {
                string responseText = await response.Content.ReadAsStringAsync(
                    cancellationToken);
                string detail = LocalAiServerManager.ExtractErrorDetail(responseText);
                throw new InvalidOperationException(
                    string.IsNullOrWhiteSpace(detail)
                        ? $"로컬 AI 서버가 HTTP {(int)response.StatusCode}를 반환했습니다."
                        : detail);
            }

            byte[] resultBytes = await response.Content.ReadAsByteArrayAsync(
                cancellationToken);
            return CreateBitmap(resultBytes);
        }

        private static Bitmap CreateBitmap(byte[] imageBytes)
        {
            if (imageBytes.Length == 0)
                throw new InvalidOperationException(
                    "로컬 AI 서버에서 빈 이미지를 반환했습니다.");

            try
            {
                using MemoryStream stream = new MemoryStream(imageBytes);
                using Image image = Image.FromStream(stream, true, true);
                return new Bitmap(image);
            }
            catch (ArgumentException ex)
            {
                throw new InvalidOperationException(
                    "AI 배경 제거 결과를 이미지로 읽을 수 없습니다.",
                    ex);
            }
        }
    }
}
