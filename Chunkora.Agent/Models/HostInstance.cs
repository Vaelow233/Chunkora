using System.CommandLine.Parsing;
using System.Diagnostics;
using Chunkora.Contracts.Agent.V1;
using StreamWriter = System.IO.StreamWriter;

namespace Chunkora.Agent.Models;

public class HostInstance : IActionableInstance
{
    public SemaphoreSlim LifecycleLock { get; } = new(1, 1);
    private Instance _instance;
    private readonly SemaphoreSlim _instanceLock = new(1, 1);
    private Process? _process;
    private StreamReader? _stdout;
    private StreamReader? _stderr;
    private StreamWriter? _stdin;
    private CancellationTokenSource _cts = new();
    private const int LogCapacity = 10_000;
    private readonly Queue<string> _logs = new();
    private int _logTotal;
    private readonly SemaphoreSlim _logsLock = new(1, 1);
    private bool _stopped = true;
    private Task _stdoutTask = Task.CompletedTask;
    private Task _stderrTask = Task.CompletedTask;
    private InstanceStatus _status = InstanceStatus.Stopped;
    private readonly Func<Instance, InstanceStatus, Task> _statusCallback;

    public HostInstance(Instance instance, Func<Instance, InstanceStatus, Task> statusCallback)
    {
        _instance = instance;
        _statusCallback = statusCallback;
    }

    private void UpdateStatus(InstanceStatus status)
    {
        _status = status; 
        _statusCallback.Invoke(_instance, status);
    }

    private void AppendLog(string line)
    {
        _logsLock.Wait();
        _logs.Enqueue(line);
        _logTotal++;
        if (_logs.Count > LogCapacity)
        {
            _logs.Dequeue();
        }

        _logsLock.Release();
    }

    public async Task StartAsync(CancellationToken cancellationToken = default)
    {
        Start();
    }

    public void Start()
    {
        if (!_stopped)
        {
            return;
        }

        UpdateStatus(InstanceStatus.Starting);
        _instanceLock.Wait();
        var launchCommand = _instance.LaunchCommand;
        var path = _instance.Path;
        _instanceLock.Release();
        var tokens = CommandLineParser.SplitCommandLine(launchCommand).ToArray();
        if (tokens.Length == 0 || string.IsNullOrWhiteSpace(tokens[0]))
        {
            throw new ArgumentException("The launch command should be non-empty.");
        }

        var startInfo = new ProcessStartInfo(tokens[0])
        {
            WorkingDirectory = path,
            UseShellExecute = false,
            RedirectStandardInput = true,
            RedirectStandardOutput = true,
            RedirectStandardError = true,
        };
        foreach (var argument in tokens.Skip(1))
        {
            startInfo.ArgumentList.Add(argument);
        }

        _process = Process.Start(startInfo);
        if (_process == null)
        {
            throw new Exception("Could not start process: " + startInfo.FileName);
        }

        _stdout = _process.StandardOutput;
        _stderr = _process.StandardError;
        _stdin = _process.StandardInput;
        _stdoutTask = ReadLinesAsync(_process.StandardOutput, _cts.Token);
        _stderrTask = ReadLinesAsync(_process.StandardError, _cts.Token);
        _stopped = false;
        UpdateStatus(InstanceStatus.Running);
    }

    public async Task StopAsync(CancellationToken cancellationToken = default)
    {
        using var timeout = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
        timeout.CancelAfter(TimeSpan.FromSeconds(30));
        if (_stopped)
        {
            return;
        }

        if (_process is not null && !_process.HasExited)
        {
            UpdateStatus(InstanceStatus.Stopping);
            await WriteAsync(_instance.StopCommand, timeout.Token);
            await _process.WaitForExitAsync(timeout.Token);
        }

        await CleanupProcessAsync();
        UpdateStatus(InstanceStatus.Stopped);
        _stopped = true;
    }

    private async Task CleanupProcessAsync()
    {
        await _cts.CancelAsync();
        await Task.WhenAll(_stdoutTask, _stderrTask);
        if (_stdin is not null)
        {
            await _stdin.DisposeAsync();
        }

        _stdout?.Dispose();
        _stderr?.Dispose();
        _process?.Dispose();
        _stdin = null;
        _stdout = null;
        _stderr = null;
        _process = null;
        UpdateStatus(InstanceStatus.Stopped);
        _stopped = true;
        _cts.Dispose();
        _cts = new CancellationTokenSource();
    }

    public async Task DisposeAsync()
    {
        if (_process is not null && !_process.HasExited)
        {
            _process.Kill();
        }

        await CleanupProcessAsync();
        UpdateStatus(InstanceStatus.Stopped);
        _stopped = true;
    }

    public async Task RestartAsync(CancellationToken cancellationToken = default)
    {
        await StopAsync(cancellationToken);
        Start();
    }

    private async Task ReadLinesAsync(StreamReader reader, CancellationToken token)
    {
        try
        {
            while (await reader.ReadLineAsync(token) is { } line)
            {
                AppendLog(line);
            }
        }
        catch (OperationCanceledException) when (token.IsCancellationRequested)
        {

        }
    }

    public async Task WriteAsync(string content, CancellationToken cancellationToken = default)
    {
        if (_cts.Token.IsCancellationRequested || _stdin is null)
        {
            return;
        }

        await _stdin.WriteLineAsync(content.AsMemory(), cancellationToken);
        await _stdin.FlushAsync(cancellationToken);
    }

    public List<string> GetLogs()
    {
        _logsLock.Wait();
        var logs = new List<string>(_logs);
        _logsLock.Release();
        return logs;
    }

    public List<string> GetLogs(int limit, int offset, out int total)
    {
        _logsLock.Wait();
        try
        {
            total = _logTotal;
            var firstOffset = total - _logs.Count;
            return _logs
                .Skip(Math.Max(0, offset - firstOffset))
                .Take(limit)
                .ToList();
        }
        finally
        {
            _logsLock.Release();
        }
    }

    public async Task UpdateAsync(Instance instance, CancellationToken cancellationToken = default)
    {
        await _instanceLock.WaitAsync(cancellationToken);
        _instance = instance;
        _instanceLock.Release();
    }

    public Instance GetInstance()
    {
        _instanceLock.Wait();
        var instance = new Instance(_instance);
        _instanceLock.Release();
        return instance;
    }
}