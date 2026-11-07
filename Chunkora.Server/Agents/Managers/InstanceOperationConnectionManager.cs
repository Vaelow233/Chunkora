using System.Collections.Concurrent;
using Chunkora.Contracts.Agent.V1;
using Instance = Chunkora.Server.Models.Instance;
using InstanceOperationService = Chunkora.Server.Agents.Services.InstanceOperationService;

namespace Chunkora.Server.Agents.Managers;

public sealed class InstanceOperationConnectionManager(ILogger<InstanceOperationConnectionManager> logger)
{
    private readonly ConcurrentDictionary<Guid, InstanceOperationService> _connections = new(); // nodeId => instanceOperationService
    private readonly ConcurrentDictionary<Guid, InstanceStatus> _instanceStatusMap = new(); // instanceId => instanceStatus

    // Agent service side methods
    public void AddConnection(Guid nodeId, InstanceOperationService instanceService)
    {
        _connections[nodeId] = instanceService;
    }
    
    public void RemoveConnection(Guid nodeId, InstanceOperationService service)
    {
        _connections.TryRemove(new KeyValuePair<Guid, InstanceOperationService>(nodeId, service));
    }

    public async Task HandleResponseAsync(AgentInstanceOperationMessage message)
    {
        if (message.Response == null || message.Response.Code != InstanceOperationResult.Success)
        {
            logger.LogWarning($"Agent-side instance operation service returned {message.Response}");
        }
    }

    public void UpdateInstanceStatus(Guid instanceId, InstanceStatus status)
    {
        _instanceStatusMap[instanceId] = status;
    }

    // Server service side methods
    public async Task StartInstanceAsync(Instance instance)
    {
        await SendRequestAsync(instance, InstanceOperationType.Start);
    }
    
    public async Task StopInstanceAsync(Instance instance)
    {
        await SendRequestAsync(instance, InstanceOperationType.Stop);
    }

    public async Task RestartInstanceAsync(Instance instance)
    {
        await SendRequestAsync(instance, InstanceOperationType.Restart);
    }

    public async Task KillInstanceAsync(Instance instance)
    {
        await SendRequestAsync(instance, InstanceOperationType.Kill);
    }

    public async Task ExecuteCommandAsync(Instance instance, string command)
    {
        await SendRequestAsync(instance, InstanceOperationType.Execute, command);
    }
    
    public async Task<bool> IsNodeConnectedAsync(Guid nodeId)
    {
        _connections.TryGetValue(nodeId, out var service);
        return service != null;
    }
    
    // Common methods
    private async Task SendRequestAsync(Instance instance, InstanceOperationType type, string? command = null)
    {
        _connections.TryGetValue(instance.NodeId, out var instanceService);
        var request = new InstanceOperationRequest
        {
            InstanceId = instance.Id.ToString("N"),
            RequestId = Guid.NewGuid().ToString("N"),
            Type = type
        };
        if (command != null)
        {
            request.Command = command;
        }
        var message = new ServerInstanceOperationMessage
        {
            Request = request
        };
        if (instanceService != null)
        {
            await instanceService.WriteAsync(message);
        }
        else
        {
            throw new InvalidOperationException("Instance agent service not found");
        }
    }

    public InstanceStatus GetInstanceStatus(Instance instance)
    {
        return _instanceStatusMap.GetValueOrDefault(instance.Id, InstanceStatus.Stopped);
    }
}