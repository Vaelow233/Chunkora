using System.Security.Claims;

namespace Chunkora.Server.Authentication;

public class CurrentUser(IHttpContextAccessor httpContextAccessor) : ICurrentUser
{
    private ClaimsPrincipal User => httpContextAccessor.HttpContext?.User ?? 
                                    throw new InvalidOperationException("No active HTTP request.");

    public Guid UserId
    {
        get
        {
            var value = User.FindFirstValue(ClaimTypes.NameIdentifier);
            return Guid.Parse(value ?? throw new InvalidOperationException());
        }
    }

    public string Name
    {
        get
        {
            var value = User.FindFirstValue(ClaimTypes.Name);
            return value ?? throw new InvalidOperationException();
        }
    }
    
    public Guid RoleId
    {
        get
        {
            var value = User.FindFirstValue(ClaimTypes.Role);
            return Guid.Parse(value ?? throw new InvalidOperationException());
        }
    }

    public List<string> Permissions
    {
        get
        {
            var value = User.FindFirstValue(ClaimTypes.UserData);
            return value?.Split(";").ToList() ?? throw new InvalidOperationException();
        }
    }
}