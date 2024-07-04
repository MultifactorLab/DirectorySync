using System.Collections.ObjectModel;
using DirectorySync.Domain;

namespace DirectorySync.Application.Integrations.Multifactor;

public interface IMultifactorApi
{
    Task DeleteManyAsync(IEnumerable<MultifactorUserId> identifiers, CancellationToken token = default);
    Task UpdateManyAsync(IUpdateBucket bucket, CancellationToken token = default);
}

public interface IUpdateBucket
{
    ReadOnlyCollection<IModifiedUser> ModifiedUsers { get; }
}

internal class UpdateBucket : IUpdateBucket
{
    private readonly List<IModifiedUser> _users = [];
    public ReadOnlyCollection<IModifiedUser> ModifiedUsers => new (_users);

    public ModifiedUser AddUser(MultifactorUserId id)
    {
        ArgumentNullException.ThrowIfNull(id);

        var user = new ModifiedUser(id);
        _users.Add(user);
        return new ModifiedUser(id);
    }
}

public interface IModifiedUser
{
    MultifactorUserId Id { get; }
    ReadOnlyCollection<MultifactorProperty> Properties { get; }
}

internal class ModifiedUser : IModifiedUser
{
    private readonly HashSet<MultifactorProperty> _props = [];
    public ReadOnlyCollection<MultifactorProperty> Properties => new (_props.ToArray());
    public MultifactorUserId Id { get; }
    
    public ModifiedUser(MultifactorUserId id)
    {
        Id = id ?? throw new ArgumentNullException(nameof(id));
    }

    public ModifiedUser AddProperty(string name, string? value)
    {
        _props.Add(new MultifactorProperty(name, value));
        return this;
    }
}

public record MultifactorProperty(string name, string? value);
