namespace Chunkora.Server.Requests;

public class CreateNodeRequest
{
    public required string Name { get; set; }
    public string Description { get; set; } = string.Empty;
    public string AgentToken { get; set; } = string.Empty;
}