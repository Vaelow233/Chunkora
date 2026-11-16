using Chunkora.Agent.Models;
using Chunkora.Agent.Utils;
using Chunkora.Contracts.Agent.V1;
using Grpc.Core;
using InstanceService = Chunkora.Agent.Services.InstanceService;

namespace Chunkora.Agent.Handler;

public class NewObjectRequestHandler : IRequestHandler<InstanceIoNewObjectResponse>
{
    public ILogger Logger { get; } = Log.For<NewObjectRequestHandler>();
    public required InstanceService InstanceService { get; init; }
    public required SemaphoreSlim WriteLock { get; init; }
    public string InstanceId => Request.InstanceId;
    public string RequestId => Request.RequestId;
    public required InstanceIoNewObjectRequest Request { get; init; }
    public required IClientStreamWriter<AgentInstanceIoMessage> Writer { get; init; }
    
    public async Task WriteFailure(InstanceIoResult result, string error, CancellationToken cancellationToken = default)
    {
        var response = new InstanceIoNewObjectResponse
        {
            Code = result,
            Error = error
        };
        await WriteResponse(response, cancellationToken);
    }

    public async Task WriteResponse(InstanceIoNewObjectResponse response, CancellationToken cancellationToken = default)
    {
        response.RequestId = RequestId;
        response.InstanceId = InstanceId;
        var message = new AgentInstanceIoMessage
        {
            NewObjectResponse = response
        };
        await ((IRequestHandler<InstanceIoNewObjectResponse>) this).WriteAsync(message, cancellationToken);
    }

    public async Task Handle(IActionableInstance instance)
    {
        try
        {
            var finalPath = IoUtils.Resolve(IoUtils.Resolve(instance.GetInstance().Path, Request.BasePath), Request.Name);
            IoUtils.OccupiedObjects.TryGetValue(finalPath, out var occupiedRequestId);
            if (occupiedRequestId != null)
            {
                var message = new InstanceIoNewObjectResponse
                {
                    Code = InstanceIoResult.ObjectIsOccupied,
                    Error = "The object is occupied currently."
                };
                await WriteResponse(message);
                return;
            }
            if (Request.Type == InstanceIoObjectType.File)
            {
                FileInfo fileInfo = new(finalPath);
                if (!fileInfo.Exists)
                {
                    await using var stream = fileInfo.Create();
                }
                var message = new InstanceIoNewObjectResponse
                {
                    Code = InstanceIoResult.Success
                };
                await WriteResponse(message);
            } else if (Request.Type == InstanceIoObjectType.Directory)
            {
                DirectoryInfo dirInfo = new(finalPath);
                if (!dirInfo.Exists)
                {
                    dirInfo.Create();
                }
                var message = new InstanceIoNewObjectResponse
                {
                    Code = InstanceIoResult.Success
                };
                await WriteResponse(message);
            }
            else
            {
                throw new ArgumentException("Invalid object type");
            }
        }
        catch (Exception exception)
        {
            var errorMessage = new InstanceIoNewObjectResponse
            {
                Code = InstanceIoResult.UnknownError,
                Error = exception.Message
            };
            await WriteResponse(errorMessage);
        }
    }
}