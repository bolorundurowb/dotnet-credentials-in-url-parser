namespace UriCredentialParser;

/// <summary>
/// Represents a single database server endpoint (host and optional port) extracted
/// from a connection URI. Multiple <see cref="HostEndpoint"/> values are produced
/// when parsing multi-host / replica-set connection strings such as
/// <c>mongodb://host1:27017,host2:27017/db</c>.
/// </summary>
/// <param name="HostName">Gets the host name or IP address (with surrounding brackets for IPv6).</param>
/// <param name="Port">Gets the port number, or <c>null</c> when no port was specified.</param>
public record HostEndpoint(string? HostName, int? Port);