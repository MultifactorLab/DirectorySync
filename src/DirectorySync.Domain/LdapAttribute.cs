using CSharpFunctionalExtensions;
using System.Text;

namespace DirectorySync.Domain;

/// <summary>
/// LDAP attribute object.
/// </summary>
public class LdapAttribute : ComparableValueObject
{
    /// <summary>
    /// Attribute name.
    /// </summary>
    public LdapAttributeName Name { get; }
    
    /// <summary>
    /// Attribute values.
    /// </summary>
    public string?[] Values { get; }

    /// <summary>
    /// Creates LdapAttribute with the sinle value.
    /// </summary>
    /// <param name="name">Attribute name.</param>
    /// <param name="value">Attribute value.</param>
    public LdapAttribute(LdapAttributeName name, string? value) : this(name, [ value ]) { }
    
    /// <summary>
    /// Creates LdapAttribute with the specified values.
    /// </summary>
    /// <param name="name">Attribute name.</param>
    /// <param name="values">Attribute values.</param>
    public LdapAttribute(LdapAttributeName name, IEnumerable<string?> values)
    {
        ArgumentNullException.ThrowIfNull(name);
        ArgumentNullException.ThrowIfNull(values);

        Name = name;
        Values = values.OrderByDescending(x => x).ToArray();
    }

    public override string ToString()
    {
        var sb = new StringBuilder(Name);
        if (Values.Length == 0)
        {
            return sb.ToString();
        }

        sb.Append($":{string.Join(',', Values.OrderDescending().Select(x => $"'{x}'"))}");
        return sb.ToString();
    }

    protected override IEnumerable<IComparable> GetComparableEqualityComponents()
    {
        const string nullValue = "_null_";

        yield return Name;

        foreach (var component in Values)
        {
            // Needed to distinguish an empty string from null value.
            yield return component ?? nullValue;
        }
    }
}

