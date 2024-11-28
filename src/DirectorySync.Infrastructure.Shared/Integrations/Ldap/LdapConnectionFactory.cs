using DirectorySync.Infrastructure.Shared.Multifactor.Core.Ldap;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using System;
using System.DirectoryServices.Protocols;
using System.Net;

namespace DirectorySync.Infrastructure.Shared.Integrations.Ldap
{
    public sealed class LdapConnectionFactory
    {
        private static readonly TimeSpan _defaultTimeout = TimeSpan.FromSeconds(30);
        private static readonly TimeSpan _minTimeout = TimeSpan.FromSeconds(1);
        private static readonly TimeSpan _maxTimeout = TimeSpan.FromMinutes(5);

        private readonly LdapConnectionString _connectionString;
        private readonly ILogger<LdapConnectionFactory> _logger;
        private readonly LdapOptions _options;

        public LdapConnectionFactory(LdapConnectionString connectionString,
            IOptions<LdapOptions> options,
            ILogger<LdapConnectionFactory> logger)
        {
            _connectionString = connectionString;
            _options = options.Value;
            _logger = logger;
        }

        internal LdapConnectionFactory(LdapConnectionString connectionString,
            IOptions<LdapOptions> options)
        {
            _connectionString = connectionString;
            _options = options.Value;
            _logger = null;
        }

        /// <summary>
        /// Establishes a LDAP connection and bind user.
        /// </summary>
        /// <returns>LdapConnection</returns>
        public LdapConnection CreateConnection()
        {
            _logger?.LogDebug("Establishing an LDAP connection...");

            var id = new LdapDirectoryIdentifier(_connectionString.Host, _connectionString.Port);
            var authType = AuthType.Basic;
            var connenction = new LdapConnection(id,
                new NetworkCredential(_options.Username, _options.Password),
                authType);

            connenction.SessionOptions.ProtocolVersion = 3;
            connenction.SessionOptions.VerifyServerCertificate = (connection, certificate) => true;
            connenction.Timeout = GetTimeout();

            connenction.Bind();

            _logger?.LogDebug("The LDAP connection to server '{LdapServer:l}' is established and the user '{Username:l}' is bound using the '{AuthType:l}' authentication type",
                _connectionString.WellFormedLdapUrl,
                _options.Username,
                authType.ToString());

            return connenction;
        }

        private TimeSpan GetTimeout()
        {
            if (_options.Timeout < _minTimeout)
            {
                return _defaultTimeout;
            }

            if (_options.Timeout > _maxTimeout) 
            {
                return _defaultTimeout;
            }

            return _options.Timeout;
        }
    }
}
