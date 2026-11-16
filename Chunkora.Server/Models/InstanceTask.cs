using System.ComponentModel.DataAnnotations;

namespace Chunkora.Server.Models;

public class InstanceTask
{
    public Guid Id { get; set; }
    public Guid NodeId { get; set; }
    public Guid InstanceId { get; set; }
    [MaxLength(64)] public required string Name { get; set; }
    [MaxLength(256)] public string Description { get; set; } = string.Empty;
    public InstanceTaskTrigger Trigger { get; set; }
    [MaxLength(256)] public string TriggerInfo { get; set; } = string.Empty;
    public InstanceTaskAction Action { get; set; }
    [MaxLength(256)] public string ActionInfo { get; set; } = string.Empty;
    
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
    public DateTime? LastRunAt { get; set; }
    public DateTime? NextRunAt { get; set; }
}