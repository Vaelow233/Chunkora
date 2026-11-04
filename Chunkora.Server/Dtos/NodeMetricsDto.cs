using Chunkora.Server.Models;

namespace Chunkora.Server.Dtos;

public class NodeMetricsDto
{
    public Guid Id { get; set; }
    public Guid NodeId { get; set; }
    public required uint Version { get; set; }
    public required string Architecture { get; set; }
    public required string OsType { get; set; }
    public required string Hostname { get; set; } 
    public required string OsDetail { get; set; }
    public required double CpuUsage { get; set; }
    public required ulong MemoryUsedBytes { get; set; }
    public required ulong MemoryTotalBytes { get; set; }
    public DateTime CreatedAt { get; set; }
    
    public static NodeMetricsDto Create(NodeMetrics metrics)
    {
        return new NodeMetricsDto
        {
            Id = metrics.Id,
            NodeId = metrics.NodeId,
            Version = metrics.Version,
            Architecture = metrics.Architecture,
            OsType = metrics.OsType,
            Hostname = metrics.Hostname,
            OsDetail = metrics.OsDetail,
            CpuUsage = metrics.CpuUsage,
            MemoryUsedBytes = metrics.MemoryUsedBytes,
            MemoryTotalBytes = metrics.MemoryTotalBytes,
            CreatedAt = metrics.CreatedAt
        };
    }
}