using Chunkora.Contracts.Agent.V1;

namespace Chunkora.Agent.Models;

public class IntervalInstanceTask : IInstanceTask
{
    public SemaphoreSlim LifecycleLock { get; } = new(1, 1);
    private InstanceTask _task;
    private IServiceScopeFactory _scopeFactory;
    private CancellationTokenSource? _cts = null;
    
    public IntervalInstanceTask(IServiceScopeFactory scopeFactory, InstanceTask task)
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
        long.TryParse(_task.TriggerInfo, out var interval);
        _ = Task.Run(async () =>
        {
            using var timer = new PeriodicTimer(TimeSpan.FromSeconds(interval));
            try
            {
                while (await timer.WaitForNextTickAsync(_cts.Token))
                {
                    await ((IInstanceTask)this).TriggerAsync(_cts.Token);
                }
            }
            catch (OperationCanceledException) when (_cts.IsCancellationRequested)
            {
                
            }
        });
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