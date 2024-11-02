using DirectorySync.Domain;

namespace DirectorySync.Application.Integrations.Multifactor.Deleting;

public interface IDeletedUser
{
    DirectoryGuid Id { get; }
    string Identity { get; }
}

internal class DeletedUser : IDeletedUser
{
    public DirectoryGuid Id { get; }
    public string Identity { get; }

    public DeletedUser(DirectoryGuid id, string identity)
    {
        if (string.IsNullOrWhiteSpace(identity))
        {
            throw new ArgumentException($"'{nameof(identity)}' cannot be null or whitespace.", nameof(identity));
        }

        Id = id ?? throw new ArgumentNullException(nameof(id));
        Identity = identity;
    }
}
