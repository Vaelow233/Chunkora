using Chunkora.Server.Models;

namespace Chunkora.Server.Dtos;

public class NodeSecureDto
{
    public Guid Id { get; set; }
    public required string Name { get; set; }
    public string Description { get; set; } = string.Empty;
    public string AgentToken { get; set; } = string.Empty;
    
    public static NodeSecureDto Create(Node node)
    {
        return new NodeSecureDto
        {
            Id = node.Id,
            Name = node.Name,
            Description = node.Description,
            AgentToken =  node.AgentToken
        };
    }
}