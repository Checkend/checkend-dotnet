namespace Checkend;

/// <summary>
/// Testing utilities for capturing notices without sending them.
/// </summary>
public static class Testing
{
    private static readonly List<Notice> Notices = new();
    private static readonly object Lock = new();
    private static volatile bool _testingMode;

    /// <summary>
    /// Enable testing mode. Notices will be captured instead of sent.
    /// </summary>
    public static void Setup()
    {
        lock (Lock)
        {
            _testingMode = true;
            Notices.Clear();
        }
    }

    /// <summary>
    /// Disable testing mode.
    /// </summary>
    public static void Teardown()
    {
        lock (Lock)
        {
            _testingMode = false;
            Notices.Clear();
        }
    }

    /// <summary>
    /// Check if testing mode is enabled.
    /// </summary>
    public static bool IsTestingMode => _testingMode;

    /// <summary>
    /// Capture a notice (called by CheckendClient when in testing mode).
    /// </summary>
    internal static void Capture(Notice notice)
    {
        if (!_testingMode) return;

        lock (Lock)
        {
            Notices.Add(notice);
        }
    }

    /// <summary>
    /// Get all captured notices.
    /// </summary>
    public static List<Notice> GetNotices()
    {
        lock (Lock)
        {
            return new List<Notice>(Notices);
        }
    }

    /// <summary>
    /// Get the last captured notice.
    /// </summary>
    public static Notice? LastNotice()
    {
        lock (Lock)
        {
            return Notices.Count > 0 ? Notices[^1] : null;
        }
    }

    /// <summary>
    /// Get the first captured notice.
    /// </summary>
    public static Notice? FirstNotice()
    {
        lock (Lock)
        {
            return Notices.Count > 0 ? Notices[0] : null;
        }
    }

    /// <summary>
    /// Get the number of captured notices.
    /// </summary>
    public static int NoticeCount()
    {
        lock (Lock)
        {
            return Notices.Count;
        }
    }

    /// <summary>
    /// Check if any notices have been captured.
    /// </summary>
    public static bool HasNotices()
    {
        lock (Lock)
        {
            return Notices.Count > 0;
        }
    }

    /// <summary>
    /// Clear all captured notices.
    /// </summary>
    public static void ClearNotices()
    {
        lock (Lock)
        {
            Notices.Clear();
        }
    }
}
