using Chunkora.Agent.Instances;
using Chunkora.Agent.Utils;
using Chunkora.Contracts.Agent.V1;
using Grpc.Core;
using InstanceService = Chunkora.Agent.Services.InstanceService;

namespace Chunkora.Agent.Handler;

public class LogRequestHandler : IRequestHandler<InstanceIoLogResponse>
{
    public ILogger Logger { get; } = Log.For<LogRequestHandler>();
    public required InstanceService InstanceService { get; init; }
    public required SemaphoreSlim WriteLock { get; init; }
    public string InstanceId => Request.InstanceId;
    public string RequestId => Request.RequestId;
    public required InstanceIoLogRequest Request { get; init; }
    public required IClientStreamWriter<AgentInstanceIoMessage> Writer { get; init; }
    
    public async Task WriteFailure(InstanceIoResult result, string error, CancellationToken cancellationToken = default)
    {
        var response = new InstanceIoLogResponse
        {
            Code = result,
            Error = error
        };
        await WriteResponse(response, cancellationToken);
    }

    public async Task WriteResponse(InstanceIoLogResponse response, CancellationToken cancellationToken = default)
    {
        response.RequestId = RequestId;
        response.InstanceId = InstanceId;
        var message = new AgentInstanceIoMessage
        {
            LogResponse = response
        };
        await ((IRequestHandler<InstanceIoLogResponse>) this).WriteAsync(message, cancellationToken);
    }

    public async Task Handle(IActionableInstance instance)
    {
        var result = instance.GetLogs(Request.Limit, Request.Offset, out var total);
        var message = new InstanceIoLogResponse
        {
            Code = InstanceIoResult.Success,
            Count = result.Count,
            Lines = { result },
            Total = total
        };
        await WriteResponse(message);
    }
}