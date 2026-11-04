namespace Chunkora.Server.Dtos;

public class FileContentDto
{
    public required string Path { get; set; }
    public required byte[] Content { get; set; }
}