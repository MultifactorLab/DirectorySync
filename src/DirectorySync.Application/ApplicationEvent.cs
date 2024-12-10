namespace DirectorySync.Application;

public static class ApplicationEvent
{
    public const ushort ApplicationStarted = 10000;
    public const ushort ApplicationStopped = 19999;

    public const ushort InvalidServiceConfiguration = 10010;

    public const ushort UserScanningServiceDisabled = 10101;
    public const ushort StartUserScanning = 10102;
    public const ushort CompleteUserScanning = 10103;
    public const ushort UserScanningServiceError = 10150;
    public const ushort UserScanningServiceStopping = 10199;

    public const ushort UserSynchronizationServiceDisabled = 11001;
    public const ushort StartUserSynchronization = 11002;
    public const ushort CompleteUsersSynchronization = 11003;
    public const ushort UserSynchronizationServiceError = 11050;
    public const ushort UserSynchronizationServiceStopping = 11999;
}
