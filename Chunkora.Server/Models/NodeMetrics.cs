using System.ComponentModel.DataAnnotations;

namespace Chunkora.Server.Models;

public class NodeMetrics
{
    public Guid Id { get; set; }
    public Guid NodeId { get; set; }
    public required uint Version { get; set; }
    [MaxLength(32)] public required string Architecture { get; set; }
    [MaxLength(32)] public required string OsType { get; set; }
    [MaxLength(256)] public required string Hostname { get; set; }
    [MaxLength(256)] public required string OsDetail { get; set; }
    public required double CpuUsage { get; set; }
    public required ulong MemoryUsedBytes { get; set; }
    public required ulong MemoryTotalBytes { get; set; }
    public DateTime CreatedAt { get; set; }
}