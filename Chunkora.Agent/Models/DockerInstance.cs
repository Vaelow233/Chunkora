using System.CommandLine.Parsing;
using System.Text;
using System.Threading.Channels;
using Chunkora.Contracts.Agent.V1;
using Docker.DotNet;
using Docker.DotNet.Models;
using RestartPolicy = Chunkora.Contracts.Agent.V1.RestartPolicy;

namespace Chunkora.Agent.Models;

public class DockerInstance : IActionableInstance
{
    public SemaphoreSlim LifecycleLock { get; } = new(1, 1);
    private Instance _instance;
    private readonly SemaphoreSlim _instanceLock = new(1, 1);
    private string _id;
    private readonly IServiceScopeFactory _scopeFactory;
    private Task _monitorTask = Task.CompletedTask;
    private readonly CancellationTokenSource _monitorCts = new();
    private readonly Channel<Message> _events = Channel.CreateUnbounded<Message>(new UnboundedChannelOptions { SingleReader = true });
    private InstanceStatus _status = InstanceStatus.Stopped;
    private readonly Func<Instance, InstanceStatus, Task> _statusCallback;
    private enum Intent { None, Stopping, Restarting }
    private volatile Intent _intent;
    
    private static readonly HashSet<string> LifecycleActions = ["start", "die", "oom", "restart", "pause", "unpause", "destroy"];
    
    private static string ToDockerProtocol(PortProtocol p) => p == PortProtocol.Udp ? "udp" : "tcp";
    
    private DockerInstance(string id, Instance instance, Func<Instance, InstanceStatus, Task> statusCallback, IServiceScopeFactory scopeFactory)
    {
        _id = id;
        _instance = instance;
        _statusCallback = statusCallback;
        _scopeFactory = scopeFactory;
        _monitorTask = Task.WhenAll(ProduceAsync(_monitorCts.Token), ConsumeAsync(_monitorCts.Token));
    }
    
    private async Task ProduceAsync(CancellationToken ct)
    {
        while (!ct.IsCancellationRequested)
        {
            try
            {
                await using var scope = _scopeFactory.CreateAsyncScope();
                var docker = scope.ServiceProvider.GetRequiredService<DockerClient>();
                var parameters = new ContainerEventsParameters
                {
                    Filters = new Dictionary<string, IDictionary<string, bool>>
                    {
                        ["type"] = new Dictionary<string, bool> { ["container"] = true },
                        ["container"] = new Dictionary<string, bool> { [_id] = true },
                    }
                };
                using var iterCts = CancellationTokenSource.CreateLinkedTokenSource(ct);
                var stream = docker.System.MonitorEventsAsync(parameters,
                    new Progress<Message>(m => _events.Writer.TryWrite(m)), iterCts.Token);
                try
                {
                    await SyncStatusAsync(iterCts.Token);
                    await stream;
                }
                finally
                {
                    await iterCts.CancelAsync();
                    await Task.WhenAny(stream);
                }
            }
            catch (OperationCanceledException) when (ct.IsCancellationRequested)
            {
                break;
            }
            catch (Exception)
            {
                // TODO: log the exception
            }
            await Task.Delay(TimeSpan.FromSeconds(1), ct);
        }
    }

    private async Task ConsumeAsync(CancellationToken ct)
    {
        try
        {
            await foreach (var msg in _events.Reader.ReadAllAsync(ct))
            {
                if (LifecycleActions.Contains(msg.Action) || msg.Action?.StartsWith("health_status") == true)
                {
                    try
                    {
                        await SyncStatusAsync(ct);
                    }
                    catch (Exception e)
                    {
                        // TODO: log the exception
                    }
                }
            }
        }
        catch (OperationCanceledException) { }
    }
    
    public Instance GetInstance()
    {
        _instanceLock.Wait();
        var instance = new Instance(_instance);
        _instanceLock.Release();
        return instance;
    }
    
    private void UpdateStatus(InstanceStatus status)
    {
        _status = status;
        _instanceLock.Wait();
        _statusCallback.Invoke(_instance, status);
        _instanceLock.Release();
    }

