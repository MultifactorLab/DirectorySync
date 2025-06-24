using System.Collections.ObjectModel;
using DirectorySync.Application.Measuring;
using DirectorySync.Application.Models.Core;
using DirectorySync.Application.Models.Options;
using DirectorySync.Application.Ports.Cloud;
using DirectorySync.Application.Ports.Databases;

namespace DirectorySync.Application.Services;

public interface IUserCreator
{
    Task<ReadOnlyCollection<MemberModel>> CreateManyAsync(IEnumerable<MemberModel> newUsers, CancellationToken cancellationToken = default);
}
    
public class UserCreator : IUserCreator
{
    private readonly IUserCloudPort _userCloudPort;
    private readonly IMemberDatabase _memberDatabase;
    private readonly UserProcessingOptions _userProcessingOptions;
    private readonly CodeTimer _codeTimer;

    public UserCreator(IUserCloudPort userCloudPort,
        IMemberDatabase memberDatabase,
        UserProcessingOptions userProcessingOptions,
        CodeTimer codeTimer)
    {
        _userCloudPort = userCloudPort;
        _memberDatabase = memberDatabase;
        _userProcessingOptions = userProcessingOptions;
        _codeTimer = codeTimer;
    }

    public async Task<ReadOnlyCollection<MemberModel>> CreateManyAsync(IEnumerable<MemberModel> newUsers, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(newUsers);

        if (newUsers.Count() == 0)
        {
            return new ReadOnlyCollection<MemberModel>(Array.Empty<MemberModel>());
        }
        
        List<MemberModel> addedMembers = new();
        
        var skip = 0;
        while (true)
        {
            var bucket = newUsers
                .Skip(skip)
                .Take(_userProcessingOptions.CreatingBatchSize)
                .ToArray();
            
            if (bucket.Length == 0)
            {
                break;
            }
            
            var timer = _codeTimer.Start("Api Request: Create Users");
            var res = await _userCloudPort.CreateManyAsync(bucket, cancellationToken);
            timer.Stop();
            
            _memberDatabase.InsertMany(res);
            
            addedMembers.AddRange(res);
            
            skip += bucket.Length;
        }
        
        return addedMembers.AsReadOnly();
    }
}
