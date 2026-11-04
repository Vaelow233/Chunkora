namespace Chunkora.Agent.Utils;

public static class Log
{
    private static ILoggerFactory? _factory;

    public static void Initialize(ILoggerFactory factory)
    {
        _factory = factory;
    }

    public static ILogger<T> For<T>()
    {
        return (_factory ?? throw new InvalidOperationException("LoggerFactory not initialized")).CreateLogger<T>();
    }
}