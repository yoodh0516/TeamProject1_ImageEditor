using System;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Drawing.Imaging;
using System.IO;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;

namespace ImageEditor.Services
{
    // Cloudflare Workers AI 기반 인물 사진 자동보정 서비스입니다.
    // 인증값은 소스 코드가 아닌 Windows 환경 변수에서만 읽습니다.
    public static class CloudflarePortraitService
    {
        private const string AccountIdVariable = "IMAGEEDITOR_CLOUDFLARE_ACCOUNT_ID";
        private const string ApiTokenVariable = "IMAGEEDITOR_CLOUDFLARE_API_TOKEN";
        private const string Model = "@cf/black-forest-labs/flux-2-dev";

        private static readonly HttpClient HttpClient = new HttpClient
        {
            Timeout = TimeSpan.FromMinutes(4)
        };

        public static async Task<Bitmap> EnhanceAsync(
            Bitmap source,
            CancellationToken cancellationToken)
        {
            return await EnhanceAsync(source, string.Empty, cancellationToken);
        }

        public static async Task<Bitmap> EnhanceAsync(
            Bitmap source,
            string enhancementInstruction,
            CancellationToken cancellationToken)
        {
            if (source == null)
                throw new ArgumentNullException(nameof(source));

            string accountId = GetRequiredSetting(AccountIdVariable);
            string apiToken = GetRequiredSetting(ApiTokenVariable);

            using Bitmap apiInput = ResizeForApi(source, 500);
            byte[] inputBytes;
            using (MemoryStream inputStream = new MemoryStream())
            {
                apiInput.Save(inputStream, ImageFormat.Jpeg);
                inputBytes = inputStream.ToArray();
            }

            GetOutputSize(source.Size, out int outputWidth, out int outputHeight);
            const string basePrompt = @"Use input_image_0 as the exact portrait reference. Retouch this same photo naturally and professionally.
Preserve the exact same person's identity, facial structure, expression, hairstyle, pose, clothing, background, composition and camera angle.
Reduce only small blemishes and minor skin imperfections, gently soften fine lines, even skin tone, and improve exposure, white balance and color when needed.
Keep natural skin texture and pores. Keep eyes, eyebrows, eyelashes, lips and hair sharp and detailed.
Do not reshape the face or body. Do not enlarge eyes, slim the jaw, change nose or lips, age, ethnicity, hairstyle, clothing, background, or add makeup/accessories/text.
Do not create a different person. Do not over-smooth or blur the skin. The result must look like the original photograph after subtle professional retouching.";
            string prompt = string.IsNullOrWhiteSpace(enhancementInstruction)
                ? basePrompt
                : basePrompt + Environment.NewLine + enhancementInstruction;

            string url = $"https://api.cloudflare.com/client/v4/accounts/{accountId}/ai/run/{Model}";
            using MultipartFormDataContent form = new MultipartFormDataContent();
            form.Add(new StringContent(prompt), "prompt");
            form.Add(new StringContent("8"), "steps");
            form.Add(new StringContent(outputWidth.ToString()), "width");
            form.Add(new StringContent(outputHeight.ToString()), "height");

            using ByteArrayContent imageContent = new ByteArrayContent(inputBytes);
            imageContent.Headers.ContentType = new MediaTypeHeaderValue("image/jpeg");
            form.Add(imageContent, "input_image_0", "portrait.jpg");

            using HttpRequestMessage request = new HttpRequestMessage(HttpMethod.Post, url);
            request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", apiToken);
            request.Content = form;

            using HttpResponseMessage response = await HttpClient.SendAsync(
                request,
                HttpCompletionOption.ResponseContentRead,
                cancellationToken);
            byte[] responseBytes = await response.Content.ReadAsByteArrayAsync();
            cancellationToken.ThrowIfCancellationRequested();

            if (!response.IsSuccessStatusCode)
            {
                string responseText = System.Text.Encoding.UTF8.GetString(responseBytes);
                throw new InvalidOperationException(GetFriendlyError(
                    responseText,
                    (int)response.StatusCode));
            }

            string? base64 = TryGetImageBase64(
                System.Text.Encoding.UTF8.GetString(responseBytes));
            if (string.IsNullOrWhiteSpace(base64))
            {
                throw new InvalidOperationException(
                    "Cloudflare AI가 보정 이미지를 반환하지 않았습니다. 잠시 후 다시 시도해 주세요.");
            }

            byte[] outputBytes;
            try
            {
                outputBytes = Convert.FromBase64String(base64);
            }
            catch (FormatException ex)
            {
                throw new InvalidOperationException(
                    "Cloudflare AI 응답의 이미지 데이터를 읽지 못했습니다.", ex);
            }

            using MemoryStream outputStream = new MemoryStream(outputBytes);
            using Bitmap temporary = new Bitmap(outputStream);
            return ResizeResultToOriginalSize(temporary, source);
        }

