using Chunkora.Server.Utils;

namespace Chunkora.Server.Errors;

public static class NodeErrors
{
    public static readonly Error NameExists = 
        new(40001, "node.name_exists", "A node with a same name already exists");
    
    public static readonly Error NotFound =
        new(40002, "node.not_found", "Node not found");
}