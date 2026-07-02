namespace UriCredentialParser;

public static class CredentialsParser
{
    /// <summary>
    /// Parses a given URL string into a <see cref="ConnectionParameters"/> object containing the components of the connection information.
    /// </summary>
    /// <param name="url">The URL string to parse. This must be a valid absolute URI.</param>
    /// <returns>An instance of <see cref="ConnectionParameters"/> representing the parsed connection details.</returns>
    /// <exception cref="ArgumentNullException">Thrown when the provided <paramref name="url"/> is null.</exception>
    /// <exception cref="ArgumentException">Thrown when the provided <paramref name="url"/> is empty, contains only whitespace, or is not a valid URL.</exception>
    public static ConnectionParameters Parse(string url)
    {
        if (url == null)
            throw new ArgumentNullException(nameof(url), "Url cannot be null.");

        if (string.IsNullOrWhiteSpace(url))
            throw new ArgumentException("Url cannot be empty or contain only whitespace characters.", nameof(url));

        var uri = new Uri(url, UriKind.Absolute);
        var auth = string.IsNullOrWhiteSpace(uri.UserInfo) ? ":" : uri.UserInfo;
        var authParts = auth.Split([':'], 2);

        int? port = null;
        var authorityStart = url.IndexOf("://");
        if (authorityStart >= 0)
        {
            authorityStart += 3;
        }
        else
        {
            authorityStart = url.IndexOf(':') + 1;
        }

        var pathStart = url.IndexOfAny(['/', '?', '#'], authorityStart);
        if (pathStart < 0)
        {
            pathStart = url.Length;
        }

        var authoritySegment = url.Substring(authorityStart, pathStart - authorityStart);
        var atIndex = authoritySegment.LastIndexOf('@');
        var hostPortSegment = atIndex >= 0 ? authoritySegment.Substring(atIndex + 1) : authoritySegment;

        string? portStr = null;
        if (hostPortSegment.StartsWith("["))
        {
            var closingBracket = hostPortSegment.IndexOf(']');
            if (closingBracket >= 0 && closingBracket < hostPortSegment.Length - 1 && hostPortSegment[closingBracket + 1] == ':')
            {
                portStr = hostPortSegment.Substring(closingBracket + 2);
            }
        }
        else
        {
            var colonIndex = hostPortSegment.IndexOf(':');
            if (colonIndex >= 0)
            {
                portStr = hostPortSegment.Substring(colonIndex + 1);
            }
        }

        if (portStr != null)
        {
            if (!int.TryParse(portStr, out var parsedPort) || parsedPort < 0 || parsedPort > 65535)
            {
                throw new UriFormatException("Invalid port specified.");
            }
            port = parsedPort;
        }
        else if (uri.Port > 0)
        {
            port = uri.Port;
        }

        var userName = Uri.UnescapeDataString(authParts[0]);
        var password = authParts.Length > 1 ? Uri.UnescapeDataString(authParts[1]) : string.Empty;
        var databaseName = uri.AbsolutePath.Trim('/');

        var query = uri.Query.TrimStart('?');
        var additionalParameters = string.IsNullOrWhiteSpace(query)
            ? null
            : query.Split(['&'], StringSplitOptions.RemoveEmptyEntries)
                .Select(p => p.Split(['='], 2))
                .ToDictionary(
                    split => split[0],
                    split => split.Length > 1 ? split[1] : string.Empty
                );

        return new ConnectionParameters(uri.Scheme, uri.Host, userName, password, databaseName, port, additionalParameters);
    }
}
