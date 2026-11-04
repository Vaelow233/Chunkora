using Chunkora.Contracts.Agent.V1;
using Instance = Chunkora.Server.Models.Instance;

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
            StopCommand = instance.StopCommand
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
            Status = status
        };
    }
}