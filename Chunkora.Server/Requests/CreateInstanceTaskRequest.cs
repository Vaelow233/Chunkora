using Chunkora.Server.Models;

namespace Chunkora.Server.Requests;

public class CreateInstanceTaskRequest
{
    public Guid InstanceId { get; set; }
    public Guid NodeId { get; set; }
    public required string Name { get; set; }
    public string Description { get; set; } = string.Empty;
    public InstanceTaskTrigger Trigger { get; set; }
    public string TriggerInfo { get; set; } = string.Empty;
    public InstanceTaskAction Action { get; set; }
    public string ActionInfo { get; set; } = string.Empty;
}