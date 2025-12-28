using System.Text.RegularExpressions;
using Xunit;

namespace Checkend.Tests;

public class ConfigurationTests
{
    [Fact]
    public void RequiresApiKey()
    {
        Assert.Throws<InvalidOperationException>(() =>
            new Configuration.Builder().Build()
        );
    }

    [Fact]
    public void DefaultValues()
    {
        var config = new Configuration.Builder()
            .ApiKey("test-key")
            .Build();

        Assert.Equal("test-key", config.ApiKey);
        Assert.Equal("https://app.checkend.io", config.Endpoint);
        Assert.Equal(15000, config.Timeout);
        Assert.Equal(1000, config.MaxQueueSize);
        Assert.True(config.AsyncSend);
        Assert.False(config.Debug);
    }

    [Fact]
    public void CustomValues()
    {
        var config = new Configuration.Builder()
            .ApiKey("test-key")
            .Endpoint("https://custom.example.com")
            .Environment("staging")
            .Enabled(false)
            .AsyncSend(false)
            .MaxQueueSize(500)
            .Timeout(30000)
            .Debug(true)
            .Build();

        Assert.Equal("https://custom.example.com", config.Endpoint);
        Assert.Equal("staging", config.Environment);
        Assert.False(config.Enabled);
        Assert.False(config.AsyncSend);
        Assert.Equal(500, config.MaxQueueSize);
        Assert.Equal(30000, config.Timeout);
        Assert.True(config.Debug);
    }

    [Fact]
    public void DefaultFilterKeys()
    {
        var config = new Configuration.Builder()
            .ApiKey("test-key")
            .Build();

        Assert.Contains("password", config.FilterKeys);
        Assert.Contains("secret", config.FilterKeys);
        Assert.Contains("api_key", config.FilterKeys);
        Assert.Contains("token", config.FilterKeys);
        Assert.Contains("credit_card", config.FilterKeys);
    }

    [Fact]
    public void AddFilterKey()
    {
        var config = new Configuration.Builder()
            .ApiKey("test-key")
            .AddFilterKey("custom_secret")
            .Build();

        Assert.Contains("custom_secret", config.FilterKeys);
        Assert.Contains("password", config.FilterKeys); // Still has defaults
    }

    [Fact]
    public void IgnoredExceptions()
    {
        var config = new Configuration.Builder()
            .ApiKey("test-key")
            .AddIgnoredException<InvalidOperationException>()
            .AddIgnoredException("CustomException")
            .AddIgnoredException(new Regex(".*NotFound.*"))
            .Build();

        Assert.Equal(3, config.IgnoredExceptions.Count);
    }

    [Fact]
    public void BeforeNotify()
    {
        var config = new Configuration.Builder()
            .ApiKey("test-key")
            .AddBeforeNotify(notice =>
            {
                notice.Context["test"] = true;
                return notice;
            })
            .Build();

        Assert.Single(config.BeforeNotify);
    }
}
