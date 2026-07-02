namespace UriCredentialParser;

public static class CredentialsParser
{
    private const int MaximumPortNumber = 65535;
    private const char PortSeparator = ':';
    private const string SchemeSeparator = "://";

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

        var uri = new Uri(url, UriKind.Absolute);
        var (userName, password) = ParseCredentials(uri.UserInfo);
        var port = ParsePort(url, uri);
        var databaseName = uri.AbsolutePath.Trim('/');
        var additionalParameters = ParseQueryParameters(uri.Query);

        return new ConnectionParameters(
            uri.Scheme,
            uri.Host,
            userName,
            password,
            databaseName,
            port,
            additionalParameters);
    }

    private static void ValidateUrl(string url)
    {
        if (url == null)
            throw new ArgumentNullException(nameof(url), "Url cannot be null.");

        if (string.IsNullOrWhiteSpace(url))
            throw new ArgumentException("Url cannot be empty or contain only whitespace characters.", nameof(url));
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

    private static int? ParsePort(string url, Uri uri)
    {
        var explicitPort = ExtractExplicitPort(url);

        if (explicitPort != null)
            return ParseExplicitPort(explicitPort);

        return uri.Port > 0 ? uri.Port : null;
    }

    private static string? ExtractExplicitPort(string url)
    {
        var authorityStart = GetAuthorityStartIndex(url);
        var pathStart = url.IndexOfAny(['/', '?', '#'], authorityStart);

        if (pathStart < 0)
            pathStart = url.Length;

        var authoritySegment = url.Substring(authorityStart, pathStart - authorityStart);
        var hostPortSegment = ExtractHostPortSegment(authoritySegment);

        return ExtractPortText(hostPortSegment);
    }

    private static int GetAuthorityStartIndex(string url)
    {
        var schemeSeparatorIndex = url.IndexOf(SchemeSeparator);

        if (schemeSeparatorIndex >= 0)
            return schemeSeparatorIndex + SchemeSeparator.Length;

        return url.IndexOf(PortSeparator) + 1;
    }

    private static string ExtractHostPortSegment(string authoritySegment)
    {
        var credentialsSeparatorIndex = authoritySegment.LastIndexOf('@');

        return credentialsSeparatorIndex >= 0
            ? authoritySegment.Substring(credentialsSeparatorIndex + 1)
            : authoritySegment;
    }

    private static string? ExtractPortText(string hostPortSegment)
    {
        if (hostPortSegment.StartsWith("["))
            return ExtractIpv6PortText(hostPortSegment);

        var colonIndex = hostPortSegment.IndexOf(PortSeparator);

        return colonIndex >= 0
            ? hostPortSegment.Substring(colonIndex + 1)
            : null;
    }

    private static string? ExtractIpv6PortText(string hostPortSegment)
    {
        var closingBracketIndex = hostPortSegment.IndexOf(']');

        if (closingBracketIndex >= 0 &&
            closingBracketIndex < hostPortSegment.Length - 1 &&
            hostPortSegment[closingBracketIndex + 1] == PortSeparator)
        {
            return hostPortSegment.Substring(closingBracketIndex + 2);
        }

        return null;
    }

    private static int ParseExplicitPort(string portText)
    {
        if (!int.TryParse(portText, out var port) || port < 0 || port > MaximumPortNumber)
            throw new UriFormatException("Invalid port specified.");

        return port;
    }

    private static Dictionary<string, string>? ParseQueryParameters(string query)
    {
        var queryText = query.TrimStart('?');

        return string.IsNullOrWhiteSpace(queryText)
            ? null
            : queryText.Split(['&'], StringSplitOptions.RemoveEmptyEntries)
                .Select(parameter => parameter.Split(['='], 2))
                .ToDictionary(
                    split => split[0],
                    split => split.Length > 1 ? split[1] : string.Empty);
    }
}
