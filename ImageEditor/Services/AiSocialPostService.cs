using System;
using System.Drawing;
using System.IO;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;

namespace ImageEditor.Services
{
    public sealed class AiSocialPostResult
    {
        internal AiSocialPostResult(string post, string hashtags)
        {
            Post = post;
            Hashtags = hashtags;
        }

        public string Post { get; }
        public string Hashtags { get; }
    }

    internal static class AiSocialPostService
    {
        internal static async Task<AiSocialPostResult> RequestAsync(
            Bitmap sourceImage,
            string platform,
            string tone,
            string postLength,
            CancellationToken cancellationToken)
        {
            await LocalAiServerManager.EnsureReadyAsync(
                cancellationToken: cancellationToken);

            byte[] imageBytes = await Task.Run(() =>
            {
                using MemoryStream stream = new MemoryStream();
                sourceImage.Save(stream, System.Drawing.Imaging.ImageFormat.Png);
                return stream.ToArray();
            }, cancellationToken).ConfigureAwait(false);

            using MultipartFormDataContent content = new MultipartFormDataContent();
            using ByteArrayContent imageContent = new ByteArrayContent(imageBytes);
            imageContent.Headers.ContentType = new MediaTypeHeaderValue("image/png");
            content.Add(imageContent, "file", "current.png");
            content.Add(new StringContent(platform), "platform");
            content.Add(new StringContent(tone), "tone");
            content.Add(new StringContent(postLength), "length");

            using HttpResponseMessage response = await LocalAiServerManager.HttpClient
                .PostAsync(
                    LocalAiServerManager.BaseUrl + "/social-post",
                    content,
                    cancellationToken)
                .ConfigureAwait(false);
            string responseText = await response.Content.ReadAsStringAsync()
                .ConfigureAwait(false);
            if (!response.IsSuccessStatusCode)
            {
                string detail = LocalAiServerManager.ExtractErrorDetail(responseText);
                throw new InvalidOperationException(string.IsNullOrWhiteSpace(detail)
                    ? $"AI 게시글 추천에 실패했습니다. ({(int)response.StatusCode})"
                    : detail);
            }

            using JsonDocument document = JsonDocument.Parse(responseText);
            string post = document.RootElement.TryGetProperty(
                    "post", out JsonElement postElement)
                ? postElement.GetString()?.Trim() ?? string.Empty
                : string.Empty;
            string hashtags = document.RootElement.TryGetProperty(
                    "hashtags", out JsonElement hashtagElement)
                ? hashtagElement.GetString()?.Trim() ?? string.Empty
                : string.Empty;

            if (string.IsNullOrWhiteSpace(post) || string.IsNullOrWhiteSpace(hashtags))
            {
                throw new InvalidOperationException(
                    "AI 게시글 또는 해시태그 추천 결과가 비어 있습니다.");
            }

            return new AiSocialPostResult(post, hashtags);
        }
    }
}
