using System.ComponentModel.DataAnnotations;

namespace Chunkora.Server.Models;

public class User
{
    public Guid Id { get; set; }
    public Guid RoleId { get; set; }
    [MaxLength(64)] public required string Name { get; set; }
    [MaxLength(60)] public required string HashedPassword { get; set; }
    [MaxLength(256)] public string Description { get; set; } = string.Empty;
    [MaxLength(256)] public string AccessToken { get; set; } = GenerateToken();
    public DateTime AccessTokenExpiresAt { get; set; }
    [MaxLength(256)] public string RefreshToken { get; set; } = GenerateToken();
    public DateTime RefreshTokenExpiresAt { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
    public DateTime? LastLoginAt { get; set; }
    
    public static string GenerateToken()
    {
        return Guid.NewGuid().ToString("N") + Guid.NewGuid().ToString("N");
    }
}