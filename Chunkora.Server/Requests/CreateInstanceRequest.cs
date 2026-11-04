namespace Chunkora.Server.Requests;

public class CreateInstanceRequest
{
    public Guid NodeId { get; set; }
    public required string Name { get; set; }
    public string Description { get; set; } = string.Empty;
    public required string Path { get; set; } = string.Empty;
    public string LaunchCommand { get; set; } = string.Empty;
    public string StopCommand { get; set; } = string.Empty;
}