    private void UpdateStatus(ContainerState state)
    {
        if (_intent == Intent.Restarting)
        {
            UpdateStatus(InstanceStatus.Restarting);
            return;
        }
        if (_intent == Intent.Stopping && state.Running)
        {
            UpdateStatus(InstanceStatus.Stopping);
            return;
        }
        if (state.Restarting)
        {
            UpdateStatus(InstanceStatus.Restarting);
            return;
        }
        if (state.Running)
        {
            UpdateStatus(InstanceStatus.Running);
            return;
        }
        UpdateStatus(InstanceStatus.Stopped);
    }
    
    private async Task SyncStatusAsync(CancellationToken ct = default)
    {
        var inspectResponse = await FindContainerAsync(_id, _scopeFactory, ct);
        if (inspectResponse != null)
        {
            _id = inspectResponse.ID;
            UpdateStatus(inspectResponse.State);
        }
        else
        {
            UpdateStatus(InstanceStatus.Stopped);
        }
    }
    
    public async Task StartAsync(CancellationToken cancellationToken = default)
    {
        _intent = Intent.None;
        UpdateStatus(InstanceStatus.Starting);
        try
        {
            await EnsureContainerExistsAsync();
            await using var scope = _scopeFactory.CreateAsyncScope();
            var docker = scope.ServiceProvider.GetRequiredService<DockerClient>();
            await docker.Containers.UpdateContainerAsync(_id, new ContainerUpdateParameters { 
                RestartPolicy = new global::Docker.DotNet.Models.RestartPolicy 
                    { Name = ParseRestartPolicy(_instance.AdditionalConfig.DockerConfig.RestartPolicy) }
            }, cancellationToken); // To ensure that the container can be stopped (instead of restarting by docker)
            await docker.Containers.StartContainerAsync(_id, new ContainerStartParameters(), cancellationToken);
        }
        finally
        {
            await SyncStatusAsync(cancellationToken);
        }
    }

    public async Task StopAsync(CancellationToken cancellationToken = default)
    {
        _intent = Intent.Stopping;
        UpdateStatus(InstanceStatus.Stopping);
        try
        {
            using var timeout = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
            timeout.CancelAfter(TimeSpan.FromSeconds(30));
            await EnsureContainerExistsAsync();
            await using var scope = _scopeFactory.CreateAsyncScope();
            var docker = scope.ServiceProvider.GetRequiredService<DockerClient>();
            await docker.Containers.UpdateContainerAsync(_id, new ContainerUpdateParameters { 
                RestartPolicy = new global::Docker.DotNet.Models.RestartPolicy { Name = RestartPolicyKind.No } 
            }, cancellationToken);
            await _instanceLock.WaitAsync(cancellationToken);
            var command = _instance.StopCommand;
            _instanceLock.Release();
            await WriteAsync(command, cancellationToken);
        }
        finally
        {
            _intent = Intent.None;
            await SyncStatusAsync(cancellationToken);
        }
    }

    public async Task RestartAsync(CancellationToken cancellationToken = default)
    {
        _intent = Intent.Restarting;
        UpdateStatus(InstanceStatus.Restarting);
        try
        {
            using var timeout = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
            timeout.CancelAfter(TimeSpan.FromSeconds(30));
            await EnsureContainerExistsAsync();
            await using var scope = _scopeFactory.CreateAsyncScope();
            var docker = scope.ServiceProvider.GetRequiredService<DockerClient>();
            await docker.Containers.RestartContainerAsync(_id, new ContainerRestartParameters(), timeout.Token);
        }
        finally
        {
            _intent = Intent.None;
            await SyncStatusAsync(cancellationToken);
        }
    }

    public async Task DisposeAsync()
    {
        await _monitorCts.CancelAsync();
        await EnsureContainerExistsAsync();
        await using var scope = _scopeFactory.CreateAsyncScope();
        var docker = scope.ServiceProvider.GetRequiredService<DockerClient>();
        await docker.Containers.KillContainerAsync(_id, new ContainerKillParameters());
        UpdateStatus(InstanceStatus.Stopped);
    }

