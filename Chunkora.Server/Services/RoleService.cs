using Chunkora.Server.Authentication;
using Chunkora.Server.Dtos;
using Chunkora.Server.Errors;
using Chunkora.Server.Models;
using Chunkora.Server.Requests;
using Chunkora.Server.Utils;
using Microsoft.EntityFrameworkCore;

namespace Chunkora.Server.Services;

public class RoleService(
    ILogger<RoleService> logger, 
    ICurrentUser currentUser, 
    GlobalDatabaseContext dbContext)
{
    public async Task<Result<RoleDto>> CreateAsync(CreateRoleRequest request)
    {
        if (!currentUser.HasPermission("admin.role.create"))
        {
            return Result<RoleDto>.Failure(UserErrors.NoPermission);
        }
        if (await dbContext.Roles.AnyAsync(role => role.Name.Equals(request.Name)))
        {
            return Result<RoleDto>.Failure(RoleErrors.NameExists);
        }
        var role = new Role
        {
            Name = request.Name,
            Description = request.Description,
            Permissions = request.Permissions,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };
        dbContext.Roles.Add(role);
        await dbContext.SaveChangesAsync();
        return Result<RoleDto>.Success(RoleDto.Create(role));
    }
    
    public async Task<Result<RoleDto>> UpdateAsync(UpdateRoleRequest request)
    {
        if (!currentUser.HasPermission("admin.role.update"))
        {
            return Result<RoleDto>.Failure(UserErrors.NoPermission);
        }
        var role = await dbContext.Roles.FirstOrDefaultAsync(role => role.Id.Equals(request.Id));
        if (role == null)
        {
            return Result<RoleDto>.Failure(RoleErrors.NotFound);
        }
        if (request.Name != string.Empty)
        {
            role.Name = request.Name;
        }
        if (request.Description != string.Empty)
        {
            // TODO: consider whether to do this (or make description required)
            role.Description = request.Description;
        }
        role.Permissions = request.Permissions;
        role.UpdatedAt = DateTime.UtcNow;
        dbContext.Roles.Update(role);
        await dbContext.SaveChangesAsync();
        return Result<RoleDto>.Success(RoleDto.Create(role));
    }

    public async Task<Result<RoleDto>> GetAsync(Guid id)
    {
        if (!currentUser.HasPermission("admin.role.get"))
        {
            return Result<RoleDto>.Failure(UserErrors.NoPermission);
        }
        var role = await dbContext.Roles.FindAsync(id);
        if (role == null)
        {
            return Result<RoleDto>.Failure(RoleErrors.NotFound);
        }
        return Result<RoleDto>.Success(RoleDto.Create(role));
    }

    public async Task<Result<RoleDto>> GetSelfAsync()
    {
        var role = await dbContext.Roles.FindAsync(currentUser.RoleId);
        if (role == null)
        {
            return Result<RoleDto>.Failure(RoleErrors.NotFound);
        }
        return Result<RoleDto>.Success(RoleDto.Create(role));
    }

    public async Task<Result<RoleDto>> DeleteAsync(Guid id)
    {
        if (!currentUser.HasPermission("admin.role.delete"))
        {
            return Result<RoleDto>.Failure(UserErrors.NoPermission);
        }
        var role = await dbContext.Roles.FindAsync(id);
        if (role == null)
        {
            return Result<RoleDto>.Failure(RoleErrors.NotFound);
        }
        dbContext.Roles.Remove(role);
        await dbContext.SaveChangesAsync();
        return Result<RoleDto>.Success(RoleDto.Create(role));
    }

    public async Task<Result<PageResult<RoleDto>>> ListAsync(int limit, int offset)
    {
        if (!currentUser.HasPermission("admin.role.list"))
        {
            return Result<PageResult<RoleDto>>.Failure(UserErrors.NoPermission);
        }
        var total = await dbContext.Roles.CountAsync();
        var roles = await dbContext.Roles.OrderBy(role => role.Name).Skip(offset).Take(limit).ToListAsync();
        return Result<PageResult<RoleDto>>.Success(new(offset, limit, total, roles.Count, roles.ConvertAll(role => RoleDto.Create(role))));
    }
}