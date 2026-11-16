using Chunkora.Server.Agents.Managers;
using Chunkora.Server.Authentication;
using Chunkora.Server.Dtos;
using Chunkora.Server.Errors;
using Chunkora.Server.Models;
using Chunkora.Server.Requests;
using Chunkora.Server.Utils;
using Cronos;
using Microsoft.EntityFrameworkCore;

namespace Chunkora.Server.Services;

public class InstanceTaskService(
    ILogger<InstanceTaskService> logger, 
    ICurrentUser currentUser, 
    InstanceTaskConnectionManager taskManager,
    GlobalDatabaseContext dbContext)
{
    public async Task<Result<InstanceTaskDto>> CreateAsync(CreateInstanceTaskRequest request)
    {
        if (!currentUser.HasPermission($"instance.{request.InstanceId}.task.create"))
        {
            return Result<InstanceTaskDto>.Failure(UserErrors.NoPermission);
        }
        var instance = await dbContext.Instances.FirstOrDefaultAsync(
            instance => instance.Id.Equals(request.InstanceId) && instance.NodeId.Equals(request.NodeId));
        if (instance == null)
        {
            return Result<InstanceTaskDto>.Failure(InstanceErrors.NotFound);
        }
        if (await dbContext.InstanceTasks.AnyAsync(task => task.Name.Equals(request.Name)))
        {
            return Result<InstanceTaskDto>.Failure(InstanceTaskErrors.NameExists);
        }
        var triggerInfo = request.TriggerInfo;
        DateTime? nextRunAt = null;
        switch (request.Trigger)
        {
            case InstanceTaskTrigger.Once:
                if (!DateTime.TryParse(triggerInfo, out var nonNullNextRunAt))
                {
                    return Result<InstanceTaskDto>.Failure(InstanceTaskErrors.InvalidTriggerInfo);
                }
                nextRunAt = nonNullNextRunAt;
                break;
            case InstanceTaskTrigger.Interval:
                if (!long.TryParse(triggerInfo, out var interval))
                {
                    return Result<InstanceTaskDto>.Failure(InstanceTaskErrors.InvalidTriggerInfo);
                }
                nextRunAt = DateTime.UtcNow.AddSeconds(interval);
                break;
            case InstanceTaskTrigger.Scheduled:
                if (!CronExpression.TryParse(triggerInfo, out var exp))
                {
                    return Result<InstanceTaskDto>.Failure(InstanceTaskErrors.InvalidTriggerInfo);
                }
                nextRunAt = exp.GetNextOccurrence(DateTime.UtcNow);
                break;
        }
        var task = new InstanceTask
        {
            NodeId = request.NodeId,
            InstanceId = request.InstanceId,
            Name = request.Name,
            Description = request.Description,
            Trigger = request.Trigger,
            TriggerInfo = request.TriggerInfo,
            Action = request.Action,
            ActionInfo = request.ActionInfo,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow,
            NextRunAt = nextRunAt
        };
        dbContext.InstanceTasks.Add(task);
        await dbContext.SaveChangesAsync();
        await taskManager.SyncTasksAsync(await dbContext.InstanceTasks.ToListAsync());
        return Result<InstanceTaskDto>.Success(InstanceTaskDto.Create(task));
    }
    
    public async Task<Result<InstanceTaskDto>> UpdateAsync(UpdateInstanceTaskRequest request)
    {
        // TODO: consider whether to add task id in permission
        if (!currentUser.HasPermission($"instance.{request.InstanceId}.task.{request.Id}.update"))
        {
            return Result<InstanceTaskDto>.Failure(UserErrors.NoPermission);
        }
        var instance = await dbContext.Instances.FirstOrDefaultAsync(
            instance => instance.Id.Equals(request.InstanceId) && instance.NodeId.Equals(request.NodeId));
        if (instance == null)
        {
            return Result<InstanceTaskDto>.Failure(InstanceErrors.NotFound);
        }
        var task = await dbContext.InstanceTasks.FirstOrDefaultAsync(task => task.Id.Equals(request.Id));
        if (task == null)
        {
            return Result<InstanceTaskDto>.Failure(InstanceTaskErrors.NotFound);
        }
        if (request.NodeId != Guid.Empty)
        {
            task.NodeId = request.NodeId;
        }
        if (request.InstanceId != Guid.Empty)
        {
            task.InstanceId = request.InstanceId;
        }
        if (request.Name != string.Empty)
        {
            if (await dbContext.InstanceTasks
                    .AnyAsync(task => task.Name.Equals(request.Name) && !task.Id.Equals(request.Id)))
            {
                return Result<InstanceTaskDto>.Failure(InstanceTaskErrors.NameExists);
            }
            task.Name = request.Name;
        }
        if (request.Description != string.Empty)
        {
            // TODO: consider whether to do this (or make description required)
            task.Description = request.Description;
        }
        if (request.Trigger != InstanceTaskTrigger.Unspecified)
        {
            task.Trigger = request.Trigger;
        }
        if (request.TriggerInfo != string.Empty)
        {
            task.TriggerInfo = request.TriggerInfo;
        }
        if (request.Action != InstanceTaskAction.Unspecified)
        {
            task.Action = request.Action;
        }
        if (request.ActionInfo != string.Empty)
        {
            task.ActionInfo = request.ActionInfo;
        }
        var triggerInfo = request.TriggerInfo;
        DateTime? nextRunAt = null;
        switch (request.Trigger)
        {
            case InstanceTaskTrigger.Once:
                if (!DateTime.TryParse(triggerInfo, out var nonNullNextRunAt))
                {
                    return Result<InstanceTaskDto>.Failure(InstanceTaskErrors.InvalidTriggerInfo);
                }
                nextRunAt = nonNullNextRunAt;
                break;
            case InstanceTaskTrigger.Interval:
                if (!long.TryParse(triggerInfo, out var interval))
                {
                    return Result<InstanceTaskDto>.Failure(InstanceTaskErrors.InvalidTriggerInfo);
                }
                nextRunAt = DateTime.UtcNow.AddSeconds(interval);
                break;
            case InstanceTaskTrigger.Scheduled:
                if (!CronExpression.TryParse(triggerInfo, out var exp))
                {
                    return Result<InstanceTaskDto>.Failure(InstanceTaskErrors.InvalidTriggerInfo);
                }
                nextRunAt = exp.GetNextOccurrence(DateTime.UtcNow);
                break;
        }
        task.NextRunAt = nextRunAt;
        task.UpdatedAt = DateTime.UtcNow;
        dbContext.InstanceTasks.Update(task);
        await dbContext.SaveChangesAsync();
        await taskManager.SyncTasksAsync(await dbContext.InstanceTasks.ToListAsync());
        return Result<InstanceTaskDto>.Success(InstanceTaskDto.Create(task));
    }

    public async Task<Result<InstanceTaskDto>> GetAsync(Guid instanceId, Guid id)
    {
        if (!currentUser.HasPermission($"instance.{instanceId}.task.{id}.get"))
        {
            return Result<InstanceTaskDto>.Failure(UserErrors.NoPermission);
        }
        var task = await dbContext.InstanceTasks
            .Where(task => task.InstanceId == instanceId && task.Id == id)
            .FirstOrDefaultAsync();
        if (task == null)
        {
            return Result<InstanceTaskDto>.Failure(InstanceTaskErrors.NotFound);
        }
        return Result<InstanceTaskDto>.Success(InstanceTaskDto.Create(task));
    }

    public async Task<Result<InstanceTaskDto>> DeleteAsync(Guid instanceId, Guid id)
    {
        if (!currentUser.HasPermission($"instance.{instanceId}.task.{id}.delete"))
        {
            return Result<InstanceTaskDto>.Failure(UserErrors.NoPermission);
        }
        var task = await dbContext.InstanceTasks.FindAsync(id);
        if (task == null)
        {
            return Result<InstanceTaskDto>.Failure(InstanceTaskErrors.NotFound);
        }
        dbContext.InstanceTasks.Remove(task);
        await dbContext.SaveChangesAsync();
        await taskManager.SyncTasksAsync(await dbContext.InstanceTasks.ToListAsync());
        return Result<InstanceTaskDto>.Success(InstanceTaskDto.Create(task));
    }

    public async Task<Result<PageResult<InstanceTaskDto>>> ListAsync(Guid instanceId, int limit, int offset)
    {
        if (!currentUser.HasPermission($"instance.{instanceId}.task.list"))
        {
            return Result<PageResult<InstanceTaskDto>>.Failure(UserErrors.NoPermission);
        }
        var total = await dbContext.InstanceTasks.CountAsync();
        var tasks = await dbContext.InstanceTasks
            .Where(task => task.InstanceId == instanceId)
            .OrderBy(task => task.Name)
            .Skip(offset)
            .Take(limit)
            .ToListAsync();
        return Result<PageResult<InstanceTaskDto>>.Success(
            new(offset, limit, total, tasks.Count, tasks.ConvertAll(task => InstanceTaskDto.Create(task))));
    }
}