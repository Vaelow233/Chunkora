using Chunkora.Server.Dtos;
using Chunkora.Server.Requests;
using Chunkora.Server.Services;
using Chunkora.Server.Utils;
using Microsoft.AspNetCore.Mvc;

namespace Chunkora.Server.Controllers;

[ApiController]
[Route("api/nodes")]
public class NodeController(ILogger<NodeController> logger, NodeService nodeService, NodeMetricsService metricsService) : ControllerBase
{
    [HttpPost]
    public async Task<ActionResult<Result<NodeDto>>> Create(CreateNodeRequest request)
    {
        var result = await nodeService.CreateAsync(request);
        return HandleResult(result);
    }
    
    [HttpPut]
    public async Task<ActionResult<Result<NodeDto>>> Update(UpdateNodeRequest request)
    {
        var result = await nodeService.UpdateAsync(request);
        return HandleResult(result);
    }

    [HttpGet("{id:guid}")]
    public async Task<ActionResult<Result<NodeDto>>> Get(Guid id)
    {
        var result = await nodeService.GetAsync(id);
        return HandleResult(result);
    }

    [HttpGet("{id:guid}/secure")]
    public async Task<ActionResult<Result<NodeSecureDto>>> GetSecure(Guid id)
    {
        var result = await nodeService.GetSecureAsync(id);
        return HandleResult(result);
    }
    
    [HttpDelete("{id:guid}")]
    public async Task<ActionResult<Result<NodeDto>>> Delete(Guid id)
    {
        var result = await nodeService.DeleteAsync(id);
        return HandleResult(result);
    }

    [HttpGet]
    public async Task<ActionResult<Result<PageResult<NodeDto>>>> List(int limit, int offset)
    {
        var result = await nodeService.ListAsync(limit, offset);
        return HandleResult(result);
    }

    [HttpGet("{id:guid}/metrics/latest")]
    public async Task<ActionResult<Result<NodeMetricsDto?>>> GetLatestMetrics(Guid id)
    {
        var result = await metricsService.GetLatestAsync(id);
        return HandleResult(result);
    }

    [HttpGet("{id:guid}/metrics")]
    public async Task<ActionResult<Result<PageResult<NodeMetricsDto>>>> GetMetrics(Guid id, DateTime from, DateTime to, int limit, int offset)
    {
        var result = await metricsService.GetAsync(id, from, to, limit, offset);
        return HandleResult(result);
    }

    [HttpGet("{id:guid}/status")]
    public async Task<ActionResult<Result<NodeStatusDto>>> GetStatus(Guid id)
    {
        var result = await metricsService.GetConnectionStatusAsync(id);
        return HandleResult(result);
    }
    
    private ActionResult<Result<T>> HandleResult<T>(Result<T> result)
    {
        return result.Error?.Identifier switch
        {
            null => Ok(result),
            "node.name_exists" => Conflict(result),
            "node.not_found" => NotFound(result),
            _ => BadRequest(result)
        };
    }
}