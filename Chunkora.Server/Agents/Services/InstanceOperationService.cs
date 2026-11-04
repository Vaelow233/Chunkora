using System.Security.Claims;
using Chunkora.Contracts.Agent.V1;
using Chunkora.Server.Agents.Managers;
using Grpc.Core;
using Microsoft.AspNetCore.Authorization;

namespace Chunkora.Server.Agents.Services;

[Authorize(Policy = "AgentOnly")]
public class InstanceOperationService(InstanceOperationConnectionManager manager, IHostApplicationLifetime lifetime) : Contracts.Agent.V1.InstanceOperationService.InstanceOperationServiceBase
{
    private IServerStreamWriter<ServerInstanceOperationMessage>? _responseStream;
    private readonly SemaphoreSlim _responseStreamLock = new(1, 1);
    
    public override async Task ConnectInstanceOperations(
        IAsyncStreamReader<AgentInstanceOperationMessage> requestStream, 
        IServerStreamWriter<ServerInstanceOperationMessage> responseStream,
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
                    var response = new ServerInstanceOperationMessage
                    {
                        Pong = new InstanceOperationStreamPong
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
    
    public async Task WriteAsync(ServerInstanceOperationMessage message)
    {
        if (_responseStream is null)
        {
            throw new InvalidOperationException("Instance operation stream is not available.");
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