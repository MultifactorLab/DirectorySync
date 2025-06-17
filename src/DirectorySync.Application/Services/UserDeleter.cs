using System.Collections.ObjectModel;
using DirectorySync.Application.Models.Core;
using DirectorySync.Application.Ports.Cloud;
using DirectorySync.Application.Workloads;

namespace DirectorySync.Application.Services
{
    public interface IUserDeleter
    {
        Task<ReadOnlyCollection<MemberModel>> DeleteManyAsync(IEnumerable<MemberModel> newUsers, CancellationToken token = default);
    }
    
    public class UserDeleter : IUserDeleter
    {
        private readonly IUserCloudPort _userCloudPort;
        private UserProcessingOptions _userProcessingOptions;

        public UserDeleter(IUserCloudPort userCloudPort,
            UserProcessingOptions userProcessingOptions)
        {
            _userCloudPort = userCloudPort;
            _userProcessingOptions = userProcessingOptions;
        }
        
        public Task<ReadOnlyCollection<MemberModel>> DeleteManyAsync(IEnumerable<MemberModel> newUsers, CancellationToken token = default)
        {
            throw new NotImplementedException();
        }
    }
}
