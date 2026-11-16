using System.Collections.Concurrent;
using Chunkora.Server.Agents.Services;
using Chunkora.Server.Models;
using Microsoft.EntityFrameworkCore;

namespace Chunkora.Server.Agents.Managers;

public sealed class InstanceTaskConnectionManager(IDbContextFactory<GlobalDatabaseContext> dbFactory)
{
    private readonly ConcurrentDictionary<Guid, InstanceTaskService> _connections = new(); // nodeId => taskService

    // Agent service side methods
    public void AddConnection(Guid nodeId, InstanceTaskService service)
    {
        _connections[nodeId] = service;
    }

    public void RemoveConnection(Guid nodeId, InstanceTaskService service)
    {
        _connections.TryRemove(new KeyValuePair<Guid, InstanceTaskService>(nodeId, service));
    }

    public async Task<List<Chunkora.Contracts.Agent.V1.InstanceTask>> GetInstanceTasksAsync(Guid nodeId)
    {
        await using var dbContext = await dbFactory.CreateDbContextAsync();
        var tasks = await dbContext.InstanceTasks
            .Where(task => task.NodeId == nodeId)
            .ToListAsync();
        return ConvertToAgentTasks(tasks);
    }

    // Server service side methods
    public async Task SyncTasksAsync(List<InstanceTask> tasks)
    {
        await Task.WhenAll(_connections.Select(pair => pair.Value.SyncInstanceTasks(
            tasks.Where(task => task.NodeId == pair.Key)
                .ToList()
                .ConvertAll(filtered => ConvertToAgentTask(filtered))
            )
        ));
    }

    public bool IsNodeConnected(Guid nodeId)
    {
        return _connections.ContainsKey(nodeId);
    }

    // Common methods
    private List<Chunkora.Contracts.Agent.V1.InstanceTask> ConvertToAgentTasks(List<InstanceTask> tasks)
    {
        return tasks.ConvertAll(task => ConvertToAgentTask(task));
    }
    
    private Chunkora.Contracts.Agent.V1.InstanceTask ConvertToAgentTask(InstanceTask task)
    {
        return new Chunkora.Contracts.Agent.V1.InstanceTask
        {
            Id = task.Id.ToString("N"),
            InstanceId = task.InstanceId.ToString("N"),
            Name = task.Name,
            Description = task.Description,
            Action = ConvertToAgentAction(task.Action),
            ActionInfo = task.ActionInfo,
            Trigger = ConvertToAgentTrigger(task.Trigger),
            TriggerInfo = task.TriggerInfo
        };
    }

    private Chunkora.Contracts.Agent.V1.InstanceTaskTrigger ConvertToAgentTrigger(InstanceTaskTrigger trigger)
    {
        return trigger switch
        {
            InstanceTaskTrigger.Once => Chunkora.Contracts.Agent.V1.InstanceTaskTrigger.Once,
            InstanceTaskTrigger.Interval => Chunkora.Contracts.Agent.V1.InstanceTaskTrigger.Interval,
            InstanceTaskTrigger.Scheduled => Chunkora.Contracts.Agent.V1.InstanceTaskTrigger.Scheduled,
            InstanceTaskTrigger.AgentLaunch => Chunkora.Contracts.Agent.V1.InstanceTaskTrigger.AgentLaunch,
            _ => Chunkora.Contracts.Agent.V1.InstanceTaskTrigger.UnspecifiedTrigger
        };
    }

    private Chunkora.Contracts.Agent.V1.InstanceTaskAction ConvertToAgentAction(InstanceTaskAction action)
    {
        return action switch
        {
            InstanceTaskAction.Stop => Chunkora.Contracts.Agent.V1.InstanceTaskAction.Stop,
            InstanceTaskAction.Start => Chunkora.Contracts.Agent.V1.InstanceTaskAction.Start,
            InstanceTaskAction.Kill => Chunkora.Contracts.Agent.V1.InstanceTaskAction.Kill,
            InstanceTaskAction.Restart => Chunkora.Contracts.Agent.V1.InstanceTaskAction.Restart,
            InstanceTaskAction.SendCommand => Chunkora.Contracts.Agent.V1.InstanceTaskAction.SendCommand,
            _ => Chunkora.Contracts.Agent.V1.InstanceTaskAction.UnspecifiedAction
        };
    }
}