using Chunkora.Agent.Configuration;
using Chunkora.Agent.Handler;
using Chunkora.Contracts.Agent.V1;
using Grpc.Core;
using Microsoft.Extensions.Options;

namespace Chunkora.Agent.Services;

public class InstanceIoService(
    ILogger<InstanceIoService> logger,
    Chunkora.Contracts.Agent.V1.InstanceIoService.InstanceIoServiceClient instanceIoClient,
    InstanceService instanceService,
    IOptions<AgentOptions> agentOptions)
{
    private readonly SemaphoreSlim _writeLock = new(1, 1);

    public async Task SyncInstanceIoAsync(CancellationToken cancellationToken = default)
    {
        var initRequestId = Guid.NewGuid().ToString("N");
        var initialized = false;
        var ping = new AgentInstanceIoMessage
        {
            Ping = new InstanceIoStreamPing
            {
                RequestId = initRequestId
            }
        };
        var metadata = new Metadata();
        metadata.Add("Authorization", "Bearer " + agentOptions.Value.Token);
        using var call = instanceIoClient.ConnectInstanceIo(cancellationToken: cancellationToken, headers: metadata);
        await call.RequestStream.WriteAsync(ping, cancellationToken);
        await foreach (var serverMessage in call.ResponseStream.ReadAllAsync(cancellationToken))
        {
            if (serverMessage.Pong != null && serverMessage.Pong.RequestId == initRequestId)
            {
                logger.LogInformation("Server has pong the instance io connection. It has been initialized.");
                initialized = true;
                continue;
            }

            // File request
            if (serverMessage.FileRequest != null)
            {
                var handler = new ReadRequestHandler
                {
                    InstanceService = instanceService,
                    Writer = call.RequestStream,
                    WriteLock = _writeLock,
                    Request = serverMessage.FileRequest
                };
                _ = ((IRequestHandler<InstanceIoFileResponse>) handler).Handle(initialized);
            }

            // Directory request
            if (serverMessage.DirectoryRequest != null)
            {
                var handler = new ListRequestHandler
                {
                    InstanceService = instanceService,
                    Writer = call.RequestStream,
                    WriteLock = _writeLock,
                    Request = serverMessage.DirectoryRequest
                };
                _ = ((IRequestHandler<InstanceIoDirectoryResponse>) handler).Handle(initialized);
            }

            // Log request
            if (serverMessage.LogRequest != null)
            {
                var handler = new LogRequestHandler
                {
                    InstanceService = instanceService,
                    Writer = call.RequestStream,
                    WriteLock = _writeLock,
                    Request = serverMessage.LogRequest
                };
                _ = ((IRequestHandler<InstanceIoLogResponse>) handler).Handle(initialized);
            }
            
            // New object request
            if (serverMessage.NewObjectRequest != null)
            {
                var handler = new NewObjectRequestHandler
                {
                    InstanceService = instanceService,
                    Writer = call.RequestStream,
                    WriteLock = _writeLock,
                    Request = serverMessage.NewObjectRequest
                };
                _ = ((IRequestHandler<InstanceIoNewObjectResponse>) handler).Handle(initialized);
            }
            
            // Upload file request
            if (serverMessage.UploadRequest != null)
            {
                var handler = new UploadRequestHandler
                {
                    InstanceService = instanceService,
                    Writer = call.RequestStream,
                    WriteLock = _writeLock,
                    Request = serverMessage.UploadRequest
                };
                _ = ((IRequestHandler<InstanceIoUploadResponse>) handler).Handle(initialized);
            }
            
            // Delete request
            if (serverMessage.DeleteRequest != null)
            {
                var handler = new DeleteRequestHandler
                {
                    InstanceService = instanceService,
                    Writer = call.RequestStream,
                    WriteLock = _writeLock,
                    Request = serverMessage.DeleteRequest
                };
                _ = ((IRequestHandler<InstanceIoDeleteResponse>) handler).Handle(initialized);
            }
            
            // Copy request
            if (serverMessage.CopyRequest != null)
            {
                var handler = new CopyRequestHandler
                {
                    InstanceService = instanceService,
                    Writer = call.RequestStream,
                    WriteLock = _writeLock,
                    Request = serverMessage.CopyRequest
                };
                _ = ((IRequestHandler<InstanceIoCopyResponse>) handler).Handle(initialized);
            }
            
            // Move request
            if (serverMessage.MoveRequest != null)
            {
                var handler = new MoveRequestHandler
                {
                    InstanceService = instanceService,
                    Writer = call.RequestStream,
                    WriteLock = _writeLock,
                    Request = serverMessage.MoveRequest
                };
                _ = ((IRequestHandler<InstanceIoMoveResponse>) handler).Handle(initialized);
            }
            
            // Rename request
            if (serverMessage.RenameRequest != null)
            {
                var handler = new RenameRequestHandler
                {
                    InstanceService = instanceService,
                    Writer = call.RequestStream,
                    WriteLock = _writeLock,
                    Request = serverMessage.RenameRequest
                };
                _ = ((IRequestHandler<InstanceIoRenameResponse>) handler).Handle(initialized);
            }
        }
    }
}