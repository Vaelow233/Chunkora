using Chunkora.Contracts.Agent.V1;

namespace Chunkora.Agent.Models;

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
    Task UpdateConfigAsync(Instance instance, CancellationToken cancellationToken = default);
    public async Task UpdateAsync(Instance instance, CancellationToken cancellationToken = default)
    {
        if (instance.Type != GetInstance().Type)
        {
            /*
             * TODO:
             * Because the instance type has changed, the instance needs to be recreated. In this process,
             * the instance needed to be closed. So currently we don't have an idea about how to handle this.
             * It'll do the same thing as 
             */
            await UpdateConfigAsync(instance, cancellationToken);
        }
        else
        {
            await UpdateConfigAsync(instance, cancellationToken);
        }
    }
    SemaphoreSlim LifecycleLock { get; }
}