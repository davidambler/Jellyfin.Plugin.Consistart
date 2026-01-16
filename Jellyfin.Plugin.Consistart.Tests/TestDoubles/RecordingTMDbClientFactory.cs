using Jellyfin.Plugin.Consistart.Services.TMDb.Client;

namespace Jellyfin.Plugin.Consistart.Tests.TestDoubles;

/// <summary>
/// Recording implementation of ITMDbClientFactory for testing purposes.
/// Tracks method calls and can return pre-configured client adapters.
/// </summary>
internal sealed class RecordingTMDbClientFactory : ITMDbClientFactory
{
    private readonly Queue<ITMDbClientAdapter> _clientsToReturn = new();
    private ITMDbClientAdapter? _defaultClient;

    public List<DateTime> CreateClientCalls { get; } = [];
    public int CreateClientCallCount => CreateClientCalls.Count;

    public bool ThrowOnCreateClient { get; set; }
    public Exception? ExceptionToThrow { get; set; }

    public ITMDbClientAdapter CreateClient()
    {
        CreateClientCalls.Add(DateTime.UtcNow);

        if (ThrowOnCreateClient)
        {
            throw ExceptionToThrow ?? new InvalidOperationException("Fake factory error");
        }

        if (_clientsToReturn.Count > 0)
        {
            return _clientsToReturn.Dequeue();
        }

        if (_defaultClient is not null)
        {
            return _defaultClient;
        }

        // Return a new fake client if none configured
        return new FakeTMDbClientAdapter();
    }

    /// <summary>
    /// Sets the default client adapter to return from all CreateClient calls.
    /// </summary>
    public void SetDefaultClient(ITMDbClientAdapter client)
    {
        _defaultClient = client;
    }

    /// <summary>
    /// Enqueues a client adapter to be returned from the next CreateClient call.
    /// Multiple calls can be enqueued for sequential returns.
    /// </summary>
    public void EnqueueClient(ITMDbClientAdapter client)
    {
        _clientsToReturn.Enqueue(client);
    }

    /// <summary>
    /// Resets the factory to its initial state, clearing all configuration and call history.
    /// </summary>
    public void Reset()
    {
        CreateClientCalls.Clear();
        _clientsToReturn.Clear();
        _defaultClient = null;
        ThrowOnCreateClient = false;
        ExceptionToThrow = null;
    }
}
