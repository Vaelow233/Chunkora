using Chunkora.Agent.Models;
using Chunkora.Agent.Utils;
using Chunkora.Contracts.Agent.V1;
using Grpc.Core;
using InstanceService = Chunkora.Agent.Services.InstanceService;

namespace Chunkora.Agent.Handler;

public record StreamWriter(Instance Instance, string Path, FileStream Stream);

public class UploadRequestHandler : IRequestHandler<InstanceIoUploadResponse>
{
    public ILogger Logger { get; } = Log.For<UploadRequestHandler>();
    public required InstanceService InstanceService { get; init; }
    public required SemaphoreSlim WriteLock { get; init; }
    public string InstanceId => Request.InstanceId;
    public string RequestId => Request.RequestId;
    public required InstanceIoUploadRequest Request { get; init; }
    public required IClientStreamWriter<AgentInstanceIoMessage> Writer { get; init; }
    
    public async Task WriteFailure(InstanceIoResult result, string error, CancellationToken cancellationToken = default)
    {
        var response = new InstanceIoUploadResponse
        {
            Code = result,
            Error = error
        };
        await WriteResponse(response, cancellationToken);
    }

    public async Task WriteResponse(InstanceIoUploadResponse response, CancellationToken cancellationToken = default)
    {
        response.RequestId = RequestId;
        response.InstanceId = InstanceId;
        var message = new AgentInstanceIoMessage
        {
            UploadResponse = response
        };
        await ((IRequestHandler<InstanceIoUploadResponse>) this).WriteAsync(message, cancellationToken);
    }

    public async Task Handle(ActionableInstance instance)
    {
        try
        {
            var finalPath = IoUtils.Resolve(instance.GetInstance().Path, Request.Path);
            IoUtils.OccupiedObjects.TryGetValue(finalPath, out var occupiedRequestId);
            if (occupiedRequestId != null && RequestId != occupiedRequestId)
            {
                var message = new InstanceIoUploadResponse
                {
                    Code = InstanceIoResult.InstanceIoObjectIsOccupied,
                    Error = "The object is occupied currently."
                };
                await WriteResponse(message);
                return;
            }
            IoUtils.OperatingWriters.TryGetValue(RequestId, out var writer);
            if (writer == null)
            {
                if (Request.Chunk != 0)
                {
                    var message = new InstanceIoUploadResponse
                    {
                        Code = InstanceIoResult.InstanceIoWriterNotFound,
                        Error = "Cannot find out the writer (maybe the writer has been destroyed)."
                    };
                    await WriteResponse(message);
                    return;
                }

                writer = new StreamWriter(instance.GetInstance(), Request.Path,
                    new FileStream(finalPath, FileMode.Create, FileAccess.Write,
                        FileShare.Write, bufferSize: 64 * 1024,
                        options: FileOptions.Asynchronous | FileOptions.SequentialScan)
                );
                IoUtils.OperatingWriters.TryAdd(RequestId, writer);
            }
            if (occupiedRequestId == null)
            {
                IoUtils.OccupiedObjects.TryAdd(finalPath, RequestId);
            }
            await writer.Stream.WriteAsync(Request.Data.ToByteArray());
            if (Request.Chunk + 1 >= Request.TotalChunks)
            {
                await writer.Stream.FlushAsync();
                writer.Stream.Close();
                IoUtils.OperatingWriters.TryRemove(RequestId, out _);
                IoUtils.OccupiedObjects.TryRemove(finalPath, out _);
            }
            var finishedMessage = new InstanceIoUploadResponse
            {
                Code = InstanceIoResult.InstanceIoSuccess,
                TotalChunks = Request.TotalChunks,
                Chunk = Request.Chunk
            };
            await WriteResponse(finishedMessage);
        }
        catch (UnauthorizedAccessException)
        {
            var errorMessage = new InstanceIoUploadResponse
            {
                Code = InstanceIoResult.InstanceIoUnauthorizedPath,
                Error = "Unauthorized access path"
            };
            await WriteResponse(errorMessage);
        }
        catch (Exception exception)
        {
            var errorMessage = new InstanceIoUploadResponse
            {
                Code = InstanceIoResult.InstanceIoUnknownError,
                Error = exception.Message
            };
            await WriteResponse(errorMessage);
        }
    }
}