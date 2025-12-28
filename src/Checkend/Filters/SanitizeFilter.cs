using System.Collections;

namespace Checkend.Filters;

/// <summary>
/// Filters sensitive data from dictionaries and objects.
/// </summary>
public static class SanitizeFilter
{
    private const string Filtered = "[FILTERED]";
    private const int MaxDepth = 10;
    private const int MaxStringLength = 10000;

    /// <summary>
    /// Filter sensitive data from a dictionary.
    /// </summary>
    public static Dictionary<string, object?> Filter(
        Dictionary<string, object?>? data,
        HashSet<string> filterKeys)
    {
        if (data == null || data.Count == 0)
        {
            return new Dictionary<string, object?>();
        }

        return FilterDictionary(data, filterKeys, 0, new HashSet<object>(ReferenceEqualityComparer.Instance));
    }

    private static Dictionary<string, object?> FilterDictionary(
        Dictionary<string, object?> data,
        HashSet<string> filterKeys,
        int depth,
        HashSet<object> seen)
    {
        if (depth > MaxDepth)
        {
            return new Dictionary<string, object?> { { "_truncated", "max depth exceeded" } };
        }

        if (seen.Contains(data))
        {
            return new Dictionary<string, object?> { { "_circular", "circular reference" } };
        }
        seen.Add(data);

        var result = new Dictionary<string, object?>();
        foreach (var kvp in data)
        {
            if (ShouldFilter(kvp.Key, filterKeys))
            {
                result[kvp.Key] = Filtered;
            }
            else
            {
                result[kvp.Key] = FilterValue(kvp.Value, filterKeys, depth + 1, seen);
            }
        }
        return result;
    }

    private static object? FilterValue(
        object? value,
        HashSet<string> filterKeys,
        int depth,
        HashSet<object> seen)
    {
        if (value == null)
        {
            return null;
        }

        if (value is string s)
        {
            return TruncateString(s);
        }

        if (value is int or long or double or float or decimal or bool)
        {
            return value;
        }

        if (value is Dictionary<string, object?> dict)
        {
            return FilterDictionary(dict, filterKeys, depth, seen);
        }

        if (value is IList list)
        {
            if (seen.Contains(list))
            {
                return new List<object?> { "_circular: circular reference" };
            }
            seen.Add(list);

            var result = new List<object?>();
            foreach (var item in list)
            {
                result.Add(FilterValue(item, filterKeys, depth + 1, seen));
            }
            return result;
        }

        return TruncateString(value.ToString() ?? "");
    }

    private static bool ShouldFilter(string key, HashSet<string> filterKeys)
    {
        var lowerKey = key.ToLowerInvariant();
        foreach (var filterKey in filterKeys)
        {
            if (lowerKey.Contains(filterKey.ToLowerInvariant()))
            {
                return true;
            }
        }
        return false;
    }

    private static string TruncateString(string s)
    {
        return s.Length > MaxStringLength ? s[..MaxStringLength] : s;
    }
}
