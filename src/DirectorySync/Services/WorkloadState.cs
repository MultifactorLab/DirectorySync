namespace DirectorySync.Services;

internal class WorkloadState
{
    private long _userSyncState;
    private long _newUserHandleState;

    public bool UserSyncPerforming
    {
        get
        {
            var value = Interlocked.Read(ref _userSyncState);
            return Convert.ToBoolean(value);
        }
    }
    
    public bool NewUserHandlePerforming
    {
        get
        {
            var value = Interlocked.Read(ref _newUserHandleState);
            return Convert.ToBoolean(value);
        }
    }

    public void StartUserSync()
    {
        Interlocked.Exchange(ref _userSyncState, 1);
    }
    
    public void StopUserSync()
    {
        Interlocked.Exchange(ref _userSyncState, 0);
    }
    
    public void StartNewUserHandle()
    {
        Interlocked.Exchange(ref _newUserHandleState, 1);
    }
    
    public void StopNewUserHandle()
    {
        Interlocked.Exchange(ref _newUserHandleState, 0);
    }
}