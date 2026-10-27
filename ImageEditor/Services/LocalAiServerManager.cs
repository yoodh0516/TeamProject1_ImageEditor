using System;
using System.Net.Http;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;

namespace ImageEditor.Services
{
    /// <summary>
    /// Checks the manually started FastAPI server shared by every AI feature.
    /// The C# application never installs Python or starts a hidden process.
    /// </summary>
    internal static class LocalAiServerManager
    {
        internal const string BaseUrl = "http://127.0.0.1:8000";
        private const string HealthEndpoint = BaseUrl + "/health";
        // A single connection pool avoids repeatedly opening localhost sockets for
        // background removal, object removal, and photo restoration requests.
        private static readonly HttpClient SharedHttpClient = new HttpClient
        {
            Timeout = TimeSpan.FromMinutes(15)
        };

        internal static HttpClient HttpClient => SharedHttpClient;

        internal static async Task EnsureReadyAsync(
            IProgress<string>? progress = null,
            CancellationToken cancellationToken = default)
        {
            progress?.Report("FastAPI 통합 AI 서버 연결을 확인하는 중...");

            if (await IsReadyAsync(cancellationToken).ConfigureAwait(false))
                return;

            throw new InvalidOperationException(
                "AI 서버가 실행되지 않았습니다.\r\n\r\n" +
                "1. AIIntegratedServer\\3_start_server.bat를 실행하세요.\r\n" +
                "2. 검은 서버 창을 닫지 마세요.\r\n" +
                "3. 서버 주소가 http://127.0.0.1:8000인지 확인하세요.");
        }

        internal static string ExtractErrorDetail(string responseText)
        {
            if (string.IsNullOrWhiteSpace(responseText))
                return string.Empty;

            try
            {
                using JsonDocument document = JsonDocument.Parse(responseText);
                if (document.RootElement.TryGetProperty(
                        "detail",
                        out JsonElement detail))
                {
                    return detail.ValueKind == JsonValueKind.String
                        ? detail.GetString() ?? string.Empty
                        : detail.ToString();
                }
            }
            catch (JsonException)
            {
            }

            string trimmed = responseText.Trim();
            return trimmed.Length <= 500
                ? trimmed
                : trimmed.Substring(0, 500) + "...";
        }

        private static async Task<bool> IsReadyAsync(
            CancellationToken cancellationToken)
        {
            try
            {
                using CancellationTokenSource healthTimeout =
                    CancellationTokenSource.CreateLinkedTokenSource(
                        cancellationToken);
                healthTimeout.CancelAfter(TimeSpan.FromSeconds(3));

                using HttpResponseMessage response = await SharedHttpClient.GetAsync(
                    HealthEndpoint,
                    healthTimeout.Token).ConfigureAwait(false);
                return response.IsSuccessStatusCode;
            }
            catch (HttpRequestException)
            {
                return false;
            }
            catch (TaskCanceledException) when (!cancellationToken.IsCancellationRequested)
            {
                return false;
            }
        }
    }
}
