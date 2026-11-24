using System.ComponentModel.DataAnnotations;

namespace Chunkora.Server.Models;

public class DockerPortBinding
{
    public required uint ContainerPort { get; set; }
    public DockerPortProtocol ContainerProtocol { get; set; } = DockerPortProtocol.Unspecified;
    [MaxLength(256)] public string HostIp { get; set; } = "0.0.0.0";
    public required uint HostPort { get; set; }
}