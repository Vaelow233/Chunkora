namespace Chunkora.Server.Requests;

public class CreateRoleRequest
{
    public required string Name { get; set; }
    public string Description { get; set; } = string.Empty;
    public required List<string> Permissions { get; set; }
}