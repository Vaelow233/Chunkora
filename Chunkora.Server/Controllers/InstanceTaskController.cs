using Chunkora.Server.Dtos;
using Chunkora.Server.Requests;
using Chunkora.Server.Services;
using Chunkora.Server.Utils;
using Microsoft.AspNetCore.Mvc;

namespace Chunkora.Server.Controllers;

[ApiController]
[Route("api/tasks")]
public class InstanceTaskController(ILogger<InstanceTaskController> logger, InstanceTaskService taskService) : ControllerBase
{
    [HttpPost]
    public async Task<ActionResult<Result<InstanceTaskDto>>> Create(CreateInstanceTaskRequest request)
    {
        var result = await taskService.CreateAsync(request);
        return HandleResult(result);
    }
    
    [HttpPut]
    public async Task<ActionResult<Result<InstanceTaskDto>>> Update(UpdateInstanceTaskRequest request)
    {
        var result = await taskService.UpdateAsync(request);
        return HandleResult(result);
    }

    [HttpGet("{instanceId:guid}/{id:guid}")]
    public async Task<ActionResult<Result<InstanceTaskDto>>> Get(Guid instanceId, Guid id)
    {
        var result = await taskService.GetAsync(instanceId, id);
        return HandleResult(result);
    }
    
    [HttpDelete("{instanceId:guid}/{id:guid}")]
    public async Task<ActionResult<Result<InstanceTaskDto>>> Delete(Guid instanceId, Guid id)
    {
        var result = await taskService.DeleteAsync(instanceId, id);
        return HandleResult(result);
    }

    [HttpGet("{instanceId:guid}")]
    public async Task<ActionResult<Result<PageResult<InstanceTaskDto>>>> List(Guid instanceId, int limit, int offset)
    {
        var result = await taskService.ListAsync(instanceId, limit, offset);
        return HandleResult(result);
    }

    private ActionResult<Result<T>> HandleResult<T>(Result<T> result)
    {
        return result.Error?.Identifier switch
        {
            null => Ok(result),
            "instance_task.name_exists" => Conflict(result),
            "instance_task.not_found" => NotFound(result),
            _ => BadRequest(result)
        };
    }
}