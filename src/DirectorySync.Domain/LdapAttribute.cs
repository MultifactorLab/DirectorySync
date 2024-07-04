using System.Text;

namespace DirectorySync.Domain;

public record LdapAttribute
{
    public LdapAttributeName Name { get; }
    public string?[] Values { get; }

    public LdapAttribute(LdapAttributeName name, string? value) : this(name, new[] { value }) { }
    
    public LdapAttribute(LdapAttributeName name, IEnumerable<string?> values)
    {
        ArgumentNullException.ThrowIfNull(name);
        ArgumentNullException.ThrowIfNull(values);

        Name = name;
        Values = values.ToArray();
    }

    public override string ToString()
    {
        var sb = new StringBuilder(Name);
        if (Values.Length == 0)
        {
            return sb.ToString();
        }

        sb.Append($":{string.Join(',', Values.Select(x => $"'{x}'"))}");
        return sb.ToString();
    }
}
