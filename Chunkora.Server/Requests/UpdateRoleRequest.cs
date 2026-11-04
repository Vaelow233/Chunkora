namespace Chunkora.Server.Requests;

public class UpdateRoleRequest
{
    public required Guid Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public required List<string> Permissions { get; set; }
}