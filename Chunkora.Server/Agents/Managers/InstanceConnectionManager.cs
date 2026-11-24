using System.Collections.Concurrent;
using Chunkora.Contracts.Agent.V1;
using Chunkora.Server.Models;
using Google.Protobuf.WellKnownTypes;
using Microsoft.EntityFrameworkCore;
using DockerFileBinding = Chunkora.Server.Models.DockerFileBinding;
using DockerPortBinding = Chunkora.Server.Models.DockerPortBinding;
using Instance = Chunkora.Server.Models.Instance;
using InstanceService = Chunkora.Server.Agents.Services.InstanceService;
using InstanceType = Chunkora.Server.Models.InstanceType;

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
            UpdatedAt = Timestamp.FromDateTime(instance.UpdatedAt),
            Type = ConvertToAgentInstanceType(instance.Type),
            AdditionalConfig = ConvertToAgentAdditionalConfig(instance)
        });
    }

    private Chunkora.Contracts.Agent.V1.InstanceType ConvertToAgentInstanceType(InstanceType type)
    {
        return type switch
        {
            InstanceType.Host => Contracts.Agent.V1.InstanceType.Host,
            InstanceType.Docker => Contracts.Agent.V1.InstanceType.Docker,
            _ => Contracts.Agent.V1.InstanceType.Host
        };
    }

    private AdditionalConfig? ConvertToAgentAdditionalConfig(Instance instance)
    {
        if (instance.Type == InstanceType.Docker && instance.DockerConfig != null)
        {
            return new AdditionalConfig
            {
                DockerConfig = new DockerConfig
                {
                    Name = instance.DockerConfig.Name,
                    Image = instance.DockerConfig.Image,
                    Environments = { instance.DockerConfig.Environments },
                    WorkingDir = instance.DockerConfig.WorkingDirectory,
                    Hostname = instance.DockerConfig.Hostname,
                    MaxMemoryBytes = instance.DockerConfig.MaxMemoryBytes,
                    MaxNanoCpus = instance.DockerConfig.MaxNanoCpus,
                    NetworkMode = instance.DockerConfig.NetworkMode,
                    RestartPolicy = ConvertToAgentRestartPolicy(instance.DockerConfig.RestartPolicy),
                    PortBindings = { ConvertToAgentPortBindings(instance.DockerConfig.PortBindings) },
                    FileBindings = { ConvertToAgentFileBindings(instance.DockerConfig.FileBindings) }
                }
            };
        }

        return null;
    }

    private RestartPolicy ConvertToAgentRestartPolicy(DockerRestartPolicy restartPolicy)
    {
        return restartPolicy switch
        {
            DockerRestartPolicy.Undefined => RestartPolicy.Undefined,
            DockerRestartPolicy.No => RestartPolicy.No,
            DockerRestartPolicy.Always => RestartPolicy.Always,
            DockerRestartPolicy.OnFailure => RestartPolicy.OnFailure,
            DockerRestartPolicy.UnlessStopped => RestartPolicy.UnlessStopped,
            _ => RestartPolicy.Undefined
        };
    }

    private List<Chunkora.Contracts.Agent.V1.DockerPortBinding> ConvertToAgentPortBindings(List<DockerPortBinding> portBindings)
    {
        return portBindings.ConvertAll(binding => ConvertToAgentPortBinding(binding));
    }
    
    private Chunkora.Contracts.Agent.V1.DockerPortBinding ConvertToAgentPortBinding(DockerPortBinding portBinding)
    {
        return new Contracts.Agent.V1.DockerPortBinding
        {
            ContainerPort = portBinding.ContainerPort,
            ContainerProtocol = ConvertToAgentPortProtocol(portBinding.ContainerProtocol),
            HostIp = portBinding.HostIp,
            HostPort = portBinding.HostPort,
        };
    }
    
    private PortProtocol ConvertToAgentPortProtocol(DockerPortProtocol portProtocol)
    {
        return portProtocol switch
        {
            DockerPortProtocol.Unspecified => PortProtocol.Unspecified,
            DockerPortProtocol.Tcp => PortProtocol.Tcp,
            DockerPortProtocol.Udp => PortProtocol.Udp,
            _ => PortProtocol.Unspecified
        };
    }
    
    private List<Chunkora.Contracts.Agent.V1.DockerFileBinding> ConvertToAgentFileBindings(List<DockerFileBinding> fileBindings)
    {
        return fileBindings.ConvertAll(binding => ConvertToAgentFileBinding(binding));
    }
    
    private Chunkora.Contracts.Agent.V1.DockerFileBinding ConvertToAgentFileBinding(DockerFileBinding fileBinding)
    {
        return new Contracts.Agent.V1.DockerFileBinding
        {
            ContainerPath = fileBinding.ContainerPath,
            HostPath = fileBinding.HostPath,
            ReadOnly = fileBinding.ReadOnly
        };
    }
}