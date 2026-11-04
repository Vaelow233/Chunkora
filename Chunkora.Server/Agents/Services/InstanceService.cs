using System.Security.Claims;
using Chunkora.Contracts.Agent.V1;
using Chunkora.Server.Agents.Managers;
using Google.Protobuf.WellKnownTypes;
using Grpc.Core;
using Microsoft.AspNetCore.Authorization;

namespace Chunkora.Server.Agents.Services;

[Authorize(Policy = "AgentOnly")]
public class InstanceService(InstanceConnectionManager manager, IHostApplicationLifetime lifetime) : Contracts.Agent.V1.InstanceService.InstanceServiceBase
{
    private IServerStreamWriter<SyncInstancesResponse> _responseStream;
    private readonly SemaphoreSlim _responseStreamLock = new(1, 1);
    private string _requestId;
    
    public override async Task ConnectInstances(SyncInstancesRequest request, IServerStreamWriter<SyncInstancesResponse> responseStream, ServerCallContext context)
    {
        var principal = context.GetHttpContext().User;
        var nodeId = new Guid(principal.FindFirstValue(ClaimTypes.NameIdentifier)!);
        using var shutdown = CancellationTokenSource.CreateLinkedTokenSource(context.CancellationToken, lifetime.ApplicationStopping);
        try
        {
            var response = new SyncInstancesResponse
            {
                RequestId = request.RequestId,
                GeneratedTime = new Timestamp(),
                Instance = { await manager.GetInstancesAsync(nodeId) }
            };
            await responseStream.WriteAsync(response);
            _requestId = request.RequestId;
            _responseStream = responseStream;
            manager.AddConnection(nodeId, this);
            await Task.Delay(Timeout.Infinite, shutdown.Token);
        }
        catch (OperationCanceledException) when (shutdown.IsCancellationRequested)
        {

        }
        finally
        {
            manager.RemoveConnection(nodeId, this);
        }
    }

    public async Task SyncInstances(List<Instance> instances)
    {
        var response = new SyncInstancesResponse
        {
            RequestId = _requestId,
            GeneratedTime = new Timestamp(),
            Instance = { instances }
        };
        await _responseStreamLock.WaitAsync();
        try
        {
            await _responseStream.WriteAsync(response);
        }
        finally
        {
            _responseStreamLock.Release();
        }
    }
}