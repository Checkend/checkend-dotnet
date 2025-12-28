using System.Net.Http.Json;
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

            if (_config.Debug)
            {
                Console.WriteLine($"[Checkend] Response: {(int)response.StatusCode} - {body}");
            }

            return new Response((int)response.StatusCode, body);
        }
        catch (Exception ex)
        {
            if (_config.Debug)
            {
                Console.Error.WriteLine($"[Checkend] Error sending notice: {ex.Message}");
            }
            return new Response(0, ex.Message);
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
public readonly record struct Response(int StatusCode, string Body)
{
    public bool IsSuccess => StatusCode >= 200 && StatusCode < 300;
}
