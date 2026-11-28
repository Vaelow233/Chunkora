using Chunkora.Agent.Models;
using Chunkora.Agent.Utils;
using Chunkora.Contracts.Agent.V1;
using Grpc.Core;
using InstanceService = Chunkora.Agent.Services.InstanceService;
using StreamWriter = Chunkora.Agent.Utils.StreamWriter;

namespace Chunkora.Agent.Handler;

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

    public async Task Handle(IActionableInstance instance)
    {
        try
        {
            var finalPath = IoUtils.Resolve(instance.GetInstance().Path, Request.Path);
            IoUtils.OperatingWriters.TryGetValue(RequestId, out var writer);
            string tempPath;
            if (writer == null)
            {
                if (Request.Chunk != 0)
                {
                    var message = new InstanceIoUploadResponse
                    {
                        Code = InstanceIoResult.WriterNotFound,
                        Error = "Cannot find out the writer (maybe the writer has been destroyed)."
                    };
                    await WriteResponse(message);
                    return;
                }
                
                tempPath = Path.GetTempFileName();
                writer = new StreamWriter(instance.GetInstance(), finalPath, tempPath,
                    new FileStream(tempPath, FileMode.Create, FileAccess.Write,
                        FileShare.Write, bufferSize: 64 * 1024,
                        options: FileOptions.Asynchronous | FileOptions.SequentialScan)
                );
                IoUtils.OperatingWriters.TryAdd(RequestId, writer);
            }
            else
            {
                tempPath = writer.TempPath;
            }
            IoUtils.OccupiedObjects.TryAdd(tempPath, RequestId);
            await writer.Stream.WriteAsync(Request.Data.ToByteArray());
            if (Request.Chunk + 1 >= Request.TotalChunks)
            {
                // Close the temp file stream
                await writer.Stream.FlushAsync();
                writer.Stream.Close();
                IoUtils.OperatingWriters.TryRemove(RequestId, out _);
                IoUtils.OccupiedObjects.TryRemove(tempPath, out _);
                // Copy it to the final path
                var occupiedRequestId = IoUtils.OccupiedObjects.GetOrAdd(finalPath, RequestId);
                if (occupiedRequestId != RequestId)
                {
                    var message = new InstanceIoUploadResponse
                    {
                        Code = InstanceIoResult.ObjectIsOccupied,
                        Error = "The object is occupied currently. The file has been written as a temp file."
                    };
                    await WriteResponse(message);
                    return;
                }
                try
                {
                    File.Move(tempPath, finalPath, overwrite: true);
                }
                catch
                {
                    var message = new InstanceIoUploadResponse
                    {
                        Code = InstanceIoResult.UnknownError,
                        Error = "Failed to copy the uploaded file to the destination path."
                    };
                    await WriteResponse(message);
                    return;
                }
                finally
                {
                    try
                    {
                        // Delete the temp file
                        File.Delete(tempPath);
                    }
                    catch
                    {
                        var message = new InstanceIoUploadResponse
                        {
                            Code = InstanceIoResult.UnknownError,
                            Error = "The file has been uploaded, but the temp file can't be deleted."
                        };
                        await WriteResponse(message);
                    }
                    finally
                    {
                        IoUtils.OccupiedObjects.TryRemove(finalPath, out _);
                    }
                }
            }
            var finishedMessage = new InstanceIoUploadResponse
            {
                Code = InstanceIoResult.Success,
                TotalChunks = Request.TotalChunks,
                Chunk = Request.Chunk
            };
            await WriteResponse(finishedMessage);
        }
        catch (UnauthorizedAccessException)
        {
            var errorMessage = new InstanceIoUploadResponse
            {
                Code = InstanceIoResult.UnauthorizedPath,
                Error = "Unauthorized access path"
            };
            await WriteResponse(errorMessage);
        }
        catch (Exception exception)
        {
            var errorMessage = new InstanceIoUploadResponse
            {
                Code = InstanceIoResult.UnknownError,
                Error = exception.Message
            };
            await WriteResponse(errorMessage);
        }
    }
}