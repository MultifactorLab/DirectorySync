namespace DirectorySync.Application;

public static class ApplicationEvent
{
    public const ushort ApplicationStarted = 10000;
    public const ushort ApplicationStopped = 19999;
    
    public const ushort UserSynchronizationServiceStarted = 11000;
    public const ushort UserSynchronizationServiceStartedDisabled = 11001;
    public const ushort StartUserSynchronization = 1102;
    public const ushort CompleteUsersSynchronization = 1103;
    public const ushort UserSynchronizationServiceError = 11050;
    public const ushort UserSynchronizationServiceStopping = 11999;
    
    public const ushort NewUserHandlingServiceStarted = 10100;
    public const ushort NewUserHandlingServiceStartedDisabled = 10101;
    public const ushort StartNewUserHandling = 10102;
    public const ushort CompleteNewUserHandling = 10103;
    public const ushort NewUserHandlingServiceError = 10150;
    public const ushort NewUserHandlingServiceStopping = 10199;
}
