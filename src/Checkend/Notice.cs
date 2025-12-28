using System.Text.Json.Serialization;

namespace Checkend;

/// <summary>
/// Represents an error notice to be sent to Checkend.
/// </summary>
public sealed class Notice
{
    [JsonPropertyName("error_class")]
    public string ErrorClass { get; set; } = "";

    [JsonPropertyName("message")]
    public string Message { get; set; } = "";

    [JsonPropertyName("backtrace")]
    public List<BacktraceFrame> Backtrace { get; set; } = new();

    [JsonPropertyName("fingerprint")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public string? Fingerprint { get; set; }

    [JsonPropertyName("tags")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public List<string>? Tags { get; set; }

    [JsonPropertyName("context")]
    public Dictionary<string, object?> Context { get; set; } = new();

    [JsonPropertyName("request")]
    public Dictionary<string, object?> Request { get; set; } = new();

    [JsonPropertyName("user")]
    public Dictionary<string, object?> User { get; set; } = new();

    [JsonPropertyName("environment")]
    public string Environment { get; set; } = "";

    [JsonPropertyName("occurred_at")]
    public string OccurredAt { get; set; } = DateTime.UtcNow.ToString("O");

    [JsonPropertyName("notifier")]
    public NotifierInfo Notifier { get; set; } = new();
}

/// <summary>
/// Represents a single frame in a stack trace.
/// </summary>
public sealed class BacktraceFrame
{
    [JsonPropertyName("file")]
    public string File { get; set; } = "";

    [JsonPropertyName("line")]
    public int Line { get; set; }

    [JsonPropertyName("method")]
    public string Method { get; set; } = "";
}

/// <summary>
/// Information about the notifier SDK.
/// </summary>
public sealed class NotifierInfo
{
    [JsonPropertyName("name")]
    public string Name { get; set; } = "checkend-dotnet";

    [JsonPropertyName("version")]
    public string Version { get; set; } = "0.1.0";

    [JsonPropertyName("language")]
    public string Language { get; set; } = "csharp";

    [JsonPropertyName("language_version")]
    public string LanguageVersion { get; set; } = System.Environment.Version.ToString();
}
