using Chunkora.Server.Utils;

namespace Chunkora.Server.Errors;

public static class RoleErrors
{
    public static readonly Error NameExists = 
        new(30001, "role.name_exists", "A role with a same name already exists");
    
    public static readonly Error NotFound =
        new(30002, "role.not_found", "Role not found");
}