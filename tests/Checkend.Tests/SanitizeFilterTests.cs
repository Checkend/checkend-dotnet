using Checkend.Filters;
using Xunit;

namespace Checkend.Tests;

public class SanitizeFilterTests
{
    private static readonly HashSet<string> DefaultKeys = new(StringComparer.OrdinalIgnoreCase)
    {
        "password", "secret", "api_key", "token", "authorization"
    };

    [Fact]
    public void FiltersPassword()
    {
        var data = new Dictionary<string, object?>
        {
            ["username"] = "john",
            ["password"] = "secret123"
        };

        var result = SanitizeFilter.Filter(data, DefaultKeys);

        Assert.Equal("john", result["username"]);
        Assert.Equal("[FILTERED]", result["password"]);
    }

    [Fact]
    public void CaseInsensitive()
    {
        var data = new Dictionary<string, object?>
        {
            ["PASSWORD"] = "secret",
            ["Password"] = "secret",
            ["user_password"] = "secret"
        };

        var result = SanitizeFilter.Filter(data, DefaultKeys);

        Assert.Equal("[FILTERED]", result["PASSWORD"]);
        Assert.Equal("[FILTERED]", result["Password"]);
        Assert.Equal("[FILTERED]", result["user_password"]);
    }

    [Fact]
    public void SubstringMatching()
    {
        var data = new Dictionary<string, object?>
        {
            ["user_api_key_id"] = "key123",
            ["auth_token_value"] = "token123",
            ["secret_data"] = "data"
        };

        var result = SanitizeFilter.Filter(data, DefaultKeys);

        Assert.Equal("[FILTERED]", result["user_api_key_id"]);
        Assert.Equal("[FILTERED]", result["auth_token_value"]);
        Assert.Equal("[FILTERED]", result["secret_data"]);
    }

    [Fact]
    public void NestedDictionaries()
    {
        var nested = new Dictionary<string, object?>
        {
            ["password"] = "nested_secret",
            ["name"] = "test"
        };

        var data = new Dictionary<string, object?>
        {
            ["user"] = nested
        };

        var result = SanitizeFilter.Filter(data, DefaultKeys);

        var resultNested = result["user"] as Dictionary<string, object?>;
        Assert.NotNull(resultNested);
        Assert.Equal("[FILTERED]", resultNested["password"]);
        Assert.Equal("test", resultNested["name"]);
    }

    [Fact]
    public void Lists()
    {
        var item1 = new Dictionary<string, object?> { ["password"] = "pass1", ["id"] = 1 };
        var item2 = new Dictionary<string, object?> { ["password"] = "pass2", ["id"] = 2 };

        var data = new Dictionary<string, object?>
        {
            ["items"] = new List<object?> { item1, item2 }
        };

        var result = SanitizeFilter.Filter(data, DefaultKeys);

        var resultItems = result["items"] as List<object?>;
        Assert.NotNull(resultItems);
        Assert.Equal(2, resultItems.Count);

        var resultItem1 = resultItems[0] as Dictionary<string, object?>;
        Assert.NotNull(resultItem1);
        Assert.Equal("[FILTERED]", resultItem1["password"]);
        Assert.Equal(1, resultItem1["id"]);
    }

    [Fact]
    public void TruncatesLongStrings()
    {
        var longString = new string('a', 15000);
        var data = new Dictionary<string, object?>
        {
            ["description"] = longString
        };

        var result = SanitizeFilter.Filter(data, DefaultKeys);

        var resultString = result["description"] as string;
        Assert.NotNull(resultString);
        Assert.Equal(10000, resultString.Length);
    }

    [Fact]
    public void HandlesNull()
    {
        var data = new Dictionary<string, object?>
        {
            ["value"] = null
        };

        var result = SanitizeFilter.Filter(data, DefaultKeys);

        Assert.Null(result["value"]);
    }

    [Fact]
    public void EmptyDictionary()
    {
        var result = SanitizeFilter.Filter(new Dictionary<string, object?>(), DefaultKeys);
        Assert.Empty(result);
    }

    [Fact]
    public void NullDictionary()
    {
        var result = SanitizeFilter.Filter(null, DefaultKeys);
        Assert.Empty(result);
    }

    [Fact]
    public void PreservesNumbers()
    {
        var data = new Dictionary<string, object?>
        {
            ["count"] = 42,
            ["price"] = 19.99
        };

        var result = SanitizeFilter.Filter(data, DefaultKeys);

        Assert.Equal(42, result["count"]);
        Assert.Equal(19.99, result["price"]);
    }

    [Fact]
    public void PreservesBooleans()
    {
        var data = new Dictionary<string, object?>
        {
            ["active"] = true,
            ["deleted"] = false
        };

        var result = SanitizeFilter.Filter(data, DefaultKeys);

        Assert.Equal(true, result["active"]);
        Assert.Equal(false, result["deleted"]);
    }
}
