using System.Collections.ObjectModel;
using DirectorySync.Domain;

namespace DirectorySync.Application.Integrations.Multifactor.Deleting
{
    internal class DeletedUsersBucket : IDeletedUsersBucket
    {
        private readonly HashSet<MultifactorUserId> _deleted = [];
        public ReadOnlyCollection<MultifactorUserId> DeletedUsers => new (_deleted.ToArray());
    
        public void AddDeletedUser(MultifactorUserId id)
        {
            ArgumentNullException.ThrowIfNull(id);
        
            if (id == MultifactorUserId.Undefined)
            {
                throw new ArgumentException("User id cannot be undefined", nameof(id));
            }

            _deleted.Add(id);
        }
    }
}
