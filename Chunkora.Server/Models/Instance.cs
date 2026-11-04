using System.ComponentModel.DataAnnotations;

namespace Chunkora.Server.Models;

public class Instance
{
    public Guid Id { get; set; }
    public Guid NodeId { get; set; }
    [MaxLength(64)] public required string Name { get; set; }
    [MaxLength(256)] public string Description { get; set; } = string.Empty;
    [MaxLength(256)] public required string Path { get; set; } = string.Empty;
    [MaxLength(256)] public string LaunchCommand { get; set; } = string.Empty;
    [MaxLength(256)] public string StopCommand { get; set; } = string.Empty; // This command is sending to the instance
    
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
}