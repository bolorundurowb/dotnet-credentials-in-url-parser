namespace CredentialsInUrlParser;

// ReSharper disable once InconsistentNaming
public class CIU
{
    /// <summary>
    /// Parse a credentials-in-url url
    /// </summary>
    /// <param name="url">A credentialed url</param>
    /// <returns>The <see cref="ConnectionParameters"/>The parsed connection parameters</returns>
    /// <exception cref="ArgumentNullException">Thrown on null input</exception>
    /// <exception cref="ArgumentException">Thrown on empty or whitespace input</exception>
    public static ConnectionParameters Parse(string url)
    {
        if (url == null)
            throw new ArgumentNullException(nameof(url), "Url cannot be null.");

        if (string.IsNullOrWhiteSpace(url))
            throw new ArgumentException("Url cannot be empty or contain only whitespace characters.", nameof(url));

        var uri = new Uri(url, UriKind.Absolute);
        var auth = string.IsNullOrWhiteSpace(uri.UserInfo) ? ":" : uri.UserInfo;
        var authParts = auth.Split([':'], 2);

        int? port = uri.Port > 0 ? uri.Port : null;
        var userName = authParts[0];
        var password = authParts[1];
        var databaseName = uri.AbsolutePath.Trim('/');

        return new ConnectionParameters(uri.Scheme, uri.Host, userName, password, databaseName, port, uri.Query);
    }
}
