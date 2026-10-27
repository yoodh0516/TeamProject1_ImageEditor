using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;

namespace ImageEditor.Services
{
    public sealed class AiCaptionSuggestion
    {
        internal AiCaptionSuggestion(
            string text,
            string placement,
            string effect,
            string category,
            float anchorX,
            float anchorY,
            string alignment,
            string textColor,
            double confidence,
            float fontSizeRatio,
            string fontStyle,
            string effectColor,
            float effectIntensity)
        {
            Text = text;
            Placement = placement;
            Effect = effect;
            Category = category;
            AnchorX = anchorX;
            AnchorY = anchorY;
            Alignment = alignment;
            TextColor = textColor;
            Confidence = confidence;
            FontSizeRatio = fontSizeRatio;
            FontStyle = fontStyle;
            EffectColor = effectColor;
            EffectIntensity = effectIntensity;
        }

        public string Text { get; }
        public string Placement { get; }
        public string Effect { get; }
        public string Category { get; }
        public float AnchorX { get; }
        public float AnchorY { get; }
        public string Alignment { get; }
        public string TextColor { get; }
        public double Confidence { get; }
        public float FontSizeRatio { get; }
        public string FontStyle { get; }
        public string EffectColor { get; }
        public float EffectIntensity { get; }
    }

    internal static class AiCaptionService
    {
        internal static async Task<IReadOnlyList<AiCaptionSuggestion>>
            RequestAsync(
                Bitmap sourceImage,
                string platform,
                string recommendationType,
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
            content.Add(new StringContent(platform, Encoding.UTF8), "platform");
            content.Add(new StringContent(recommendationType, Encoding.UTF8),
                "recommendation_type");
            content.Add(new StringContent("유머", Encoding.UTF8), "tone");
            content.Add(new StringContent("짧게", Encoding.UTF8), "length");
            content.Add(new StringContent("4", Encoding.UTF8), "count");

            using HttpResponseMessage response = await LocalAiServerManager.HttpClient
                .PostAsync(
                    LocalAiServerManager.BaseUrl + "/generate-captions",
                    content,
                    cancellationToken)
                .ConfigureAwait(false);
            string responseText = await response.Content.ReadAsStringAsync()
                .ConfigureAwait(false);
            if (!response.IsSuccessStatusCode)
            {
                string detail = LocalAiServerManager.ExtractErrorDetail(responseText);
                throw new InvalidOperationException(string.IsNullOrWhiteSpace(detail)
                    ? $"유행어·밈 추천에 실패했습니다. ({(int)response.StatusCode})"
                    : detail);
            }

            using JsonDocument document = JsonDocument.Parse(responseText);
            if (!document.RootElement.TryGetProperty("captions", out JsonElement captions) ||
                captions.ValueKind != JsonValueKind.Array)
            {
                throw new InvalidOperationException("유행어·밈 추천 결과 형식이 올바르지 않습니다.");
            }

            List<AiCaptionSuggestion> results = new List<AiCaptionSuggestion>();
            foreach (JsonElement item in captions.EnumerateArray())
            {
                string text = item.TryGetProperty("text", out JsonElement textElement)
                    ? textElement.GetString()?.Trim() ?? string.Empty
                    : string.Empty;
                if (string.IsNullOrWhiteSpace(text))
                    continue;

                string placement = item.TryGetProperty("placement", out JsonElement placementElement)
                    ? placementElement.GetString() ?? "bottom-left"
                    : "bottom-left";
                string effect = item.TryGetProperty("effect", out JsonElement effectElement)
                    ? effectElement.GetString() ?? "shadow"
                    : "shadow";
                string category = item.TryGetProperty("category", out JsonElement categoryElement)
                    ? categoryElement.GetString() ?? "밈"
                    : "밈";
                if (category != "유행어" && category != "밈")
                    category = "밈";
                (float fallbackX, float fallbackY, string fallbackAlignment) =
                    GetPlacementFallback(placement);
                float anchorX = ReadNormalizedNumber(item, "anchor_x", fallbackX);
                float anchorY = ReadNormalizedNumber(item, "anchor_y", fallbackY);
                string alignment = item.TryGetProperty("alignment", out JsonElement alignmentElement)
                    ? alignmentElement.GetString() ?? fallbackAlignment
                    : fallbackAlignment;
                if (alignment != "left" && alignment != "center" && alignment != "right")
                    alignment = fallbackAlignment;
                string textColor = item.TryGetProperty("text_color", out JsonElement colorElement)
                    ? colorElement.GetString() ?? "black"
                    : "black";
                if (!IsSupportedColor(textColor))
                    textColor = "black";
                string effectColor = item.TryGetProperty("effect_color", out JsonElement effectColorElement)
                    ? effectColorElement.GetString() ?? "black"
                    : "black";
                if (!IsSupportedColor(effectColor))
                    effectColor = "black";
                float effectIntensity = ReadNormalizedNumber(
                    item,
                    "effect_intensity",
                    0.55f);
                double confidence = item.TryGetProperty("confidence", out JsonElement confidenceElement) &&
                    confidenceElement.TryGetDouble(out double parsedConfidence)
                        ? Math.Max(0d, Math.Min(1d, parsedConfidence))
                        : 0.5d;
                float fontSizeRatio = ReadNormalizedNumber(
                    item,
                    "font_size_ratio",
                    0.058f);
                fontSizeRatio = Math.Max(0.03f, Math.Min(0.10f, fontSizeRatio));
                string fontStyle = item.TryGetProperty("font_style", out JsonElement fontStyleElement)
                    ? fontStyleElement.GetString() ?? "clean"
                    : "clean";
                if (fontStyle != "playful" && fontStyle != "bold" &&
                    fontStyle != "emotional" && fontStyle != "clean")
                {
                    fontStyle = "clean";
                }
                results.Add(new AiCaptionSuggestion(
                    text,
                    placement,
                    effect,
                    category,
                    anchorX,
                    anchorY,
                    alignment,
                    textColor,
                    confidence,
                    fontSizeRatio,
                    fontStyle,
                    effectColor,
                    effectIntensity));
                if (results.Count == 4)
                    break;
            }

            if (results.Count == 0)
                throw new InvalidOperationException("사용할 수 있는 유행어·밈 추천 결과가 없습니다.");
            return results;
        }

