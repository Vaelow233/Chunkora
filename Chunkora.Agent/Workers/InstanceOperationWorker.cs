using Chunkora.Agent.Configuration;
using Chunkora.Agent.Services;
using Grpc.Core;
using Microsoft.Extensions.Options;

namespace Chunkora.Agent.Workers;

public class InstanceOperationWorker(
    ILogger<InstanceOperationWorker> logger, 
    IOptions<AgentOptions> agentOptions, 
    InstanceOperationService operationService) : BackgroundService
{
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        try
        {
            while (!stoppingToken.IsCancellationRequested)
            {
                try
                {
                    await operationService.SyncInstanceOperationsAsync(stoppingToken);

                    stoppingToken.ThrowIfCancellationRequested();
                    logger.LogWarning("Instance operations stream was stopped due to stop request.");
                }
                catch (RpcException e) when (e.StatusCode == StatusCode.Unavailable)
                {
                    logger.LogWarning(e, "Failed to connect to the server to create instance operations stream. Trying to reconnect in {} seconds.", 
                        agentOptions.Value.ReconnectDelaySeconds);
                }
                await Task.Delay(TimeSpan.FromSeconds(agentOptions.Value.ReconnectDelaySeconds), stoppingToken);
            }
        } 
        catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested) 
        {
            
        }
        catch (RpcException e) when (stoppingToken.IsCancellationRequested && e.StatusCode == StatusCode.Cancelled)
        {
            
        }
    }
}