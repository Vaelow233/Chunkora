using Chunkora.Agent.Configuration;
using Chunkora.Agent.Models;
using Chunkora.Contracts.Agent.V1;
using Grpc.Core;
using Microsoft.Extensions.Options;

namespace Chunkora.Agent.Services;

public class InstanceTaskService(
    ILogger<InstanceTaskService> logger, 
    IServiceScopeFactory scopeFactory,
    Chunkora.Contracts.Agent.V1.InstanceTaskService.InstanceTaskServiceClient taskClient,
    IOptions<AgentOptions> agentOptions)
{
    private readonly LinkedList<IInstanceTask> _tasks = new();
    private readonly SemaphoreSlim _tasksLock = new(1, 1);
    
    public async Task SyncInstanceTasksAsync(CancellationToken cancellationToken = default)
    {
        var request = new SyncInstanceTasksRequest
        {
            RequestId = Guid.NewGuid().ToString("N")
        };
        var metadata = new Metadata();
        var initialized = false;
        metadata.Add("Authorization", "Bearer " + agentOptions.Value.Token);
        using var call = taskClient.ConnectInstanceTasks(request, cancellationToken: cancellationToken, headers: metadata);
        await foreach (var response in call.ResponseStream.ReadAllAsync(cancellationToken))
        {
            List<IInstanceTask> removedTasks;
            await _tasksLock.WaitAsync(cancellationToken);
            try
            {
                LinkedList<IInstanceTask> tasks = new();
                foreach (var task in response.Task)
                {
                    var handled = false;
                    foreach (var originTask in _tasks)
                    {
                        // For exists instance
                        if (originTask.GetTask().Id == task.Id)
                        {
                            if (task.Trigger != originTask.GetTask().Trigger)
                            {
                                // The task type has changed, so we need to recreate it
                                await originTask.LifecycleLock.WaitAsync(cancellationToken);
                                try
                                {
                                    await originTask.DisposeAsync();
                                }
                                finally
                                {
                                    originTask.LifecycleLock.Release();
                                }
                                var newTask = await ActionableInstanceTaskFactory.CreateAndScheduleAsync(
                                    scopeFactory, task, true); // Since it has been added to list, it must be initialized
                                tasks.AddLast(newTask);
                            }
                            else
                            {
                                await originTask.UpdateAsync(task);
                                tasks.AddLast(originTask);
                            }
                            _tasks.Remove(originTask);
                            handled = true;
                            break;
                        }
                    }

                    // For new instance
                    if (!handled)
                    {
                        tasks.AddLast(
                            await ActionableInstanceTaskFactory.CreateAndScheduleAsync(scopeFactory, task, initialized));
                    }
                }
                
                removedTasks = _tasks.ToList();
                _tasks.Clear();

                // Add handled instances
                foreach (var instance in tasks)
                {
                    _tasks.AddLast(instance);
                }
            }
            finally
            {
                initialized = true;
                _tasksLock.Release();
            }
            
            // For deleted instance
            foreach (var task in removedTasks)
            {
                await task.LifecycleLock.WaitAsync(cancellationToken);
                try
                {
                    await task.DisposeAsync();
                }
                finally
                {
                    task.LifecycleLock.Release();
                }
            }
        }
    }

    public async Task SyncRanEventAsync(InstanceTask task, CancellationToken cancellationToken = default)
    {
        var request = new SyncRanEventRequest
        {
            RequestId = Guid.NewGuid().ToString("N"),
            Id = task.Id,
            InstanceId = task.InstanceId
        };
        var metadata = new Metadata();
        metadata.Add("Authorization", "Bearer " + agentOptions.Value.Token);
        await taskClient.SyncRanEventAsync(request, cancellationToken: cancellationToken, headers: metadata);
    }
}