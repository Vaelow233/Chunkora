using System.Runtime.InteropServices;
using Chunkora.Agent.Configuration;
using Chunkora.Contracts.Agent.V1;
using Grpc.Core;
using Hardware.Info;
using Microsoft.Extensions.Options;

namespace Chunkora.Agent.Services;

public class StatusService(Contracts.Agent.V1.StatusService.StatusServiceClient statusClient, IOptions<AgentOptions> agentOptions)
{
    private readonly HardwareInfo _hardware = new();
    private readonly LinkedList<SystemMetrics> _metrics = new();
    
    private readonly Lock _lock = new();

    public async Task RefreshAsync(CancellationToken cancellationToken = default)
    {
        _hardware.RefreshCPUList();
        _hardware.RefreshMemoryStatus();
        var metrics = new SystemMetrics(
            _hardware.CpuList.Average(x => (double) x.PercentProcessorTime), 
            _hardware.MemoryStatus.TotalPhysical - _hardware.MemoryStatus.AvailablePhysical, 
            _hardware.MemoryStatus.TotalPhysical
        );
        lock (_lock)
        {
            _metrics.AddLast(metrics);
            if (_metrics.Count > 360)
            {
                _metrics.RemoveFirst();
            }
        }
        
        await SyncLatestAsync(metrics, cancellationToken);
    }

    private async Task SyncLatestAsync(SystemMetrics metrics, CancellationToken cancellationToken = default)
    {
        var request = new Overview
        {
            RequestId = Guid.NewGuid().ToString("N"),
            Architecture = RuntimeInformation.OSArchitecture.ToString(),
            OsType = GetOSPlatform(),
            Hostname = Environment.MachineName,
            OsDetail = RuntimeInformation.OSDescription,
            CpuUsage = metrics?.CpuUsage ?? 0,
            MemoryUsedBytes = metrics?.MemoryUsed ?? 0,
            MemoryTotalBytes = metrics?.MemoryTotal ?? 0,
            Version = agentOptions.Value.Version
        };
        var metadata = new Metadata();
        metadata.Add("Authorization", "Bearer " + agentOptions.Value.Token);
        await statusClient.NotifyOverviewAsync(request, cancellationToken: cancellationToken, headers: metadata);
    }
    
    private static string GetOSPlatform()
    {
        if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
        {
            return "Windows";
        }
        if (RuntimeInformation.IsOSPlatform(OSPlatform.Linux))
        {
            return "Linux";
        }
        if (RuntimeInformation.IsOSPlatform(OSPlatform.OSX))
        {
            return "OSX";
        }
        if (RuntimeInformation.IsOSPlatform(OSPlatform.FreeBSD))
        {
            return "FreeBSD";
        }

        return "Unknown";
    }
}

public record SystemMetrics(double CpuUsage, ulong MemoryUsed, ulong MemoryTotal);