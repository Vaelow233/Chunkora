using System.Text;
using Chunkora.Agent.Models;
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
    private readonly Func<Instance, InstanceStatus, Task> _statusCallback;
    private readonly IServiceScopeFactory _scopeFactory;
    
    private static string ToDockerProtocol(PortProtocol p) => p == PortProtocol.Udp ? "udp" : "tcp";
    
    private DockerInstance(string id, Instance instance, Func<Instance, InstanceStatus, Task> statusCallback, IServiceScopeFactory scopeFactory)
    {
        _id = id;
        _instance = instance;
        _statusCallback = statusCallback;
        _scopeFactory = scopeFactory;
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
        _statusCallback.Invoke(_instance, status);
    }
    
    public async Task StartAsync(CancellationToken cancellationToken = default)
    {
        UpdateStatus(InstanceStatus.Starting);
        await EnsureContainerExistsAsync();
        await using var scope = _scopeFactory.CreateAsyncScope();
        var docker = scope.ServiceProvider.GetRequiredService<DockerClient>();
        var started = await docker.Containers.StartContainerAsync(_id, new ContainerStartParameters(), cancellationToken);
        if (!started)
        {
            throw new Exception("Could not start the container");
        }
        UpdateStatus(InstanceStatus.Running);
    }

    public async Task StopAsync(CancellationToken cancellationToken = default)
    {
        UpdateStatus(InstanceStatus.Stopping);
        using var timeout = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
        timeout.CancelAfter(TimeSpan.FromSeconds(30));
        await EnsureContainerExistsAsync();
        await using var scope = _scopeFactory.CreateAsyncScope();
        var docker = scope.ServiceProvider.GetRequiredService<DockerClient>();
        var stopped = await docker.Containers.StopContainerAsync(_id, new ContainerStopParameters(), timeout.Token);
        if (!stopped)
        {
            throw new Exception("Could not stop the container");
        }
        UpdateStatus(InstanceStatus.Stopped);
    }

    public async Task RestartAsync(CancellationToken cancellationToken = default)
    {
        UpdateStatus(InstanceStatus.Restarting);
        using var timeout = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
        timeout.CancelAfter(TimeSpan.FromSeconds(30));
        await EnsureContainerExistsAsync();
        await using var scope = _scopeFactory.CreateAsyncScope();
        var docker = scope.ServiceProvider.GetRequiredService<DockerClient>();
        await docker.Containers.RestartContainerAsync(_id, new ContainerRestartParameters(), timeout.Token);
        UpdateStatus(InstanceStatus.Running);
    }

    public async Task DisposeAsync()
    {
        await EnsureContainerExistsAsync();
        await using var scope = _scopeFactory.CreateAsyncScope();
        var docker = scope.ServiceProvider.GetRequiredService<DockerClient>();
        await docker.Containers.KillContainerAsync(_id, new ContainerKillParameters());
        UpdateStatus(InstanceStatus.Stopped);
    }

    public async Task DeleteAsync()
    {
        await EnsureContainerExistsAsync();
        await using var scope = _scopeFactory.CreateAsyncScope();
        var docker = scope.ServiceProvider.GetRequiredService<DockerClient>();
        await docker.Containers.KillContainerAsync(_id, new ContainerKillParameters());
        await docker.Containers.RemoveContainerAsync(_id, new ContainerRemoveParameters());
        UpdateStatus(InstanceStatus.Stopped);
    }

    public async Task WriteAsync(string content, CancellationToken cancellationToken = default)
    {
        await EnsureContainerExistsAsync();
        await using var scope = _scopeFactory.CreateAsyncScope();
        var docker = scope.ServiceProvider.GetRequiredService<DockerClient>();
        using var stream = await docker.Containers.AttachContainerAsync(_id, true, new ContainerAttachParameters(), cancellationToken);
        if (stream == null)
        {
            throw new Exception("Failed to attach the container and get the stream");
        }
        var bytes = Encoding.UTF8.GetBytes(content);
        await stream.WriteAsync(bytes, 0, bytes.Length, cancellationToken);
    }

    public async Task<KeyValuePair<int, List<string>>> GetLogsAsync(int limit, int offset, CancellationToken cancellationToken = default)
    {
        await EnsureContainerExistsAsync();
        await using var scope = _scopeFactory.CreateAsyncScope();
        var docker = scope.ServiceProvider.GetRequiredService<DockerClient>();
        using var stream = await docker.Containers.GetContainerLogsAsync(_id, true, new ContainerLogsParameters 
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
        var (stdout, _) = await stream.ReadOutputToEndAsync(cancellationToken);
        var lines = new List<string>();
        using var reader = new StringReader(stdout);
        string? line;
        while ((line = await reader.ReadLineAsync(cancellationToken)) != null)
        {
            lines.Add(line);
        }
        return new KeyValuePair<int, List<string>>(lines.Count, lines.Skip(offset).Take(limit).ToList());
    }

    public async Task UpdateAsync(Instance instance, CancellationToken cancellationToken = default)
    {
        await using var scope = _scopeFactory.CreateAsyncScope();
        var docker = scope.ServiceProvider.GetRequiredService<DockerClient>();
        _instance = instance;
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
        // Create new container
        var parameters = new CreateContainerParameters
        {
            Name = config.Name,
            Image = config.Image,
            Cmd = instance.LaunchCommand.Split(" "), // TODO: change this parse method
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
            }
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