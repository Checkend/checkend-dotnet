using Xunit;

namespace Checkend.Tests;

public class ResponseTests
{
    [Fact]
    public void IsSuccess_TrueFor2xxStatusCodes()
    {
        Assert.True(new Response(200, "", null).IsSuccess);
        Assert.True(new Response(201, "", null).IsSuccess);
        Assert.True(new Response(204, "", null).IsSuccess);
    }

    [Fact]
    public void IsSuccess_FalseForNon2xxStatusCodes()
    {
        Assert.False(new Response(400, "", null).IsSuccess);
        Assert.False(new Response(401, "", null).IsSuccess);
        Assert.False(new Response(429, "", null).IsSuccess);
        Assert.False(new Response(500, "", null).IsSuccess);
    }

    [Fact]
    public void IsRateLimited_TrueFor429()
    {
        Assert.True(new Response(429, "", null).IsRateLimited);
    }

    [Fact]
    public void IsRateLimited_FalseForOtherStatusCodes()
    {
        Assert.False(new Response(200, "", null).IsRateLimited);
        Assert.False(new Response(201, "", null).IsRateLimited);
        Assert.False(new Response(400, "", null).IsRateLimited);
        Assert.False(new Response(401, "", null).IsRateLimited);
        Assert.False(new Response(500, "", null).IsRateLimited);
    }

    [Fact]
    public void GetRetryAfterMs_ReturnsDefaultWhenHeaderIsNull()
    {
        var response = new Response(429, "", null);
        Assert.Equal(60000, response.GetRetryAfterMs());
        Assert.Equal(30000, response.GetRetryAfterMs(30000));
    }

    [Fact]
    public void GetRetryAfterMs_ReturnsDefaultWhenHeaderIsEmpty()
    {
        var response = new Response(429, "", "");
        Assert.Equal(60000, response.GetRetryAfterMs());
    }

    [Fact]
    public void GetRetryAfterMs_ParsesSecondsToMilliseconds()
    {
        var response = new Response(429, "", "45");
        Assert.Equal(45000, response.GetRetryAfterMs());
    }

    [Fact]
    public void GetRetryAfterMs_ReturnsDefaultForInvalidValue()
    {
        // HTTP-date format is not supported, should return default
        var response = new Response(429, "", "Wed, 21 Oct 2025 07:28:00 GMT");
        Assert.Equal(60000, response.GetRetryAfterMs());
    }

    [Fact]
    public void GetRetryAfterMs_HandlesLargeValues()
    {
        var response = new Response(429, "", "3600");
        Assert.Equal(3600000, response.GetRetryAfterMs());
    }

    [Fact]
    public void GetRetryAfterMs_HandlesZero()
    {
        var response = new Response(429, "", "0");
        Assert.Equal(0, response.GetRetryAfterMs());
    }
}
