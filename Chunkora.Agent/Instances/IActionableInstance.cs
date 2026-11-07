using Chunkora.Contracts.Agent.V1;

namespace Chunkora.Agent.Models;

public interface IActionableInstance
{
    Task StartAsync(CancellationToken cancellationToken = default);
    Task StopAsync(CancellationToken cancellationToken = default);
    Task RestartAsync(CancellationToken cancellationToken = default);
    Task DisposeAsync();
    Task WriteAsync(string content, CancellationToken cancellationToken = default);
    List<string> GetLogs(int limit, int offset, out int total);
    Instance GetInstance();
    Task UpdateAsync(Instance instance, CancellationToken cancellationToken = default);
    SemaphoreSlim LifecycleLock { get; }
}