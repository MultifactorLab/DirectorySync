using DirectorySync.Application.Models.ValueObjects;

namespace DirectorySync.Application.Extensions;

internal static class LdapAttributeCollectionExtensions
{
    public static string GetSingle(this LdapAttributeCollection attrs, string name)
    {
        var attr = attrs[name];
        if (attr is null)
        {
            throw new InvalidOperationException($"'{name}' attribute is required");
        }

        var values = attr.GetNotEmptyValues();
        if (values.Length == 0)
        {
            throw new InvalidOperationException($"'{name}' attribute is required");
        }

        if (values.Length != 1)
        {
            throw new InvalidOperationException($"Single '{name}' attribute is required, but more than one was found");
        }

        return values[0];
    }

    public static string? GetSingleOrDefault(this LdapAttributeCollection attrs, string name)
    {
        var attr = attrs[name];
        if (attr is null)
        {
            return default;
        }

        var values = attr.GetNotEmptyValues();
        if (values.Length == 0)
        {
            return default;
        }

        if (values.Length != 1)
        {
            throw new InvalidOperationException($"Single '{name}' attribute is required, but more than one was found");
        }

        return values[0];
    }

    public static string? GetFirstOrDefault(this LdapAttributeCollection attrs, string name)
    {
        var attr = attrs[name];
        if (attr is null)
        {
            return default;
        }

        var values = attr.GetNotEmptyValues();
        if (values.Length == 0)
        {
            return default;
        }

        return values[0];
    }

    public static string? GetFirstOrDefault(this LdapAttributeCollection attrs, string[] names)
    {
        if (names.Length == 0)
        {
            return default;
        }

        foreach (var name in names)
        {
            var attr = attrs[name];
            if (attr is null)
            {
                continue;
            }

            var values = attr.GetNotEmptyValues();
            if (values.Length == 0)
            {
                continue;
            }

            return values[0];
        }

        return default;
    }
}
