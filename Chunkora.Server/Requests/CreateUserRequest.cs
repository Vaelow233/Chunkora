namespace Chunkora.Server.Requests;

public class CreateUserRequest
{
    public required string Name { get; set; }
    public required string Password { get; set; }
    public required Guid RoleId { get; set; }
    public string Description { get; set; } = string.Empty;
}