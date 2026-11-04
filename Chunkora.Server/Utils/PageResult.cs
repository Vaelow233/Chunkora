namespace Chunkora.Server.Utils;

public sealed record PageResult<T>(int Offset, int Limit, int Total, int Count, List<T> Items);