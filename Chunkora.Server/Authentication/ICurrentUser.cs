namespace Chunkora.Server.Authentication;

public interface ICurrentUser
{
    Guid UserId { get; }
    string Name { get; }
    Guid RoleId { get; }
    List<string> Permissions { get; }

    public bool HasPermission(string required)
    {
        var parts = required.Split('.');
        return Permissions.Any(owned => TryMatch(owned, parts, -1, out _));
    }

    public List<string> MatchPermissionTemplate(string template)
    {
        var parts = template.Split('.');
        var captureIndex = Array.IndexOf(parts, "{}");
        if (captureIndex < 0 || Array.LastIndexOf(parts, "{}") != captureIndex)
        {
            throw new ArgumentException("Template must contain exactly one {}.", nameof(template));
        }
        var result = new HashSet<string>(StringComparer.Ordinal);
        foreach (var owned in Permissions)
        {
            if (TryMatch(owned, parts, captureIndex, out var value) && value is not null)
            {
                result.Add(value);
            }
        }
        return result.ToList();
    }

    private static bool TryMatch(string ownedPermission, string[] required, int captureIndex, out string? captured)
    {
        captured = null;
        var owned = ownedPermission.Split('.');
        for (var i = 0; i < owned.Length; i++)
        {
            if (i >= required.Length) return false;

            if (owned[i] == "*")
            {
                if (i == owned.Length - 1)
                {
                    if (captureIndex >= i) captured = "*";
                    return true;
                }

                if (i == captureIndex) captured = "*";

                continue;
            }

            if (i == captureIndex)
            {
                captured = owned[i];
                continue;
            }

            if (!string.Equals(owned[i], required[i], StringComparison.Ordinal))
            {
                return false;
            }
        }

        return owned.Length == required.Length;
    }
}