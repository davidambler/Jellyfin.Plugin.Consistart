using System.Collections.Concurrent;

namespace Jellyfin.Plugin.Consistart.Infrastructure.Concurrency;

/// <summary>
/// Provides per-key single-flight de-duplication for asynchronous operations.
/// Concurrent callers for the same key await the same underlying task.
/// Cancellation only affects the waiter (via WaitAsync) and does not cancel the shared operation.
/// </summary>
internal sealed class SingleFlight<TKey, TValue>(IEqualityComparer<TKey>? comparer = null)
    where TKey : notnull
{
    private readonly ConcurrentDictionary<TKey, Lazy<Task<TValue>>> _inFlight = new(
        comparer ?? EqualityComparer<TKey>.Default
    );

    /// <summary>
    /// Runs (or joins) a single in-flight operation for the given key.
    /// </summary>
    public Task<TValue> RunAsync(
        TKey key,
        Func<CancellationToken, Task<TValue>> operation,
        CancellationToken waiterCancellation = default,
        CancellationToken operationCancellation = default
    )
    {
        var lazy = _inFlight.GetOrAdd(
            key,
            k => new Lazy<Task<TValue>>(
                () => StartAndTrackAsync(k, operation, operationCancellation),
                LazyThreadSafetyMode.ExecutionAndPublication
            )
        );

        return lazy.Value.WaitAsync(waiterCancellation);
    }

    private Task<TValue> StartAndTrackAsync(
        TKey key,
        Func<CancellationToken, Task<TValue>> operation,
        CancellationToken operationCancellation
    )
    {
        var task = operation(operationCancellation);

        _ = task.ContinueWith(
            _ => _inFlight.TryRemove(key, out var _),
            CancellationToken.None,
            TaskContinuationOptions.ExecuteSynchronously,
            TaskScheduler.Default
        );

        return task;
    }
}
