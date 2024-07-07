using DirectorySync.Domain;
using Microsoft.Extensions.Logging;

namespace DirectorySync.Application.Extensions;

public static class LoggerExtensions
{
    public static IDisposable? EnrichWithGroup(this ILogger logger, DirectoryGuid groupGuid)
    {
        ArgumentNullException.ThrowIfNull(logger);
        ArgumentNullException.ThrowIfNull(groupGuid);

        return logger.BeginScope(GetState("GroupGuid", groupGuid));
    }
    
    public static IDisposable? EnrichWithLdapUser(this ILogger logger, DirectoryGuid ldapUserGuid)
    {
        ArgumentNullException.ThrowIfNull(logger);
        ArgumentNullException.ThrowIfNull(ldapUserGuid);

        return logger.BeginScope(GetState("LdapUserGuid", ldapUserGuid));
    }
    
    public static IDisposable? EnrichWithMultifactorUser(this ILogger logger, MultifactorUserId userId)
    {
        ArgumentNullException.ThrowIfNull(logger);
        ArgumentNullException.ThrowIfNull(userId);

        return logger.BeginScope(GetState("MultifactorUserId", userId));
    }

    private static Dictionary<string, object> GetState(string name, string value)
    {
        return new Dictionary<string, object>(1) { { name, value } };
    }
}
