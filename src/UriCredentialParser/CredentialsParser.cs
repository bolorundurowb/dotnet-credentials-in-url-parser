namespace UriCredentialParser;

public static class CredentialsParser
{
    private const int MaximumPortNumber = 65535;
    private const char PortSeparator = ':';
    private const string SchemeSeparator = "://";

    private static readonly char[] AuthorityTerminators = ['/', '?', '#'];
    private static readonly char[] FragmentOrQuery = ['?', '#'];

    private static readonly Dictionary<string, int> DefaultPorts = new(StringComparer.OrdinalIgnoreCase)
    {
        { "http", 80 },
        { "https", 443 },
        { "ftp", 21 },
        { "gopher", 70 }
    };

    /// <summary>
    /// Parses a given URL string into a <see cref="ConnectionParameters"/> object containing the components of the connection information.
    /// </summary>
    /// <param name="url">The URL string to parse. This must be a valid absolute URI.</param>
    /// <returns>An instance of <see cref="ConnectionParameters"/> representing the parsed connection details.</returns>
    /// <exception cref="ArgumentNullException">Thrown when the provided <paramref name="url"/> is null.</exception>
    /// <exception cref="ArgumentException">Thrown when the provided <paramref name="url"/> is empty, contains only whitespace, or is not a valid URL.</exception>
    public static ConnectionParameters Parse(string url)
    {
        ValidateUrl(url);

        var (scheme, authorityStart) = ExtractScheme(url);
        var authorityEnd = IndexOfAuthorityEnd(url, authorityStart);
        var authoritySegment = url.Substring(authorityStart, authorityEnd - authorityStart);

        var (userInfoRaw, hostListText) = SplitUserInfoAndHosts(authoritySegment);
        var (userName, password) = ParseCredentials(userInfoRaw);
        var hosts = ParseHosts(hostListText);
        var primaryHost = hosts.Count > 0 ? hosts[0] : null;

        var databaseName = ExtractDatabasePath(url, authorityEnd);
        var query = ExtractQuery(url, authorityEnd);
        var additionalParameters = ParseQueryParameters(query);
        var port = ResolvePort(scheme, primaryHost, hosts.Count);

        return new ConnectionParameters(
            scheme,
            primaryHost?.HostName,
            userName,
            password,
            databaseName,
            port,
            additionalParameters)
        { Hosts = hosts.Count > 0 ? hosts : null };
    }

    /// <summary>
    /// Tries to parse a URL string into a <see cref="ConnectionParameters"/> object without throwing on invalid input.
    /// Useful for user-facing input scenarios where validation failures are expected.
    /// </summary>
    /// <param name="url">The URL string to parse.</param>
    /// <param name="parameters">
    /// When this method returns <c>true</c>, contains the parsed <see cref="ConnectionParameters"/>;
    /// otherwise <c>null</c>.
    /// </param>
    /// <returns><c>true</c> if the URL was parsed successfully; otherwise <c>false</c>.</returns>
    public static bool TryParse(string url, out ConnectionParameters? parameters)
    {
        if (url is null || string.IsNullOrWhiteSpace(url))
        {
            parameters = null;
            return false;
        }

        try
        {
            parameters = Parse(url);
            return true;
        }
        catch (ArgumentException)
        {
            parameters = null;
            return false;
        }
        catch (UriFormatException)
        {
            parameters = null;
            return false;
        }
    }

    private static void ValidateUrl(string url)
    {
        if (url == null)
            throw new ArgumentNullException(nameof(url), "Url cannot be null.");

        if (string.IsNullOrWhiteSpace(url))
            throw new ArgumentException("Url cannot be empty or contain only whitespace characters.", nameof(url));
    }

    private static (string Scheme, int AuthorityStart) ExtractScheme(string url)
    {
        var schemeSeparatorIndex = url.IndexOf(SchemeSeparator, StringComparison.OrdinalIgnoreCase);

        if (schemeSeparatorIndex >= 0)
        {
            var scheme = url.Substring(0, schemeSeparatorIndex).ToLowerInvariant();

            if (string.IsNullOrWhiteSpace(scheme))
                throw new UriFormatException("Invalid URL format: missing scheme.");

            return (scheme, schemeSeparatorIndex + SchemeSeparator.Length);
        }

        if (url.StartsWith("//", StringComparison.Ordinal))
            return ("file", 2);

        throw new UriFormatException("Invalid URL format.");
    }

    private static int IndexOfAuthorityEnd(string url, int authorityStart)
    {
        var end = url.IndexOfAny(AuthorityTerminators, authorityStart);
        return end < 0 ? url.Length : end;
    }

