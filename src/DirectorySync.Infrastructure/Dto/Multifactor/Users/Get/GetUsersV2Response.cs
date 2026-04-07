using System.Collections.ObjectModel;
using DirectorySync.Application.Models.Core;
using DirectorySync.Application.Models.ValueObjects;

namespace DirectorySync.Infrastructure.Dto.Multifactor.Users.Get;

internal sealed class GetUsersV2Response
{
    public CloudUserDto[] Users { get; init; } = [];

    internal static ReadOnlyCollection<CloudUserModel> ToDomainModels(GetUsersV2Response? model)
    {
        if (model is null)
        {
            return ReadOnlyCollection<CloudUserModel>.Empty;
        }

        var result = model.Users
            .Where(u => !string.IsNullOrWhiteSpace(u.Identity))
            .Select(u =>
            {
                var identity = new Identity(u.Identity!);
                DirectoryGuid? guid = ParseGuid(u.ExternalObjectId);
                return new CloudUserModel(identity, guid);
            })
            .ToArray();

        return result.AsReadOnly();
    }

    private static DirectoryGuid? ParseGuid(string? raw)
    {
        if (string.IsNullOrWhiteSpace(raw))
        {
            return null;
        }

        return Guid.TryParse(raw, out var parsed) && parsed != Guid.Empty
            ? new DirectoryGuid(parsed)
            : null;
    }
}

internal sealed class CloudUserDto
{
    public string? Identity { get; init; }
    public string? ExternalObjectId { get; init; }
}
