using System.Text;
using UriCredentialParser.Enums;

namespace UriCredentialParser;

public static class Extensions
{
    /// <param name="connectionParameters">
    /// The connection parameters containing details such as hostname, username, password, and database name.
    /// </param>
    extension(ConnectionParameters connectionParameters)
    {
        /// <summary>
        /// Converts the provided <paramref name="connectionParameters"/> into a Npgsql connection string.
        /// </summary>
        /// <param name="pooling">
        /// Specifies whether connection pooling should be enabled. Defaults to true.
        /// </param>
        /// <param name="sslMode">
        /// The SSL mode to use for the PostgreSQL connection. Defaults to <see cref="PostgresSSLMode.Prefer"/>.
        /// </param>
        /// <param name="trustServerCertificate">
        /// Specifies whether to trust the server certificate. Defaults to true.
        /// </param>
        /// <returns>
        /// A formatted Npgsql connection string based on the provided parameters. When the URI contained
        /// multiple hosts (replica set), the hosts and ports are emitted as comma-separated lists, which
        /// Npgsql supports for failover and load balancing.
        /// </returns>
        public string ToNpgsqlConnectionString(bool pooling = true, PostgresSSLMode sslMode = PostgresSSLMode.Prefer,
            bool trustServerCertificate = true)
        {
            var (hosts, ports) = connectionParameters.ResolveHostsAndPorts();

            return $"User ID={connectionParameters.UserName ?? string.Empty};" +
                   $"Password={connectionParameters.Password ?? string.Empty};" +
                   $"Server={hosts};" +
                   $"Port={ports};" +
                   $"Database={connectionParameters.DatabasePath ?? string.Empty};" +
                   $"Pooling={pooling.ToString().ToLowerInvariant()};" +
                   $"SSL Mode={sslMode.ToString()};" +
                   $"Trust Server Certificate={trustServerCertificate.ToString().ToLowerInvariant()}";
        }

        /// <summary>
        /// Converts the provided <paramref name="connectionParameters"/> into a MySQL connection string.
        /// </summary>
        /// <returns>
        /// A formatted MySQL connection string suitable for MySqlConnector or MySql.Data.
        /// </returns>
        public string ToMySqlConnectionString()
        {
            var (hosts, _) = connectionParameters.ResolveHostsAndPorts();

            return $"Server={hosts};" +
                   $"Port={connectionParameters.Port?.ToString() ?? string.Empty};" +
                   $"Database={connectionParameters.DatabasePath ?? string.Empty};" +
                   $"User ID={connectionParameters.UserName ?? string.Empty};" +
                   $"Password={connectionParameters.Password ?? string.Empty}";
        }

        /// <summary>
        /// Converts the provided <paramref name="connectionParameters"/> into a connection string compatible
        /// with StackExchange.Redis. Hosts are emitted as <c>host:port</c> pairs separated by commas; options
        /// and the password (if any) are appended as <c>,name=value</c> tokens.
        /// </summary>
        /// <returns>A StackExchange.Redis-compatible connection string.</returns>
        public string ToRedisConnectionString()
        {
            var builder = new StringBuilder();

            if (connectionParameters.Hosts is { Count: > 0 })
            {
                builder.Append(string.Join(",",
                    connectionParameters.Hosts.Select(endpoint =>
                        endpoint.Port.HasValue
                            ? $"{endpoint.HostName ?? string.Empty}:{endpoint.Port.Value}"
                            : endpoint.HostName ?? string.Empty)));
            }
            else
            {
                builder.Append(connectionParameters.HostName ?? string.Empty);
                if (connectionParameters.Port.HasValue)
                    builder.Append($":{connectionParameters.Port.Value}");
            }

            if (!string.IsNullOrWhiteSpace(connectionParameters.Password))
                builder.Append($",password={connectionParameters.Password!}");

            if (connectionParameters.AdditionalQueryParameters is { Count: > 0 })
            {
                foreach (var kvp in connectionParameters.AdditionalQueryParameters)
                {
                    if (string.Equals(kvp.Key, "password", StringComparison.OrdinalIgnoreCase))
                        continue;

                    builder.Append($",{kvp.Key}={kvp.Value}");
                }
            }

            return builder.ToString();
        }

