namespace Chunkora.Server.Requests;

public class UpdateUserRequest
{
    public required Guid Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Password { get; set; } = string.Empty;
    public Guid RoleId { get; set; } = Guid.Empty;
    public string Description { get; set; } = string.Empty;
}