using Chunkora.Server.Models;

namespace Chunkora.Server.Requests;

public class UpdateInstanceDockerConfigRequest
{
    public required Guid Id { get; set; } // Instance ID
    public string Name { get; set; } = string.Empty;
    public string Image { get; set; } = string.Empty;
    public List<string> Environments { get; set; } = new();
    public string WorkingDirectory { get; set; } = string.Empty;
    public string Hostname { get; set; } = string.Empty;
    public long MaxMemoryBytes { get; set; }
    public long MaxNanoCpus { get; set; }
    public string NetworkMode { get; set; } = string.Empty;
    public DockerRestartPolicy RestartPolicy { get; set; } = DockerRestartPolicy.Undefined;
    public List<DockerPortBinding> PortBindings { get; set; } = new();
    public List<DockerFileBinding> FileBindings { get; set; } = new();
}