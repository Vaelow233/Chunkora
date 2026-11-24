using Chunkora.Contracts.Agent.V1;

namespace Chunkora.Agent.Models;

public class InitialInstanceTask : IInstanceTask
{
    public SemaphoreSlim LifecycleLock { get; } = new(1, 1);
    private InstanceTask _task;
    private IServiceScopeFactory _scopeFactory;
    private CancellationTokenSource? _cts = null;
    private bool _initialized;
    
    public InitialInstanceTask(IServiceScopeFactory scopeFactory, InstanceTask task, bool initialized)
    {
        _task = task;
        _scopeFactory = scopeFactory;
        _initialized = initialized;
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
        if (!_initialized)
        {
            _ = ((IInstanceTask)this).TriggerAsync();
        }
    }

    public async Task UpdateAsync(InstanceTask task)
    {
        if (task.Equals(_task))
        {
            return;
        }
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