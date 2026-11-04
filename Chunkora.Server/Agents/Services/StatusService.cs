using System.Security.Claims;
using Chunkora.Contracts.Agent.V1;
using Chunkora.Server.Models;
using Grpc.Core;
using Microsoft.AspNetCore.Authorization;

namespace Chunkora.Server.Agents.Services;

[Authorize(Policy = "AgentOnly")]
public class StatusService(GlobalDatabaseContext dbContext) : Contracts.Agent.V1.StatusService.StatusServiceBase
{
    public override async Task<OverviewResponse> NotifyOverview(Overview request, ServerCallContext context)
    {
        var principal = context.GetHttpContext().User;
        var nodeId = new Guid(principal.FindFirstValue(ClaimTypes.NameIdentifier)!);
        var nodeMetrics = new NodeMetrics
        {
            NodeId = nodeId,
            Version = request.Version,
            Architecture = request.Architecture,
            OsType = request.OsType,
            Hostname = request.Hostname,
            OsDetail = request.OsDetail,
            CpuUsage = request.CpuUsage,
            MemoryUsedBytes = request.MemoryUsedBytes,
            MemoryTotalBytes = request.MemoryTotalBytes,
            CreatedAt = DateTime.UtcNow
        };
        dbContext.NodeMetrics.Add(nodeMetrics);
        await dbContext.SaveChangesAsync();
        return new OverviewResponse
        {
            RequestId = request.RequestId
        };
    }
}