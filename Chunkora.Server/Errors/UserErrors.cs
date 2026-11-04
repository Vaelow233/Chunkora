using Chunkora.Server.Utils;

namespace Chunkora.Server.Errors;

public static class UserErrors
{
    public static readonly Error NameExists = 
        new(20001, "user.name_exists", "An user with a same name already exists");
    
    public static readonly Error NotFound =
        new(20002, "user.not_found", "User not found");
    
    public static readonly Error IncorrectNameOrPassword = 
        new(21001, "user.incorrect_name_or_password", "Incorrect name or password");
    
    public static readonly Error RefreshTokenExpired = 
        new(21002, "user.refresh_token_expired", "Refresh token expired");
    
    public static readonly Error InvalidRefreshToken =
        new(21003, "user.invalid_refresh_token", "Invalid refresh token");
    
    public static readonly Error NoPermission = 
        new(22001, "user.no_permission", "No permission");
}