    public async Task DeleteAsync()
    {
        await _monitorCts.CancelAsync();
        await EnsureContainerExistsAsync();
        await using var scope = _scopeFactory.CreateAsyncScope();
        var docker = scope.ServiceProvider.GetRequiredService<DockerClient>();
        await docker.Containers.KillContainerAsync(_id, new ContainerKillParameters());
        await _monitorCts.CancelAsync();
        UpdateStatus(InstanceStatus.Stopped);
        await docker.Containers.RemoveContainerAsync(_id, new ContainerRemoveParameters());
    }

    public async Task WriteAsync(string content, CancellationToken cancellationToken = default)
    {
        await EnsureContainerExistsAsync();
        await using var scope = _scopeFactory.CreateAsyncScope();
        var docker = scope.ServiceProvider.GetRequiredService<DockerClient>();
        using var stream = await docker.Containers.AttachContainerAsync(_id, false, 
            new ContainerAttachParameters { Stream = true, Stdin = true }, cancellationToken);
        if (stream == null)
        {
            throw new Exception("Failed to attach the container and get the stream");
        }
        var bytes = Encoding.UTF8.GetBytes(content + "\n");
        await stream.WriteAsync(bytes, 0, bytes.Length, cancellationToken);
    }

    public async Task<KeyValuePair<int, List<string>>> GetLogsAsync(int limit, int offset, CancellationToken cancellationToken = default)
    {
        await EnsureContainerExistsAsync();
        await using var scope = _scopeFactory.CreateAsyncScope();
        var docker = scope.ServiceProvider.GetRequiredService<DockerClient>();
        using var stream = await docker.Containers.GetContainerLogsAsync(_id, false, new ContainerLogsParameters 
            {
                ShowStdout = true,
                ShowStderr = true,
                Follow = false,
                Tail = "all"
            }, cancellationToken
        );
        if (stream == null)
        {
            throw new Exception("Failed to get the logs of the container");
        }
        var sb = new StringBuilder();
        var decoder = Encoding.UTF8.GetDecoder();
        var buf = new byte[8192];
        var chars = new char[8192];
        while (true)
        {
            var r = await stream.ReadOutputAsync(buf, 0, buf.Length, cancellationToken);
            if (r.EOF) break;
            var n = decoder.GetChars(buf, 0, r.Count, chars, 0);
            sb.Append(chars, 0, n);
        }
        var lines = new List<string>();
        using var reader = new StringReader(sb.ToString());
        string? line;
        while ((line = await reader.ReadLineAsync(cancellationToken)) != null)
        {
            lines.Add(line);
        }
        return new KeyValuePair<int, List<string>>(lines.Count, lines.Skip(offset).Take(limit).ToList());
    }

    public async Task UpdateConfigAsync(Instance instance, CancellationToken cancellationToken = default)
    {
        await using var scope = _scopeFactory.CreateAsyncScope();
        var docker = scope.ServiceProvider.GetRequiredService<DockerClient>();
        await _instanceLock.WaitAsync(cancellationToken);
        _instance = instance;
        _instanceLock.Release();
        try
        {
            var container = await docker.Containers.InspectContainerAsync(_id, cancellationToken);
            if (container.State.Running)
            {
                /*
                 * TODO:
                 * Because the update method only supports updating few parameters, so we should destroy the container first
                 * when it's closed and recreate it. We need to consider when the instance is running, how to handle it.
                 * Currently, if the container is running, the request will be ignored. Just the _instance will be changed.
                 */
                return;
            }
            await docker.Containers.RemoveContainerAsync(_id, new ContainerRemoveParameters(), cancellationToken);
        }
        catch (DockerContainerNotFoundException)
        {
            
        }
        await EnsureContainerExistsAsync();
    }

    // id & name are both supported
    public static async Task<ContainerInspectResponse?> FindContainerAsync(string id, IServiceScopeFactory scopeFactory, 
        CancellationToken cancellationToken = default)
    {
        await using var scope = scopeFactory.CreateAsyncScope();
        var docker = scope.ServiceProvider.GetRequiredService<DockerClient>();
        try
        {
            return await docker.Containers.InspectContainerAsync(id, cancellationToken);
        }
        catch (DockerContainerNotFoundException)
        {
            return null;
        }
    }

