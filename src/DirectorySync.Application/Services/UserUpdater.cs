using System.Collections.ObjectModel;
using DirectorySync.Application.Measuring;
using DirectorySync.Application.Models.Core;
using DirectorySync.Application.Ports.Cloud;
using DirectorySync.Application.Ports.Databases;
using DirectorySync.Application.Workloads;

namespace DirectorySync.Application.Services;

public interface IUserUpdater
{
    Task<ReadOnlyCollection<MemberModel>> UpdateManyAsync(IEnumerable<MemberModel> updUsers, CancellationToken token = default);
}
    
public class UserUpdater : IUserUpdater
{
    private readonly IUserCloudPort _userCloudPort;
    private readonly IMemberDatabase _memberDatabase;
    private UserProcessingOptions _userProcessingOptions;
    private CodeTimer _codeTimer;

    public UserUpdater(IUserCloudPort userCloudPort,
        IMemberDatabase memberDatabase,
        UserProcessingOptions userProcessingOptions,
        CodeTimer codeTimer)
    {
        _userCloudPort = userCloudPort;
        _userProcessingOptions = userProcessingOptions;
    }

    public async Task<ReadOnlyCollection<MemberModel>> UpdateManyAsync(IEnumerable<MemberModel> updUsers, CancellationToken token = default)
    {
        ArgumentNullException.ThrowIfNull(updUsers);

        if (updUsers.Count() == 0)
        {
            return new ReadOnlyCollection<MemberModel>(Array.Empty<MemberModel>());
        }
        
        List<MemberModel> updatedMembers = new();

        var skip = 0;
        while (true)
        {
            var bucket = updUsers
                .Skip(skip)
                .Take(_userProcessingOptions.UpdatingBatchSize)
                .ToArray();
            
            if (bucket.Length == 0)
            {
                break;
            }
            
            var timer = _codeTimer.Start("Api Request: Update Users");
            var res = await _userCloudPort.UpdateManyAsync(bucket, token);
            timer.Stop();
            
            _memberDatabase.UpdateMany(res);
            
            updatedMembers.AddRange(res);
            
            skip += bucket.Length;
        }
        
        return updatedMembers.AsReadOnly();
    }
}
