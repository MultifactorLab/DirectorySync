namespace DirectorySync.Application;

public static class ApplicationEvent
{
    public const int UserSyncStarted = 0001;
    public const int NewUserHandleStarted = 0002;
    
    public const int UserSyncTimerTriggered = 1000;
    public const int UserSyncTimerSkipping = 1001;
    
    public const int NewUserHandleTimerTriggered = 1100;
    public const int NewUserHandleTimerSkipping = 1101;
    
    public const int UserSyncStopping = 9000;
    public const int NewUserHandleStopping = 9100;
}
