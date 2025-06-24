using DirectorySync.Application.Models.Core;

namespace DirectorySync.Infrastructure.Dto.Multifactor.Users.Delete;

internal class DeleteUsersRequest
{
    public string[] Identities { get; }

    public DeleteUsersRequest(IEnumerable<string> identities)
    {
        ArgumentNullException.ThrowIfNull(identities);
        Identities = identities.ToArray();
    }
    
    internal static DeleteUsersRequest FromDomainModels(IEnumerable<MemberModel> domainModels)
    {
        ArgumentNullException.ThrowIfNull(domainModels);

        return new DeleteUsersRequest(domainModels.Select(c => c.Identity.Value));
    }
}
