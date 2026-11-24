using System.ComponentModel.DataAnnotations;

namespace Chunkora.Server.Models;

public class InstanceDockerConfig
{
    [MaxLength(64)] public required string Name { get; set; }
    [MaxLength(256)] public required string Image { get; set; }
    public List<string> Environments { get; set; } = new();
    [MaxLength(256)] public string WorkingDirectory { get; set; } = string.Empty;
    [MaxLength(256)] public string Hostname { get; set; } = string.Empty;
    public long MaxMemoryBytes { get; set; }
    public long MaxNanoCpus { get; set; }
    [MaxLength(64)] public string NetworkMode { get; set; } = string.Empty;
    public DockerRestartPolicy RestartPolicy { get; set; } = DockerRestartPolicy.Undefined;
    public List<DockerPortBinding> PortBindings { get; set; } = new();
    public List<DockerFileBinding> FileBindings { get; set; } = new();
}