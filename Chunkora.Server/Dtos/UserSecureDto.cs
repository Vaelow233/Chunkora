using Chunkora.Server.Models;

namespace Chunkora.Server.Dtos;

public class UserSecureDto
{
    public Guid Id { get; set; }
    public Guid RoleId { get; set; }
    public required string Name { get; set; }
    public string Description { get; set; } = string.Empty;
    public required string AccessToken { get; set; }
    public DateTime AccessTokenExpiresAt { get; set; }
    public required string RefreshToken { get; set; }
    public DateTime RefreshTokenExpiresAt { get; set; }
    public DateTime? LastLoginAt { get; set; }
    
    public static UserSecureDto Create(User user)
    {
        return new UserSecureDto
        {
            Id = user.Id,
            RoleId = user.RoleId,
            Name = user.Name,
            Description = user.Description,
            AccessToken = user.AccessToken,
            AccessTokenExpiresAt = user.AccessTokenExpiresAt,
            RefreshToken = user.RefreshToken,
            RefreshTokenExpiresAt = user.RefreshTokenExpiresAt,
            LastLoginAt = user.LastLoginAt
        };
    }
}