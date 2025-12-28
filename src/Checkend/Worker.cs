using System.Threading.Channels;

namespace Checkend;

/// <summary>
/// Background worker for async notice sending.
/// </summary>
public sealed class Worker : IDisposable
{
    private static readonly int[] RetryDelaysMs = { 100, 200, 400 };
    private const int MaxRetries = 3;

    private readonly Configuration _config;
    private readonly Client _client;
    private readonly Channel<Notice> _channel;
    private readonly Task _workerTask;
    private readonly CancellationTokenSource _cts;

    public Worker(Configuration config, Client client)
    {
        _config = config;
        _client = client;
        _cts = new CancellationTokenSource();

        _channel = Channel.CreateBounded<Notice>(new BoundedChannelOptions(config.MaxQueueSize)
        {
            FullMode = BoundedChannelFullMode.DropOldest
        });

        _workerTask = Task.Run(ProcessQueueAsync);
    }

    private async Task ProcessQueueAsync()
    {
        try
        {
            await foreach (var notice in _channel.Reader.ReadAllAsync(_cts.Token))
            {
                await SendWithRetryAsync(notice);
            }
        }
        catch (OperationCanceledException)
        {
            // Drain remaining items on shutdown
            while (_channel.Reader.TryRead(out var notice))
            {
                await SendWithRetryAsync(notice);
            }
        }
    }

    private async Task SendWithRetryAsync(Notice notice)
    {
        for (var attempt = 0; attempt < MaxRetries; attempt++)
        {
            try
            {
                var response = await _client.SendAsync(notice);

                if (response.IsSuccess)
                {
                    if (_config.Debug)
                    {
                        Console.WriteLine("[Checkend] Notice sent successfully");
                    }
                    return;
                }

                // Don't retry client errors (4xx)
                if (response.StatusCode >= 400 && response.StatusCode < 500)
                {
                    if (_config.Debug)
                    {
                        Console.Error.WriteLine($"[Checkend] Client error, not retrying: {response.StatusCode}");
                    }
                    return;
                }

                // Retry server errors (5xx)
                if (attempt < MaxRetries - 1)
                {
                    if (_config.Debug)
                    {
                        Console.WriteLine($"[Checkend] Retrying after server error: {response.StatusCode}");
                    }
                    await Task.Delay(RetryDelaysMs[attempt]);
                }
            }
            catch (Exception ex)
            {
                if (_config.Debug)
                {
                    Console.Error.WriteLine($"[Checkend] Error sending notice: {ex.Message}");
                }
                if (attempt < MaxRetries - 1)
                {
                    await Task.Delay(RetryDelaysMs[attempt]);
                }
            }
        }

        if (_config.Debug)
        {
            Console.Error.WriteLine($"[Checkend] Failed to send notice after {MaxRetries} attempts");
        }
    }

    /// <summary>
    /// Queue a notice for sending.
    /// </summary>
    public bool Enqueue(Notice notice)
    {
        return _channel.Writer.TryWrite(notice);
    }

    /// <summary>
    /// Wait for all pending notices to be sent.
    /// </summary>
    public async Task FlushAsync(TimeSpan? timeout = null)
    {
        timeout ??= TimeSpan.FromSeconds(30);
        var deadline = DateTime.UtcNow + timeout.Value;

        while (_channel.Reader.Count > 0 && DateTime.UtcNow < deadline)
        {
            await Task.Delay(50);
        }
    }

    /// <summary>
    /// Stop the worker gracefully.
    /// </summary>
    public async Task StopAsync()
    {
        _channel.Writer.Complete();
        await _cts.CancelAsync();

        try
        {
            await _workerTask.WaitAsync(TimeSpan.FromSeconds(5));
        }
        catch (TimeoutException)
        {
            // Force stop
        }
    }

    /// <summary>
    /// Get the current queue size.
    /// </summary>
    public int QueueSize => _channel.Reader.Count;

    public void Dispose()
    {
        _cts.Dispose();
    }
}
