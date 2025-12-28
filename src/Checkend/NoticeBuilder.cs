using System.Diagnostics;
using Checkend.Filters;

namespace Checkend;

/// <summary>
/// Builds Notice objects from exceptions.
/// </summary>
public sealed class NoticeBuilder
{
    private const int MaxMessageLength = 10000;
    private const int MaxBacktraceLines = 100;

    private readonly Configuration _config;

    public NoticeBuilder(Configuration config)
    {
        _config = config;
    }

    /// <summary>
    /// Build a notice from an exception.
    /// </summary>
    public Notice Build(Exception exception, NoticeOptions? options = null)
    {
        options ??= new NoticeOptions();

        var notice = new Notice
        {
            ErrorClass = exception.GetType().FullName ?? exception.GetType().Name,
            Message = TruncateMessage(exception.Message),
            Backtrace = BuildBacktrace(exception),
            Environment = _config.Environment,
            OccurredAt = DateTime.UtcNow.ToString("O")
        };

        if (!string.IsNullOrEmpty(options.Fingerprint))
        {
            notice.Fingerprint = options.Fingerprint;
        }

        if (options.Tags is { Count: > 0 })
        {
            notice.Tags = options.Tags;
        }

        // Merge context
        var mergedContext = new Dictionary<string, object?>(CheckendClient.GetContext());
        if (options.Context != null)
        {
            foreach (var kvp in options.Context)
            {
                mergedContext[kvp.Key] = kvp.Value;
            }
        }
        notice.Context = SanitizeFilter.Filter(mergedContext, _config.FilterKeys);

        // Merge request
        var mergedRequest = new Dictionary<string, object?>(CheckendClient.GetRequest());
        if (options.Request != null)
        {
            foreach (var kvp in options.Request)
            {
                mergedRequest[kvp.Key] = kvp.Value;
            }
        }
        notice.Request = SanitizeFilter.Filter(mergedRequest, _config.FilterKeys);

        // Merge user
        var mergedUser = new Dictionary<string, object?>(CheckendClient.GetUser());
        if (options.User != null)
        {
            foreach (var kvp in options.User)
            {
                mergedUser[kvp.Key] = kvp.Value;
            }
        }
        notice.User = SanitizeFilter.Filter(mergedUser, _config.FilterKeys);

        return notice;
    }

    private static string TruncateMessage(string? message)
    {
        if (string.IsNullOrEmpty(message))
        {
            return "";
        }
        return message.Length > MaxMessageLength ? message[..MaxMessageLength] : message;
    }

    private static List<BacktraceFrame> BuildBacktrace(Exception exception)
    {
        var frames = new List<BacktraceFrame>();
        var stackTrace = new StackTrace(exception, true);
        var stackFrames = stackTrace.GetFrames();

        if (stackFrames == null)
        {
            return frames;
        }

        var count = Math.Min(stackFrames.Length, MaxBacktraceLines);
        for (var i = 0; i < count; i++)
        {
            var frame = stackFrames[i];
            var method = frame.GetMethod();

            frames.Add(new BacktraceFrame
            {
                File = frame.GetFileName() ?? "Unknown",
                Line = frame.GetFileLineNumber(),
                Method = method != null
                    ? $"{method.DeclaringType?.FullName ?? "Unknown"}.{method.Name}"
                    : "Unknown"
            });
        }

        return frames;
    }
}

/// <summary>
/// Options for building a notice.
/// </summary>
public sealed class NoticeOptions
{
    public string? Fingerprint { get; set; }
    public List<string>? Tags { get; set; }
    public Dictionary<string, object?>? Context { get; set; }
    public Dictionary<string, object?>? Request { get; set; }
    public Dictionary<string, object?>? User { get; set; }
}
