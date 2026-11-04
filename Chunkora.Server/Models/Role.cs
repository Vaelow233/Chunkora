using System.ComponentModel.DataAnnotations;

namespace Chunkora.Server.Models;

public class Role
{
    public Guid Id { get; set; }
    [MaxLength(64)] public required string Name { get; set; }
    [MaxLength(256)] public string Description { get; set; } = string.Empty;
    public required List<string> Permissions { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
}