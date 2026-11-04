using Chunkora.Server.Models;

namespace Chunkora.Server.Dtos;

public class UserDto
{
    public Guid Id { get; set; }
    public Guid RoleId { get; set; }
    public required string Name { get; set; }
    public string Description { get; set; } = string.Empty;
    public DateTime? LastLoginAt { get; set; }

    public static UserDto Create(User user)
    {
        return new UserDto
        {
            Id = user.Id,
            RoleId = user.RoleId,
            Name = user.Name,
            Description = user.Description,
            LastLoginAt = user.LastLoginAt
        };
    }
}