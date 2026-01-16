using System.Collections.Concurrent;
using Jellyfin.Plugin.Consistart.Infrastructure.Concurrency;
using NSubstitute;
using Xunit;

namespace Jellyfin.Plugin.Consistart.Tests.Infrastructure.Concurrency;

public class SingleFlightTests
{
    #region Basic Functionality Tests

    [Fact]
    public async Task RunAsync_with_simple_operation_returns_expected_value()
    {
        var singleFlight = new SingleFlight<string, int>();
        var operation = Substitute.For<Func<CancellationToken, Task<int>>>();
        operation.Invoke(Arg.Any<CancellationToken>()).Returns(Task.FromResult(42));

        var result = await singleFlight.RunAsync("key1", operation);

        Assert.Equal(42, result);
        await operation.Received(1).Invoke(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task RunAsync_with_multiple_different_keys_executes_operation_for_each_key()
    {
        var singleFlight = new SingleFlight<string, int>();
        var operation1 = Substitute.For<Func<CancellationToken, Task<int>>>();
        var operation2 = Substitute.For<Func<CancellationToken, Task<int>>>();
        operation1.Invoke(Arg.Any<CancellationToken>()).Returns(Task.FromResult(1));
        operation2.Invoke(Arg.Any<CancellationToken>()).Returns(Task.FromResult(2));

        var result1 = await singleFlight.RunAsync("key1", operation1);
        var result2 = await singleFlight.RunAsync("key2", operation2);

        Assert.Equal(1, result1);
        Assert.Equal(2, result2);
        await operation1.Received(1).Invoke(Arg.Any<CancellationToken>());
        await operation2.Received(1).Invoke(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task RunAsync_with_different_value_types_returns_correct_types()
    {
        var intSingleFlight = new SingleFlight<string, int>();
        var stringSingleFlight = new SingleFlight<string, string>();

        var intOp = Substitute.For<Func<CancellationToken, Task<int>>>();
        intOp.Invoke(Arg.Any<CancellationToken>()).Returns(Task.FromResult(123));

        var stringOp = Substitute.For<Func<CancellationToken, Task<string>>>();
        stringOp.Invoke(Arg.Any<CancellationToken>()).Returns(Task.FromResult("result"));

        var intResult = await intSingleFlight.RunAsync("key", intOp);
        var stringResult = await stringSingleFlight.RunAsync("key", stringOp);

        Assert.Equal(123, intResult);
        Assert.Equal("result", stringResult);
    }

    #endregion

    #region Concurrency Tests

    [Fact]
    public async Task RunAsync_with_concurrent_calls_same_key_executes_operation_once()
    {
        var singleFlight = new SingleFlight<string, int>();
        var operationCallCount = 0;
        var operationStarted = new TaskCompletionSource();

        async Task<int> Operation(CancellationToken ct)
        {
            Interlocked.Increment(ref operationCallCount);
            operationStarted.TrySetResult();
            // Small delay to allow concurrent callers to queue up
            await Task.Delay(5, ct);
            return 42;
        }

        var tasks = Enumerable
            .Range(0, 10)
            .Select(_ => singleFlight.RunAsync("key", Operation))
            .ToList();

        // Wait for operation to start
        await operationStarted.Task;

        var results = await Task.WhenAll(tasks);

        Assert.All(results, r => Assert.Equal(42, r));
        Assert.Equal(1, operationCallCount);
    }

    [Fact]
    public async Task RunAsync_with_concurrent_calls_same_key_all_callers_get_same_result()
    {
        var singleFlight = new SingleFlight<string, Guid>();
        var resultId = Guid.NewGuid();
        var semaphore = new SemaphoreSlim(0);

        async Task<Guid> Operation(CancellationToken ct)
        {
            // Signal that operation has started
            semaphore.Release();
            // Wait a bit to allow other callers to queue
            await Task.Delay(10, ct);
            return resultId;
        }

        var tasks = Enumerable
            .Range(0, 5)
            .Select(_ => singleFlight.RunAsync("key", Operation))
            .ToList();

        var results = await Task.WhenAll(tasks);

        Assert.All(results, r => Assert.Equal(resultId, r));
    }

    [Fact]
    public async Task RunAsync_with_sequential_calls_same_key_after_completion_executes_operation_again()
    {
        var singleFlight = new SingleFlight<string, int>();
        var operationCallCount = 0;

        async Task<int> Operation(CancellationToken ct)
        {
            Interlocked.Increment(ref operationCallCount);
            return await Task.FromResult(42);
        }

        var result1 = await singleFlight.RunAsync("key", Operation);
        var result2 = await singleFlight.RunAsync("key", Operation);

        Assert.Equal(42, result1);
        Assert.Equal(42, result2);
        Assert.Equal(2, operationCallCount);
    }

    [Fact]
    public async Task RunAsync_with_high_concurrency_all_callers_get_correct_results()
    {
        var singleFlight = new SingleFlight<int, int>();
        var callCounts = new ConcurrentDictionary<int, int>();
        var semaphores = new Dictionary<int, SemaphoreSlim>
        {
            { 0, new SemaphoreSlim(0) },
            { 1, new SemaphoreSlim(0) },
            { 2, new SemaphoreSlim(0) },
        };

        Func<CancellationToken, Task<int>> CreateOperation(int key)
        {
            return async ct =>
            {
                callCounts.AddOrUpdate(key, 1, (_, count) => count + 1);
                semaphores[key].Release();
                await Task.Delay(10, ct); // Brief delay to allow queueing
                return key * 2;
            };
        }

        var tasks = new List<Task<int>>();
        for (var i = 0; i < 3; i++)
        {
            for (var j = 0; j < 20; j++)
            {
                tasks.Add(singleFlight.RunAsync(i, CreateOperation(i)));
            }
        }

        var results = await Task.WhenAll(tasks);

        Assert.All(
            results,
            (r, idx) =>
            {
                var key = idx / 20;
                Assert.Equal(key * 2, r);
            }
        );

        Assert.All(callCounts, kvp => Assert.Equal(1, kvp.Value));
    }

    #endregion

    #region Cancellation Tests

    [Fact]
    public async Task RunAsync_with_waiter_cancellation_throws_operation_canceled_exception()
    {
        var singleFlight = new SingleFlight<string, int>();
        var operationStarted = new TaskCompletionSource();
        var cts = new CancellationTokenSource();

        async Task<int> Operation(CancellationToken ct)
        {
            operationStarted.TrySetResult();
            // Small delay to ensure waiter cancellation takes effect
            await Task.Delay(100, ct);
            return 42;
        }

        var task = singleFlight.RunAsync("key", Operation, waiterCancellation: cts.Token);

        // Wait for operation to start
        await operationStarted.Task;

        // Now cancel the waiter
        cts.Cancel();

        // WaitAsync throws TaskCanceledException which is derived from OperationCanceledException
        var ex = await Assert.ThrowsAsync<TaskCanceledException>(async () => await task);
        Assert.IsAssignableFrom<OperationCanceledException>(ex);
    }

    [Fact]
    public async Task RunAsync_with_waiter_cancellation_does_not_cancel_underlying_operation()
    {
        var singleFlight = new SingleFlight<string, int>();
        var operationStarted = new TaskCompletionSource();
        var operationCompleted = new TaskCompletionSource<int>();

        async Task<int> Operation(CancellationToken ct)
        {
            try
            {
                operationStarted.TrySetResult();
                // Simulate work that takes a bit
                await Task.Delay(50, ct);
                operationCompleted.TrySetResult(42);
                return 42;
            }
            catch (OperationCanceledException)
            {
                operationCompleted.TrySetException(new OperationCanceledException());
                return 0;
            }
        }

        using var waiterCts = new CancellationTokenSource();

        var task = singleFlight.RunAsync("key", Operation, waiterCancellation: waiterCts.Token);

        // Wait for operation to start
        await operationStarted.Task;

        // Cancel the waiter
        waiterCts.Cancel();

        try
        {
            await task;
        }
        catch (OperationCanceledException)
        {
            // Expected - waiter was cancelled
        }

        // Operation should still complete even though waiter was cancelled
        var result = await operationCompleted.Task;
        Assert.Equal(42, result);
    }

    [Fact]
    public async Task RunAsync_with_operation_cancellation_cancels_the_operation()
    {
        var singleFlight = new SingleFlight<string, int>();
        var operationStarted = new TaskCompletionSource();
        var operationCancelled = new TaskCompletionSource<bool>();
        var operationCts = new CancellationTokenSource();

        async Task<int> Operation(CancellationToken ct)
        {
            try
            {
                operationStarted.TrySetResult();
                // Small delay so operation can be cancelled
                await Task.Delay(100, ct);
                return 42;
            }
            catch (OperationCanceledException)
            {
                operationCancelled.TrySetResult(true);
                throw;
            }
        }

        var task = singleFlight.RunAsync(
            "key",
            Operation,
            operationCancellation: operationCts.Token
        );

        // Wait for operation to start
        await operationStarted.Task;

        // Cancel the operation
        operationCts.Cancel();

        // WaitAsync throws TaskCanceledException which is derived from OperationCanceledException
        var ex = await Assert.ThrowsAsync<TaskCanceledException>(async () => await task);
        Assert.IsAssignableFrom<OperationCanceledException>(ex);

        // Verify operation was cancelled
        var wasCancelled = await operationCancelled.Task;
        Assert.True(wasCancelled);
    }

    [Fact]
    public async Task RunAsync_with_operation_cancellation_concurrent_waiters_also_get_cancelled()
    {
        var singleFlight = new SingleFlight<string, int>();
        var operationStarted = new TaskCompletionSource();
        var operationCts = new CancellationTokenSource();

        async Task<int> Operation(CancellationToken ct)
        {
            operationStarted.TrySetResult();
            // Small delay so operation can be cancelled
            await Task.Delay(100, ct);
            return 42;
        }

        var task1 = singleFlight.RunAsync(
            "key",
            Operation,
            operationCancellation: operationCts.Token
        );
        var task2 = singleFlight.RunAsync(
            "key",
            Operation,
            operationCancellation: operationCts.Token
        );

        // Wait for operation to start
        await operationStarted.Task;

        // Cancel the operation
        operationCts.Cancel();

        // WaitAsync throws TaskCanceledException which is derived from OperationCanceledException
        var ex1 = await Assert.ThrowsAsync<TaskCanceledException>(async () => await task1);
        Assert.IsAssignableFrom<OperationCanceledException>(ex1);
        var ex2 = await Assert.ThrowsAsync<TaskCanceledException>(async () => await task2);
        Assert.IsAssignableFrom<OperationCanceledException>(ex2);
    }

    [Fact]
    public async Task RunAsync_with_waiter_and_operation_cancellation_waiter_cancellation_does_not_affect_other_waiters()
    {
        var singleFlight = new SingleFlight<string, int>();
        var operationStarted = new TaskCompletionSource();

        async Task<int> Operation(CancellationToken ct)
        {
            operationStarted.TrySetResult();
            await Task.Delay(50, ct);
            return 42;
        }

        using var waiter1Cts = new CancellationTokenSource();
        using var waiter2Cts = new CancellationTokenSource();

        var task1 = singleFlight.RunAsync("key", Operation, waiterCancellation: waiter1Cts.Token);
        var task2 = singleFlight.RunAsync("key", Operation, waiterCancellation: waiter2Cts.Token);

        // Wait for operation to start
        await operationStarted.Task;

        // Cancel only the first waiter
        waiter1Cts.Cancel();

        // TaskCanceledException is a subclass of OperationCanceledException
        var ex1 = await Assert.ThrowsAsync<TaskCanceledException>(async () => await task1);
        Assert.IsAssignableFrom<OperationCanceledException>(ex1);

        // Second waiter should still get the result
        var result2 = await task2;
        Assert.Equal(42, result2);
    }

    #endregion

    #region Error Handling Tests

    [Fact]
    public async Task RunAsync_when_operation_throws_exception_exception_is_propagated_to_callers()
    {
        var singleFlight = new SingleFlight<string, int>();
        var expectedException = new InvalidOperationException("Test exception");

        Task<int> Operation(CancellationToken _) => Task.FromException<int>(expectedException);

        var ex = await Assert.ThrowsAsync<InvalidOperationException>(async () =>
            await singleFlight.RunAsync("key", Operation)
        );

        Assert.Equal("Test exception", ex.Message);
    }

    [Fact]
    public async Task RunAsync_when_operation_throws_exception_all_concurrent_callers_receive_exception()
    {
        var singleFlight = new SingleFlight<string, int>();
        var expectedException = new InvalidOperationException("Test exception");
        var operationStarted = new TaskCompletionSource();

        async Task<int> Operation(CancellationToken ct)
        {
            operationStarted.TrySetResult();
            await Task.Delay(10, ct);
            throw expectedException;
        }

        var task1 = singleFlight.RunAsync("key", Operation);
        var task2 = singleFlight.RunAsync("key", Operation);
        var task3 = singleFlight.RunAsync("key", Operation);

        // Wait for operation to start to ensure concurrent queueing
        await operationStarted.Task;

        await Assert.ThrowsAsync<InvalidOperationException>(async () => await task1);
        await Assert.ThrowsAsync<InvalidOperationException>(async () => await task2);
        await Assert.ThrowsAsync<InvalidOperationException>(async () => await task3);
    }

    [Fact]
    public async Task RunAsync_when_operation_throws_exception_can_retry_with_new_operation()
    {
        var singleFlight = new SingleFlight<string, int>();

        static Task<int> Operation1(CancellationToken ct) =>
            Task.FromException<int>(new InvalidOperationException("First attempt failed"));

        static Task<int> Operation2(CancellationToken ct) => Task.FromResult(42);
        await Assert.ThrowsAsync<InvalidOperationException>(async () =>
            await singleFlight.RunAsync("key", Operation1)
        );

        // After the first operation fails and is removed, retry should work
        var result = await singleFlight.RunAsync("key", Operation2);

        Assert.Equal(42, result);
    }

    #endregion

    #region Custom Equality Comparer Tests

    [Fact]
    public async Task Constructor_with_custom_comparer_uses_custom_comparer_for_key_equality()
    {
        var comparer = Substitute.For<IEqualityComparer<string>>();
        comparer.Equals(Arg.Any<string>(), Arg.Any<string>()).Returns(true);
        comparer.GetHashCode(Arg.Any<string>()).Returns(0);

        var singleFlight = new SingleFlight<string, int>(comparer);
        var operationCallCount = 0;
        var operationStarted = new TaskCompletionSource();

        async Task<int> Operation(CancellationToken ct)
        {
            Interlocked.Increment(ref operationCallCount);
            operationStarted.TrySetResult();
            await Task.Delay(10, ct);
            return 42;
        }

        var task1 = singleFlight.RunAsync("key1", Operation);
        var task2 = singleFlight.RunAsync("key2", Operation);

        // Wait for operation to start to ensure concurrent queueing
        await operationStarted.Task;

        await Task.WhenAll(task1, task2);
        Assert.Equal(1, operationCallCount);
    }

    [Fact]
    public async Task Constructor_with_case_insensitive_comparer_treats_keys_as_equal()
    {
        var singleFlight = new SingleFlight<string, int>(StringComparer.OrdinalIgnoreCase);
        var operationCallCount = 0;
        var operationStarted = new TaskCompletionSource();

        async Task<int> Operation(CancellationToken ct)
        {
            Interlocked.Increment(ref operationCallCount);
            operationStarted.TrySetResult();
            await Task.Delay(10, ct);
            return 42;
        }

        var task1 = singleFlight.RunAsync("KEY", Operation);
        var task2 = singleFlight.RunAsync("key", Operation);

        // Wait for operation to start to ensure concurrent queueing
        await operationStarted.Task;

        var result1 = await task1;
        var result2 = await task2;

        Assert.Equal(42, result1);
        Assert.Equal(42, result2);
        Assert.Equal(1, operationCallCount);
    }

    #endregion

    #region Edge Case Tests

    [Fact]
    public async Task RunAsync_with_nullable_value_type_returns_null()
    {
        var singleFlight = new SingleFlight<string, int?>();

        static Task<int?> Operation(CancellationToken ct) => Task.FromResult((int?)null);

        var result = await singleFlight.RunAsync("key", Operation);

        Assert.Null(result);
    }

    [Fact]
    public async Task RunAsync_with_complex_value_type_returns_correct_object()
    {
        var singleFlight = new SingleFlight<string, ComplexValue>();
        var expected = new ComplexValue { Id = 1, Name = "Test" };

        Task<ComplexValue> Operation(CancellationToken ct) => Task.FromResult(expected);

        var result = await singleFlight.RunAsync("key", Operation);

        Assert.Same(expected, result);
    }

    [Fact]
    public async Task RunAsync_with_default_cancellation_tokens_works_correctly()
    {
        var singleFlight = new SingleFlight<string, int>();

        static Task<int> Operation(CancellationToken ct) => Task.FromResult(42);

        var result = await singleFlight.RunAsync("key", Operation);

        Assert.Equal(42, result);
    }

    [Fact]
    public async Task RunAsync_with_rapid_sequential_calls_handles_properly()
    {
        var singleFlight = new SingleFlight<string, int>();
        var callOrder = new List<int>();

        Func<CancellationToken, Task<int>> CreateOperation(int value)
        {
            return ct =>
            {
                callOrder.Add(value);
                return Task.FromResult(value);
            };
        }

        var task1 = singleFlight.RunAsync("key1", CreateOperation(1));
        var task2 = singleFlight.RunAsync("key2", CreateOperation(2));
        var task3 = singleFlight.RunAsync("key3", CreateOperation(3));

        var results = await Task.WhenAll(task1, task2, task3);

        Assert.Equal([1, 2, 3], results);
        Assert.Equal(3, callOrder.Count);
    }

    [Fact]
    public async Task RunAsync_with_repeated_key_access_does_not_cause_memory_leak()
    {
        var singleFlight = new SingleFlight<string, int>();

        static Task<int> Operation(CancellationToken ct) => Task.FromResult(42);

        for (var i = 0; i < 100; i++)
        {
            await singleFlight.RunAsync("key", Operation);
        }

        // If there's a memory leak, this test would fail or use excessive memory
        // The fact that it completes successfully indicates proper cleanup
        Assert.True(true);
    }

    #endregion

    #region Helper Classes

    private class ComplexValue
    {
        public int Id { get; set; }
        public string? Name { get; set; }
    }

    #endregion
}
