namespace Chunkora.Agent.Configuration;

public sealed class AgentOptions
{
    public const string SectionName = "Agent";
    public const string DefaultServerAddress = "http://localhost:5201";

    public uint Version { get; set; } = 1;

    public string ServerAddress { get; set; } = DefaultServerAddress;

    public string Token { get; set; } = string.Empty;

    public int ReconnectDelaySeconds { get; set; } = 5;
}