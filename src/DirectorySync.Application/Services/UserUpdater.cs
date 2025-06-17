using System.Collections.ObjectModel;
using DirectorySync.Application.Models.Core;
using DirectorySync.Application.Ports.Cloud;
using DirectorySync.Application.Workloads;

namespace DirectorySync.Application.Services;

public interface IUserUpdater
{
    Task<ReadOnlyCollection<MemberModel>> UpdateManyAsync(IEnumerable<MemberModel> newUsers, CancellationToken token = default);
}
    
public class UserUpdater : IUserUpdater
{
    private readonly IUserCloudPort _userCloudPort;
    private UserProcessingOptions _userProcessingOptions;

    public UserUpdater(IUserCloudPort userCloudPort,
        UserProcessingOptions userProcessingOptions)
    {
        _userCloudPort = userCloudPort;
        _userProcessingOptions = userProcessingOptions;
    }

    public async Task<ReadOnlyCollection<MemberModel>> UpdateManyAsync(IEnumerable<MemberModel> newUsers, CancellationToken token = default)
    {
        throw new NotImplementedException();
    }
}
