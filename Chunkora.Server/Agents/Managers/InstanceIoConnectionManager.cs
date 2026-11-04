using System.Buffers;
using System.Collections.Concurrent;
using System.Threading.Channels;
using Chunkora.Contracts.Agent.V1;
using Chunkora.Server.Agents.Models;
using Google.Protobuf;
using Instance = Chunkora.Server.Models.Instance;
using InstanceIoService = Chunkora.Server.Agents.Services.InstanceIoService;

namespace Chunkora.Server.Agents.Managers;

public class InstanceIoConnectionManager
{
    private readonly ConcurrentDictionary<Guid, InstanceIoService> _connections = new(); // nodeId => instanceOperationService
    private readonly ConcurrentDictionary<string, IPendingRequest> _pendingRequests = new(); // requestId => pendingRequest
    private readonly ConcurrentDictionary<string, KeyValuePair<InstanceIoService, Channel<byte[]>>> _pendingChannels = new(); // requestId => pendingSplittingRequests
    
    // Agent service side methods
    public void AddConnection(Guid nodeId, InstanceIoService instanceService)
    {
        _connections[nodeId] = instanceService;
    }
    
    public void RemoveConnection(Guid nodeId, InstanceIoService service)
    {
        foreach (var keyValuePair in _pendingChannels)
        {
            if (keyValuePair.Value.Key == service)
            {
                keyValuePair.Value.Value.Writer.TryComplete(
                    new IOException("The connection with agent has been closed"));
                _pendingChannels.Remove(keyValuePair.Key, out _);
            }
        }
        _connections.TryRemove(new KeyValuePair<Guid, InstanceIoService>(nodeId, service));
    }
    
    public async Task HandleResponseAsync(AgentInstanceIoMessage message)
    {
        if (message.DirectoryResponse != null)
        {
            _pendingRequests.TryGetValue(message.DirectoryResponse.RequestId, out var pendingRequest);
            if (pendingRequest == null)
            {
                return;
            }
            pendingRequest.TrySetResult(message.DirectoryResponse);
        }

        if (message.FileResponse != null)
        {
            _pendingRequests.TryGetValue(message.FileResponse.RequestId, out var pendingRequest);
            if (pendingRequest == null)
            {
                // Check whether it's splitting file request
                _pendingChannels.TryGetValue(message.FileResponse.RequestId, out var channel);
                if (channel.Value == null)
                {
                    return;
                }
                Exception? error = null;
                if (message.FileResponse.Code != InstanceIoResult.InstanceIoSuccess)
                {
                    error = new IOException(message.FileResponse.Error);
                }
                else if (!channel.Value.Writer.TryWrite(message.FileResponse.Data.ToByteArray()))
                {
                    // TODO: add speed control
                    error = new IOException("The download buffer is full");
                }
                if (error is not null || message.FileResponse.Chunk + 1 >= message.FileResponse.TotalChunks)
                {
                    _pendingChannels.TryRemove(message.FileResponse.RequestId, out _);
                    channel.Value.Writer.TryComplete(error);
                }
                return;
            }
            pendingRequest.TrySetResult(message.FileResponse);
        }

        if (message.LogResponse != null)
        {
            _pendingRequests.TryGetValue(message.LogResponse.RequestId, out var pendingRequest);
            if (pendingRequest == null)
            {
                return;
            }
            pendingRequest.TrySetResult(message.LogResponse);
        }

        if (message.NewObjectResponse != null)
        {
            _pendingRequests.TryGetValue(message.NewObjectResponse.RequestId, out var pendingRequest);
            if (pendingRequest == null)
            {
                return;
            }
            pendingRequest.TrySetResult(message.NewObjectResponse);
        }

        if (message.UploadResponse != null)
        {
            _pendingRequests.TryGetValue(message.UploadResponse.RequestId, out var pendingRequest);
            if (pendingRequest == null)
            {
                return;
            }
            // When an error occurred or upload is finished, set the result
            if (message.UploadResponse.HasError || message.UploadResponse.Chunk + 1 >= message.UploadResponse.TotalChunks)
            {
                pendingRequest.TrySetResult(message.UploadResponse);
            }
        }
        
        if (message.DeleteResponse != null)
        {
            _pendingRequests.TryGetValue(message.DeleteResponse.RequestId, out var pendingRequest);
            if (pendingRequest == null)
            {
                return;
            }
            pendingRequest.TrySetResult(message.DeleteResponse);
        }

        if (message.CopyResponse != null)
        {
            _pendingRequests.TryGetValue(message.CopyResponse.RequestId, out var pendingRequest);
            if (pendingRequest == null)
            {
                return;
            }
            pendingRequest.TrySetResult(message.CopyResponse);
        }
        
        if (message.MoveResponse != null)
        {
            _pendingRequests.TryGetValue(message.MoveResponse.RequestId, out var pendingRequest);
            if (pendingRequest == null)
            {
                return;
            }
            pendingRequest.TrySetResult(message.MoveResponse);
        }
        
        if (message.RenameResponse != null)
        {
            _pendingRequests.TryGetValue(message.RenameResponse.RequestId, out var pendingRequest);
            if (pendingRequest == null)
            {
                return;
            }
            pendingRequest.TrySetResult(message.RenameResponse);
        }
    }
    
