namespace Chunkora.Server.Models;

public enum DockerRestartPolicy
{
    Undefined = 0,
    No = 1,
    Always = 2,
    OnFailure = 3,
    UnlessStopped = 4
}