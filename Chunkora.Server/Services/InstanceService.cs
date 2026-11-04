using System.Threading.Channels;
using Chunkora.Contracts.Agent.V1;
using Chunkora.Server.Agents.Managers;
using Chunkora.Server.Authentication;
using Chunkora.Server.Dtos;
using Chunkora.Server.Errors;
using Chunkora.Server.Requests;
using Chunkora.Server.Utils;
using Microsoft.EntityFrameworkCore;
using Instance = Chunkora.Server.Models.Instance;

namespace Chunkora.Server.Services;

public class InstanceService(
    ILogger<InstanceService> logger, 
    ICurrentUser currentUser, 
    GlobalDatabaseContext dbContext, 
    InstanceConnectionManager instanceManager,
    InstanceOperationConnectionManager operationManager,
    InstanceIoConnectionManager ioManager)
{
    public async Task<Result<InstanceDto>> CreateAsync(CreateInstanceRequest request)
    {
        if (!currentUser.HasPermission($"admin.node.{request.NodeId}.instance.create"))
        {
            return Result<InstanceDto>.Failure(UserErrors.NoPermission);
        }
        if (await dbContext.Instances.AnyAsync(instance => instance.Name.Equals(request.Name)))
        {
            return Result<InstanceDto>.Failure(InstanceErrors.NameExists);
        }
        var instance = new Instance
        {
            NodeId = request.NodeId,
            Name = request.Name,
            Description = request.Description,
            Path = request.Path,
            LaunchCommand = request.LaunchCommand,
            StopCommand = request.StopCommand,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };
        dbContext.Instances.Add(instance);
        await dbContext.SaveChangesAsync();
        await instanceManager.SyncInstancesAsync(await dbContext.Instances.ToListAsync());
        return Result<InstanceDto>.Success(InstanceDto.Create(instance));
    }
    
    public async Task<Result<InstanceDto>> UpdateAsync(UpdateInstanceRequest request)
    {
        if (!currentUser.HasPermission("admin.instance.update"))
        {
            return Result<InstanceDto>.Failure(UserErrors.NoPermission);
        }
        var instance = await dbContext.Instances.FirstOrDefaultAsync(instance => instance.Id.Equals(request.Id));
        if (instance == null)
        {
            return Result<InstanceDto>.Failure(InstanceErrors.NotFound);
        }
        if (request.NodeId != Guid.Empty)
        {
            instance.NodeId = request.NodeId;
        }
        if (request.Name != string.Empty)
        {
            instance.Name = request.Name;
        }
        if (request.Description != string.Empty)
        {
            // TODO: consider whether to do this (or make description required)
            instance.Description = request.Description;
        }
        if (request.Path != string.Empty)
        {
            instance.Path = request.Path;
        }
        if (request.LaunchCommand != string.Empty)
        {
            instance.LaunchCommand = request.LaunchCommand;
        }
        if (request.StopCommand != string.Empty)
        {
            instance.StopCommand = request.StopCommand;
        }
        instance.UpdatedAt = DateTime.UtcNow;
        dbContext.Instances.Update(instance);
        await dbContext.SaveChangesAsync();
        await instanceManager.SyncInstancesAsync(await dbContext.Instances.ToListAsync());
        return Result<InstanceDto>.Success(InstanceDto.Create(instance));
    }

    public async Task<Result<InstanceDto>> GetAsync(Guid id)
    {
        var instance = await dbContext.Instances.FindAsync(id);
        if (instance == null)
        {
            return Result<InstanceDto>.Failure(InstanceErrors.NotFound);
        }
        if (!currentUser.HasPermission($"instance.{id}.get"))
        {
            return Result<InstanceDto>.Failure(UserErrors.NoPermission);
        }
        return Result<InstanceDto>.Success(InstanceDto.Create(instance, operationManager.GetInstanceStatus(instance)));
    }

    public async Task<Result<InstanceDto>> DeleteAsync(Guid id)
    {
        var instance = await dbContext.Instances.FindAsync(id);
        if (instance == null)
        {
            return Result<InstanceDto>.Failure(InstanceErrors.NotFound);
        }
        if (!currentUser.HasPermission($"admin.instance.{id}.delete"))
        {
            return Result<InstanceDto>.Failure(UserErrors.NoPermission);
        }
        await operationManager.KillInstanceAsync(instance);
        dbContext.Instances.Remove(instance);
        await dbContext.SaveChangesAsync();
        await instanceManager.SyncInstancesAsync(await dbContext.Instances.ToListAsync());
        return Result<InstanceDto>.Success(InstanceDto.Create(instance));
    }
    
    public async Task<Result<PageResult<InstanceDto>>> ListAsync(int limit, int offset)
    {
        var allowedIds = currentUser
            .MatchPermissionTemplate("instance.{}.get")
            .Distinct()
            .ToArray();
        var query = dbContext.Instances.AsNoTracking();
        if (!allowedIds.Contains("*"))
        {
            var ids = allowedIds.Select(id => Guid.TryParse(id, out var guid) ? (Guid?)guid : null)
                .OfType<Guid>()
                .ToArray();
            query = query.Where(instance => ids.Contains(instance.Id));
        }
        var total = await query.CountAsync();
        var instances = await query.OrderBy(node => node.Name).Skip(offset).Take(limit).ToListAsync();
        return Result<PageResult<InstanceDto>>.Success(new(offset, limit, total, instances.Count, 
            instances.ConvertAll(instance => InstanceDto.Create(instance))));
    }

    public async Task<Result<InstanceDto>> StartAsync(Guid id)
    {
        var instance = await dbContext.Instances.FindAsync(id);
        if (instance == null)
        {
            return Result<InstanceDto>.Failure(InstanceErrors.NotFound);
        }
        if (!currentUser.HasPermission($"instance.{id}.start"))
        {
            return Result<InstanceDto>.Failure(UserErrors.NoPermission);
        }
        await operationManager.StartInstanceAsync(instance);
        return Result<InstanceDto>.Success(InstanceDto.Create(instance));
    }

    public async Task<Result<InstanceDto>> StopAsync(Guid id)
    {
        var instance = await dbContext.Instances.FindAsync(id);
        if (instance == null)
        {
            return Result<InstanceDto>.Failure(InstanceErrors.NotFound);
        }
        if (!currentUser.HasPermission($"instance.{id}.stop"))
        {
            return Result<InstanceDto>.Failure(UserErrors.NoPermission);
        }
        await operationManager.StopInstanceAsync(instance);
        return Result<InstanceDto>.Success(InstanceDto.Create(instance));
    }

    public async Task<Result<InstanceDto>> KillAsync(Guid id)
    {
        var instance = await dbContext.Instances.FindAsync(id);
        if (instance == null)
        {
            return Result<InstanceDto>.Failure(InstanceErrors.NotFound);
        }
        if (!currentUser.HasPermission($"instance.{id}.kill"))
        {
            return Result<InstanceDto>.Failure(UserErrors.NoPermission);
        }
        await operationManager.KillInstanceAsync(instance);
        return Result<InstanceDto>.Success(InstanceDto.Create(instance));
    }

    public async Task<Result<InstanceDto>> RestartAsync(Guid id)
    {
        var instance = await dbContext.Instances.FindAsync(id);
        if (instance == null)
        {
            return Result<InstanceDto>.Failure(InstanceErrors.NotFound);
        }

        if (!currentUser.HasPermission($"instance.{id}.restart"))
        {
            return Result<InstanceDto>.Failure(UserErrors.NoPermission);
        }
        await operationManager.RestartInstanceAsync(instance);
        return Result<InstanceDto>.Success(InstanceDto.Create(instance));
    }

    public async Task<Result<InstanceDto>> ExecuteCommandAsync(Guid id, string command)
    {
        var instance = await dbContext.Instances.FindAsync(id);
        if (instance == null)
        {
            return Result<InstanceDto>.Failure(InstanceErrors.NotFound);
        }
        if (!currentUser.HasPermission($"instance.{id}.execute"))
        {
            return Result<InstanceDto>.Failure(UserErrors.NoPermission);
        }
        await operationManager.ExecuteCommandAsync(instance, command);
        return Result<InstanceDto>.Success(InstanceDto.Create(instance));
    }

    public async Task<Result<PageResult<FileObjectDto>>> ListDirectoryObjectsAsync(Guid id, string path, int limit, int offset)
    {
        var instance = await dbContext.Instances.FindAsync(id);
        if (instance == null)
        {
            return Result<PageResult<FileObjectDto>>.Failure(InstanceErrors.NotFound);
        }
        if (!currentUser.HasPermission($"instance.{id}.files.list"))
        {
            return Result<PageResult<FileObjectDto>>.Failure(UserErrors.NoPermission);
        }
        var result = await ioManager.ListDirectoryAsync(instance, path, limit, offset);
        if (result.HasError)
        {
            return Result<PageResult<FileObjectDto>>.Failure(InstanceErrors.FailedToListDirectoryObjects);
        }
        return Result<PageResult<FileObjectDto>>.Success(new PageResult<FileObjectDto>(offset, limit, result.Total, result.Count, 
            result.Objects.ToList().ConvertAll(item => new FileObjectDto 
            {
                CreatedAt = item.CreatedAt.ToDateTime(),
                LastUpdatedAt = item.LastUpdatedAt.ToDateTime(),
                Name = item.Name,
                Type = FileObjectDto.GetTypeName(item.Type),
                SizeBytes = item.SizeBytes,
                Permission = item.Permission
            })));
    }

    public async Task<Result<FileContentDto>> ReadFileAsync(Guid id, string path)
    {
        var instance = await dbContext.Instances.FindAsync(id);
        if (instance == null)
        {
            return Result<FileContentDto>.Failure(InstanceErrors.NotFound);
        }
        if (!currentUser.HasPermission($"instance.{id}.files.read"))
        {
            return Result<FileContentDto>.Failure(UserErrors.NoPermission);
        }
        var result = await ioManager.ReadFileAsync(instance, path);
        if (result.HasError)
        {
            return Result<FileContentDto>.Failure(InstanceErrors.FailedToReadFile);
        }
        return Result<FileContentDto>.Success(new FileContentDto
        {
            Path = path,
            Content = result.Data.ToByteArray()
        });
    }

    public async Task<Result<IAsyncEnumerable<byte[]>>> BeginDownloadAsync(Guid id, string path, CancellationToken cancellationToken = default)
    {
        var instance = await dbContext.Instances.FindAsync(id);
        if (instance == null)
        {
            return Result<IAsyncEnumerable<byte[]>>.Failure(InstanceErrors.NotFound);
        }
        if (!currentUser.HasPermission($"instance.{id}.files.download"))
        {
            return Result<IAsyncEnumerable<byte[]>>.Failure(UserErrors.NoPermission);
        }
        var result = ioManager.ReadSplittingFileAsync(instance, path, cancellationToken);
        return Result<IAsyncEnumerable<byte[]>>.Success(result);
    }

    public async Task<Result<PageResult<string>>> GetLogsAsync(Guid id, int limit, int offset)
    {
        var instance = await dbContext.Instances.FindAsync(id);
        if (instance == null)
        {
            return Result<PageResult<string>>.Failure(InstanceErrors.NotFound);
        }
        if (!currentUser.HasPermission($"instance.{id}.logs"))
        {
            return Result<PageResult<string>>.Failure(UserErrors.NoPermission);
        }
        var result = await ioManager.GetLogsAsync(instance, limit, offset);
        if (result.HasError)
        {
            return Result<PageResult<string>>.Failure(InstanceErrors.FailedToReadLogs);
        }
        return Result<PageResult<string>>.Success(new PageResult<string>(offset, limit, result.Total, result.Count, result.Lines.ToList()));
    }
    
    public async Task<Result> CreateFileAsync(Guid id, string basePath, string name)
    {
        var instance = await dbContext.Instances.FindAsync(id);
        if (instance == null)
        {
            return Result.Failure(InstanceErrors.NotFound);
        }
        if (!currentUser.HasPermission($"instance.{id}.files.create"))
        {
            return Result.Failure(UserErrors.NoPermission);
        }
        var result = await ioManager.CreateNewObjectAsync(instance, basePath, name, InstanceIoObjectType.File);
        if (result.HasError)
        {
            return Result.Failure(InstanceErrors.FailedToCreateFile);
        }
        return Result.Success();
    }
    
    public async Task<Result> CreateDirectoryAsync(Guid id, string basePath, string name)
    {
        var instance = await dbContext.Instances.FindAsync(id);
        if (instance == null)
        {
            return Result.Failure(InstanceErrors.NotFound);
        }
        if (!currentUser.HasPermission($"instance.{id}.directories.create"))
        {
            return Result.Failure(UserErrors.NoPermission);
        }
        var result = await ioManager.CreateNewObjectAsync(instance, basePath, name, InstanceIoObjectType.Directory);
        if (result.HasError)
        {
            return Result.Failure(InstanceErrors.FailedToCreateDirectory);
        }
        return Result.Success();
    }

    public async Task<Result> UploadFileAsync(Guid id, string path, Stream stream, long? contentLength)
    {
        if (contentLength == null)
        {
            return Result.Failure(InstanceErrors.UnknownContentLength);
        }
        var instance = await dbContext.Instances.FindAsync(id);
        if (instance == null)
        {
            return Result.Failure(InstanceErrors.NotFound);
        }
        if (!currentUser.HasPermission($"instance.{id}.files.upload"))
        {
            return Result.Failure(UserErrors.NoPermission);
        }
        var result = await ioManager.UploadFileAsync(instance, path, stream, (long) contentLength);
        if (result.HasError)
        {
            return Result.Failure(InstanceErrors.FailedToUploadFile(result.Error));
        }
        return Result.Success();
    }
    
    public async Task<Result> DeleteFileAsync(Guid id, string basePath, string name)
    {
        var instance = await dbContext.Instances.FindAsync(id);
        if (instance == null)
        {
            return Result.Failure(InstanceErrors.NotFound);
        }
        if (!currentUser.HasPermission($"instance.{id}.files.delete"))
        {
            return Result.Failure(UserErrors.NoPermission);
        }
        var result = await ioManager.DeleteObjectAsync(instance, basePath, name, InstanceIoObjectType.File);
        if (result.HasError)
        {
            return Result.Failure(InstanceErrors.FailedToDeleteFile(result.Error));
        }
        return Result.Success();
    }
    
    public async Task<Result> DeleteDirectoryAsync(Guid id, string basePath, string name)
    {
        var instance = await dbContext.Instances.FindAsync(id);
        if (instance == null)
        {
            return Result.Failure(InstanceErrors.NotFound);
        }
        if (!currentUser.HasPermission($"instance.{id}.directories.delete"))
        {
            return Result.Failure(UserErrors.NoPermission);
        }
        var result = await ioManager.DeleteObjectAsync(instance, basePath, name, InstanceIoObjectType.Directory);
        if (result.HasError)
        {
            return Result.Failure(InstanceErrors.FailedToDeleteDirectory(result.Error));
        }
        return Result.Success();
    }
    
    public async Task<Result> CopyFileAsync(Guid id, string basePath, string name, string destination)
    {
        var instance = await dbContext.Instances.FindAsync(id);
        if (instance == null)
        {
            return Result.Failure(InstanceErrors.NotFound);
        }
        if (!currentUser.HasPermission($"instance.{id}.files.copy"))
        {
            return Result.Failure(UserErrors.NoPermission);
        }
        var result = await ioManager.CopyObjectAsync(instance, basePath, name, destination, InstanceIoObjectType.File);
        if (result.HasError)
        {
            return Result.Failure(InstanceErrors.FailedToCopyFile(result.Error));
        }
        return Result.Success();
    }
    
    public async Task<Result> CopyDirectoryAsync(Guid id, string basePath, string name, string destination)
    {
        var instance = await dbContext.Instances.FindAsync(id);
        if (instance == null)
        {
            return Result.Failure(InstanceErrors.NotFound);
        }
        if (!currentUser.HasPermission($"instance.{id}.directories.copy"))
        {
            return Result.Failure(UserErrors.NoPermission);
        }
        var result = await ioManager.CopyObjectAsync(instance, basePath, name, destination, InstanceIoObjectType.Directory);
        if (result.HasError)
        {
            return Result.Failure(InstanceErrors.FailedToCopyFile(result.Error));
        }
        return Result.Success();
    }
    
    public async Task<Result> MoveFileAsync(Guid id, string basePath, string name, string destination)
    {
        var instance = await dbContext.Instances.FindAsync(id);
        if (instance == null)
        {
            return Result.Failure(InstanceErrors.NotFound);
        }
        if (!currentUser.HasPermission($"instance.{id}.files.move"))
        {
            return Result.Failure(UserErrors.NoPermission);
        }
        var result = await ioManager.MoveObjectAsync(instance, basePath, name, destination, InstanceIoObjectType.File);
        if (result.HasError)
        {
            return Result.Failure(InstanceErrors.FailedToMoveFile(result.Error));
        }
        return Result.Success();
    }
    
    public async Task<Result> MoveDirectoryAsync(Guid id, string basePath, string name, string destination)
    {
        var instance = await dbContext.Instances.FindAsync(id);
        if (instance == null)
        {
            return Result.Failure(InstanceErrors.NotFound);
        }
        if (!currentUser.HasPermission($"instance.{id}.directories.move"))
        {
            return Result.Failure(UserErrors.NoPermission);
        }
        var result = await ioManager.MoveObjectAsync(instance, basePath, name, destination, InstanceIoObjectType.Directory);
        if (result.HasError)
        {
            return Result.Failure(InstanceErrors.FailedToMoveFile(result.Error));
        }
        return Result.Success();
    }
    
    public async Task<Result> RenameFileAsync(Guid id, string basePath, string name, string newName)
    {
        var instance = await dbContext.Instances.FindAsync(id);
        if (instance == null)
        {
            return Result.Failure(InstanceErrors.NotFound);
        }
        if (!currentUser.HasPermission($"instance.{id}.files.rename"))
        {
            return Result.Failure(UserErrors.NoPermission);
        }
        var result = await ioManager.RenameObjectAsync(instance, basePath, name, newName, InstanceIoObjectType.File);
        if (result.HasError)
        {
            return Result.Failure(InstanceErrors.FailedToRenameFile(result.Error));
        }
        return Result.Success();
    }
    
    public async Task<Result> RenameDirectoryAsync(Guid id, string basePath, string name, string newName)
    {
        var instance = await dbContext.Instances.FindAsync(id);
        if (instance == null)
        {
            return Result.Failure(InstanceErrors.NotFound);
        }
        if (!currentUser.HasPermission($"instance.{id}.directories.rename"))
        {
            return Result.Failure(UserErrors.NoPermission);
        }
        var result = await ioManager.RenameObjectAsync(instance, basePath, name, newName, InstanceIoObjectType.Directory);
        if (result.HasError)
        {
            return Result.Failure(InstanceErrors.FailedToRenameFile(result.Error));
        }
        return Result.Success();
    }
}