using System.Net.Http.Json;
using System.Text.Json;
using LTSFrontend.Core.Exceptions;
using LTSFrontend.Core.Models;

namespace LTSFrontend.Core.Http
{
    public class ApiClient
    {
        public HttpClient Http { get; }

        private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);

        public ApiClient(HttpClient httpClient)
        {
            Http = httpClient;
        }

        public Task<T?> GetAsync<T>(string url, CancellationToken ct = default) =>
            SendAsync<T>(new HttpRequestMessage(HttpMethod.Get, url), ct);

        public Task<T?> PostAsync<T>(string url, object? body = null, CancellationToken ct = default)
        {
            var request = new HttpRequestMessage(HttpMethod.Post, url);
            if (body != null)
                request.Content = JsonContent.Create(body, options: JsonOptions);
            return SendAsync<T>(request, ct);
        }

        public Task<T?> PostFormAsync<T>(string url, MultipartFormDataContent form, CancellationToken ct = default)
        {
            var request = new HttpRequestMessage(HttpMethod.Post, url) { Content = form };
            return SendAsync<T>(request, ct);
        }

        public Task<T?> PutFormAsync<T>(string url, MultipartFormDataContent form, CancellationToken ct = default)
        {
            var request = new HttpRequestMessage(HttpMethod.Put, url) { Content = form };
            return SendAsync<T>(request, ct);
        }

        public Task<T?> DeleteAsync<T>(string url, CancellationToken ct = default) =>
            SendAsync<T>(new HttpRequestMessage(HttpMethod.Delete, url), ct);

        private async Task<T?> SendAsync<T>(HttpRequestMessage request, CancellationToken ct)
        {
            HttpResponseMessage response;
            try
            {
                response = await Http.SendAsync(request, ct);
            }
            catch (HttpRequestException ex)
            {
                throw new ApiException(
                    "Backend se connect nahi ho saka. Ensure LTSBackend API is running on " +
                    Http.BaseAddress, null, new() { ex.Message });
            }

            var raw = await response.Content.ReadAsStringAsync(ct);

            ApiResponse<T>? parsed = null;
            if (!string.IsNullOrWhiteSpace(raw))
            {
                try
                {
                    parsed = JsonSerializer.Deserialize<ApiResponse<T>>(raw, JsonOptions);
                }
                catch (JsonException)
                {
                    parsed = null;
                }
            }

            if (!response.IsSuccessStatusCode)
            {
                var message = parsed?.Message ??
                    $"Request failed with status {(int)response.StatusCode} ({response.StatusCode}).";
                throw new ApiException(message, (int)response.StatusCode, parsed?.Errors);
            }

            // If expected return type is bool and response is successful with null/empty content
            if (typeof(T) == typeof(bool) && parsed == null)
            {
                return (T)(object)true;
            }

            if (parsed == null)
            {
                throw new ApiException("Server se invalid response mila.", (int)response.StatusCode);
            }

            if (!parsed.Success)
            {
                throw new ApiException(parsed.Message, (int)response.StatusCode, parsed.Errors);
            }

            return parsed.Data;
        }
    }
}