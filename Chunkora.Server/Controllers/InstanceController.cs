using System.Net.Http.Headers;
using Chunkora.Server.Dtos;
using Chunkora.Server.Requests;
using Chunkora.Server.Services;
using Chunkora.Server.Utils;
using Microsoft.AspNetCore.Mvc;

namespace Chunkora.Server.Controllers;

[ApiController]
[Route("api/instances")]
public class InstanceController(ILogger<RoleController> logger, InstanceService instanceService) : ControllerBase
{
    [HttpPost]
    public async Task<ActionResult<Result<InstanceDto>>> Create(CreateInstanceRequest request)
    {
        var result = await instanceService.CreateAsync(request);
        return HandleResult(result);
    }
    
    [HttpPut]
    public async Task<ActionResult<Result<InstanceDto>>> Update(UpdateInstanceRequest request)
    {
        var result = await instanceService.UpdateAsync(request);
        return HandleResult(result);
    }

    [HttpGet("{id:guid}")]
    public async Task<ActionResult<Result<InstanceDto>>> Get(Guid id)
    {
        var result = await instanceService.GetAsync(id);
        return HandleResult(result);
    }
    
    [HttpDelete("{id:guid}")]
    public async Task<ActionResult<Result<InstanceDto>>> Delete(Guid id)
    {
        var result = await instanceService.DeleteAsync(id);
        return HandleResult(result);
    }

    [HttpGet]
    public async Task<ActionResult<Result<PageResult<InstanceDto>>>> List(int limit, int offset)
    {
        var result = await instanceService.ListAsync(limit, offset);
        return HandleResult(result);
    }

    [HttpPost("{id:guid}/start")]
    public async Task<ActionResult<Result<InstanceDto>>> Start(Guid id)
    {
        var result = await instanceService.StartAsync(id);
        return HandleResult(result);
    }

    [HttpPost("{id:guid}/stop")]
    public async Task<ActionResult<Result<InstanceDto>>> Stop(Guid id)
    {
        var result = await instanceService.StopAsync(id);
        return HandleResult(result);
    }
    
    [HttpPost("{id:guid}/restart")]
    public async Task<ActionResult<Result<InstanceDto>>> Restart(Guid id)
    {
        var result = await instanceService.RestartAsync(id);
        return HandleResult(result);
    }
    
    [HttpPost("{id:guid}/kill")]
    public async Task<ActionResult<Result<InstanceDto>>> Kill(Guid id)
    {
        var result = await instanceService.KillAsync(id);
        return HandleResult(result);
    }
    
    [HttpPost("{id:guid}/execute")]
    public async Task<ActionResult<Result<InstanceDto>>> Execute(Guid id, string command)
    {
        var result = await instanceService.ExecuteCommandAsync(id, command);
        return HandleResult(result);
    }

    [HttpGet("{id:guid}/files/list")]
    public async Task<ActionResult<Result<PageResult<FileObjectDto>>>> ListFiles(Guid id, string path, int limit, int offset)
    {
        var result = await instanceService.ListDirectoryObjectsAsync(id, path, limit, offset);
        return HandleResult(result);
    }
    
    [HttpGet("{id:guid}/files/read")]
    public async Task<ActionResult<Result<FileContentDto>>> ReadFile(Guid id, string path)
    {
        var result = await instanceService.ReadFileAsync(id, path);
        return HandleResult(result);
    }

    [HttpGet("{id:guid}/files/download")]
    public async Task Download(Guid id, string path, CancellationToken cancellationToken)
    {
        var reader = await instanceService.BeginDownloadAsync(id, path, cancellationToken);

        if (!reader.IsSuccess)
        {
            // TODO: change there
            var result = await instanceService.ReadFileAsync(id, path);
            Response.ContentType = "application/json";
            Response.StatusCode = 400;
            await Response.WriteAsJsonAsync(result, cancellationToken);
            return;
        }

        Response.ContentType = "application/octet-stream";
        Response.Headers.ContentDisposition = "attachment";
        
        var disposition = new ContentDispositionHeaderValue("attachment")
        {
            FileNameStar = Path.GetFileName(path.Replace('\\', '/'))
        };

        Response.Headers.ContentDisposition = disposition.ToString();

        await foreach (var chunk in reader.Data!)
        {
            await Response.Body.WriteAsync(chunk, cancellationToken);
        }
    }
    