    private static (string UserInfo, string HostList) SplitUserInfoAndHosts(string authoritySegment)
    {
        var credentialsSeparatorIndex = authoritySegment.LastIndexOf('@');

        return credentialsSeparatorIndex >= 0
            ? (authoritySegment.Substring(0, credentialsSeparatorIndex),
                authoritySegment.Substring(credentialsSeparatorIndex + 1))
            : (string.Empty, authoritySegment);
    }

    private static (string UserName, string Password) ParseCredentials(string userInfo)
    {
        var credentials = string.IsNullOrWhiteSpace(userInfo) ? ":" : userInfo;
        var credentialParts = credentials.Split([PortSeparator], 2);

        var userName = Uri.UnescapeDataString(credentialParts[0]);
        var password = credentialParts.Length > 1
            ? Uri.UnescapeDataString(credentialParts[1])
            : string.Empty;

        return (userName, password);
    }

    private static List<HostEndpoint> ParseHosts(string hostListText)
    {
        if (string.IsNullOrWhiteSpace(hostListText))
            return [];

        var hosts = new List<HostEndpoint>();

        foreach (var segment in hostListText.Split([','], StringSplitOptions.RemoveEmptyEntries))
        {
            var trimmed = segment.Trim();
            if (string.IsNullOrEmpty(trimmed))
                throw new UriFormatException("Invalid host specified.");

            hosts.Add(trimmed.StartsWith("[", StringComparison.Ordinal)
                ? ParseIpv6Host(trimmed)
                : ParsePlainHost(trimmed));
        }

        return hosts;
    }

    private static HostEndpoint ParsePlainHost(string hostPort)
    {
        var colonIndex = hostPort.IndexOf(PortSeparator);

        if (colonIndex < 0)
            return new HostEndpoint(hostPort, null);

        var host = hostPort.Substring(0, colonIndex);
        var portText = hostPort.Substring(colonIndex + 1);

        if (string.IsNullOrEmpty(portText))
            throw new UriFormatException("Invalid port specified.");

        return new HostEndpoint(host, ParseExplicitPort(portText));
    }

    private static HostEndpoint ParseIpv6Host(string hostPort)
    {
        var closingBracketIndex = hostPort.IndexOf(']');

        if (closingBracketIndex < 0)
            throw new UriFormatException("Invalid IPv6 host specified.");

        var host = hostPort.Substring(0, closingBracketIndex + 1);

        if (closingBracketIndex + 1 >= hostPort.Length)
            return new HostEndpoint(host, null);

        if (hostPort[closingBracketIndex + 1] != PortSeparator)
            throw new UriFormatException("Invalid IPv6 port specified.");

        var portText = hostPort.Substring(closingBracketIndex + 2);

        if (string.IsNullOrEmpty(portText))
            throw new UriFormatException("Invalid port specified.");

        return new HostEndpoint(host, ParseExplicitPort(portText));
    }

    private static int ParseExplicitPort(string portText)
    {
        if (!int.TryParse(portText, out var port) || port < 0 || port > MaximumPortNumber)
            throw new UriFormatException("Invalid port specified.");

        return port;
    }

    private static int? ResolvePort(string scheme, HostEndpoint? primaryHost, int hostCount)
    {
        if (primaryHost == null)
            return null;

        if (primaryHost.Port.HasValue)
            return primaryHost.Port;

        if (hostCount == 1 && !string.IsNullOrWhiteSpace(scheme) && DefaultPorts.TryGetValue(scheme, out var defaultPort))
            return defaultPort;

        return null;
    }

    private static string ExtractDatabasePath(string url, int authorityEnd)
    {
        var pathEnd = IndexOfFragmentOrQuery(url, authorityEnd);
        var path = pathEnd > authorityEnd ? url.Substring(authorityEnd, pathEnd - authorityEnd) : string.Empty;

        return path.Trim('/');
    }

    private static string ExtractQuery(string url, int authorityEnd)
    {
        var queryStart = url.IndexOf('?', authorityEnd);
        if (queryStart < 0)
            return string.Empty;

        var fragmentStart = url.IndexOf('#', queryStart);
        var queryEnd = fragmentStart >= 0 ? fragmentStart : url.Length;

        return url.Substring(queryStart, queryEnd - queryStart);
    }

    private static int IndexOfFragmentOrQuery(string url, int start)
    {
        var index = url.IndexOfAny(FragmentOrQuery, start);
        return index < 0 ? url.Length : index;
    }

    private static Dictionary<string, string>? ParseQueryParameters(string query)
    {
        var queryText = query.TrimStart('?');

        return string.IsNullOrWhiteSpace(queryText)
            ? null
            : queryText.Split(['&'], StringSplitOptions.RemoveEmptyEntries)
                .Select(parameter => parameter.Split(['='], 2))
                .ToDictionary(
                    split => Uri.UnescapeDataString(split[0]),
                    split => split.Length > 1 ? Uri.UnescapeDataString(split[1]) : string.Empty);
    }
}