using System.Security.Claims;
using Chunkora.Contracts.Agent.V1;
using Chunkora.Server.Agents.Managers;
using Google.Protobuf.WellKnownTypes;
using Grpc.Core;
using Microsoft.AspNetCore.Authorization;

namespace Chunkora.Server.Agents.Services;

[Authorize(Policy = "AgentOnly")]
public class InstanceTaskService(InstanceTaskConnectionManager manager, IHostApplicationLifetime lifetime) : Contracts.Agent.V1.InstanceTaskService.InstanceTaskServiceBase
{
    private IServerStreamWriter<SyncInstanceTasksResponse> _responseStream;
    private readonly SemaphoreSlim _responseStreamLock = new(1, 1);
    private string _requestId;
    
    public override async Task ConnectInstanceTasks(SyncInstanceTasksRequest request, IServerStreamWriter<SyncInstanceTasksResponse> responseStream, ServerCallContext context)
    {
        var principal = context.GetHttpContext().User;
        var nodeId = new Guid(principal.FindFirstValue(ClaimTypes.NameIdentifier)!);
        using var shutdown = CancellationTokenSource.CreateLinkedTokenSource(context.CancellationToken, lifetime.ApplicationStopping);
        try
        {
            var response = new SyncInstanceTasksResponse
            {
                RequestId = request.RequestId,
                GeneratedTime = new Timestamp(),
                Task = { await manager.GetInstanceTasksAsync(nodeId) }
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

    public async Task SyncInstanceTasks(List<InstanceTask> tasks)
    {
        var response = new SyncInstanceTasksResponse
        {
            RequestId = _requestId,
            GeneratedTime = new Timestamp(),
            Task = { tasks }
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