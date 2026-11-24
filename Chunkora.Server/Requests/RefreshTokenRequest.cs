namespace Chunkora.Server.Requests;

public class RefreshTokenRequest
{
    public required Guid Id { get; set; }
    public required string RefreshToken { get; set; }
}