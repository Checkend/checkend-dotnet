using Xunit;

namespace Checkend.Tests;

public class CheckendClientTests : IDisposable
{
    public CheckendClientTests()
    {
        Testing.Setup();
    }

    public void Dispose()
    {
        Testing.Teardown();
        CheckendClient.ResetAsync().GetAwaiter().GetResult();
    }

    [Fact]
    public void Configure_SetsConfiguration()
    {
        CheckendClient.Configure(builder => builder
            .ApiKey("test-key")
            .Endpoint("https://test.example.com")
            .Environment("test")
            .Enabled(true));

        Assert.True(CheckendClient.IsConfigured);
        Assert.Equal("test-key", CheckendClient.GetConfiguration()?.ApiKey);
        Assert.Equal("https://test.example.com", CheckendClient.GetConfiguration()?.Endpoint);
    }

    [Fact]
    public void Notify_CapturesNotice()
    {
        CheckendClient.Configure(builder => builder
            .ApiKey("test-key")
            .Enabled(true));

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
        Assert.NotNull(notice);
        Assert.Equal("System.InvalidOperationException", notice.ErrorClass);
        Assert.Equal("Test error", notice.Message);
    }

    [Fact]
    public void Notify_WithOptions()
    {
        CheckendClient.Configure(builder => builder
            .ApiKey("test-key")
            .Enabled(true));

        try
        {
            throw new InvalidOperationException("Test error");
        }
        catch (Exception ex)
        {
            CheckendClient.Notify(ex, new NoticeOptions
            {
                Fingerprint = "custom-fingerprint",
                Tags = new List<string> { "critical", "backend" },
                Context = new Dictionary<string, object?> { ["order_id"] = 123 }
            });
        }

        var notice = Testing.LastNotice();
        Assert.NotNull(notice);
        Assert.Equal("custom-fingerprint", notice.Fingerprint);
        Assert.Equal(new List<string> { "critical", "backend" }, notice.Tags);
        Assert.Equal(123, notice.Context["order_id"]);
    }

    [Fact]
    public void ContextManagement_WorksCorrectly()
    {
        CheckendClient.Configure(builder => builder
            .ApiKey("test-key")
            .Enabled(true));

        CheckendClient.SetContext(new Dictionary<string, object?> { ["key1"] = "value1" });
        CheckendClient.SetUser(new Dictionary<string, object?> { ["id"] = 42 });
        CheckendClient.SetRequest(new Dictionary<string, object?> { ["url"] = "/test" });

        Assert.Equal("value1", CheckendClient.GetContext()["key1"]);
        Assert.Equal(42, CheckendClient.GetUser()["id"]);
        Assert.Equal("/test", CheckendClient.GetRequest()["url"]);

        CheckendClient.Clear();

        Assert.Empty(CheckendClient.GetContext());
        Assert.Empty(CheckendClient.GetUser());
        Assert.Empty(CheckendClient.GetRequest());
    }

    [Fact]
    public void ContextIncludedInNotice()
    {
        CheckendClient.Configure(builder => builder
            .ApiKey("test-key")
            .Enabled(true));

        CheckendClient.SetContext(new Dictionary<string, object?> { ["feature"] = "checkout" });
        CheckendClient.SetUser(new Dictionary<string, object?> { ["id"] = 123, ["email"] = "test@example.com" });
        CheckendClient.SetRequest(new Dictionary<string, object?> { ["url"] = "/checkout", ["method"] = "POST" });

        try
        {
            throw new InvalidOperationException("Test error");
        }
        catch (Exception ex)
        {
            CheckendClient.Notify(ex);
        }

        var notice = Testing.LastNotice();
        Assert.NotNull(notice);
        Assert.Equal("checkout", notice.Context["feature"]);
        Assert.Equal(123, notice.User["id"]);
        Assert.Equal("/checkout", notice.Request["url"]);
    }

    [Fact]
    public void Disabled_DoesNotCapture()
    {
        CheckendClient.Configure(builder => builder
            .ApiKey("test-key")
            .Enabled(false));

        try
        {
            throw new InvalidOperationException("Test error");
        }
        catch (Exception ex)
        {
            CheckendClient.Notify(ex);
        }

        Assert.False(Testing.HasNotices());
    }

    [Fact]
    public void IgnoredException_IsNotCaptured()
    {
        CheckendClient.Configure(builder => builder
            .ApiKey("test-key")
            .Enabled(true)
            .AddIgnoredException<ArgumentException>());

        try
        {
            throw new ArgumentException("Ignored error");
        }
        catch (Exception ex)
        {
            CheckendClient.Notify(ex);
        }

        Assert.False(Testing.HasNotices());

        // Non-ignored exception should be captured
        try
        {
            throw new InvalidOperationException("Not ignored");
        }
        catch (Exception ex)
        {
            CheckendClient.Notify(ex);
        }

        Assert.True(Testing.HasNotices());
    }

    [Fact]
    public void BeforeNotify_CanModifyNotice()
    {
        CheckendClient.Configure(builder => builder
            .ApiKey("test-key")
            .Enabled(true)
            .AddBeforeNotify(notice =>
            {
                notice.Context["added_by_callback"] = true;
                return notice;
            }));

        try
        {
            throw new InvalidOperationException("Test error");
        }
        catch (Exception ex)
        {
            CheckendClient.Notify(ex);
        }

        var notice = Testing.LastNotice();
        Assert.NotNull(notice);
        Assert.Equal(true, notice.Context["added_by_callback"]);
    }

    [Fact]
    public void BeforeNotify_CanFilterNotice()
    {
        CheckendClient.Configure(builder => builder
            .ApiKey("test-key")
            .Enabled(true)
            .AddBeforeNotify(notice =>
            {
                if (notice.Message.Contains("skip"))
                {
                    return false;
                }
                return true;
            }));

        try
        {
            throw new InvalidOperationException("skip this error");
        }
        catch (Exception ex)
        {
            CheckendClient.Notify(ex);
        }

        Assert.False(Testing.HasNotices());

        try
        {
            throw new InvalidOperationException("regular error");
        }
        catch (Exception ex)
        {
            CheckendClient.Notify(ex);
        }

        Assert.True(Testing.HasNotices());
    }
}
