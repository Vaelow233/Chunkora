using Chunkora.Contracts.Agent.V1;
using Chunkora.Server.Models;
using Instance = Chunkora.Server.Models.Instance;
using InstanceType = Chunkora.Server.Models.InstanceType;

namespace Chunkora.Server.Dtos;

public class InstanceDto
{
    public Guid Id { get; set; }
    public Guid NodeId { get; set; }
    public required string Name { get; set; }
    public string Description { get; set; } = string.Empty;
    public required string Path { get; set; } = string.Empty;
    public string LaunchCommand { get; set; } = string.Empty;
    public string StopCommand { get; set; } = string.Empty;
    public InstanceStatus Status { get; set; } = InstanceStatus.Stopped;

    public InstanceType Type { get; set; } = InstanceType.Host;
    
    public InstanceDockerConfig? DockerConfig { get; set; }

    public static InstanceDto Create(Instance instance)
    {
        return new InstanceDto
        {
            Id = instance.Id,
            NodeId = instance.NodeId,
            Name = instance.Name,
            Description = instance.Description,
            Path = instance.Path,
            LaunchCommand = instance.LaunchCommand,
            StopCommand = instance.StopCommand,
            Type = instance.Type,
            DockerConfig = instance.DockerConfig
        };
    }
    
    public static InstanceDto Create(Instance instance, InstanceStatus status)
    {
        return new InstanceDto
        {
            Id = instance.Id,
            NodeId = instance.NodeId,
            Name = instance.Name,
            Description = instance.Description,
            Path = instance.Path,
            LaunchCommand = instance.LaunchCommand,
            StopCommand = instance.StopCommand,
            Status = status,
            Type = instance.Type,
            DockerConfig = instance.DockerConfig
        };
    }
}