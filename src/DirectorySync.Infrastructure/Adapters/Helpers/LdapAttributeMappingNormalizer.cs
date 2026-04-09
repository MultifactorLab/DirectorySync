namespace DirectorySync.Infrastructure.Adapters.Helpers;

public static class LdapAttributeMappingNormalizer
{
    public static string[] NormalizeOrdered(IEnumerable<string?>? source)
    {
        if (source is null)
        {
            return [];
        }

        var list = new List<string>();
        var seen = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

        foreach (var raw in source)
        {
            if (raw is null)
            {
                continue;
            }

            var trimmed = raw.Trim();
            if (trimmed.Length == 0)
            {
                continue;
            }

            if (seen.Add(trimmed))
            {
                list.Add(trimmed);
            }
        }

        return list.Count == 0 ? [] : list.ToArray();
    }
}