    private static CreateContainerParameters BuildCreateParameters(Instance instance)
    {
        var config = instance.AdditionalConfig.DockerConfig;
        // Handle port bindings
        var exposedPorts = new Dictionary<string, EmptyStruct>();
        var portBindings = new Dictionary<string, IList<PortBinding>>();
        foreach (var binding in config.PortBindings)
        {
            var key = $"{binding.ContainerPort}/{ToDockerProtocol(binding.ContainerProtocol)}";
            exposedPorts[key] = default;
            if (!portBindings.TryGetValue(key, out var list))
            {
                portBindings[key] = list = new List<PortBinding>();
            }
            list.Add(new PortBinding { HostIP = binding.HostIp, HostPort = binding.HostPort.ToString() });
        }
        // Handle restart policy mapping
        var restartPolicy = ParseRestartPolicy(config.RestartPolicy);
        // Handle launch command parsing
        var commands = CommandLineParser.SplitCommandLine(instance.LaunchCommand).ToArray();
        // Create new container
        var parameters = new CreateContainerParameters
        {
            Name = config.Name,
            Image = config.Image,
            Cmd = commands.Length == 0 ? null : commands,
            Env = config.Environments.ToList(),
            WorkingDir = config.WorkingDir,
            Hostname = config.Hostname,
            ExposedPorts = exposedPorts,
            HostConfig = new HostConfig
            {
                NetworkMode = config.NetworkMode,
                Binds = config.FileBindings
                    .Select(b => $"{b.HostPath}:{b.ContainerPath}{(b.ReadOnly ? ":ro" : "")}")
                    .ToList(),
                RestartPolicy = new global::Docker.DotNet.Models.RestartPolicy
                {
                    Name = restartPolicy
                },
                PortBindings = portBindings,
                NanoCPUs = config.MaxNanoCpus,
                Memory = config.MaxMemoryBytes
            },
            OpenStdin = true,
            Tty = false
        };
        return parameters;
    }

    private async Task EnsureContainerExistsAsync()
    {
        var config = _instance.AdditionalConfig.DockerConfig;
        var inspectResponse = await FindContainerAsync(config.Name, _scopeFactory);
        // Check whether the container is existed
        if (inspectResponse != null)
        {
            return;
        }
        await using var scope = _scopeFactory.CreateAsyncScope();
        var docker = scope.ServiceProvider.GetRequiredService<DockerClient>();
        var response = await docker.Containers.CreateContainerAsync(BuildCreateParameters(_instance));
        _id = response.ID;
    }

    public static async Task<DockerInstance> GetOrCreateContainerAsync(Instance instance, 
        Func<Instance, InstanceStatus, Task> statusCallback, IServiceScopeFactory scopeFactory)
    {
        var config = instance.AdditionalConfig.DockerConfig;
        var inspectResponse = await FindContainerAsync(config.Name, scopeFactory);
        // Check whether the container has been created (exists)
        if (inspectResponse != null)
        {
            return new DockerInstance(inspectResponse.ID, instance, statusCallback, scopeFactory);
        }
        await using var scope = scopeFactory.CreateAsyncScope();
        var docker = scope.ServiceProvider.GetRequiredService<DockerClient>();
        var response = await docker.Containers.CreateContainerAsync(BuildCreateParameters(instance));
        return new DockerInstance(response.ID, instance, statusCallback, scopeFactory);
    }

    private static RestartPolicyKind ParseRestartPolicy(RestartPolicy policy) => policy switch
    {
        RestartPolicy.Undefined => RestartPolicyKind.Undefined,
        RestartPolicy.No => RestartPolicyKind.No,
        RestartPolicy.Always => RestartPolicyKind.Always,
        RestartPolicy.OnFailure => RestartPolicyKind.OnFailure,
        RestartPolicy.UnlessStopped => RestartPolicyKind.UnlessStopped,
        _ => RestartPolicyKind.Undefined
    };
}