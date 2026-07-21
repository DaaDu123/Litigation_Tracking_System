using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using LTSFrontend.Core.Exceptions;
using LTSFrontend.Core.Models;

namespace LTSFrontend.Core.Http
{
    /// <summary>
    /// Thin wrapper around HttpClient that talks to LTSBackend.
    /// - Automatically unwraps the ApiResponse&lt;T&gt; envelope.
    /// - Throws ApiException with the backend's message/errors on failure.
    /// - Keeps cookies (the refresh-token HttpOnly cookie) alive for the
    ///   lifetime of this instance, which is Scoped -> one per user circuit.
    /// Registered as Scoped in ServiceCollectionExtensions.
    /// </summary>
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
                    "Backend tak connect nahi ho saka. Please make sure LTSBackend API is running (" +
                    Http.BaseAddress + ") aur network settings theek hain.", null, new() { ex.Message });
            }

            var raw = await response.Content.ReadAsStringAsync(ct);

            ApiResponse<T>? parsed;
            try
            {
                parsed = string.IsNullOrWhiteSpace(raw)
                    ? null
                    : JsonSerializer.Deserialize<ApiResponse<T>>(raw, JsonOptions);
            }
            catch (JsonException)
            {
                parsed = null;
            }

            if (!response.IsSuccessStatusCode)
            {
                var message = parsed?.Message ??
                    $"Request failed with status {(int)response.StatusCode} ({response.StatusCode}).";
                throw new ApiException(message, (int)response.StatusCode, parsed?.Errors);
            }

            if (parsed == null)
            {
                throw new ApiException("Server se ghalat response mila (invalid JSON).", (int)response.StatusCode);
            }

            if (!parsed.Success)
            {
                throw new ApiException(parsed.Message, (int)response.StatusCode, parsed.Errors);
            }

            return parsed.Data;
        }
    }
}
