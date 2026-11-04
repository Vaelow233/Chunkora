namespace Chunkora.Server.Requests;

public class LoginRequest
{
    public required string Name { get; set; }
    public required string Password { get; set; }
}