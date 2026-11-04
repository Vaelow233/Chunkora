using System.Security.Claims;
using Chunkora.Contracts.Agent.V1;
using Chunkora.Server.Agents.Managers;
using Grpc.Core;
using Microsoft.AspNetCore.Authorization;

namespace Chunkora.Server.Agents.Services;

[Authorize(Policy = "AgentOnly")]
public class InstanceIoService(InstanceIoConnectionManager manager, IHostApplicationLifetime lifetime) : Contracts.Agent.V1.InstanceIoService.InstanceIoServiceBase
{
    private IServerStreamWriter<ServerInstanceIoMessage>? _responseStream;
    private readonly SemaphoreSlim _responseStreamLock = new(1, 1);

    public override async Task ConnectInstanceIo(
        IAsyncStreamReader<AgentInstanceIoMessage> requestStream, 
        IServerStreamWriter<ServerInstanceIoMessage> responseStream,
        ServerCallContext context)
    {
        var principal = context.GetHttpContext().User;
        var nodeId = new Guid(principal.FindFirstValue(ClaimTypes.NameIdentifier)!);
        using var shutdown = CancellationTokenSource.CreateLinkedTokenSource(context.CancellationToken, lifetime.ApplicationStopping);
        var token = shutdown.Token;
        manager.AddConnection(nodeId, this);
        try
        {
            await foreach (var message in requestStream.ReadAllAsync(token))
            {
                if (message.Ping != null)
                {
                    var response = new ServerInstanceIoMessage
                    {
                        Pong = new InstanceIoStreamPong
                        {
                            RequestId = message.Ping.RequestId,
                        }
                    };
                    await responseStream.WriteAsync(response, token);
                    _responseStream = responseStream;
                    continue;
                }

                await manager.HandleResponseAsync(message);
            }
        }
        catch (OperationCanceledException) when (token.IsCancellationRequested)
        {
            
        }
        finally
        {
            manager.RemoveConnection(nodeId, this);
        }
    }
    
    public async Task WriteAsync(ServerInstanceIoMessage message)
    {
        if (_responseStream is null)
        {
            throw new InvalidOperationException("Instance io stream is not available.");
        }

        await _responseStreamLock.WaitAsync();
        try
        {
            await _responseStream.WriteAsync(message);
        }
        finally
        {
            _responseStreamLock.Release();
        }
    }
}