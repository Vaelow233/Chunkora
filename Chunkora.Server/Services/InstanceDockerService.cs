using Chunkora.Server.Agents.Managers;
using Chunkora.Server.Authentication;
using Chunkora.Server.Dtos;
using Chunkora.Server.Errors;
using Chunkora.Server.Models;
using Chunkora.Server.Requests;
using Chunkora.Server.Utils;
using Microsoft.EntityFrameworkCore;

namespace Chunkora.Server.Services;

public class InstanceDockerService(
    ILogger<InstanceDockerService> logger, 
    ICurrentUser currentUser, 
    GlobalDatabaseContext dbContext, 
    InstanceConnectionManager instanceManager) 
{
    public async Task<Result<InstanceDto>> UpdateAsync(UpdateInstanceDockerConfigRequest request)
    {
        if (!currentUser.HasPermission($"admin.instance.{request.Id}.docker.update"))
        {
            return Result<InstanceDto>.Failure(UserErrors.NoPermission);
        }
        var instance = await dbContext.Instances
            .Include(instance => instance.DockerConfig)
            .FirstOrDefaultAsync(instance => instance.Id.Equals(request.Id));
        if (instance == null)
        {
            return Result<InstanceDto>.Failure(InstanceErrors.NotFound);
        }
        if (instance.DockerConfig == null)
        {
            if (request.Name == string.Empty || request.Image == string.Empty)
            {
                return Result<InstanceDto>.Failure(InstanceErrors.InvalidDockerArguments);
            }
            instance.DockerConfig = new InstanceDockerConfig
            {
                Name = request.Name,
                Image = request.Image
            };
        }
        if (request.Name != string.Empty)
        {
            instance.DockerConfig.Name = request.Name;
        }
        if (request.Image != string.Empty)
        {
            instance.DockerConfig.Image = request.Image;
        }
        instance.DockerConfig.Environments = request.Environments;
        if (request.WorkingDirectory != string.Empty)
        {
            instance.DockerConfig.WorkingDirectory = request.WorkingDirectory;
        }
        if (request.Hostname != string.Empty)
        {
            instance.DockerConfig.Hostname = request.Hostname;
        }
        instance.DockerConfig.MaxNanoCpus = request.MaxNanoCpus;
        instance.DockerConfig.MaxMemoryBytes = request.MaxMemoryBytes;
        if (request.NetworkMode != string.Empty)
        {
            instance.DockerConfig.NetworkMode = request.NetworkMode;
        }
        instance.DockerConfig.RestartPolicy = request.RestartPolicy;
        instance.DockerConfig.PortBindings = request.PortBindings;
        instance.DockerConfig.FileBindings = request.FileBindings;
        instance.UpdatedAt = DateTime.UtcNow;
        dbContext.Instances.Update(instance);
        await dbContext.SaveChangesAsync();
        await instanceManager.SyncInstancesAsync(await dbContext.Instances.ToListAsync());
        return Result<InstanceDto>.Success(InstanceDto.Create(instance));
    }
}