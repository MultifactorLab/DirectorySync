using System.Text;

namespace Multifactor.Core.Ldap;

/// <summary>
/// LDAP connection string object.
/// </summary>
public sealed class LdapConnectionString
{
    /// <summary>
    /// LDAP or LDAPS
    /// </summary>
    public string Scheme { get; }

    /// <summary>
    /// Server host IP or name.
    /// </summary>
    public string Host { get; }

    /// <summary>
    /// Server port.
    /// </summary>
    public int Port { get; }

    /// <summary>
    /// Container (base object search).
    /// </summary>
    public string Container { get; }

    /// <summary>
    /// Well-formed LDAP url.
    /// For more information see <a href="http://www.rfc-editor.org/rfc/rfc2255">RFC2255</a>.
    /// </summary>
    public string WellFormedLdapUrl { get; }

    /// <summary>
    /// Tries to create a new LDAP connection string from any string.
    /// </summary>
    /// <param name="connectionString">String.</param>
    /// <exception cref="ArgumentException">If connectionString is null, empty or whitespaces.</exception>
    public LdapConnectionString(string connectionString)
    {
        if (string.IsNullOrWhiteSpace(connectionString))
        {
            throw new ArgumentException("Value cannot be null or whitespace.", nameof(connectionString));
        }

        connectionString = connectionString.Trim();

        // parse URI-like string
        if (TryParseLdapUri(connectionString, out var uri))
        {
            // SCHEME
            if (uri!.Scheme.Equals(LdapScheme.Ldap.Name, StringComparison.OrdinalIgnoreCase))
            {
                Scheme = LdapScheme.Ldap.Name;
            }
            else if (uri.Scheme.Equals(LdapScheme.Ldaps.Name, StringComparison.OrdinalIgnoreCase))
            {
                Scheme = LdapScheme.Ldaps.Name;
            }
            else
            {
                throw new ArgumentException("Unknown LDAP scheme");
            }

            // HOST
            Host = uri.DnsSafeHost;

            // PORT
            Port = Scheme == LdapScheme.Ldap.Name
                ? uri.Port
                : LdapScheme.Ldaps.Port;
            Scheme = AdjustScheme(Port);

            // CONTAINER
            Container = uri.GetComponents(UriComponents.Path, UriFormat.SafeUnescaped);

            // Build url
            WellFormedLdapUrl = BuildUrl(Host, Port, Container);

            return;
        }

        // Parse any other string...

        // SCHEME
        Scheme = LdapScheme.Ldap.Name;

        // HOST
        Host = GetHost(connectionString).ToString();

        // PORT
        Port = GetPort(connectionString);
        Scheme = AdjustScheme(Port);

        // CONTAINER
        var cont = GetContainer(connectionString);
        Container = cont.Length == 0
            ? string.Empty
            : cont.ToString();

        // Build url
        WellFormedLdapUrl = BuildUrl(Host, Port, Container);
    }

    private static string AdjustScheme(int port)
    {
        return port == LdapScheme.Ldaps.Port
            ? LdapScheme.Ldaps.Name
            : LdapScheme.Ldap.Name;
    }

    private static ReadOnlySpan<char> GetHost(ReadOnlySpan<char> str)
    {
        var idx = str.IndexOf(':');
        if (idx != -1)
        {
            return str[..idx];
        }

        idx = str.IndexOf('/');
        if (idx != -1)
        {
            return str[..idx];
        }

        return str;
    }

    private static int GetPort(ReadOnlySpan<char> str)
    {
        var idx = str.IndexOf(':');
        if (idx == -1)
        {
            return LdapScheme.Ldap.Port;
        }

        var port = new StringBuilder();
        for (var i = idx + 1; i < str.Length && int.TryParse(str[i].ToString(), out var portDigit); i++)
        {
            port.Append(portDigit);
        }

        if (port.Length == 0)
        {
            throw new ArgumentException("Invalid Ldap url port definition");
        }

        return int.Parse(port.ToString());
    }

    private static bool TryParseLdapUri(string uriString, out Uri? parsedUri)
    {
        if (!Uri.IsWellFormedUriString(uriString, UriKind.Absolute))
        {
            parsedUri = default;
            return false;
        }

        var uri = new Uri(uriString);
        if (!IsPossibleToFormLdapUrl(uri))
        {
            parsedUri = default;
            return false;
        }

        parsedUri = uri;
        return true;
    }

    private static bool IsPossibleToFormLdapUrl(Uri uri)
    {
        // 'System.Uri' class considers these to be well-formed URI 
        // with a 'domain.local' scheme, empty host and '-1' port:
        // 'domain.local:389'
        // 'domain.local:'
        // (-)_(-)
        return uri.Authority != string.Empty && uri.Host != string.Empty;
    }

    private static ReadOnlySpan<char> GetContainer(ReadOnlySpan<char> str)
    {
        var idx = str.IndexOf('/');
        if (idx != -1)
        {
            return str[(idx + 1)..];
        }

        return ReadOnlySpan<char>.Empty;
    }

    private static string BuildUrl(string host, int port, string? container)
    {
        var containerPart = container != null
            ? $"/{container}"
            : string.Empty;
        return $"LDAP://{host}:{port}{containerPart}";
    }

    public class LdapScheme
    {
        /// <summary>
        /// Default scheme.
        /// </summary>
        public static LdapScheme Ldap { get; } = new("LDAP", 389);

        /// <summary>
        /// Scheme with secure connection (TLS/SSL).
        /// </summary>
        public static LdapScheme Ldaps { get; } = new("LDAPS", 636);

        /// <summary>
        /// Scheme name.
        /// </summary>
        public string Name { get; }

        /// <summary>
        /// Scheme port.
        /// </summary>
        public int Port { get; }

        private LdapScheme(string name, int port)
        {
            (Name, Port) = (name, port);
        }
    }
}
