using Chunkora.Server.Models;

namespace Chunkora.Server.Dtos;

public class NodeDto
{
    public Guid Id { get; set; }
    public required string Name { get; set; }
    public string Description { get; set; } = string.Empty;
    
    public static NodeDto Create(Node node)
    {
        return new NodeDto
        {
            Id = node.Id,
            Name = node.Name,
            Description = node.Description
        };
    }
}