using Chunkora.Server.Dtos;
using Chunkora.Server.Requests;
using Chunkora.Server.Services;
using Chunkora.Server.Utils;
using Microsoft.AspNetCore.Mvc;

namespace Chunkora.Server.Controllers;

[ApiController]
[Route("api/roles")]
public class RoleController(ILogger<RoleController> logger, RoleService roleService) : ControllerBase
{
    [HttpPost]
    public async Task<ActionResult<Result<RoleDto>>> Create(CreateRoleRequest request)
    {
        var result = await roleService.CreateAsync(request);
        return HandleResult(result);
    }
    
    [HttpPut]
    public async Task<ActionResult<Result<RoleDto>>> Update(UpdateRoleRequest request)
    {
        var result = await roleService.UpdateAsync(request);
        return HandleResult(result);
    }

    [HttpGet("{id:guid}")]
    public async Task<ActionResult<Result<RoleDto>>> Get(Guid id)
    {
        var result = await roleService.GetAsync(id);
        return HandleResult(result);
    }

    [HttpGet]
    [Route("self")]
    public async Task<ActionResult<Result<RoleDto>>> GetSelf()
    {
        var result = await roleService.GetSelfAsync();
        return HandleResult(result);
    }
    
    [HttpDelete("{id:guid}")]
    public async Task<ActionResult<Result<RoleDto>>> Delete(Guid id)
    {
        var result = await roleService.DeleteAsync(id);
        return HandleResult(result);
    }

    [HttpGet]
    public async Task<ActionResult<Result<PageResult<RoleDto>>>> List(int limit, int offset)
    {
        var result = await roleService.ListAsync(limit, offset);
        return HandleResult(result);
    }

    private ActionResult<Result<T>> HandleResult<T>(Result<T> result)
    {
        return result.Error?.Identifier switch
        {
            null => Ok(result),
            "role.name_exists" => Conflict(result),
            "role.not_found" => NotFound(result),
            _ => BadRequest(result)
        };
    }
}