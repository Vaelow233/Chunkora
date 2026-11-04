using Chunkora.Agent.Models;
using Chunkora.Contracts.Agent.V1;
using Grpc.Core;
using InstanceService = Chunkora.Agent.Services.InstanceService;

namespace Chunkora.Agent.Handler;

public interface IRequestHandler<TResponse>
{
    ILogger Logger { get; }
    
    InstanceService InstanceService { get; }
    
    SemaphoreSlim WriteLock { get; }
    
    IClientStreamWriter<AgentInstanceIoMessage> Writer { get; }
    
    string RequestId { get; }
    
    string InstanceId { get; }
    
    async Task WriteAsync(AgentInstanceIoMessage response, CancellationToken cancellationToken = default)
    {
        await WriteLock.WaitAsync(cancellationToken);
        try
        {
            await Writer.WriteAsync(response, cancellationToken);
        }
        finally
        {
            WriteLock.Release();
        }
    }

    Task WriteResponse(TResponse response, CancellationToken cancellationToken = default);

    Task WriteFailure(InstanceIoResult result, string error, CancellationToken cancellationToken = default);
    
    Task Handle(ActionableInstance instance);
    
    public async Task Handle(bool initialized)
    {
        if (!initialized)
        {
            await WriteFailure(InstanceIoResult.Uninitialized, "Uninitialized");
            return;
        }
        var target = InstanceService.GetInstance(InstanceId);
        if (target == null)
        {
            Logger.LogWarning($"Unknown instance to move object: {InstanceId}");
            await WriteFailure(InstanceIoResult.InstanceNotFound, "Instance not found");
            return;
        }
        await Handle(target);
    }
}