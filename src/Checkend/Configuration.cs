namespace Checkend;

/// <summary>
/// Configuration for the Checkend SDK.
/// </summary>
public sealed class Configuration
{
    private const string DefaultEndpoint = "https://app.checkend.io";
    private const int DefaultTimeout = 15000;
    private const int DefaultMaxQueueSize = 1000;

    private static readonly HashSet<string> DefaultFilterKeys = new(StringComparer.OrdinalIgnoreCase)
    {
        "password", "password_confirmation", "secret", "secret_key",
        "api_key", "apikey", "access_token", "auth_token", "authorization",
        "token", "credit_card", "card_number", "cvv", "cvc", "ssn", "social_security"
    };

    public string ApiKey { get; init; } = "";
    public string Endpoint { get; init; } = DefaultEndpoint;
    public string Environment { get; init; } = DetectEnvironment();
    public bool Enabled { get; init; } = true;
    public bool AsyncSend { get; init; } = true;
    public int MaxQueueSize { get; init; } = DefaultMaxQueueSize;
    public int Timeout { get; init; } = DefaultTimeout;
    public HashSet<string> FilterKeys { get; init; } = new(DefaultFilterKeys, StringComparer.OrdinalIgnoreCase);
    public List<object> IgnoredExceptions { get; init; } = new();
    public List<Func<Notice, object?>> BeforeNotify { get; init; } = new();
    public bool Debug { get; init; }

    /// <summary>
    /// Create a new configuration with the specified API key.
    /// </summary>
    public Configuration(string apiKey)
    {
        if (string.IsNullOrEmpty(apiKey))
        {
            throw new ArgumentException("API key is required", nameof(apiKey));
        }
        ApiKey = apiKey;
    }

    /// <summary>
    /// Create a configuration from environment variables.
    /// </summary>
    public Configuration()
    {
        var envApiKey = System.Environment.GetEnvironmentVariable("CHECKEND_API_KEY");
        if (!string.IsNullOrEmpty(envApiKey))
        {
            ApiKey = envApiKey;
        }

        var envEndpoint = System.Environment.GetEnvironmentVariable("CHECKEND_ENDPOINT");
        if (!string.IsNullOrEmpty(envEndpoint))
        {
            Endpoint = envEndpoint;
        }

        var envEnvironment = System.Environment.GetEnvironmentVariable("CHECKEND_ENVIRONMENT");
        if (!string.IsNullOrEmpty(envEnvironment))
        {
            Environment = envEnvironment;
        }

        var envDebug = System.Environment.GetEnvironmentVariable("CHECKEND_DEBUG");
        if (envDebug is "true" or "1")
        {
            Debug = true;
        }

        // Auto-enable for production/staging
        Enabled = Environment.Equals("production", StringComparison.OrdinalIgnoreCase) ||
                  Environment.Equals("staging", StringComparison.OrdinalIgnoreCase);
    }

    private static string DetectEnvironment()
    {
        string[] envVars = { "ASPNETCORE_ENVIRONMENT", "DOTNET_ENVIRONMENT", "ENVIRONMENT", "ENV" };
        foreach (var envVar in envVars)
        {
            var value = System.Environment.GetEnvironmentVariable(envVar);
            if (!string.IsNullOrEmpty(value))
            {
                return value;
            }
        }
        return "development";
    }

    /// <summary>
    /// Fluent builder for Configuration.
    /// </summary>
    public class Builder
    {
        private string _apiKey = "";
        private string _endpoint = DefaultEndpoint;
        private string _environment = DetectEnvironment();
        private bool _enabled = true;
        private bool _asyncSend = true;
        private int _maxQueueSize = DefaultMaxQueueSize;
        private int _timeout = DefaultTimeout;
        private readonly HashSet<string> _filterKeys = new(DefaultFilterKeys, StringComparer.OrdinalIgnoreCase);
        private readonly List<object> _ignoredExceptions = new();
        private readonly List<Func<Notice, object?>> _beforeNotify = new();
        private bool _debug;

        public Builder()
        {
            // Read from environment variables
            var envApiKey = System.Environment.GetEnvironmentVariable("CHECKEND_API_KEY");
            if (!string.IsNullOrEmpty(envApiKey))
            {
                _apiKey = envApiKey;
            }

            var envEndpoint = System.Environment.GetEnvironmentVariable("CHECKEND_ENDPOINT");
            if (!string.IsNullOrEmpty(envEndpoint))
            {
                _endpoint = envEndpoint;
            }

            var envEnvironment = System.Environment.GetEnvironmentVariable("CHECKEND_ENVIRONMENT");
            if (!string.IsNullOrEmpty(envEnvironment))
            {
                _environment = envEnvironment;
            }

            var envDebug = System.Environment.GetEnvironmentVariable("CHECKEND_DEBUG");
            if (envDebug is "true" or "1")
            {
                _debug = true;
            }

            _enabled = _environment.Equals("production", StringComparison.OrdinalIgnoreCase) ||
                      _environment.Equals("staging", StringComparison.OrdinalIgnoreCase);
        }

        public Builder ApiKey(string apiKey) { _apiKey = apiKey; return this; }
        public Builder Endpoint(string endpoint) { _endpoint = endpoint; return this; }
        public Builder Environment(string environment) { _environment = environment; return this; }
        public Builder Enabled(bool enabled) { _enabled = enabled; return this; }
        public Builder AsyncSend(bool asyncSend) { _asyncSend = asyncSend; return this; }
        public Builder MaxQueueSize(int maxQueueSize) { _maxQueueSize = maxQueueSize; return this; }
        public Builder Timeout(int timeout) { _timeout = timeout; return this; }
        public Builder Debug(bool debug) { _debug = debug; return this; }

        public Builder AddFilterKey(string key)
        {
            _filterKeys.Add(key);
            return this;
        }

        public Builder AddIgnoredException<T>() where T : Exception
        {
            _ignoredExceptions.Add(typeof(T));
            return this;
        }

        public Builder AddIgnoredException(string exceptionName)
        {
            _ignoredExceptions.Add(exceptionName);
            return this;
        }

        public Builder AddIgnoredException(System.Text.RegularExpressions.Regex pattern)
        {
            _ignoredExceptions.Add(pattern);
            return this;
        }

        public Builder AddBeforeNotify(Func<Notice, object?> callback)
        {
            _beforeNotify.Add(callback);
            return this;
        }

        public Configuration Build()
        {
            if (string.IsNullOrEmpty(_apiKey))
            {
                throw new InvalidOperationException("API key is required");
            }

            return new Configuration(_apiKey)
            {
                Endpoint = _endpoint,
                Environment = _environment,
                Enabled = _enabled,
                AsyncSend = _asyncSend,
                MaxQueueSize = _maxQueueSize,
                Timeout = _timeout,
                FilterKeys = _filterKeys,
                IgnoredExceptions = _ignoredExceptions,
                BeforeNotify = _beforeNotify,
                Debug = _debug
            };
        }
    }
}