        private static float ReadNormalizedNumber(
            JsonElement item,
            string propertyName,
            float fallback)
        {
            if (!item.TryGetProperty(propertyName, out JsonElement element) ||
                !element.TryGetDouble(out double value))
            {
                return fallback;
            }

            return (float)Math.Max(0d, Math.Min(1d, value));
        }

        private static bool IsSupportedColor(string value)
        {
            if (string.Equals(value, "black", StringComparison.OrdinalIgnoreCase) ||
                string.Equals(value, "white", StringComparison.OrdinalIgnoreCase))
            {
                return true;
            }

            if (value.Length != 7 || value[0] != '#')
                return false;

            for (int i = 1; i < value.Length; i++)
            {
                if (!Uri.IsHexDigit(value[i]))
                    return false;
            }

            return true;
        }

        private static (float X, float Y, string Alignment) GetPlacementFallback(
            string placement)
        {
            return placement switch
            {
                "top-center" => (0.5f, 0.18f, "center"),
                "top-right" => (0.94f, 0.18f, "right"),
                "middle-left" => (0.06f, 0.5f, "left"),
                "center" => (0.5f, 0.5f, "center"),
                "middle-right" => (0.94f, 0.5f, "right"),
                "bottom-center" => (0.5f, 0.82f, "center"),
                "bottom-right" => (0.94f, 0.82f, "right"),
                "top-left" => (0.06f, 0.18f, "left"),
                _ => (0.06f, 0.82f, "left")
            };
        }
    }
}
