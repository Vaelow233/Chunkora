namespace Chunkora.Server.Dtos;

public class NodeStatusDto
{
    public Guid NodeId { get; set; }
    public required bool InstanceConnected { get; set; }
    public required bool InstanceOperationConnected { get; set; }
}