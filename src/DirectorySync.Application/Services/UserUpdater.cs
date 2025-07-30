using System.Collections.ObjectModel;
using DirectorySync.Application.Measuring;
using DirectorySync.Application.Models.Core;
using DirectorySync.Application.Models.Options;
using DirectorySync.Application.Ports.Cloud;
using DirectorySync.Application.Ports.Databases;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace DirectorySync.Application.Services;

public interface IUserUpdater
{
    Task<ReadOnlyCollection<MemberModel>> UpdateManyAsync(IEnumerable<MemberModel> updUsers, CancellationToken cancellationToken = default);
}
    
public class UserUpdater : IUserUpdater
{
    private readonly IUserCloudPort _userCloudPort;
    private readonly IMemberDatabase _memberDatabase;
    private readonly UserProcessingOptions _userProcessingOptions;
    private readonly CodeTimer _codeTimer;
    private readonly ILogger<UserUpdater> _logger;

    public UserUpdater(IUserCloudPort userCloudPort,
        IMemberDatabase memberDatabase,
        IOptions<UserProcessingOptions> userProcessingOptions,
        ILogger<UserUpdater> logger,
        CodeTimer codeTimer)
    {
        _userCloudPort = userCloudPort;
        _userProcessingOptions = userProcessingOptions.Value;
        _codeTimer = codeTimer;
        _logger = logger;
        _memberDatabase = memberDatabase;
    }

    public async Task<ReadOnlyCollection<MemberModel>> UpdateManyAsync(IEnumerable<MemberModel> updUsers, CancellationToken cancellationToken = default)
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
            var res = await _userCloudPort.UpdateManyAsync(bucket, cancellationToken);
            timer.Stop();
            
            var delay = Task.Delay(_userProcessingOptions.RequestInterval, cancellationToken);
            
            timer = _codeTimer.Start("Update Cached Group: Modified Users");
            _memberDatabase.UpdateMany(res);
            timer.Stop();
            
            updatedMembers.AddRange(res);
            
            skip += bucket.Length;
            
            await delay;
        }
        
        return updatedMembers.AsReadOnly();
    }
}
