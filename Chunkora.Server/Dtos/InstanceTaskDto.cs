using Chunkora.Server.Models;

namespace Chunkora.Server.Dtos;

public class InstanceTaskDto
{
    public Guid Id { get; set; }
    public Guid NodeId { get; set; }
    public Guid InstanceId { get; set; }
    public required string Name { get; set; }
    public string Description { get; set; } = string.Empty;
    public InstanceTaskTrigger Trigger { get; set; }
    public string TriggerInfo { get; set; } = string.Empty;
    public InstanceTaskAction Action { get; set; }
    public string ActionInfo { get; set; } = string.Empty;
    
    public DateTime? LastRunAt { get; set; }
    public DateTime? NextRunAt { get; set; }
    
    public static InstanceTaskDto Create(InstanceTask task)
    {
        return new InstanceTaskDto
        {
            Id = task.Id,
            NodeId = task.NodeId,
            InstanceId = task.InstanceId,
            Name = task.Name,
            Description = task.Description,
            Trigger = task.Trigger,
            TriggerInfo = task.TriggerInfo,
            Action = task.Action,
            ActionInfo = task.ActionInfo,
            LastRunAt = task.LastRunAt,
            NextRunAt = task.NextRunAt
        };
    }
}