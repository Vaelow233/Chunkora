using Chunkora.Agent.Models;
using Chunkora.Agent.Utils;
using Chunkora.Contracts.Agent.V1;
using Grpc.Core;
using InstanceService = Chunkora.Agent.Services.InstanceService;

namespace Chunkora.Agent.Handler;

public class CopyRequestHandler : IRequestHandler<InstanceIoCopyResponse>
{
    public ILogger Logger { get; } = Log.For<CopyRequestHandler>();
    public required InstanceService InstanceService { get; init; }
    public required SemaphoreSlim WriteLock { get; init; }
    public string InstanceId => Request.InstanceId;
    public string RequestId => Request.RequestId;
    public required InstanceIoCopyRequest Request { get; init; }
    public required IClientStreamWriter<AgentInstanceIoMessage> Writer { get; init; }
    
    public async Task WriteFailure(InstanceIoResult result, string error, CancellationToken cancellationToken = default)
    {
        var response = new InstanceIoCopyResponse
        {
            Code = result,
            Error = error
        };
        await WriteResponse(response, cancellationToken);
    }

    public async Task WriteResponse(InstanceIoCopyResponse response, CancellationToken cancellationToken = default)
    {
        response.RequestId = RequestId;
        response.InstanceId = InstanceId;
        var message = new AgentInstanceIoMessage
        {
            CopyResponse = response
        };
        await ((IRequestHandler<InstanceIoCopyResponse>) this).WriteAsync(message, cancellationToken);
    }

    public async Task Handle(ActionableInstance instance)
    {
        try
        {
            var instanceInfo = instance.GetInstance();
            var from = IoUtils.Resolve(IoUtils.Resolve(instanceInfo.Path, Request.BasePath), Request.Name);
            var to = IoUtils.Resolve(IoUtils.Resolve(instanceInfo.Path, Request.Destination), Request.Name);
            IoUtils.OccupiedObjects.TryGetValue(from, out var occupiedRequestId);
            if (occupiedRequestId != null)
            {
                var message = new InstanceIoCopyResponse
                {
                    Code = InstanceIoResult.InstanceIoObjectIsOccupied,
                    Error = "The object is occupied currently."
                };
                await WriteResponse(message);
                return;
            }

            if (Request.Type == InstanceIoObjectType.File)
            {
                File.Copy(from, to);
            } 
            else if (Request.Type == InstanceIoObjectType.Directory)
            {
                CopyDirectory(from, to);
            }
            else
            {
                throw new ArgumentException("Invalid object type");
            }
            var finishedMessage = new InstanceIoCopyResponse
            {
                Code = InstanceIoResult.InstanceIoSuccess
            };
            await WriteResponse(finishedMessage);
        }
        catch (Exception exception)
        {
            var errorMessage = new InstanceIoCopyResponse
            {
                Code = InstanceIoResult.InstanceIoUnknownError,
                Error = exception.Message
            };
            await WriteResponse(errorMessage);
        }
    }
    
    private static void CopyDirectory(string source, string destination, bool overwrite = true)
    {
        if (!Directory.Exists(source)) throw new DirectoryNotFoundException(source);
        Directory.CreateDirectory(destination);
        // Copy files
        foreach (var file in Directory.GetFiles(source))
        {
            var target = Path.Combine(destination, Path.GetFileName(file));
            File.Copy(file, target, overwrite);
        }
        // Copy subdirectories
        foreach (var directory in Directory.GetDirectories(source))
        {
            var target = Path.Combine(destination, Path.GetFileName(directory));
            CopyDirectory(directory, target, overwrite);
        }
    }
}