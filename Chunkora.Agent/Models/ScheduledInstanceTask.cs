using Chunkora.Contracts.Agent.V1;
using Cronos;

namespace Chunkora.Agent.Models;

// aka CronInstanceTask
public class ScheduledInstanceTask : IInstanceTask
{
    public SemaphoreSlim LifecycleLock { get; } = new(1, 1);
    private InstanceTask _task;
    private IServiceScopeFactory _scopeFactory;
    private CancellationTokenSource? _cts = null;
    
    public ScheduledInstanceTask(IServiceScopeFactory scopeFactory, InstanceTask task)
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
        var fieldCount = _task.TriggerInfo.Split(" ").Length;
        var format = fieldCount switch
        {
            5 => CronFormat.Standard,
            6 => CronFormat.IncludeSeconds,
            _ => (CronFormat?)null
        };
        if (format == null)
        {
            throw new ArgumentException();
        }
        CronExpression.TryParse(_task.TriggerInfo, format.Value, out var cron);
        _ = Task.Run(async () =>
        {
            try
            {
                while (!_cts.Token.IsCancellationRequested)
                {
                    var next = cron.GetNextOccurrence(DateTimeOffset.UtcNow, TimeZoneInfo.Local);
                    if (next is null)
                    {
                        break;
                    }
                    var delay = next.Value - DateTimeOffset.UtcNow;
                    if (delay > TimeSpan.Zero)
                    {
                        await Task.Delay(delay, _cts.Token);
                    }
                    _cts.Token.ThrowIfCancellationRequested();
                    await ((IInstanceTask)this).TriggerAsync(_cts.Token);
                }
            }
            catch (OperationCanceledException)when (_cts.IsCancellationRequested)
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