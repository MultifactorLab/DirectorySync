using System.Text;

namespace DirectorySync.Infrastructure.Integrations.Ldap;

public sealed class LdapConnectionString
{
    public string Scheme { get; }
    public string Host { get; }
    public int Port { get; }
    public string? Container { get; }
    public string Rfc2255LdapUrl { get; }

    public LdapConnectionString(string connectionString)
    {
        if (string.IsNullOrWhiteSpace(connectionString))
        {
            throw new ArgumentException("Value cannot be null or whitespace.", nameof(connectionString));
        }

        connectionString = connectionString.Trim();

        if (Uri.IsWellFormedUriString(connectionString, UriKind.Absolute))
        {
            var uri = new Uri(connectionString);

            // scheme
            if (uri.Scheme.Equals(LdapScheme.Ldap, StringComparison.OrdinalIgnoreCase))
            {
                Scheme = LdapScheme.Ldap;
            }
            else if (uri.Scheme.Equals(LdapScheme.Ldaps, StringComparison.OrdinalIgnoreCase))
            {
                Scheme = LdapScheme.Ldaps;
            }
            else
            {
                throw new ArgumentException("Unknown LDAP schema");
            }

            // host
            Host = uri.Authority;

            // port
            Port = Scheme == LdapScheme.Ldap
                ? uri.Port
                : 636;

            // container path
            var path = uri.GetComponents(UriComponents.Path, UriFormat.SafeUnescaped);
            if (path != string.Empty)
            {
                Container = path;
            }

            Rfc2255LdapUrl = GetRfc(Host, Port, Container);

            return;
        }

        // scheme
        Scheme = LdapScheme.Ldap;

        // host
        Host = GetHost(connectionString);

        // port
        Port = GetPort(connectionString);
        if (Port == 636)
        {
            Scheme = LdapScheme.Ldaps;
        }

        Container = GetContainer(connectionString);

        Rfc2255LdapUrl = GetRfc(Host, Port, Container);
    }

    private static string GetRfc(string host, int port, string? container)
    {
        var containerPart = container != null
            ? $"/{container}"
            : string.Empty;
        return $"LDAP://{host}:{port}{containerPart}";
    }

    private static string GetHost(string str)
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

    private static int GetPort(string str)
    {
        var idx = str.IndexOf(':');
        if (idx == -1)
        {
            return 389;
        }

        var port = new StringBuilder();
        for (var i = idx + 1; int.TryParse(str[i].ToString(), out var portDigit); i++)
        {
            port.Append(portDigit);
        }

        if (port.Length == 0)
        {
            throw new ArgumentException("Invalid Ldap url port definition");
        }

        return int.Parse(port.ToString());
    }

    private static string? GetContainer(string str)
    {
        var idx = str.IndexOf('/');
        if (idx != -1)
        {
            return str[(idx + 1)..];
        }

        return str;
    }

    public static class LdapScheme
    {
        public const string Ldap = "LDAP";
        public const string Ldaps = "LDAPS";
    }
}
