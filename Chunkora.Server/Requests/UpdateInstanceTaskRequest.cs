using Chunkora.Server.Models;

namespace Chunkora.Server.Requests;

public class UpdateInstanceTaskRequest
{
    public required Guid Id { get; set; }
    public Guid NodeId { get; set; } = Guid.Empty;
    public Guid InstanceId { get; set; } = Guid.Empty;
    public string Name { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public InstanceTaskTrigger Trigger { get; set; } = InstanceTaskTrigger.Unspecified;
    public string TriggerInfo { get; set; } = string.Empty;
    public InstanceTaskAction Action { get; set; } = InstanceTaskAction.Unspecified;
    public string ActionInfo { get; set; } = string.Empty;
}