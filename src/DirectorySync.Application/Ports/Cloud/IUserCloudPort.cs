using System.Collections.ObjectModel;
using DirectorySync.Application.Models.Core;
using DirectorySync.Application.Models.ValueObjects;

namespace DirectorySync.Application.Ports.Cloud
{
    public interface IUserCloudPort
    {
        Task<ReadOnlyCollection<Identity>> GetUsersIdentitesAsync(CancellationToken ct = default);
        Task<ReadOnlyCollection<MemberModel>> CreateManyAsync(IEnumerable<MemberModel> newMembers, CancellationToken ct = default);
        Task<ReadOnlyCollection<MemberModel>> UpdateManyAsync(IEnumerable<MemberModel> updMembers, CancellationToken ct = default);
        Task<ReadOnlyCollection<MemberModel>> DeleteManyAsync(IEnumerable<MemberModel> delMembers, CancellationToken ct = default);
    }
}
