namespace UriCredentialParser;

/// <summary>
/// Encapsulates connection details extracted from a URI, such as credentials, host, port, and database path.
/// </summary>
/// <param name="Scheme">Gets the URI scheme (e.g., "postgres", "mongodb").</param>
/// <param name="HostName">Gets the host name or IP address of the database server.</param>
/// <param name="UserName">Gets the username for authentication.</param>
/// <param name="Password">Gets the password for authentication.</param>
/// <param name="DatabasePath">Gets the database name or path extracted from the URI.</param>
/// <param name="Port">Gets the port number which the database server is listening on.</param>
/// <param name="AdditionalQueryParameters">Gets additional query parameters parsed from the URI query string.</param>
public record ConnectionParameters(
    string? Scheme,
    string? HostName,
    string? UserName,
    string? Password,
    string? DatabasePath,
    int? Port,
    Dictionary<string, string>? AdditionalQueryParameters)
{
    /// <summary>
    /// Gets the full list of host endpoints parsed from a connection URI. This is populated
    /// for multi-host / replica-set connection strings such as
    /// <c>mongodb://host1:27017,host2:27017/db</c>. <see cref="HostName"/> and <see cref="Port"/>
    /// always reflect the primary (first) endpoint for backward compatibility.
    /// </summary>
    public IReadOnlyList<HostEndpoint>? Hosts { get; init; }

    /// <summary>
    /// Combines additional query parameters into a single query string by concatenating
    /// key-value pairs with an equals sign ('=') and separating them with an ampersand ('&').
    /// </summary>
    /// <returns>
    /// A string representing the combined query parameters if the AdditionalQueryParameters
    /// property contains any key-value pairs; otherwise, null if no additional parameters are present.
    /// </returns>
    public string? ComposeAdditionalQueryParameters() => AdditionalQueryParameters is not { Count: > 0 }
        ? null
        : string.Join("&", AdditionalQueryParameters.Select(kvp =>
            $"{Uri.EscapeDataString(kvp.Key)}={Uri.EscapeDataString(kvp.Value)}"));

    /// <summary>
    /// Reconstructs the URI from the current connection parameters.
    /// </summary>
    /// <returns>A URI string composed from the current record values.</returns>
    public override string ToString() => BuildUri(maskPassword: false);

    /// <summary>
    /// Reconstructs the URI while masking the password value for safe logging.
    /// </summary>
    /// <returns>A URI string with password replaced by <c>***</c> when present.</returns>
    public string ToSafeString() => BuildUri(maskPassword: true);

    private string BuildUri(bool maskPassword)
    {
        var scheme = string.IsNullOrWhiteSpace(Scheme) ? "unknown" : Scheme;
        var authority = ComposeAuthority();
        var credentials = ComposeCredentials(maskPassword);
        var databasePath = string.IsNullOrWhiteSpace(DatabasePath)
            ? string.Empty
            : $"/{Uri.EscapeDataString(DatabasePath)}";
        var query = ComposeAdditionalQueryParameters();
        var queryText = string.IsNullOrWhiteSpace(query) ? string.Empty : $"?{query}";

        return $"{scheme}://{credentials}{authority}{databasePath}{queryText}";
    }

    private string ComposeAuthority()
    {
        if (Hosts is { Count: > 0 })
        {
            return string.Join(",", Hosts.Select(endpoint =>
                endpoint.Port.HasValue
                    ? $"{endpoint.HostName ?? string.Empty}:{endpoint.Port.Value}"
                    : endpoint.HostName ?? string.Empty));
        }

        var host = HostName ?? string.Empty;
        var port = Port.HasValue ? $":{Port.Value}" : string.Empty;
        return $"{host}{port}";
    }

    private string ComposeCredentials(bool maskPassword)
    {
        var hasUserName = !string.IsNullOrWhiteSpace(UserName);
        var hasPassword = !string.IsNullOrWhiteSpace(Password);

        if (!hasUserName && !hasPassword)
            return string.Empty;

        var encodedUserName = hasUserName ? Uri.EscapeDataString(UserName!) : string.Empty;

        if (!hasPassword)
            return $"{encodedUserName}@";

        var password = maskPassword
            ? "***"
            : Uri.EscapeDataString(Password!);

        return $"{encodedUserName}:{password}@";
    }
}