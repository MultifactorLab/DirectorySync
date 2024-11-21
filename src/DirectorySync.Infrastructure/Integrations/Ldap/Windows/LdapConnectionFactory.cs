using System.DirectoryServices.Protocols;
using System.Net;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Multifactor.Core.Ldap;

namespace DirectorySync.Application.Integrations.Ldap.Windows;

internal sealed class LdapConnectionFactory
{
    private readonly LdapConnectionString _connectionString;
    private readonly ILogger<LdapConnectionFactory> _logger;
    private readonly LdapOptions _options;

    public LdapConnectionFactory(LdapConnectionString connectionString,
        IOptions<LdapOptions> options,
        ILogger<LdapConnectionFactory> logger)
    {
        _connectionString = connectionString;
        _logger = logger;
        _options = options.Value;
    }

    /// <summary>
    /// Establishes a LDAP connection and bind user.
    /// </summary>
    /// <returns>LdapConnection</returns>
    public LdapConnection CreateConnection()
    {
        _logger.LogDebug("Establishing an LDAP connection...");

        var id = new LdapDirectoryIdentifier(_connectionString.Host, _connectionString.Port);
        var authType = AuthType.Basic;
        var connenction = new LdapConnection(id,
            new NetworkCredential(_options.Username, _options.Password),
            authType);

        connenction.SessionOptions.ProtocolVersion = 3;
        connenction.SessionOptions.VerifyServerCertificate = (connection, certificate) => true;

        connenction.Bind();

        _logger.LogDebug("The LDAP connection to server '{LdapServer:l}' is established and the user '{Username:l}' is bound using the '{AuthType:l}' authentication type",
            _connectionString.WellFormedLdapUrl,
            _options.Username, 
            authType.ToString());

        return connenction;
    }
}
