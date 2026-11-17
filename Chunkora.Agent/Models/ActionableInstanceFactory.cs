using Chunkora.Agent.Instances;
using Chunkora.Contracts.Agent.V1;
using StatusService = Chunkora.Agent.Services.StatusService;

namespace Chunkora.Agent.Models;

public sealed class ActionableInstanceFactory(StatusService statusService, IServiceScopeFactory scopeFactory)
{
    private Func<Instance, InstanceStatus, Task> GetStatusCallback() => (instance, status) => statusService.SyncInstanceStatusAsync(instance, status);
    
    public async Task<IActionableInstance> Create(Instance instance)
    {
        var statusCallback = GetStatusCallback();
        return instance.Type switch
        {
            // For host, the container (instance object) will NOT be created since it isn't persisted
            // So we should always instantiate a new one
            InstanceType.Host => new HostInstance(instance, statusCallback),
            // For docker, the container may already be created (persistent)
            // So for created container, we just need to get it directly
            InstanceType.Docker => await DockerInstance.GetOrCreateContainerAsync(instance, statusCallback, scopeFactory),
            _ => throw new NotSupportedException($"Unsupported instance type: {instance.Type}")
        };
    }
}