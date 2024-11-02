using DirectorySync.Domain;

namespace DirectorySync.Application.Integrations.Multifactor;

public record HandledUser
{
    public DirectoryGuid Id { get; }
    public string Identity { get; }

    public HandledUser(DirectoryGuid id, string identity)
    {
        if (string.IsNullOrWhiteSpace(identity))
        {
            throw new ArgumentException($"'{nameof(identity)}' cannot be null or whitespace.", nameof(identity));
        }

        Id = id ?? throw new ArgumentNullException(nameof(id));
        Identity = identity;
    }
}
