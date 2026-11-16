namespace Chunkora.Server.Models;

public enum InstanceTaskAction
{
    Unspecified = 0,
    Stop = 1,
    Start = 2,
    Kill = 3,
    Restart = 4,
    SendCommand = 5
}