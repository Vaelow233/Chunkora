using System.Collections.Concurrent;
using Chunkora.Server.Agents.Services;
using Chunkora.Server.Models;
using Google.Protobuf.WellKnownTypes;
using Microsoft.EntityFrameworkCore;

namespace Chunkora.Server.Agents.Managers;

public sealed class InstanceConnectionManager(IDbContextFactory<GlobalDatabaseContext> dbFactory)
{
    private readonly ConcurrentDictionary<Guid, InstanceService> _connections = new(); // nodeId => instanceService

    // Agent service side methods
    public void AddConnection(Guid nodeId, InstanceService instanceService)
    {
        _connections[nodeId] = instanceService;
    }

    public void RemoveConnection(Guid nodeId, InstanceService service)
    {
        _connections.TryRemove(new KeyValuePair<Guid, InstanceService>(nodeId, service));
    }

    public async Task<List<Chunkora.Contracts.Agent.V1.Instance>> GetInstancesAsync(Guid nodeId)
    {
        await using var dbContext = await dbFactory.CreateDbContextAsync();
        var instances = await dbContext.Instances
            .Where(instance => instance.NodeId == nodeId)
            .OrderBy(role => role.Name)
            .ToListAsync();
        return ConvertToAgentInstances(instances);
    }

    // Server service side methods
    public async Task SyncInstancesAsync(List<Instance> instances)
    {
        var convertedInstances = ConvertToAgentInstances(instances);
        await Task.WhenAll(_connections.Select(pair => pair.Value.SyncInstances(
            convertedInstances.Where(instance => instance.NodeId == pair.Key.ToString("N")).ToList())));
    }

    public bool IsNodeConnected(Guid nodeId)
    {
        return _connections.ContainsKey(nodeId);
    }

    // Common methods
    private List<Chunkora.Contracts.Agent.V1.Instance> ConvertToAgentInstances(List<Instance> instances)
    {
        return instances.ConvertAll(instance => new Chunkora.Contracts.Agent.V1.Instance
        {
            Id = instance.Id.ToString("N"),
            NodeId = instance.NodeId.ToString("N"),
            Name = instance.Name,
            Description = instance.Description,
            Path = instance.Path,
            LaunchCommand = instance.LaunchCommand,
            StopCommand = instance.StopCommand,
            CreatedAt = Timestamp.FromDateTime(instance.CreatedAt),
            UpdatedAt = Timestamp.FromDateTime(instance.UpdatedAt)
        });
    }
}