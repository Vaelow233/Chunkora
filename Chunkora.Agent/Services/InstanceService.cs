using Chunkora.Agent.Configuration;
using Chunkora.Agent.Instances;
using Chunkora.Contracts.Agent.V1;
using Grpc.Core;
using Microsoft.Extensions.Options;

namespace Chunkora.Agent.Services;

public class InstanceService(
    Chunkora.Contracts.Agent.V1.InstanceService.InstanceServiceClient instanceClient, 
    StatusService statusService, 
    IOptions<AgentOptions> agentOptions)
{
    private readonly LinkedList<IActionableInstance> _instances = new();
    private readonly SemaphoreSlim _instancesLock = new(1, 1);

    public async Task SyncInstancesAsync(CancellationToken cancellationToken = default)
    {
        var request = new SyncInstancesRequest
        {
            RequestId = Guid.NewGuid().ToString("N")
        };
        var metadata = new Metadata();
        metadata.Add("Authorization", "Bearer " + agentOptions.Value.Token);
        using var call = instanceClient.ConnectInstances(request, cancellationToken: cancellationToken, headers: metadata);
        await foreach (var response in call.ResponseStream.ReadAllAsync(cancellationToken))
        {
            List<IActionableInstance> removedInstances;
            await _instancesLock.WaitAsync(cancellationToken);
            try
            {
                LinkedList<IActionableInstance> instances = new();
                foreach (var instance in response.Instance)
                {
                    var handled = false;
                    foreach (var originInstance in _instances)
                    {
                        // For exists instance
                        if (originInstance.GetInstance().Id == instance.Id)
                        {
                            await originInstance.UpdateAsync(instance, cancellationToken);
                            instances.AddLast(originInstance);
                            _instances.Remove(originInstance);
                            handled = true;
                            break;
                        }
                    }

                    // For new instance
                    if (!handled)
                    {
                        instances.AddLast(ActionableInstanceFactory.Create(instance, GetStatusCallback()));
                    }
                }
                
                removedInstances = _instances.ToList();
                _instances.Clear();

                // Add handled instances
                foreach (var instance in instances)
                {
                    _instances.AddLast(instance);
                }
            }
            finally
            {
                _instancesLock.Release();
            }
            
            // For deleted instance
            foreach (var instance in removedInstances)
            {
                await instance.LifecycleLock.WaitAsync(cancellationToken);
                try
                {
                    await instance.DisposeAsync();
                }
                finally
                {
                    instance.LifecycleLock.Release();
                }
            }
        }
    }

    private Func<Instance, InstanceStatus, Task> GetStatusCallback() => (instance, status) => statusService.SyncInstanceStatusAsync(instance, status);

    public IActionableInstance? GetInstance(string instanceId)
    {
        _instancesLock.Wait();
        foreach (var instance in _instances)
        {
            if (instance.GetInstance().Id == instanceId)
            {
                _instancesLock.Release();
                return instance;
            }
        }
        _instancesLock.Release();
        return null;
    }
}