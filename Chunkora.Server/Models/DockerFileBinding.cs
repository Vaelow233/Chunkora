using System.ComponentModel.DataAnnotations;

namespace Chunkora.Server.Models;

public class DockerFileBinding
{
    [MaxLength(256)] public required string HostPath { get; set; }
    [MaxLength(256)] public required string ContainerPath { get; set; }
    public bool ReadOnly { get; set; }
}