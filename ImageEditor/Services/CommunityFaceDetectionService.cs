using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;

namespace ImageEditor.Services
{
    internal sealed class CommunityFacePlacement
    {
        internal CommunityFacePlacement(
            float x,
            float y,
            float width,
            float height,
            float noseX,
            float noseY,
            float rotation,
            float confidence)
        {
            X = x;
            Y = y;
            Width = width;
            Height = height;
            NoseX = noseX;
            NoseY = noseY;
            Rotation = rotation;
            Confidence = confidence;
        }

        internal float X { get; }
        internal float Y { get; }
        internal float Width { get; }
        internal float Height { get; }
        internal float NoseX { get; }
        internal float NoseY { get; }
        internal float Rotation { get; }
        internal float Confidence { get; }
    }

    internal static class CommunityFaceDetectionService
    {
        private const string Endpoint =
            LocalAiServerManager.BaseUrl + "/detect-faces";

        internal static async Task<IReadOnlyList<CommunityFacePlacement>>
            DetectAsync(Bitmap sourceImage, CancellationToken cancellationToken)
        {
            if (sourceImage == null)
                throw new ArgumentNullException(nameof(sourceImage));

            await LocalAiServerManager.EnsureReadyAsync(
                cancellationToken: cancellationToken);

            byte[] imageBytes = await Task.Run(() =>
            {
                using MemoryStream stream = new MemoryStream();
                sourceImage.Save(stream, ImageFormat.Png);
                return stream.ToArray();
            }, cancellationToken).ConfigureAwait(false);

            using MultipartFormDataContent content = new MultipartFormDataContent();
            using ByteArrayContent imageContent = new ByteArrayContent(imageBytes);
            imageContent.Headers.ContentType = new MediaTypeHeaderValue("image/png");
            content.Add(imageContent, "file", "community-face-input.png");

            using HttpResponseMessage response = await
                LocalAiServerManager.HttpClient.PostAsync(
                    Endpoint,
                    content,
                    cancellationToken).ConfigureAwait(false);
            string responseText = await response.Content.ReadAsStringAsync(
                cancellationToken).ConfigureAwait(false);
            if (!response.IsSuccessStatusCode)
            {
                string detail = LocalAiServerManager.ExtractErrorDetail(responseText);
                throw new InvalidOperationException(
                    string.IsNullOrWhiteSpace(detail)
                        ? $"얼굴 자동 인식에 실패했습니다. ({(int)response.StatusCode})"
                        : detail);
            }

            try
            {
                using JsonDocument document = JsonDocument.Parse(responseText);
                if (!document.RootElement.TryGetProperty(
                        "faces",
                        out JsonElement facesElement) ||
                    facesElement.ValueKind != JsonValueKind.Array)
                {
                    throw new InvalidOperationException(
                        "얼굴 자동 인식 결과 형식이 올바르지 않습니다.");
                }

                List<CommunityFacePlacement> faces = new List<CommunityFacePlacement>();
                foreach (JsonElement face in facesElement.EnumerateArray())
                {
                    float width = ReadSingle(face, "width");
                    float height = ReadSingle(face, "height");
                    if (width <= 0f || height <= 0f)
                        continue;

                    faces.Add(new CommunityFacePlacement(
                        ReadSingle(face, "x"),
                        ReadSingle(face, "y"),
                        width,
                        height,
                        ReadSingle(face, "nose_x"),
                        ReadSingle(face, "nose_y"),
                        ReadSingle(face, "rotation"),
                        ReadSingle(face, "confidence")));
                }

                return faces;
            }
            catch (JsonException ex)
            {
                throw new InvalidOperationException(
                    "얼굴 자동 인식 결과를 읽을 수 없습니다.",
                    ex);
            }
        }

        private static float ReadSingle(JsonElement element, string propertyName)
        {
            return element.TryGetProperty(propertyName, out JsonElement value) &&
                   value.TryGetSingle(out float result)
                ? result
                : 0f;
        }
    }
}
