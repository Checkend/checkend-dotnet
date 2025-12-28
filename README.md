# Checkend .NET SDK

.NET SDK for [Checkend](https://checkend.io) error monitoring. Async by default with ASP.NET Core integration.

## Features

- **Async by default** - Non-blocking error sending via Channel<T>
- **ASP.NET Core Middleware** - Easy integration with web apps
- **Automatic context** - Request, user, and custom context tracking
- **Sensitive data filtering** - Automatic scrubbing of passwords, tokens, etc.
- **Testing utilities** - Capture errors in tests without sending

## Requirements

- .NET 8.0+
- No external dependencies

## Installation

### NuGet

```bash
dotnet add package Checkend
```

### Package Manager

```powershell
Install-Package Checkend
```

## Quick Start

```csharp
using Checkend;

// Configure the SDK
CheckendClient.Configure(builder => builder
    .ApiKey("your-api-key")
);

// Report an error
try
{
    DoSomething();
}
catch (Exception ex)
{
    CheckendClient.Notify(ex);
}
```

## Configuration

```csharp
CheckendClient.Configure(builder => builder
    .ApiKey("your-api-key")              // Required
    .Endpoint("https://app.checkend.io") // Optional: Custom endpoint
    .Environment("production")            // Optional: Auto-detected
    .Enabled(true)                        // Optional: Enable/disable
    .AsyncSend(true)                      // Optional: Async sending (default: true)
    .MaxQueueSize(1000)                   // Optional: Max queue size
    .Timeout(15000)                       // Optional: HTTP timeout in ms
    .AddFilterKey("custom_secret")        // Optional: Additional keys to filter
    .AddIgnoredException<MyException>()   // Optional: Exceptions to ignore
    .Debug(false)                         // Optional: Enable debug logging
);
```

### Environment Variables

```bash
CHECKEND_API_KEY=your-api-key
CHECKEND_ENDPOINT=https://your-server.com
CHECKEND_ENVIRONMENT=production
CHECKEND_DEBUG=true
```

## Manual Error Reporting

```csharp
// Basic error reporting
try
{
    RiskyOperation();
}
catch (Exception ex)
{
    CheckendClient.Notify(ex);
}

// With additional context
try
{
    ProcessOrder(orderId);
}
catch (Exception ex)
{
    CheckendClient.Notify(ex, new NoticeOptions
    {
        Context = new Dictionary<string, object?> { ["order_id"] = orderId },
        User = new Dictionary<string, object?> { ["id"] = userId, ["email"] = userEmail },
        Tags = new List<string> { "orders", "critical" },
        Fingerprint = "order-processing-error"
    });
}

// Synchronous sending (blocks until sent)
var response = await CheckendClient.NotifySyncAsync(ex);
if (response.IsSuccess)
{
    Console.WriteLine("Notice sent successfully");
}
```

## Context & User Tracking

```csharp
// Set context for all errors in this async context
CheckendClient.SetContext(new Dictionary<string, object?>
{
    ["order_id"] = 12345,
    ["feature_flag"] = "new-checkout"
});

// Set user information
CheckendClient.SetUser(new Dictionary<string, object?>
{
    ["id"] = user.Id,
    ["email"] = user.Email,
    ["name"] = user.Name
});

// Set request information
CheckendClient.SetRequest(new Dictionary<string, object?>
{
    ["url"] = HttpContext.Request.Path,
    ["method"] = HttpContext.Request.Method
});

// Clear all context (call at end of request)
CheckendClient.Clear();
```

## ASP.NET Core Integration

### Using Middleware

```csharp
// Program.cs
var builder = WebApplication.CreateBuilder(args);

// Add Checkend using configuration
builder.Services.AddCheckend(builder.Configuration);
// Or configure directly
builder.Services.AddCheckend(config => config
    .ApiKey("your-api-key")
    .Environment("production")
);

var app = builder.Build();

// Use Checkend middleware for automatic error reporting
app.UseCheckend();

app.Run();
```

### appsettings.json

```json
{
  "Checkend": {
    "ApiKey": "your-api-key",
    "Endpoint": "https://app.checkend.io",
    "Environment": "production",
    "Enabled": true,
    "Debug": false
  }
}
```

### Global Exception Handler

```csharp
app.UseExceptionHandler(errorApp =>
{
    errorApp.Run(async context =>
    {
        var exceptionFeature = context.Features.Get<IExceptionHandlerFeature>();
        if (exceptionFeature?.Error != null)
        {
            CheckendClient.Notify(exceptionFeature.Error);
        }

        context.Response.StatusCode = 500;
        await context.Response.WriteAsync("Internal Server Error");
    });
});
```

## Testing

Use the `Testing` class to capture errors without sending them:

```csharp
using Checkend;
using Xunit;

public class MyTests : IDisposable
{
    public MyTests()
    {
        Testing.Setup();
        CheckendClient.Configure(builder => builder
            .ApiKey("test-key")
            .Enabled(true)
        );
    }

    public void Dispose()
    {
        Testing.Teardown();
        CheckendClient.ResetAsync().GetAwaiter().GetResult();
    }

    [Fact]
    public void TestErrorReporting()
    {
        try
        {
            throw new InvalidOperationException("Test error");
        }
        catch (Exception ex)
        {
            CheckendClient.Notify(ex);
        }

        Assert.True(Testing.HasNotices());
        Assert.Equal(1, Testing.NoticeCount());

        var notice = Testing.LastNotice();
        Assert.Equal("System.InvalidOperationException", notice?.ErrorClass);
    }
}
```

## Filtering Sensitive Data

By default, these keys are filtered: `password`, `secret`, `token`, `api_key`, `authorization`, `credit_card`, `cvv`, `ssn`, etc.

Add custom keys:

```csharp
CheckendClient.Configure(builder => builder
    .ApiKey("your-api-key")
    .AddFilterKey("custom_secret")
    .AddFilterKey("internal_token")
);
```

Filtered values appear as `[FILTERED]` in the dashboard.

## Ignoring Exceptions

```csharp
CheckendClient.Configure(builder => builder
    .ApiKey("your-api-key")
    .AddIgnoredException<ResourceNotFoundException>()
    .AddIgnoredException("OperationCanceledException")
    .AddIgnoredException(new Regex(".*NotFound.*"))
);
```

## Before Notify Callbacks

```csharp
CheckendClient.Configure(builder => builder
    .ApiKey("your-api-key")
    .AddBeforeNotify(notice =>
    {
        // Add extra context
        notice.Context["server"] = Environment.MachineName;
        return notice;
    })
    .AddBeforeNotify(notice =>
    {
        // Skip certain errors
        if (notice.Message.Contains("ignore-me"))
        {
            return false;
        }
        return true;
    })
);
```

## Graceful Shutdown

The SDK automatically flushes pending notices. For manual control:

```csharp
// Wait for pending notices to send
await CheckendClient.FlushAsync();

// Stop the worker
await CheckendClient.StopAsync();
```

## Development

```bash
# Build
dotnet build

# Run tests
dotnet test

# Pack
dotnet pack
```

## License

MIT License - see [LICENSE](LICENSE) for details.
