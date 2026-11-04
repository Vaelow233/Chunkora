namespace Chunkora.Agent.Configuration;

public sealed class AgentOptions
{
    public const string SectionName = "Agent";

    public uint Version { get; set; } = 1;

    public string ServerAddress { get; set; } = "http://localhost:50051";

    public string Token { get; set; } = string.Empty;

    public int ReconnectDelaySeconds { get; set; } = 5;
}