namespace Chunkora.Server.Agents.Models;

public class PendingRequest<T> : IPendingRequest
{
    public TaskCompletionSource<T> Source { get; } = new(TaskCreationOptions.RunContinuationsAsynchronously);

    public Task<T> Task => Source.Task;

    public bool TrySetResult(object? result)
    {
        if (result is T value)
        {
            return Source.TrySetResult(value);
        }
        if (result is null && default(T) is null)
        {
            return Source.TrySetResult((T)result!);
        }
        return Source.TrySetException(new InvalidCastException($"Expected result type {typeof(T).Name}."));
    }

    public bool TrySetException(Exception exception) => Source.TrySetException(exception);

    public bool TrySetCanceled(CancellationToken cancellationToken = default) => Source.TrySetCanceled(cancellationToken);
}