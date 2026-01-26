using System.Text.Json;

namespace Checkend;

/// <summary>
/// HTTP client for sending notices to Checkend.
/// </summary>
public sealed class Client : IDisposable
{
    private const string SdkVersion = "0.1.0";

    private readonly Configuration _config;
    private readonly HttpClient _httpClient;

    public Client(Configuration config)
    {
        _config = config;
        _httpClient = new HttpClient
        {
            Timeout = TimeSpan.FromMilliseconds(config.Timeout)
        };
        _httpClient.DefaultRequestHeaders.Add("Checkend-Ingestion-Key", config.ApiKey);
        _httpClient.DefaultRequestHeaders.Add("User-Agent", $"checkend-dotnet/{SdkVersion}");
    }

    /// <summary>
    /// Send a notice to Checkend.
    /// </summary>
    public async Task<Response> SendAsync(Notice notice, CancellationToken cancellationToken = default)
    {
        try
        {
            var url = $"{_config.Endpoint}/ingest/v1/errors";
            var json = JsonSerializer.Serialize(notice);

            if (_config.Debug)
            {
                Console.WriteLine($"[Checkend] Sending to {url}");
            }

            var content = new StringContent(json, System.Text.Encoding.UTF8, "application/json");
            var response = await _httpClient.PostAsync(url, content, cancellationToken);
            var body = await response.Content.ReadAsStringAsync(cancellationToken);

            // Extract Retry-After header if present
            string? retryAfter = null;
            if (response.Headers.TryGetValues("Retry-After", out var values))
            {
                retryAfter = values.FirstOrDefault();
            }

            var statusCode = (int)response.StatusCode;

            // Log specific error conditions
            LogResponseStatus(statusCode, body);

            return new Response(statusCode, body, retryAfter);
        }
        catch (Exception ex)
        {
            if (_config.Debug)
            {
                Console.Error.WriteLine($"[Checkend] Error sending notice: {ex.Message}");
            }
            return new Response(0, ex.Message, null);
        }
    }

    private void LogResponseStatus(int statusCode, string body)
    {
        switch (statusCode)
        {
            case 201:
                if (_config.Debug)
                {
                    Console.WriteLine($"[Checkend] Response: {statusCode} - {body}");
                }
                break;
            case 401:
                Console.Error.WriteLine("[Checkend] Authentication failed - check your API key");
                break;
            case 422:
                Console.Error.WriteLine($"[Checkend] Validation error: {body}");
                break;
            case 429:
                Console.Error.WriteLine("[Checkend] Rate limited by server - backing off");
                break;
            case >= 500:
                Console.Error.WriteLine($"[Checkend] Server error: {statusCode}");
                break;
            default:
                if (_config.Debug)
                {
                    Console.WriteLine($"[Checkend] Response: {statusCode} - {body}");
                }
                break;
        }
    }

    public void Dispose()
    {
        _httpClient.Dispose();
    }
}

/// <summary>
/// Response from the Checkend API.
/// </summary>
public readonly record struct Response(int StatusCode, string Body, string? RetryAfter)
{
    /// <summary>
    /// Whether the request was successful (2xx status code).
    /// </summary>
    public bool IsSuccess => StatusCode >= 200 && StatusCode < 300;

    /// <summary>
    /// Whether the request was rate limited (429 status code).
    /// </summary>
    public bool IsRateLimited => StatusCode == 429;

    /// <summary>
    /// Get the retry delay in milliseconds from the Retry-After header.
    /// Returns the default value if the header is not present or invalid.
    /// </summary>
    /// <param name="defaultMs">Default delay in milliseconds if Retry-After is not available.</param>
    /// <returns>Delay in milliseconds before retrying.</returns>
    public long GetRetryAfterMs(long defaultMs = 60000)
    {
        if (string.IsNullOrEmpty(RetryAfter))
        {
            return defaultMs;
        }

        // Retry-After can be seconds or HTTP-date; we only support seconds
        if (long.TryParse(RetryAfter, out var seconds))
        {
            return seconds * 1000;
        }

        return defaultMs;
    }
}
