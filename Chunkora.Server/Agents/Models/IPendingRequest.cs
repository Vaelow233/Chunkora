namespace Chunkora.Server.Agents.Models;

public interface IPendingRequest
{
    bool TrySetResult(object? result);
    bool TrySetException(Exception exception);
    bool TrySetCanceled(CancellationToken cancellationToken = default);
}