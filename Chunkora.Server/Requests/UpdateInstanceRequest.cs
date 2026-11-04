namespace Chunkora.Server.Requests;

public class UpdateInstanceRequest
{
    public required Guid Id { get; set; }
    public Guid NodeId { get; set; } = Guid.Empty;
    public string Name { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public string Path { get; set; } = string.Empty;
    public string LaunchCommand { get; set; } = string.Empty;
    public string StopCommand { get; set; } = string.Empty;
}