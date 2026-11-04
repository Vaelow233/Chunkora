using Chunkora.Agent.Models;
using Chunkora.Agent.Utils;
using Chunkora.Contracts.Agent.V1;
using Grpc.Core;
using InstanceService = Chunkora.Agent.Services.InstanceService;

namespace Chunkora.Agent.Handler;

public class RenameRequestHandler : IRequestHandler<InstanceIoRenameResponse>
{
    public ILogger Logger { get; } = Log.For<RenameRequestHandler>();
    public required InstanceService InstanceService { get; init; }
    public required SemaphoreSlim WriteLock { get; init; }
    public string InstanceId => Request.InstanceId;
    public string RequestId => Request.RequestId;
    public required InstanceIoRenameRequest Request { get; init; }
    public required IClientStreamWriter<AgentInstanceIoMessage> Writer { get; init; }
    
    public async Task WriteFailure(InstanceIoResult result, string error, CancellationToken cancellationToken = default)
    {
        var response = new InstanceIoRenameResponse
        {
            Code = result,
            Error = error
        };
        await WriteResponse(response, cancellationToken);
    }

    public async Task WriteResponse(InstanceIoRenameResponse response, CancellationToken cancellationToken = default)
    {
        response.RequestId = RequestId;
        response.InstanceId = InstanceId;
        var message = new AgentInstanceIoMessage
        {
            RenameResponse = response
        };
        await ((IRequestHandler<InstanceIoRenameResponse>) this).WriteAsync(message, cancellationToken);
    }

    public async Task Handle(ActionableInstance instance)
    {
        try
        {
            var instanceInfo = instance.GetInstance();
            var from = IoUtils.Resolve(IoUtils.Resolve(instanceInfo.Path, Request.BasePath), Request.Name);
            var to = IoUtils.Resolve(IoUtils.Resolve(instanceInfo.Path, Request.BasePath), Request.NewName);
            IoUtils.OccupiedObjects.TryGetValue(from, out var occupiedRequestId);
            if (occupiedRequestId != null)
            {
                var message = new InstanceIoRenameResponse
                {
                    Code = InstanceIoResult.InstanceIoObjectIsOccupied,
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
            var finishedMessage = new InstanceIoRenameResponse
            {
                Code = InstanceIoResult.InstanceIoSuccess
            };
            await WriteResponse(finishedMessage);
        }
        catch (Exception exception)
        {
            var errorMessage = new InstanceIoRenameResponse
            {
                Code = InstanceIoResult.InstanceIoUnknownError,
                Error = exception.Message
            };
            await WriteResponse(errorMessage);
        }
    }
}