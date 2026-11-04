using System.Collections.Concurrent;
using StreamWriter = Chunkora.Agent.Handler.StreamWriter;

namespace Chunkora.Agent.Utils;

public static class IoUtils
{
    public static readonly ConcurrentDictionary<string, string> OccupiedObjects = new(); // resolvedPath => requestId
    public static readonly ConcurrentDictionary<string, StreamWriter> OperatingWriters = new(); // requestId => writer
    
    public static string Resolve(string rootPath, string relativePath)
    {
        var root = Path.GetFullPath(rootPath);
        if (Path.IsPathRooted(relativePath))
        {
            throw new UnauthorizedAccessException("Absolute paths are not allowed.");
        }
        var fullPath = Path.GetFullPath(Path.Combine(root, relativePath));
        var relative = Path.GetRelativePath(root, fullPath);
        if (relative == ".")
        {
            RejectLinks(fullPath);
            return fullPath;
        }
        if (relative == ".." ||
            relative.StartsWith(".." + Path.DirectorySeparatorChar, StringComparison.Ordinal) ||
            relative.StartsWith(".." + Path.AltDirectorySeparatorChar, StringComparison.Ordinal) ||
            Path.IsPathRooted(relative))
        {
            throw new UnauthorizedAccessException("Path traversal detected.");
        }
        RejectLinks(fullPath);
        return fullPath;
    }
    
    private static void RejectLinks(string fullPath)
    {
        for (string? path = fullPath; path is not null; path = Path.GetDirectoryName(path))
        {
            try
            {
                if ((File.GetAttributes(path) & FileAttributes.ReparsePoint) != 0)
                {
                    throw new UnauthorizedAccessException("Link is not allowed");
                }
            }
            catch (FileNotFoundException) { }
            catch (DirectoryNotFoundException) { }
        }
    }
    
    public static string FormatObjectPermission(UnixFileMode mode, char fileType = '-')
    {
        if (mode == UnixFileMode.None || (int)mode == -1)
        {
            return "";
        }
        
        bool Has(UnixFileMode flag) => (mode & flag) != 0;

        char Execute(UnixFileMode execute, UnixFileMode special, char specialChar)
        {
            var x = Has(execute);
            var s = Has(special);
            return s ? x ? specialChar : char.ToUpperInvariant(specialChar) : x ? 'x' : '-';
        }

        return new string(new[]
        {
            fileType,

            Has(UnixFileMode.UserRead) ? 'r' : '-',
            Has(UnixFileMode.UserWrite) ? 'w' : '-',
            Execute(UnixFileMode.UserExecute, UnixFileMode.SetUser, 's'),

            Has(UnixFileMode.GroupRead) ? 'r' : '-',
            Has(UnixFileMode.GroupWrite) ? 'w' : '-',
            Execute(UnixFileMode.GroupExecute, UnixFileMode.SetGroup, 's'),

            Has(UnixFileMode.OtherRead) ? 'r' : '-',
            Has(UnixFileMode.OtherWrite) ? 'w' : '-',
            Execute(UnixFileMode.OtherExecute, UnixFileMode.StickyBit, 't')
        });
    }
}