using Chunkora.Contracts.Agent.V1;

namespace Chunkora.Agent.Instances;

public static class ActionableInstanceFactory
{
    public static IActionableInstance Create(Instance instance, Func<Instance, InstanceStatus, Task> statusCallback)
    {
        return instance.Type switch
        {
            InstanceType.Host => new HostInstance(instance, statusCallback),
            _ => throw new NotSupportedException()
        };
    }
}