    [HttpGet("{id:guid}/logs")]
    public async Task<ActionResult<Result<PageResult<string>>>> GetLogs(Guid id, int limit, int offset)
    {
        var result = await instanceService.GetLogsAsync(id, limit, offset);
        return HandleResult(result);
    }
    
    [HttpPost("{id:guid}/files/create")]
    public async Task<ActionResult<Result>> CreateFile(Guid id, string basePath, string name)
    {
        var result = await instanceService.CreateFileAsync(id, basePath, name);
        return HandleResult(result);
    }
    
    [HttpPost("{id:guid}/directories/create")]
    public async Task<ActionResult<Result>> CreateDirectory(Guid id, string basePath, string name)
    {
        var result = await instanceService.CreateDirectoryAsync(id, basePath, name);
        return HandleResult(result);
    }
    
    [HttpPut("{id:guid}/files/upload")]
    [Consumes("application/octet-stream")]
    public async Task<ActionResult<Result>> UploadFile(Guid id, string path, CancellationToken cancellationToken)
    {
        var result = await instanceService.UploadFileAsync(id, path, Request.Body, Request.ContentLength);
        return HandleResult(result);
    }
    
    [HttpDelete("{id:guid}/files/delete")]
    public async Task<ActionResult<Result>> DeleteFile(Guid id, string basePath, string name)
    {
        var result = await instanceService.DeleteFileAsync(id, basePath, name);
        return HandleResult(result);
    }
    
    [HttpDelete("{id:guid}/directories/delete")]
    public async Task<ActionResult<Result>> DeleteDirectory(Guid id, string basePath, string name)
    {
        var result = await instanceService.DeleteDirectoryAsync(id, basePath, name);
        return HandleResult(result);
    }
    
    [HttpPost("{id:guid}/files/copy")]
    public async Task<ActionResult<Result>> CopyFile(Guid id, string basePath, string name, string destination)
    {
        var result = await instanceService.CopyFileAsync(id, basePath, name, destination);
        return HandleResult(result);
    }
    
    [HttpPost("{id:guid}/directories/copy")]
    public async Task<ActionResult<Result>> CopyDirectory(Guid id, string basePath, string name, string destination)
    {
        var result = await instanceService.CopyDirectoryAsync(id, basePath, name, destination);
        return HandleResult(result);
    }
    
    [HttpPost("{id:guid}/files/move")]
    public async Task<ActionResult<Result>> MoveFile(Guid id, string basePath, string name, string destination)
    {
        var result = await instanceService.MoveFileAsync(id, basePath, name, destination);
        return HandleResult(result);
    }
    
    [HttpPost("{id:guid}/directories/move")]
    public async Task<ActionResult<Result>> MoveDirectory(Guid id, string basePath, string name, string destination)
    {
        var result = await instanceService.MoveDirectoryAsync(id, basePath, name, destination);
        return HandleResult(result);
    }
    
    [HttpPost("{id:guid}/files/rename")]
    public async Task<ActionResult<Result>> RenameFile(Guid id, string basePath, string name, string newName)
    {
        var result = await instanceService.RenameFileAsync(id, basePath, name, newName);
        return HandleResult(result);
    }
    
    [HttpPost("{id:guid}/directories/rename")]
    public async Task<ActionResult<Result>> RenameDirectory(Guid id, string basePath, string name, string newName)
    {
        var result = await instanceService.RenameDirectoryAsync(id, basePath, name, newName);
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