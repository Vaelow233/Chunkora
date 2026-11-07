using System.Threading.Channels;
using Chunkora.Agent.Configuration;
using Chunkora.Contracts.Agent.V1;
using Grpc.Core;
using Microsoft.Extensions.Options;

namespace Chunkora.Agent.Services;

public class InstanceOperationService(
    ILogger<InstanceOperationService> logger,
    Chunkora.Contracts.Agent.V1.InstanceOperationService.InstanceOperationServiceClient operationClient,
    InstanceService instanceService,
    IOptions<AgentOptions> agentOptions)
{
    private async Task<InstanceOperationResponse> ExecuteOneAsync(InstanceOperationRequest request, CancellationToken cancellationToken)
    {
        var target = instanceService.GetInstance(request.InstanceId);
        if (target is null)
        {
            return Reply(request, InstanceOperationResult.InstanceNotFound);
        }
        await target.LifecycleLock.WaitAsync(cancellationToken);
        try
        {
            if (!ReferenceEquals(instanceService.GetInstance(request.InstanceId), target))
            {
                return Reply(request, InstanceOperationResult.InstanceNotFound);
            }
            switch (request.Type)
            {
                case InstanceOperationType.Start:
                    await target.StartAsync(cancellationToken);
                    break;
                case InstanceOperationType.Stop:
                    await target.StopAsync(cancellationToken);
                    break;
                case InstanceOperationType.Restart:
                    await target.RestartAsync(cancellationToken);
                    break;
                case InstanceOperationType.Kill:
                    await target.DisposeAsync();
                    break;
                case InstanceOperationType.Execute:
                    await target.WriteAsync(request.Command, cancellationToken);
                    break;
                default:
                    return Reply(request, InstanceOperationResult.TypeUnsupported);
            }
            return Reply(request, InstanceOperationResult.Success);
        }
        finally
        {
            target.LifecycleLock.Release();
        }
    }

    private static InstanceOperationResponse Reply(InstanceOperationRequest request, InstanceOperationResult code, string? error = null)
    {
        var response = new InstanceOperationResponse
        {
            RequestId = request.RequestId,
            InstanceId = request.InstanceId,
            Type = request.Type,
            Code = code,
        };
        if (error is not null)
        {
            response.Error = error;
        }
        return response;
    }

    
    public async Task SyncInstanceOperationsAsync(CancellationToken cancellationToken = default)
    {
        var initRequestId = Guid.NewGuid().ToString("N");
        var initialized = false;
        var ping = new AgentInstanceOperationMessage
        {
            Ping = new InstanceOperationStreamPing
            {
                RequestId = initRequestId
            }
        };
        var metadata = new Metadata();
        metadata.Add("Authorization", "Bearer " + agentOptions.Value.Token);
        using var session = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
        var token = session.Token;
        using var call = operationClient.ConnectInstanceOperations(cancellationToken: token, headers: metadata);
        using var writeLock = new SemaphoreSlim(1, 1);
        
        var queues = new Dictionary<string, Channel<InstanceOperationRequest>>();
        var workers = new List<Task>();
        
        async Task SendAsync(InstanceOperationResponse response)
        {
            await writeLock.WaitAsync(token);
            try
            {
                await call.RequestStream.WriteAsync(
                    new AgentInstanceOperationMessage { Response = response },
                    token);
            }
            finally
            {
                writeLock.Release();
            }
        }
        
        async Task RunWorkerAsync(ChannelReader<InstanceOperationRequest> reader)
        {
            try
            {
                await foreach (var request in reader.ReadAllAsync(token))
                {
                    InstanceOperationResponse response;
                    try
                    {
                        response = await ExecuteOneAsync(request, token);
                    }
                    catch (OperationCanceledException) when (token.IsCancellationRequested)
                    {
                        throw;
                    }
                    catch (Exception exception)
                    {
                        logger.LogError(exception, "Instance operation failed: {RequestId}", request.RequestId);
                        response = Reply(request, InstanceOperationResult.InstanceOperationFailed, exception.Message);
                    }
                    await SendAsync(response);
                }
            }
            catch (Exception) when (token.IsCancellationRequested)
            {
                
            }
            catch (Exception exception)
            {
                logger.LogError(exception, "An error occurred in the worker.");
                session.Cancel();
            }
        }

        try
        {
            await call.RequestStream.WriteAsync(ping, token);
            
            await foreach (var serverMessage in call.ResponseStream.ReadAllAsync(cancellationToken))
            {
                if (serverMessage.Pong != null && serverMessage.Pong.RequestId == initRequestId)
                {
                    logger.LogInformation("Server has pong the instance operation connection. It has been initialized.");
                    initialized = true;
                    continue;
                }

                if (serverMessage.Request is not { } request)
                {
                    continue;
                }
                
                if (!initialized)
                {
                    await SendAsync(Reply(request, InstanceOperationResult.Uninitialized));
                    continue;
                }
                
                if (!queues.TryGetValue(request.InstanceId, out var queue))
                {
                    queue = Channel.CreateBounded<InstanceOperationRequest>(
                        new BoundedChannelOptions(32)
                        {
                            SingleReader = true,
                            SingleWriter = true,
                            FullMode = BoundedChannelFullMode.Wait,
                            AllowSynchronousContinuations = false,
                        });

                    queues.Add(request.InstanceId, queue);
                    workers.Add(RunWorkerAsync(queue.Reader));
                }
                
                if (!queue.Writer.TryWrite(request))
                {
                    await SendAsync(Reply(request, InstanceOperationResult.InstanceOperationFailed, "The pending tasks of the instance is full."));
                }
            }
        }
        catch (Exception exception)
        {
            logger.LogError(exception, "An error occurred in the instance operation.");
        }
        finally
        {
            await session.CancelAsync();
            foreach (var queue in queues.Values)
            {
                queue.Writer.TryComplete();
            }
            await Task.WhenAll(workers);
        }
    }
}