using Checkend.Filters;

namespace Checkend;

/// <summary>
/// Main entry point for the Checkend .NET SDK.
/// </summary>
public static class CheckendClient
{
    private static Configuration? _config;
    private static Client? _client;
    private static Worker? _worker;
    private static NoticeBuilder? _noticeBuilder;

    // AsyncLocal for request-scoped context
    private static readonly AsyncLocal<Dictionary<string, object?>?> ContextStore = new();
    private static readonly AsyncLocal<Dictionary<string, object?>?> UserStore = new();
    private static readonly AsyncLocal<Dictionary<string, object?>?> RequestStore = new();

    /// <summary>
    /// Configure the SDK with a Configuration object.
    /// </summary>
    public static void Configure(Configuration configuration)
    {
        _config = configuration;
        _client = new Client(configuration);
        _worker = new Worker(configuration, _client);
        _noticeBuilder = new NoticeBuilder(configuration);

        if (configuration.Debug)
        {
            Console.WriteLine($"[Checkend] Configured with endpoint: {configuration.Endpoint}");
        }
    }

    /// <summary>
    /// Configure the SDK with a builder action.
    /// </summary>
    public static void Configure(Action<Configuration.Builder> builderAction)
    {
        var builder = new Configuration.Builder();
        builderAction(builder);
        Configure(builder.Build());
    }

    /// <summary>
    /// Report an exception asynchronously.
    /// </summary>
    public static void Notify(Exception exception, NoticeOptions? options = null)
    {
        if (!IsConfigured || !_config!.Enabled)
        {
            return;
        }

        if (IgnoreFilter.ShouldIgnore(exception, _config.IgnoredExceptions))
        {
            if (_config.Debug)
            {
                Console.WriteLine($"[Checkend] Ignoring exception: {exception.GetType().Name}");
            }
            return;
        }

        var notice = _noticeBuilder!.Build(exception, options);

        // Run before_notify callbacks
        foreach (var callback in _config.BeforeNotify)
        {
            var result = callback(notice);
            if (result is false)
            {
                if (_config.Debug)
                {
                    Console.WriteLine("[Checkend] Notice filtered by BeforeNotify callback");
                }
                return;
            }
            if (result is Notice modifiedNotice)
            {
                notice = modifiedNotice;
            }
        }

        // Testing mode
        if (Testing.IsTestingMode)
        {
            Testing.Capture(notice);
            return;
        }

        // Queue for async sending
        if (_config.AsyncSend)
        {
            _worker!.Enqueue(notice);
        }
        else
        {
            _client!.SendAsync(notice).GetAwaiter().GetResult();
        }
    }

    /// <summary>
    /// Report an exception synchronously and return the response.
    /// </summary>
    public static async Task<Response> NotifySyncAsync(
        Exception exception,
        NoticeOptions? options = null,
        CancellationToken cancellationToken = default)
    {
        if (!IsConfigured || !_config!.Enabled)
        {
            return new Response(0, "SDK not configured or disabled");
        }

        if (IgnoreFilter.ShouldIgnore(exception, _config.IgnoredExceptions))
        {
            return new Response(0, "Exception ignored");
        }

        var notice = _noticeBuilder!.Build(exception, options);

        // Run before_notify callbacks
        foreach (var callback in _config.BeforeNotify)
        {
            var result = callback(notice);
            if (result is false)
            {
                return new Response(0, "Filtered by BeforeNotify");
            }
            if (result is Notice modifiedNotice)
            {
                notice = modifiedNotice;
            }
        }

        // Testing mode
        if (Testing.IsTestingMode)
        {
            Testing.Capture(notice);
            return new Response(200, "Captured in testing mode");
        }

        return await _client!.SendAsync(notice, cancellationToken);
    }

    // Context management

    /// <summary>
    /// Set custom context for the current async context.
    /// </summary>
    public static void SetContext(Dictionary<string, object?> context)
    {
        var current = ContextStore.Value ?? new Dictionary<string, object?>();
        foreach (var kvp in context)
        {
            current[kvp.Key] = kvp.Value;
        }
        ContextStore.Value = current;
    }

    /// <summary>
    /// Get the current context.
    /// </summary>
    public static Dictionary<string, object?> GetContext()
    {
        return ContextStore.Value ?? new Dictionary<string, object?>();
    }

    /// <summary>
    /// Set user information for the current async context.
    /// </summary>
    public static void SetUser(Dictionary<string, object?> user)
    {
        var current = UserStore.Value ?? new Dictionary<string, object?>();
        foreach (var kvp in user)
        {
            current[kvp.Key] = kvp.Value;
        }
        UserStore.Value = current;
    }

    /// <summary>
    /// Get the current user information.
    /// </summary>
    public static Dictionary<string, object?> GetUser()
    {
        return UserStore.Value ?? new Dictionary<string, object?>();
    }

    /// <summary>
    /// Set request information for the current async context.
    /// </summary>
    public static void SetRequest(Dictionary<string, object?> request)
    {
        var current = RequestStore.Value ?? new Dictionary<string, object?>();
        foreach (var kvp in request)
        {
            current[kvp.Key] = kvp.Value;
        }
        RequestStore.Value = current;
    }

    /// <summary>
    /// Get the current request information.
    /// </summary>
    public static Dictionary<string, object?> GetRequest()
    {
        return RequestStore.Value ?? new Dictionary<string, object?>();
    }

    /// <summary>
    /// Clear all context for the current async context.
    /// </summary>
    public static void Clear()
    {
        ContextStore.Value = null;
        UserStore.Value = null;
        RequestStore.Value = null;
    }

    /// <summary>
    /// Wait for all pending notices to be sent.
    /// </summary>
    public static async Task FlushAsync(TimeSpan? timeout = null)
    {
        if (_worker != null)
        {
            await _worker.FlushAsync(timeout);
        }
    }

    /// <summary>
    /// Stop the worker.
    /// </summary>
    public static async Task StopAsync()
    {
        if (_worker != null)
        {
            await _worker.StopAsync();
        }
    }

    /// <summary>
    /// Reset all state (useful for testing).
    /// </summary>
    public static async Task ResetAsync()
    {
        if (_worker != null)
        {
            await _worker.StopAsync();
            _worker.Dispose();
        }
        _client?.Dispose();

        _config = null;
        _client = null;
        _worker = null;
        _noticeBuilder = null;
        Clear();
    }

    /// <summary>
    /// Check if the SDK is configured.
    /// </summary>
    public static bool IsConfigured => _config != null;

    /// <summary>
    /// Get the current configuration (for testing/debugging).
    /// </summary>
    public static Configuration? GetConfiguration() => _config;
}
