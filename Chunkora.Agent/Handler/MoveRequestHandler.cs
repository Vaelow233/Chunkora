using Chunkora.Agent.Models;
using Chunkora.Agent.Utils;
using Chunkora.Contracts.Agent.V1;
using Grpc.Core;
using InstanceService = Chunkora.Agent.Services.InstanceService;

namespace Chunkora.Agent.Handler;

public class MoveRequestHandler : IRequestHandler<InstanceIoMoveResponse>
{
    public ILogger Logger { get; } = Log.For<MoveRequestHandler>();
    public required InstanceService InstanceService { get; init; }
    public required SemaphoreSlim WriteLock { get; init; }
    public string InstanceId => Request.InstanceId;
    public string RequestId => Request.RequestId;
    public required InstanceIoMoveRequest Request { get; init; }
    public required IClientStreamWriter<AgentInstanceIoMessage> Writer { get; init; }
    
    public async Task WriteFailure(InstanceIoResult result, string error, CancellationToken cancellationToken = default)
    {
        var response = new InstanceIoMoveResponse
        {
            Code = result,
            Error = error
        };
        await WriteResponse(response, cancellationToken);
    }

    public async Task WriteResponse(InstanceIoMoveResponse response, CancellationToken cancellationToken = default)
    {
        response.RequestId = RequestId;
        response.InstanceId = InstanceId;
        var message = new AgentInstanceIoMessage
        {
            MoveResponse = response
        };
        await ((IRequestHandler<InstanceIoMoveResponse>) this).WriteAsync(message, cancellationToken);
    }

    public async Task Handle(IActionableInstance instance)
    {
        try
        {
            var instanceInfo = instance.GetInstance();
            var from = IoUtils.Resolve(IoUtils.Resolve(instanceInfo.Path, Request.BasePath), Request.Name);
            var to = IoUtils.Resolve(IoUtils.Resolve(instanceInfo.Path, Request.Destination), Request.Name);
            IoUtils.OccupiedObjects.TryGetValue(from, out var occupiedRequestId);
            if (occupiedRequestId != null)
            {
                var message = new InstanceIoMoveResponse
                {
                    Code = InstanceIoResult.ObjectIsOccupied,
                    Error = "The object is occupied currently."
                };
                await WriteResponse(message);
                return;
            }

            if (Request.Type == InstanceIoObjectType.File)
            {
                File.Move(from, to);
            }
            else if (Request.Type == InstanceIoObjectType.Directory)
            {
                Directory.Move(from, to);
            }
            else
            {
                throw new ArgumentException("Invalid object type");
            }
            var finishedMessage = new InstanceIoMoveResponse
            {
                Code = InstanceIoResult.Success
            };
            await WriteResponse(finishedMessage);
        }
        catch (Exception exception)
        {
            var errorMessage = new InstanceIoMoveResponse
            {
                Code = InstanceIoResult.UnknownError,
                Error = exception.Message
            };
            await WriteResponse(errorMessage);
        }
    }
}