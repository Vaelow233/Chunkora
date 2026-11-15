using Chunkora.Contracts.Agent.V1;

namespace Chunkora.Agent.Instances;

public interface IActionableInstance
{
    Task StartAsync(CancellationToken cancellationToken = default);
    Task StopAsync(CancellationToken cancellationToken = default);
    Task RestartAsync(CancellationToken cancellationToken = default);
    Task DisposeAsync();
    Task DeleteAsync(); // For docker instance, this method will also delete the container that is persistent in docker
    Task WriteAsync(string content, CancellationToken cancellationToken = default);
    Task<KeyValuePair<int, List<string>>> GetLogsAsync(int limit, int offset, CancellationToken cancellationToken = default); // pair: total, lines
    Instance GetInstance();
    Task UpdateAsync(Instance instance, CancellationToken cancellationToken = default);
    SemaphoreSlim LifecycleLock { get; }
}