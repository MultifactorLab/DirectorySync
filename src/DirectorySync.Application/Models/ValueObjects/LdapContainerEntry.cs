using DirectorySync.Shared.Karnel;

namespace DirectorySync.Application.Models.ValueObjects;

public class LdapContainerEntry : ValueObject
{
    public string DistinguishedName { get; }
    public string ObjectClass { get; }

    public LdapContainerEntry(string distinguishedName, string objectClass)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(distinguishedName);
        ArgumentException.ThrowIfNullOrWhiteSpace(objectClass);
        
        DistinguishedName = distinguishedName;
        ObjectClass = objectClass;
    }

    public override string ToString()
    {
        return DistinguishedName;
    }

    protected override IEnumerable<object?> GetEqualityComponents()
    {
        yield return DistinguishedName;
        yield return ObjectClass;
    }
}