        /// <summary>
        /// Formats a connection string using a user-provided template and placeholders from the current parameters.
        /// </summary>
        /// <param name="formatTemplate">
        /// The template to apply. Supported placeholders are
        /// <c>{Scheme}</c>, <c>{HostName}</c>, <c>{UserName}</c>, <c>{Password}</c>,
        /// <c>{DatabasePath}</c>, <c>{Port}</c> and <c>{QueryParameters}</c>.
        /// </param>
        /// <returns>A formatted connection string generated from the supplied template.</returns>
        /// <exception cref="ArgumentNullException">Thrown when <paramref name="formatTemplate"/> is null.</exception>
        public string ToConnectionString(string formatTemplate)
        {
            if (formatTemplate == null)
                throw new ArgumentNullException(nameof(formatTemplate));

            var queryParameters = connectionParameters.ComposeAdditionalQueryParameters() ?? string.Empty;

            return formatTemplate
                .Replace("{Scheme}", connectionParameters.Scheme ?? string.Empty)
                .Replace("{HostName}", connectionParameters.HostName ?? string.Empty)
                .Replace("{UserName}", connectionParameters.UserName ?? string.Empty)
                .Replace("{Password}", connectionParameters.Password ?? string.Empty)
                .Replace("{DatabasePath}", connectionParameters.DatabasePath ?? string.Empty)
                .Replace("{Port}", connectionParameters.Port?.ToString() ?? string.Empty)
                .Replace("{QueryParameters}", queryParameters);
        }

        /// <summary>
        /// Generates a MongoDB connection string and extracts the database name from the provided connection parameters.
        /// </summary>
        /// <returns>
        /// A tuple where the first element is the assembled MongoDB connection string and the second element is the extracted database name.
        /// </returns>
        public (string DatabaseUrl, string? DatabaseName) ToMongoConnectionSplit()
        {
            string userInfo;

            if (!string.IsNullOrWhiteSpace(connectionParameters.UserName))
                userInfo = $"{Uri.EscapeDataString(connectionParameters.UserName)}:" +
                           $"{Uri.EscapeDataString(connectionParameters.Password ?? string.Empty)}@";
            else
                userInfo = string.Empty;

            var builder =
                new StringBuilder($"{connectionParameters.Scheme}://{userInfo}");

            AppendHostList(builder, connectionParameters);

            if (connectionParameters.AdditionalQueryParameters is { Count: > 0 })
            {
                builder.Append('?');
                builder.Append(string.Join("&",
                    connectionParameters.AdditionalQueryParameters.Select(kvp =>
                        $"{Uri.EscapeDataString(kvp.Key)}={Uri.EscapeDataString(kvp.Value)}")));
            }

            return (builder.ToString(), connectionParameters.DatabasePath);
        }
    }

    internal static void AppendHostList(StringBuilder builder, ConnectionParameters connectionParameters)
    {
        if (connectionParameters.Hosts is { Count: > 0 })
        {
            builder.Append(string.Join(",",
                connectionParameters.Hosts.Select(endpoint =>
                    endpoint.Port.HasValue
                        ? $"{endpoint.HostName ?? string.Empty}:{endpoint.Port.Value}"
                        : endpoint.HostName ?? string.Empty)));
            return;
        }

        builder.Append(connectionParameters.HostName ?? string.Empty);
        if (connectionParameters.Port.HasValue)
            builder.Append($":{connectionParameters.Port.Value}");
    }

    internal static (string Hosts, string Ports) ResolveHostsAndPorts(this ConnectionParameters connectionParameters)
    {
        if (connectionParameters.Hosts is { Count: > 0 })
        {
            var hosts = string.Join(",",
                connectionParameters.Hosts.Select(endpoint => endpoint.HostName ?? string.Empty));
            var ports = string.Join(",",
                connectionParameters.Hosts.Select(endpoint => endpoint.Port?.ToString() ?? string.Empty));
            return (hosts, ports);
        }

        return (connectionParameters.HostName ?? string.Empty,
            connectionParameters.Port?.ToString() ?? string.Empty);
    }
}