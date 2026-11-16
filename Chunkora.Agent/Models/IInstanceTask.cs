using Chunkora.Contracts.Agent.V1;
using InstanceService = Chunkora.Agent.Services.InstanceService;
using InstanceTaskService = Chunkora.Agent.Services.InstanceTaskService;

namespace Chunkora.Agent.Models;

public interface IInstanceTask
{
    InstanceTask GetTask();
    IServiceScopeFactory GetScopeFactory();
    Task DisposeAsync();
    Task UpdateAsync(InstanceTask task);
    Task ScheduleAsync();
    SemaphoreSlim LifecycleLock { get; }
    
    public async Task TriggerAsync(CancellationToken cancellationToken = default)
    {
        var task = GetTask();
        await using var scope = GetScopeFactory().CreateAsyncScope();
        var service = scope.ServiceProvider.GetService<InstanceService>();
        if (service == null)
        {
            throw new Exception("Instance service not found");
        }
        var instance = service.GetInstance(task.InstanceId);
        if (instance == null)
        {
            throw new Exception("Instance not found");
        }
        var taskService = scope.ServiceProvider.GetService<InstanceTaskService>();
        if (taskService == null)
        {
            throw new Exception("Instance task service not found");
        }
        switch (task.Action)
        {
            case InstanceTaskAction.Stop:
                await instance.StopAsync(cancellationToken);
                break;
            case InstanceTaskAction.Start:
                await instance.StartAsync(cancellationToken);
                break;
            case InstanceTaskAction.Kill:
                await instance.DisposeAsync();
                break;
            case InstanceTaskAction.Restart:
                await instance.RestartAsync(cancellationToken);
                break;
            case InstanceTaskAction.SendCommand:
                await instance.WriteAsync(task.ActionInfo, cancellationToken);
                break;
        }
        await taskService.SyncRanEventAsync(GetTask(), cancellationToken);
    }
}