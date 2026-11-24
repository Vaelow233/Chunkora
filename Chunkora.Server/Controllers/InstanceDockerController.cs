using Chunkora.Server.Dtos;
using Chunkora.Server.Requests;
using Chunkora.Server.Services;
using Chunkora.Server.Utils;
using Microsoft.AspNetCore.Mvc;

namespace Chunkora.Server.Controllers;

[ApiController]
[Route("api/instances/docker")]
public class InstanceDockerController(
    ILogger<InstanceDockerController> logger,
    InstanceDockerService instanceDockerService) : ControllerBase
{
    [HttpPut]
    public async Task<ActionResult<Result<InstanceDto>>> Update(UpdateInstanceDockerConfigRequest request)
    {
        var result = await instanceDockerService.UpdateAsync(request);
        return HandleResult(result);
    }
    
    private ActionResult<Result<T>> HandleResult<T>(Result<T> result)
    {
        return result.Error?.Identifier switch
        {
            null => Ok(result),
            "instance.name_exists" => Conflict(result),
            "instance.not_found" => NotFound(result),
            _ => BadRequest(result)
        };
    }
    
    private ActionResult<Result> HandleResult(Result result)
    {
        return result.Error?.Identifier switch
        {
            null => Ok(result),
            "instance.name_exists" => Conflict(result),
            "instance.not_found" => NotFound(result),
            _ => BadRequest(result)
        };
    }
}