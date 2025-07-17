using System.Collections.ObjectModel;
using DirectorySync.Application.Models.ValueObjects;

namespace DirectorySync.Infrastructure.Dto.Multifactor.Users.Get;

internal class GetIdentitiesResponse
{
    public string[] Identities { get; init; } = [];


    internal static ReadOnlyCollection<Identity> ToDomainModels(GetIdentitiesResponse model)
    {
        ArgumentNullException.ThrowIfNull(model);
        
        return new ReadOnlyCollection<Identity>(model.Identities.Select(id => new Identity(id)).ToArray());
    }
}
