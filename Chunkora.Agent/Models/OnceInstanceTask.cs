using Chunkora.Contracts.Agent.V1;

namespace Chunkora.Agent.Models;

public class OnceInstanceTask : IInstanceTask
{
    public SemaphoreSlim LifecycleLock { get; } = new(1, 1);
    private InstanceTask _task;
    private IServiceScopeFactory _scopeFactory;
    private CancellationTokenSource? _cts = null;
    
    public OnceInstanceTask(IServiceScopeFactory scopeFactory, InstanceTask task)
    {
        _task = task;
        _scopeFactory = scopeFactory;
    }

    public InstanceTask GetTask()
    {
        return _task;
    }

    public IServiceScopeFactory GetScopeFactory()
    {
        return _scopeFactory;
    }

    public async Task ScheduleAsync()
    {
        _cts = new CancellationTokenSource();
        DateTime.TryParse(_task.TriggerInfo, out var dateTime);
        _ = Task.Run(async () =>
        {
            try
            {
                var delay = dateTime - DateTime.UtcNow;
                if (delay > TimeSpan.Zero) await Task.Delay(delay, _cts.Token);
                _cts.Token.ThrowIfCancellationRequested();
                await ((IInstanceTask)this).TriggerAsync(_cts.Token);
            }
            catch (OperationCanceledException) when (_cts.IsCancellationRequested)
            {
                
            }
        });
    }

    public async Task UpdateAsync(InstanceTask task)
    {
        _task = task;
        if (_cts != null)
        {
            await _cts.CancelAsync();
        }
        await ScheduleAsync();
    }

    public async Task DisposeAsync()
    {
        if (_cts != null)
        {
            await _cts.CancelAsync();
            _cts.Dispose();
        }
    }
}