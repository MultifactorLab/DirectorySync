using System.Collections.ObjectModel;
using DirectorySync.Application.Measuring;
using DirectorySync.Application.Models.Core;
using DirectorySync.Application.Models.Options;
using DirectorySync.Application.Ports.Cloud;
using DirectorySync.Application.Ports.Databases;
using Microsoft.Extensions.Options;

namespace DirectorySync.Application.Services;

public interface IUserDeleter
{
    Task<ReadOnlyCollection<MemberModel>> DeleteManyAsync(IEnumerable<MemberModel> delUsers, CancellationToken cancellationToken = default);
}
    
public class UserDeleter : IUserDeleter
{
    private readonly IUserCloudPort _userCloudPort;
    private readonly IMemberDatabase _memberDatabase;
    private readonly UserProcessingOptions _userProcessingOptions;
    private readonly CodeTimer _codeTimer;

    public UserDeleter(IUserCloudPort userCloudPort,
        IMemberDatabase memberDatabase,
        IOptions<UserProcessingOptions> userProcessingOptions,
        CodeTimer codeTimer)
    {
        _userCloudPort = userCloudPort;
        _memberDatabase = memberDatabase;
        _userProcessingOptions = userProcessingOptions.Value;
        _codeTimer = codeTimer;
    }
        
    public async Task<ReadOnlyCollection<MemberModel>> DeleteManyAsync(IEnumerable<MemberModel> delUsers, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(delUsers);

        if (delUsers.Count() == 0)
        {
            return new ReadOnlyCollection<MemberModel>(Array.Empty<MemberModel>());
        }

        List<MemberModel> deletedMembers = new();

        var skip = 0;
        while (true)
        {
            var bucket = delUsers
                .Skip(skip)
                .Take(_userProcessingOptions.UpdatingBatchSize)
                .ToArray();
            
            if (bucket.Length == 0)
            {
                break;
            }
            
            var timer = _codeTimer.Start("Api Request: Delete Users");
            var res = await _userCloudPort.DeleteManyAsync(bucket, cancellationToken);
            timer.Stop();
            
            timer = _codeTimer.Start("Update Cached Group: Deleted Users");
            _memberDatabase.DeleteMany(res.Select(x => x.Id));
            timer.Stop();
            
            skip += bucket.Length;
            
            deletedMembers.AddRange(res);
        }
        
        return deletedMembers.AsReadOnly();
    }
}
