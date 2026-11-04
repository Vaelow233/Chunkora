namespace Chunkora.Server.Requests;

public class UpdateNodeRequest
{
    public required Guid Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public string AgentToken { get; set; } = string.Empty;
}