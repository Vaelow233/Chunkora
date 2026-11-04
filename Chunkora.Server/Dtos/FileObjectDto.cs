using Chunkora.Contracts.Agent.V1;

namespace Chunkora.Server.Dtos;

public class FileObjectDto
{
    public required string Type { get; set; }
    public required string Name { get; set; }
    public long SizeBytes { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime LastUpdatedAt { get; set; }
    public required string Permission { get; set; }

    public static string GetTypeName(InstanceIoObjectType type) => type switch
    {
        InstanceIoObjectType.File => "file",
        InstanceIoObjectType.Directory => "directory",
        _ => "unspecified"
    };
}