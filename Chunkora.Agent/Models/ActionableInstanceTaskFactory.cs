using Chunkora.Contracts.Agent.V1;

namespace Chunkora.Agent.Models;

public static class ActionableInstanceTaskFactory
{
    public static IInstanceTask Create(IServiceScopeFactory scopeFactory, InstanceTask task, bool initialized)
    {
        return task.Trigger switch 
        {
            InstanceTaskTrigger.Once => new OnceInstanceTask(scopeFactory, task),
            InstanceTaskTrigger.Interval => new IntervalInstanceTask(scopeFactory, task),
            InstanceTaskTrigger.Scheduled => new ScheduledInstanceTask(scopeFactory, task),
            InstanceTaskTrigger.AgentLaunch => new InitialInstanceTask(scopeFactory, task, initialized),
            _ => throw new NotSupportedException()
        };
    }
    
    public static async Task<IInstanceTask> CreateAndScheduleAsync
        (IServiceScopeFactory scopeFactory, InstanceTask task, bool initialized)
    {
        var createdTask = Create(scopeFactory, task, initialized);
        await createdTask.ScheduleAsync();
        return createdTask;
;    }
}