    // Server service side methods
    public async Task<InstanceIoDirectoryResponse> ListDirectoryAsync(Instance instance, string path, 
        int limit, int offset, CancellationToken cancellationToken = default)
    {
        var requestId = Guid.NewGuid().ToString("N");
        var pending = new PendingRequest<InstanceIoDirectoryResponse>();
        _pendingRequests[requestId] = pending;
        using var timeoutCts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
        timeoutCts.CancelAfter(TimeSpan.FromSeconds(30));
        try
        {
            var message = new ServerInstanceIoMessage
            {
                DirectoryRequest = new InstanceIoDirectoryRequest
                {
                    RequestId = requestId,
                    InstanceId = instance.Id.ToString("N"),
                    Limit = limit,
                    Offset = offset,
                    Path = path
                }
            };
            _connections.TryGetValue(instance.NodeId, out var instanceService);
            if (instanceService != null)
            {
                await instanceService.WriteAsync(message);
            }
            else
            {
                throw new InvalidOperationException("Instance agent service not found");
            }
            return await pending.Task.WaitAsync(timeoutCts.Token);
        }
        finally
        {
            _pendingRequests.TryRemove(requestId, out _);
            pending.TrySetCanceled();
        }
    }

    public async Task<InstanceIoFileResponse> ReadFileAsync(Instance instance, string path, CancellationToken cancellationToken = default)
    {
        var requestId = Guid.NewGuid().ToString("N");
        var pending = new PendingRequest<InstanceIoFileResponse>();
        _pendingRequests[requestId] = pending;
        using var timeoutCts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
        timeoutCts.CancelAfter(TimeSpan.FromSeconds(30));
        try
        {
            var message = new ServerInstanceIoMessage
            {
                FileRequest = new InstanceIoFileRequest
                {
                    RequestId = requestId,
                    InstanceId = instance.Id.ToString("N"),
                    Splitting = false,
                    Path = path
                }
            };
            _connections.TryGetValue(instance.NodeId, out var instanceService);
            if (instanceService != null)
            {
                await instanceService.WriteAsync(message);
            }
            else
            {
                throw new InvalidOperationException("Instance agent service not found");
            }
            return await pending.Task.WaitAsync(timeoutCts.Token);
        }
        finally
        {
            _pendingRequests.TryRemove(requestId, out _);
            pending.TrySetCanceled();
        }
    }

    public async IAsyncEnumerable<byte[]> ReadSplittingFileAsync(Instance instance, string path,
        [System.Runtime.CompilerServices.EnumeratorCancellation] CancellationToken cancellationToken = default)
    {
        var requestId = Guid.NewGuid().ToString("N");
        var channel = Channel.CreateBounded<byte[]>(32);
        try
        {
            cancellationToken.ThrowIfCancellationRequested();
            var message = new ServerInstanceIoMessage
            {
                FileRequest = new InstanceIoFileRequest
                {
                    RequestId = requestId,
                    InstanceId = instance.Id.ToString("N"),
                    Splitting = true,
                    Path = path
                }
            };
            _connections.TryGetValue(instance.NodeId, out var instanceService);
            if (instanceService != null)
            {
                _pendingChannels[requestId] = new KeyValuePair<InstanceIoService, Channel<byte[]>>(instanceService, channel);
                await instanceService.WriteAsync(message);
                await foreach (var chunk in channel.Reader.ReadAllAsync(cancellationToken))
                {
                    yield return chunk;
                }
            }
            else
            {
                throw new InvalidOperationException("Instance agent service not found");
            }
        }
        finally
        {
            _pendingChannels.TryRemove(requestId, out _);
            channel.Writer.TryComplete();
        }
    }
    
