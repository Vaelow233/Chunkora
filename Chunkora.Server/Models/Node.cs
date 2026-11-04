using System.ComponentModel.DataAnnotations;

namespace Chunkora.Server.Models;

public class Node
{
    public Guid Id { get; set; }
    [MaxLength(64)] public required string Name { get; set; }
    [MaxLength(256)] public string Description { get; set; } = string.Empty;
    [MaxLength(256)] public string AgentToken { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
}