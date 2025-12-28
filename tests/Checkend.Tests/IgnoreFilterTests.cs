using System.Text.RegularExpressions;
using Checkend.Filters;
using Xunit;

namespace Checkend.Tests;

public class IgnoreFilterTests
{
    [Fact]
    public void IgnoresByType()
    {
        var exception = new InvalidOperationException("test");

        Assert.True(IgnoreFilter.ShouldIgnore(exception, new List<object> { typeof(InvalidOperationException) }));
        Assert.False(IgnoreFilter.ShouldIgnore(exception, new List<object> { typeof(ArgumentException) }));
    }

    [Fact]
    public void IgnoresByStringName()
    {
        var exception = new InvalidOperationException("test");

        Assert.True(IgnoreFilter.ShouldIgnore(exception, new List<object> { "InvalidOperationException" }));
        Assert.True(IgnoreFilter.ShouldIgnore(exception, new List<object> { "System.InvalidOperationException" }));
        Assert.False(IgnoreFilter.ShouldIgnore(exception, new List<object> { "ArgumentException" }));
    }

    [Fact]
    public void IgnoresByRegex()
    {
        var exception = new InvalidOperationException("test");

        Assert.True(IgnoreFilter.ShouldIgnore(exception, new List<object> { new Regex(".*Invalid.*") }));
        Assert.True(IgnoreFilter.ShouldIgnore(exception, new List<object> { new Regex(".*Exception") }));
        Assert.False(IgnoreFilter.ShouldIgnore(exception, new List<object> { new Regex("Custom.*") }));
    }

    [Fact]
    public void IgnoresWithMultiplePatterns()
    {
        Assert.True(IgnoreFilter.ShouldIgnore(
            new InvalidOperationException("test"),
            new List<object> { typeof(InvalidOperationException), typeof(ArgumentException) }));

        Assert.True(IgnoreFilter.ShouldIgnore(
            new ArgumentException("test"),
            new List<object> { typeof(InvalidOperationException), typeof(ArgumentException) }));

        Assert.False(IgnoreFilter.ShouldIgnore(
            new NullReferenceException("test"),
            new List<object> { typeof(InvalidOperationException), typeof(ArgumentException) }));
    }

    [Fact]
    public void EmptyPatternsList()
    {
        Assert.False(IgnoreFilter.ShouldIgnore(new InvalidOperationException("test"), new List<object>()));
    }

    [Fact]
    public void IgnoresSubclasses()
    {
        // ArgumentNullException is a subclass of ArgumentException
        var exception = new ArgumentNullException("test");

        // Should match by type inheritance
        Assert.True(IgnoreFilter.ShouldIgnore(exception, new List<object> { typeof(ArgumentException) }));
    }

    [Fact]
    public void MixedPatternTypes()
    {
        var exception = new InvalidOperationException("test");

        Assert.True(IgnoreFilter.ShouldIgnore(exception, new List<object>
        {
            typeof(ArgumentException),
            "InvalidOperationException",
            new Regex("NotMatching.*")
        }));
    }
}