    public async Task<InstanceIoLogResponse> GetLogsAsync(Instance instance, int limit, int offset, 
        CancellationToken cancellationToken = default)
    {
        var requestId = Guid.NewGuid().ToString("N");
        var pending = new PendingRequest<InstanceIoLogResponse>();
        _pendingRequests[requestId] = pending;
        using var timeoutCts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
        timeoutCts.CancelAfter(TimeSpan.FromSeconds(30));
        try
        {
            var message = new ServerInstanceIoMessage
            {
                LogRequest = new InstanceIoLogRequest
                {
                    RequestId = requestId,
                    InstanceId = instance.Id.ToString("N"),
                    Limit = limit,
                    Offset = offset
                }
            };
            _connections.TryGetValue(instance.NodeId, out var instanceService);
            if (instanceService != null)
            {
                await instanceService.WriteAsync(message);
            }
            else
            {
                throw new InvalidOperationException("Instance agent service not found");
            }
            return await pending.Task.WaitAsync(timeoutCts.Token);
        }
        finally
        {
            _pendingRequests.TryRemove(requestId, out _);
            pending.TrySetCanceled();
        }
    }
    
    public async Task<InstanceIoNewObjectResponse> CreateNewObjectAsync(Instance instance, string basePath, string name, 
        InstanceIoObjectType type, CancellationToken cancellationToken = default)
    {
        var requestId = Guid.NewGuid().ToString("N");
        var pending = new PendingRequest<InstanceIoNewObjectResponse>();
        _pendingRequests[requestId] = pending;
        using var timeoutCts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
        timeoutCts.CancelAfter(TimeSpan.FromSeconds(30));
        try
        {
            var message = new ServerInstanceIoMessage
            {
                NewObjectRequest = new InstanceIoNewObjectRequest
                {
                    RequestId = requestId,
                    InstanceId = instance.Id.ToString("N"),
                    Type = type,
                    BasePath = basePath,
                    Name = name
                }
            };
            _connections.TryGetValue(instance.NodeId, out var instanceService);
            if (instanceService != null)
            {
                await instanceService.WriteAsync(message);
            }
            else
            {
                throw new InvalidOperationException("Instance agent service not found");
            }
            return await pending.Task.WaitAsync(timeoutCts.Token);
        }
        finally
        {
            _pendingRequests.TryRemove(requestId, out _);
            pending.TrySetCanceled();
        }
    }

    public async Task<InstanceIoUploadResponse> UploadFileAsync(Instance instance, string path, Stream stream,
        long contentLength, CancellationToken cancellationToken = default)
    {
        var requestId = Guid.NewGuid().ToString("N");
        var pending = new PendingRequest<InstanceIoUploadResponse>();
        _pendingRequests[requestId] = pending;
        using var timeoutCts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
        timeoutCts.CancelAfter(TimeSpan.FromHours(3));
        const int chunkSize = 1024 * 1024;
        byte[] buffer = ArrayPool<byte>.Shared.Rent(chunkSize);
        var chunkIndex = 0;
        var totalChunk = (int) (contentLength / chunkSize + (contentLength % chunkSize == 0 ? 0 : 1));
        try
        {
            _connections.TryGetValue(instance.NodeId, out var instanceService);
            if (instanceService != null)
            {
                while (true)
                {
                    int bytesRead = await stream.ReadAsync(buffer.AsMemory(0, chunkSize), cancellationToken);
                    if (bytesRead == 0) break;
                    var message = new ServerInstanceIoMessage
                    {
                        UploadRequest = new InstanceIoUploadRequest
                        {
                            RequestId = requestId,
                            InstanceId = instance.Id.ToString("N"),
                            TotalChunks = totalChunk,
                            Path = path,
                            Chunk = chunkIndex,
                            Data = ByteString.CopyFrom(buffer, 0, bytesRead)
                        }
                    };
                    await instanceService.WriteAsync(message);
                    chunkIndex++;
                }
            }
            else
            {
                throw new InvalidOperationException("Instance agent service not found");
            }
            return await pending.Task.WaitAsync(timeoutCts.Token);
        }
        finally
        {
            _pendingRequests.TryRemove(requestId, out _);
            pending.TrySetCanceled();
        }
    }
    
