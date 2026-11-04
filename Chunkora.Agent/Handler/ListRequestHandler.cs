using Chunkora.Agent.Models;
using Chunkora.Agent.Utils;
using Chunkora.Contracts.Agent.V1;
using Google.Protobuf.WellKnownTypes;
using Grpc.Core;
using InstanceService = Chunkora.Agent.Services.InstanceService;

namespace Chunkora.Agent.Handler;

public class ListRequestHandler : IRequestHandler<InstanceIoDirectoryResponse>
{
    public ILogger Logger { get; } = Log.For<ListRequestHandler>();
    public required InstanceService InstanceService { get; init; }
    public required SemaphoreSlim WriteLock { get; init; }
    public string InstanceId => Request.InstanceId;
    public string RequestId => Request.RequestId;
    public required InstanceIoDirectoryRequest Request { get; init; }
    public required IClientStreamWriter<AgentInstanceIoMessage> Writer { get; init; }
    
    public async Task WriteFailure(InstanceIoResult result, string error, CancellationToken cancellationToken = default)
    {
        var response = new InstanceIoDirectoryResponse
        {
            Code = result,
            Error = error,
        };
        await WriteResponse(response, cancellationToken);
    }

    public async Task WriteResponse(InstanceIoDirectoryResponse response, CancellationToken cancellationToken = default)
    {
        response.RequestId = RequestId;
        response.InstanceId = InstanceId;
        var message = new AgentInstanceIoMessage
        {
            DirectoryResponse = response
        };
        await ((IRequestHandler<InstanceIoDirectoryResponse>) this).WriteAsync(message, cancellationToken);
    }

    public async Task Handle(ActionableInstance instance)
    {
        try
        {
            var finalPath = IoUtils.Resolve(instance.GetInstance().Path, Request.Path);
            DirectoryInfo info = new DirectoryInfo(finalPath);
            if (!info.Exists)
            {
                var errorMessage = new InstanceIoDirectoryResponse
                {
                    Code = InstanceIoResult.InstanceIoDirectoryNotFound,
                    Error = "Directory not found"
                };
                await WriteResponse(errorMessage);
                return;
            }

            var list = new List<InstanceIoDirectoryObject>();
            foreach (var dirInfo in info.GetDirectories())
            {
                list.Add(new InstanceIoDirectoryObject
                {
                    Type = InstanceIoObjectType.Directory,
                    Name = dirInfo.Name,
                    SizeBytes = 0,
                    CreatedAt = Timestamp.FromDateTime(dirInfo.CreationTimeUtc),
                    LastUpdatedAt = Timestamp.FromDateTime(dirInfo.LastWriteTimeUtc),
                    Permission = IoUtils.FormatObjectPermission(dirInfo.UnixFileMode, 'd')
                });
            }
            
            foreach (var fileInfo in info.GetFiles())
            {
                
                list.Add(new InstanceIoDirectoryObject
                {
                    Type = InstanceIoObjectType.File,
                    Name = fileInfo.Name,
                    SizeBytes = fileInfo.Length,
                    CreatedAt = Timestamp.FromDateTime(fileInfo.CreationTimeUtc),
                    LastUpdatedAt = Timestamp.FromDateTime(fileInfo.LastWriteTimeUtc),
                    Permission = IoUtils.FormatObjectPermission(fileInfo.UnixFileMode)
                });
            }

            var newList = await list.ToAsyncEnumerable().Skip(Request.Offset).Take(Request.Limit).ToListAsync();
            var message = new InstanceIoDirectoryResponse
            {
                Code = InstanceIoResult.InstanceIoSuccess,
                Total = list.Count,
                Count = newList.Count,
                Objects = { newList }
            };
            await WriteResponse(message);
        }
        catch (UnauthorizedAccessException)
        {
            var errorMessage = new InstanceIoDirectoryResponse
            {
                Code = InstanceIoResult.InstanceIoUnauthorizedPath,
                Error = "Unauthorized access path"
            };
            await WriteResponse(errorMessage);
        }
        catch (Exception exception)
        {
            var errorMessage = new InstanceIoDirectoryResponse
            {
                Code = InstanceIoResult.InstanceIoUnknownError,
                Error = exception.Message
            };
            await WriteResponse(errorMessage);
        }
    }
}