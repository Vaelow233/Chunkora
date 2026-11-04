using Microsoft.EntityFrameworkCore;

namespace Chunkora.Server.Authentication;

public record AuthenticatedUser(Guid Id, string Name, Guid RoleId, List<string> Permissions);

public sealed class AccessTokenValidator(GlobalDatabaseContext db)
{
    public async Task<AuthenticatedUser?> ValidateAsync(string token)
    {
        var user = await db.Users
            .Where(user => user.AccessToken == token && user.AccessTokenExpiresAt > DateTimeOffset.UtcNow)
            .FirstOrDefaultAsync();
        if (user is null)
        {
            return null;
        }
        var role = await db.Roles.FindAsync(user.RoleId);
        if (role is null)
        {
            return null;
        }
        return new AuthenticatedUser(
            user.Id,
            user.Name,
            user.RoleId,
            role.Permissions
        );
    }
}