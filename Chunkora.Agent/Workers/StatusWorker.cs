using Chunkora.Agent.Services;
using Grpc.Core;

namespace Chunkora.Agent.Workers;

public class StatusWorker(ILogger<StatusWorker> logger, StatusService statusService) : BackgroundService
{
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                await statusService.RefreshAsync(stoppingToken);
            }
            catch (RpcException e) when (e.StatusCode == StatusCode.Unavailable)
            {
                logger.LogWarning(e, "Failed to connect to the server to send metrics data.");
            }
            await Task.Delay(30_000, stoppingToken);
        }
    }
}