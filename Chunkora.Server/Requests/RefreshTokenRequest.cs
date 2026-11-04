namespace Chunkora.Server.Requests;

public class RefreshTokenRequest
{
    public required string Name { get; set; } // TODO: consider change this to id
    public required string RefreshToken { get; set; }
}