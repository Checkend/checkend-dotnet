using System.Text.RegularExpressions;

namespace Checkend.Filters;

/// <summary>
/// Filters exceptions that should be ignored.
/// </summary>
public static class IgnoreFilter
{
    /// <summary>
    /// Check if an exception should be ignored.
    /// </summary>
    public static bool ShouldIgnore(Exception exception, List<object> patterns)
    {
        if (patterns.Count == 0)
        {
            return false;
        }

        var exceptionType = exception.GetType();
        var className = exceptionType.FullName ?? exceptionType.Name;
        var simpleName = exceptionType.Name;

        foreach (var pattern in patterns)
        {
            if (Matches(exception, exceptionType, className, simpleName, pattern))
            {
                return true;
            }
        }
        return false;
    }

    private static bool Matches(
        Exception exception,
        Type exceptionType,
        string className,
        string simpleName,
        object pattern)
    {
        // Match by type
        if (pattern is Type type)
        {
            return type.IsInstanceOfType(exception);
        }

        // Match by string name
        if (pattern is string name)
        {
            return className.Equals(name, StringComparison.OrdinalIgnoreCase) ||
                   simpleName.Equals(name, StringComparison.OrdinalIgnoreCase) ||
                   className.EndsWith("." + name, StringComparison.OrdinalIgnoreCase);
        }

        // Match by regex
        if (pattern is Regex regex)
        {
            return regex.IsMatch(className) || regex.IsMatch(simpleName);
        }

        return false;
    }
}
