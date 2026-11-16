using Chunkora.Server.Utils;

namespace Chunkora.Server.Errors;

public static class InstanceTaskErrors
{
    public static readonly Error NameExists = 
        new(60001, "instance_task.name_exists", "An instance task with a same name already exists");
    
    public static readonly Error NotFound =
        new(60002, "instance_task.not_found", "Instance task not found");
    
    public static readonly Error InvalidTriggerInfo =
        new(60003, "instance_task.invalid_trigger_info", "Invalid trigger information in instance task");
}