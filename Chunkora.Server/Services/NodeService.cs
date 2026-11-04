using Chunkora.Server.Authentication;
using Chunkora.Server.Dtos;
using Chunkora.Server.Errors;
using Chunkora.Server.Models;
using Chunkora.Server.Requests;
using Chunkora.Server.Utils;
using Microsoft.EntityFrameworkCore;

namespace Chunkora.Server.Services;

public class NodeService(
    ILogger<NodeService> logger, 
    ICurrentUser currentUser, 
    GlobalDatabaseContext dbContext)
{
    public async Task<Result<NodeDto>> CreateAsync(CreateNodeRequest request)
    {
        if (!currentUser.HasPermission("admin.node.create"))
        {
            return Result<NodeDto>.Failure(UserErrors.NoPermission);
        }
        if (await dbContext.Nodes.AnyAsync(node => node.Name.Equals(request.Name)))
        {
            return Result<NodeDto>.Failure(NodeErrors.NameExists);
        }
        var node = new Node
        {
            Name = request.Name,
            Description = request.Description,
            AgentToken = request.AgentToken,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };
        dbContext.Nodes.Add(node);
        await dbContext.SaveChangesAsync();
        return Result<NodeDto>.Success(NodeDto.Create(node));
    }
    
    public async Task<Result<NodeDto>> UpdateAsync(UpdateNodeRequest request)
    {
        if (!currentUser.HasPermission("admin.node.update"))
        {
            return Result<NodeDto>.Failure(UserErrors.NoPermission);
        }
        var node = await dbContext.Nodes.FirstOrDefaultAsync(node => node.Id.Equals(request.Id));
        if (node == null)
        {
            return Result<NodeDto>.Failure(NodeErrors.NotFound);
        }
        if (request.Name != string.Empty)
        {
            node.Name = request.Name;
        }
        if (request.Description != string.Empty)
        {
            // TODO: consider whether to do this (or make description required)
            node.Description = request.Description;
        }
        if (request.AgentToken != string.Empty)
        {
            node.AgentToken = request.AgentToken;
            // TODO: reset the connection that used the original token
        }
        node.UpdatedAt = DateTime.UtcNow;
        dbContext.Nodes.Update(node);
        await dbContext.SaveChangesAsync();
        return Result<NodeDto>.Success(NodeDto.Create(node));
    }

    public async Task<Result<NodeDto>> GetAsync(Guid id)
    {
        if (!currentUser.HasPermission($"admin.node.{id}.get"))
        {
            return Result<NodeDto>.Failure(UserErrors.NoPermission);
        }
        var node = await dbContext.Nodes.FindAsync(id);
        if (node == null)
        {
            return Result<NodeDto>.Failure(NodeErrors.NotFound);
        }
        return Result<NodeDto>.Success(NodeDto.Create(node));
    }

    public async Task<Result<NodeSecureDto>> GetSecureAsync(Guid id)
    {
        if (!currentUser.HasPermission($"admin.node.{id}.get.secure"))
        {
            return Result<NodeSecureDto>.Failure(UserErrors.NoPermission);
        }
        var node = await dbContext.Nodes.FindAsync(id);
        if (node == null)
        {
            return Result<NodeSecureDto>.Failure(NodeErrors.NotFound);
        }
        return Result<NodeSecureDto>.Success(NodeSecureDto.Create(node));
    }

    public async Task<Result<NodeDto>> DeleteAsync(Guid id)
    {
        if (!currentUser.HasPermission($"admin.node.{id}.delete"))
        {
            return Result<NodeDto>.Failure(UserErrors.NoPermission);
        }
        var node = await dbContext.Nodes.FindAsync(id);
        if (node == null)
        {
            return Result<NodeDto>.Failure(NodeErrors.NotFound);
        }
        dbContext.Nodes.Remove(node);
        await dbContext.SaveChangesAsync();
        return Result<NodeDto>.Success(NodeDto.Create(node));
    }

    public async Task<Result<PageResult<NodeDto>>> ListAsync(int limit, int offset)
    {
        var allowedIds = currentUser
            .MatchPermissionTemplate("admin.node.{}.get")
            .Concat(currentUser.MatchPermissionTemplate("admin.node.{}.get.secure"))
            .Distinct()
            .ToArray();
        var query = dbContext.Nodes.AsNoTracking();
        if (!allowedIds.Contains("*"))
        {
            var ids = allowedIds.Select(id => Guid.TryParse(id, out var guid) ? (Guid?)guid : null)
                .OfType<Guid>()
                .ToArray();
            query = query.Where(node => ids.Contains(node.Id));
        }
        var total = await query.CountAsync();
        var nodes = await query.OrderBy(node => node.Name).Skip(offset).Take(limit).ToListAsync();
        return Result<PageResult<NodeDto>>.Success(new(offset, limit, total, nodes.Count, nodes.ConvertAll(node => NodeDto.Create(node))));
    }
}