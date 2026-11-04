using Chunkora.Server.Authentication;
using Chunkora.Server.Dtos;
using Chunkora.Server.Requests;
using Chunkora.Server.Services;
using Chunkora.Server.Utils;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Chunkora.Server.Controllers;

[ApiController]
[Route("api/users")]
public class UserController(ILogger<UserController> logger, UserService userService) : ControllerBase
{
    [HttpPost]
    public async Task<ActionResult<Result<UserDto>>> Create(CreateUserRequest request)
    {
        var result = await userService.CreateAsync(request);
        return HandleResult(result);
    }
    
    [HttpPut]
    public async Task<ActionResult<Result<UserDto>>> Update(UpdateUserRequest request)
    {
        var result = await userService.UpdateAsync(request);
        return HandleResult(result);
    }

    [HttpPost]
    [AllowAnonymous]
    [Route("login")]
    public async Task<ActionResult<Result<UserSecureDto>>> Login(LoginRequest request)
    {
        var result = await userService.LoginAsync(request);
        return HandleResult(result);
    }

    [HttpPost]
    [AllowAnonymous]
    [Route("refresh")]
    public async Task<ActionResult<Result<UserSecureDto>>> Refresh(RefreshTokenRequest request)
    {
        var result = await userService.RefreshAsync(request);
        return HandleResult(result);
    }

    [HttpGet("{id:guid}")]
    public async Task<ActionResult<Result<UserDto>>> Get(Guid id)
    {
        var result = await userService.GetAsync(id);
        return HandleResult(result);
    }
    
    [HttpDelete("{id:guid}")]
    public async Task<ActionResult<Result<UserDto>>> Delete(Guid id)
    {
        var result = await userService.DeleteAsync(id);
        return HandleResult(result);
    }

    [HttpGet]
    public async Task<ActionResult<Result<PageResult<UserDto>>>> List(int limit, int offset)
    {
        var result = await userService.ListAsync(limit, offset);
        return HandleResult(result);
    }

    private ActionResult<Result<T>> HandleResult<T>(Result<T> result)
    {
        return result.Error?.Identifier switch
        {
            null => Ok(result),
            "user.name_exists" => Conflict(result),
            "user.not_found" => NotFound(result),
            "user.incorrect_name_or_password" => Unauthorized(result),
            "user.invalid_refresh_token" => Unauthorized(result),
            "user.refresh_token_expired" => Unauthorized(result),
            _ => BadRequest(result)
        };
    }
}