    public async Task<InstanceIoDeleteResponse> DeleteObjectAsync(Instance instance, string basePath, string name, 
        InstanceIoObjectType type, CancellationToken cancellationToken = default)
    {
        var requestId = Guid.NewGuid().ToString("N");
        var pending = new PendingRequest<InstanceIoDeleteResponse>();
        _pendingRequests[requestId] = pending;
        using var timeoutCts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
        timeoutCts.CancelAfter(TimeSpan.FromSeconds(30));
        try
        {
            var message = new ServerInstanceIoMessage
            {
                DeleteRequest = new InstanceIoDeleteRequest
                {
                    RequestId = requestId,
                    InstanceId = instance.Id.ToString("N"),
                    Type = type,
                    BasePath = basePath,
                    Name = name
                }
            };
            _connections.TryGetValue(instance.NodeId, out var instanceService);
            if (instanceService != null)
            {
                await instanceService.WriteAsync(message);
            }
            else
            {
                throw new InvalidOperationException("Instance agent service not found");
            }
            return await pending.Task.WaitAsync(timeoutCts.Token);
        }
        finally
        {
            _pendingRequests.TryRemove(requestId, out _);
            pending.TrySetCanceled();
        }
    }
    
    public async Task<InstanceIoCopyResponse> CopyObjectAsync(Instance instance, string basePath, string name, string destination, 
        InstanceIoObjectType type, CancellationToken cancellationToken = default)
    {
        var requestId = Guid.NewGuid().ToString("N");
        var pending = new PendingRequest<InstanceIoCopyResponse>();
        _pendingRequests[requestId] = pending;
        using var timeoutCts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
        timeoutCts.CancelAfter(TimeSpan.FromSeconds(30));
        try
        {
            var message = new ServerInstanceIoMessage
            {
                CopyRequest = new InstanceIoCopyRequest
                {
                    RequestId = requestId,
                    InstanceId = instance.Id.ToString("N"),
                    Type = type,
                    BasePath = basePath,
                    Name = name,
                    Destination = destination
                }
            };
            _connections.TryGetValue(instance.NodeId, out var instanceService);
            if (instanceService != null)
            {
                await instanceService.WriteAsync(message);
            }
            else
            {
                throw new InvalidOperationException("Instance agent service not found");
            }
            return await pending.Task.WaitAsync(timeoutCts.Token);
        }
        finally
        {
            _pendingRequests.TryRemove(requestId, out _);
            pending.TrySetCanceled();
        }
    }
    
    public async Task<InstanceIoMoveResponse> MoveObjectAsync(Instance instance, string basePath, string name, string destination, 
        InstanceIoObjectType type, CancellationToken cancellationToken = default)
    {
        var requestId = Guid.NewGuid().ToString("N");
        var pending = new PendingRequest<InstanceIoMoveResponse>();
        _pendingRequests[requestId] = pending;
        using var timeoutCts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
        timeoutCts.CancelAfter(TimeSpan.FromSeconds(30));
        try
        {
            var message = new ServerInstanceIoMessage
            {
                MoveRequest = new InstanceIoMoveRequest
                {
                    RequestId = requestId,
                    InstanceId = instance.Id.ToString("N"),
                    Type = type,
                    BasePath = basePath,
                    Name = name,
                    Destination = destination
                }
            };
            _connections.TryGetValue(instance.NodeId, out var instanceService);
            if (instanceService != null)
            {
                await instanceService.WriteAsync(message);
            }
            else
            {
                throw new InvalidOperationException("Instance agent service not found");
            }
            return await pending.Task.WaitAsync(timeoutCts.Token);
        }
        finally
        {
            _pendingRequests.TryRemove(requestId, out _);
            pending.TrySetCanceled();
        }
    }
    
    public async Task<InstanceIoRenameResponse> RenameObjectAsync(Instance instance, string basePath, string name, string newName, 
        InstanceIoObjectType type, CancellationToken cancellationToken = default)
    {
        var requestId = Guid.NewGuid().ToString("N");
        var pending = new PendingRequest<InstanceIoRenameResponse>();
        _pendingRequests[requestId] = pending;
        using var timeoutCts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
        timeoutCts.CancelAfter(TimeSpan.FromSeconds(30));
        try
        {
            var message = new ServerInstanceIoMessage
            {
                RenameRequest = new InstanceIoRenameRequest
                {
                    RequestId = requestId,
                    InstanceId = instance.Id.ToString("N"),
                    Type = type,
                    BasePath = basePath,
                    Name = name,
                    NewName = newName
                }
            };
            _connections.TryGetValue(instance.NodeId, out var instanceService);
            if (instanceService != null)
            {
                await instanceService.WriteAsync(message);
            }
            else
            {
                throw new InvalidOperationException("Instance agent service not found");
            }
            return await pending.Task.WaitAsync(timeoutCts.Token);
        }
        finally
        {
            _pendingRequests.TryRemove(requestId, out _);
            pending.TrySetCanceled();
        }
    }
}