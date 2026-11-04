using Chunkora.Server.Agents.Managers;
using Chunkora.Server.Authentication;
using Chunkora.Server.Dtos;
using Chunkora.Server.Errors;
using Chunkora.Server.Utils;
using Microsoft.EntityFrameworkCore;

namespace Chunkora.Server.Services;

public class NodeMetricsService(
    ILogger<NodeService> logger, 
    ICurrentUser currentUser, 
    InstanceConnectionManager instanceConnManager,
    InstanceOperationConnectionManager instanceOperationConnManager,
    GlobalDatabaseContext dbContext)
{
    public async Task<Result<NodeMetricsDto?>> GetLatestAsync(Guid nodeId)
    {
        if (!currentUser.HasPermission($"admin.node.{nodeId}.metrics"))
        {
            return Result<NodeMetricsDto?>.Failure(UserErrors.NoPermission);
        }
        var metrics = await dbContext.NodeMetrics
            .Where(metrics => metrics.NodeId == nodeId)
            .OrderByDescending(metrics => metrics.CreatedAt)
            .FirstOrDefaultAsync();
        return Result<NodeMetricsDto?>.Success(metrics is null ? null : NodeMetricsDto.Create(metrics));
    }

    public async Task<Result<PageResult<NodeMetricsDto>>> GetAsync(Guid nodeId, DateTime from, DateTime to, int limit, int offset)
    {
        if (!currentUser.HasPermission($"admin.node.{nodeId}.metrics"))
        {
            return Result<PageResult<NodeMetricsDto>>.Failure(UserErrors.NoPermission);
        }
        var count = await dbContext.NodeMetrics
            .Where(metrics => metrics.NodeId == nodeId && metrics.CreatedAt >= from && metrics.CreatedAt <= to)
            .OrderByDescending(metrics => metrics.CreatedAt)
            .CountAsync();
        var metricsList = await dbContext.NodeMetrics
            .Where(metrics => metrics.NodeId == nodeId && metrics.CreatedAt >= from && metrics.CreatedAt <= to)
            .OrderByDescending(metrics => metrics.CreatedAt)
            .Skip(offset)
            .Take(limit)
            .ToListAsync();
        return Result<PageResult<NodeMetricsDto>>.Success(new(offset, limit, count, 
            metricsList.Count, metricsList.ConvertAll(metrics => NodeMetricsDto.Create(metrics))));
    }

    public async Task<Result<NodeStatusDto>> GetConnectionStatusAsync(Guid nodeId)
    {
        if (!currentUser.HasPermission($"admin.node.{nodeId}.status"))
        {
            return Result<NodeStatusDto>.Failure(UserErrors.NoPermission);
        }
        return Result<NodeStatusDto>.Success(new NodeStatusDto
        {
            NodeId = nodeId,
            InstanceConnected = instanceConnManager.IsNodeConnected(nodeId),
            InstanceOperationConnected = await instanceOperationConnManager.IsNodeConnectedAsync(nodeId)
        });
    }
}