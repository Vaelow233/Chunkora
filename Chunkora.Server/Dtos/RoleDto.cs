using Chunkora.Server.Models;

namespace Chunkora.Server.Dtos;

public class RoleDto
{
    public Guid Id { get; set; }
    public required string Name { get; set; }
    public string Description { get; set; } = string.Empty;
    public required List<string> Permissions { get; set; }

    public static RoleDto Create(Role role)
    {
        return new RoleDto
        {
            Id = role.Id,
            Name = role.Name,
            Description = role.Description,
            Permissions = role.Permissions
        };
    }
}