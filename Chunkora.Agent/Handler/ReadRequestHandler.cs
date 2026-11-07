using Chunkora.Agent.Instances;
using Chunkora.Agent.Utils;
using Chunkora.Contracts.Agent.V1;
using Google.Protobuf;
using Grpc.Core;
using InstanceService = Chunkora.Agent.Services.InstanceService;

namespace Chunkora.Agent.Handler;

public class ReadRequestHandler : IRequestHandler<InstanceIoFileResponse>
{
    public ILogger Logger { get; } = Log.For<ReadRequestHandler>();
    public required InstanceService InstanceService { get; init; }
    public required SemaphoreSlim WriteLock { get; init; }
    public string InstanceId => Request.InstanceId;
    public string RequestId => Request.RequestId;
    public required InstanceIoFileRequest Request { get; init; }
    public required IClientStreamWriter<AgentInstanceIoMessage> Writer { get; init; }
    
    public async Task WriteFailure(InstanceIoResult result, string error, CancellationToken cancellationToken = default)
    {
        var response = new InstanceIoFileResponse
        {
            Code = result,
            Error = error
        };
        await WriteResponse(response, cancellationToken);
    }

    public async Task WriteResponse(InstanceIoFileResponse response, CancellationToken cancellationToken = default)
    {
        response.RequestId = RequestId;
        response.InstanceId = InstanceId;
        var message = new AgentInstanceIoMessage
        {
            FileResponse = response
        };
        await ((IRequestHandler<InstanceIoFileResponse>) this).WriteAsync(message, cancellationToken);
    }

    public async Task Handle(IActionableInstance instance)
    {
        try
        {
            var finalPath = IoUtils.Resolve(instance.GetInstance().Path, Request.Path);
            if (!Request.Splitting)
            {
                var data = await File.ReadAllBytesAsync(finalPath);
                var message = new InstanceIoFileResponse
                {
                    Code = InstanceIoResult.Success,
                    TotalChunks = 1,
                    Chunk = 0,
                    Data = ByteString.CopyFrom(data)
                };
                await WriteResponse(message);
            }
            else
            {
                const int chunkSize = 1024 * 1024;
                await using var stream = new FileStream(finalPath, FileMode.Open, FileAccess.Read, FileShare.Read, 
                    bufferSize: 64 * 1024, options: FileOptions.Asynchronous | FileOptions.SequentialScan);
                var fileSize = stream.Length;
                if (fileSize == 0)
                {
                    var message = new InstanceIoFileResponse
                    {
                        Code = InstanceIoResult.Success,
                        TotalChunks = 1,
                        Chunk = 0,
                        Data = ByteString.Empty
                    };
                    await WriteResponse(message);
                    return;
                }
                var chunkCount = fileSize / chunkSize + (fileSize % chunkSize == 0 ? 0 : 1);
            
                var buffer = new byte[chunkSize];
                var chunkIndex = 0;

                while (true)
                {
                    var bytesRead = await stream.ReadAsync(buffer.AsMemory());
                    if (bytesRead == 0) break;
                    var message = new InstanceIoFileResponse
                    {
                        Code = InstanceIoResult.Success,
                        TotalChunks = (int) chunkCount,
                        Chunk = chunkIndex,
                        Data = ByteString.CopyFrom(buffer, 0, bytesRead)
                    };
                    await WriteResponse(message);
                    chunkIndex++;
                }
            }
        }
        catch (UnauthorizedAccessException)
        {
            var errorMessage = new InstanceIoFileResponse
            {
                Code = InstanceIoResult.UnauthorizedPath,
                Error = "Unauthorized access path"
            };
            await WriteResponse(errorMessage);
        }
        catch (Exception exception)
        {
            var errorMessage = new InstanceIoFileResponse
            {
                Code = InstanceIoResult.UnknownError,
                Error = exception.Message
            };
            await WriteResponse(errorMessage);
        }
    }
}