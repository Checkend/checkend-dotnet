using Microsoft.AspNetCore.Http;

namespace Checkend.Integrations.AspNetCore;

/// <summary>
/// ASP.NET Core middleware for automatic error reporting.
/// </summary>
public class CheckendMiddleware
{
    private readonly RequestDelegate _next;

    public CheckendMiddleware(RequestDelegate next)
    {
        _next = next;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        try
        {
            SetRequestContext(context);
            await _next(context);
        }
        catch (Exception ex)
        {
            CheckendClient.Notify(ex);
            throw;
        }
        finally
        {
            CheckendClient.Clear();
        }
    }

    private static void SetRequestContext(HttpContext context)
    {
        var request = context.Request;

        var requestInfo = new Dictionary<string, object?>
        {
            ["url"] = GetFullUrl(request),
            ["method"] = request.Method,
            ["remote_addr"] = context.Connection.RemoteIpAddress?.ToString(),
            ["user_agent"] = request.Headers.UserAgent.ToString()
        };

        // Collect headers (excluding sensitive ones)
        var headers = new Dictionary<string, string>();
        foreach (var header in request.Headers)
        {
            var lowerName = header.Key.ToLowerInvariant();
            if (!lowerName.Contains("authorization") &&
                !lowerName.Contains("cookie") &&
                !lowerName.Contains("token"))
            {
                headers[header.Key] = header.Value.ToString();
            }
        }
        requestInfo["headers"] = headers;

        // Collect query parameters
        if (request.Query.Count > 0)
        {
            var queryParams = new Dictionary<string, object?>();
            foreach (var param in request.Query)
            {
                queryParams[param.Key] = param.Value.Count == 1
                    ? param.Value.ToString()
                    : param.Value.ToArray();
            }
            requestInfo["params"] = queryParams;
        }

        CheckendClient.SetRequest(requestInfo);
    }

    private static string GetFullUrl(HttpRequest request)
    {
        return $"{request.Scheme}://{request.Host}{request.Path}{request.QueryString}";
    }
}
