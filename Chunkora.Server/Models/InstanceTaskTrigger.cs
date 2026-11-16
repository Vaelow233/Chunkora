namespace Chunkora.Server.Models;

public enum InstanceTaskTrigger
{
    Unspecified = 0,
    Once = 1,
    Interval = 2,
    Scheduled = 3,
    AgentLaunch = 4
}