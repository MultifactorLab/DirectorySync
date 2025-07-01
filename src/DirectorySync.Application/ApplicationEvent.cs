namespace DirectorySync.Application;

public static class ApplicationEvent
{
    public const ushort ApplicationStarted = 10000;
    public const ushort ApplicationStopped = 19999;

    public const ushort InvalidServiceConfiguration = 10010;

    public const ushort SynchronizeGrpousServiceDisabled = 10101;
    public const ushort StartGrpousSynchronizing = 10102;
    public const ushort CompleteGrpousSynchronizing = 10103;
    public const ushort SynchronizeGrpousServiceError = 10150;
    public const ushort SynchronizeGrpousServiceStopping = 10199;

    public const ushort UserSynchronizationServiceDisabled = 11101;
    public const ushort StartUserSynchronization = 11102;
    public const ushort CompleteUsersSynchronization = 11103;
    public const ushort UserSynchronizationServiceError = 11150;
    public const ushort UserSynchronizationServiceStopping = 11999;
    
    public const ushort StartCloudSynchronizationService = 12102;
    public const ushort CloudSynchronizationServiceStopped = 12103;
    public const ushort CloudSynchronizationServiceError = 12150;
    public const ushort CloudSynchronizationServiceStopping = 12199;
    
    public const ushort CloudSettingsSynchronizationServiceDisabled = 13101;
    public const ushort StartCloudSettingSynchronization = 13102;
    public const ushort CompleteCloudSettingSynchronization = 13103;
    public const ushort CloudSettingSynchronizationServiceError = 13150;
    public const ushort CloudSettingSynchronizationServiceStopping = 13199;
}
