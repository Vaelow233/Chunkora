using Chunkora.Server.Utils;

namespace Chunkora.Server.Errors;

public static class GlobalErrors
{
    public static Error OfException(Exception e)
    {
        return new Error(10001, "global.exception", $"Unhandled exception occurred: {e}");
    }
}