        private static Bitmap ResizeResultToOriginalSize(Bitmap result, Bitmap source)
        {
            if (result.Size == source.Size)
                return new Bitmap(result);

            Bitmap resized = new Bitmap(
                source.Width,
                source.Height,
                PixelFormat.Format24bppRgb);
            if (source.HorizontalResolution > 0 && source.VerticalResolution > 0)
                resized.SetResolution(source.HorizontalResolution, source.VerticalResolution);

            using Graphics graphics = Graphics.FromImage(resized);
            graphics.CompositingMode = CompositingMode.SourceCopy;
            graphics.CompositingQuality = CompositingQuality.HighQuality;
            graphics.InterpolationMode = InterpolationMode.HighQualityBicubic;
            graphics.SmoothingMode = SmoothingMode.HighQuality;
            graphics.PixelOffsetMode = PixelOffsetMode.HighQuality;
            graphics.DrawImage(result, new Rectangle(0, 0, resized.Width, resized.Height));
            return resized;
        }

        private static string GetRequiredSetting(string variableName)
        {
            string? value = Environment.GetEnvironmentVariable(variableName);
            if (!string.IsNullOrWhiteSpace(value))
                return value;

            throw new InvalidOperationException(
                "Cloudflare AI 인증값이 설정되지 않았습니다.\r\n\r\n" +
                "Windows 환경 변수 " + variableName + "를 설정한 뒤 프로그램을 다시 실행해 주세요.");
        }

        private static Bitmap ResizeForApi(Bitmap source, int maxSide)
        {
            double scale = Math.Min(1d, (double)maxSide / Math.Max(source.Width, source.Height));
            int width = Math.Max(1, (int)Math.Round(source.Width * scale));
            int height = Math.Max(1, (int)Math.Round(source.Height * scale));

            Bitmap result = new Bitmap(width, height, PixelFormat.Format24bppRgb);
            using Graphics graphics = Graphics.FromImage(result);
            graphics.InterpolationMode = InterpolationMode.HighQualityBicubic;
            graphics.SmoothingMode = SmoothingMode.HighQuality;
            graphics.PixelOffsetMode = PixelOffsetMode.HighQuality;
            graphics.DrawImage(source, 0, 0, width, height);
            return result;
        }

        private static void GetOutputSize(Size sourceSize, out int width, out int height)
        {
            double scale = Math.Min(1d, 1024d / Math.Max(sourceSize.Width, sourceSize.Height));
            width = Math.Max(256, (int)Math.Round(sourceSize.Width * scale));
            height = Math.Max(256, (int)Math.Round(sourceSize.Height * scale));
            width = Math.Max(256, (width / 16) * 16);
            height = Math.Max(256, (height / 16) * 16);
        }

        private static string? TryGetImageBase64(string json)
        {
            try
            {
                using JsonDocument document = JsonDocument.Parse(json);
                JsonElement root = document.RootElement;
                if (root.TryGetProperty("result", out JsonElement result) &&
                    result.TryGetProperty("image", out JsonElement image))
                {
                    return image.GetString();
                }

                return root.TryGetProperty("image", out JsonElement directImage)
                    ? directImage.GetString()
                    : null;
            }
            catch (JsonException)
            {
                return null;
            }
        }

        private static string GetFriendlyError(string responseText, int statusCode)
        {
            string message = responseText;
            try
            {
                using JsonDocument document = JsonDocument.Parse(responseText);
                if (document.RootElement.TryGetProperty("errors", out JsonElement errors) &&
                    errors.ValueKind == JsonValueKind.Array)
                {
                    foreach (JsonElement error in errors.EnumerateArray())
                    {
                        if (error.TryGetProperty("message", out JsonElement errorMessage))
                        {
                            message = errorMessage.GetString() ?? responseText;
                            break;
                        }
                    }
                }
            }
            catch (JsonException)
            {
                // JSON 형식이 아닌 오류 본문은 그대로 사용자에게 안내한다.
            }

            if (statusCode is 401 or 403)
                return $"Cloudflare 인증 오류 ({statusCode})\r\n\r\nAccount ID와 API Token을 확인해 주세요.\r\n\r\n{message}";
            if (statusCode == 429)
                return $"Cloudflare Workers AI 사용량 제한 오류 (429)\r\n\r\n잠시 후 다시 시도해 주세요.\r\n\r\n{message}";

            return $"Cloudflare Workers AI 오류 ({statusCode})\r\n\r\n{message}";
        }
    }
}
