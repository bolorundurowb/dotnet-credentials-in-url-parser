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
        /// A formatted Npgsql connection string based on the provided parameters.
        /// </returns>
        public string ToNpgsqlConnectionString(bool pooling = true, PostgresSSLMode sslMode = PostgresSSLMode.Prefer,
            bool trustServerCertificate = true) =>
            $"User ID={connectionParameters.UserName};Password={connectionParameters.Password};Server={connectionParameters.HostName};Port={connectionParameters.Port};Database={connectionParameters.DatabasePath};Pooling={pooling.ToString().ToLowerInvariant()};SSL Mode={sslMode.ToString()};Trust Server Certificate={trustServerCertificate.ToString().ToLowerInvariant()}";

        /// <summary>
        /// Converts the provided <paramref name="connectionParameters"/> into a MySQL connection string.
        /// </summary>
        /// <returns>
        /// A formatted MySQL connection string suitable for MySqlConnector or MySql.Data.
        /// </returns>
        public string ToMySqlConnectionString() =>
            $"Server={connectionParameters.HostName};Port={connectionParameters.Port};Database={connectionParameters.DatabasePath};User ID={connectionParameters.UserName};Password={connectionParameters.Password}";

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

            if (string.IsNullOrWhiteSpace(connectionParameters.UserName) &&
                string.IsNullOrWhiteSpace(connectionParameters.Password))
                userInfo = string.Empty;
            else
                userInfo = $"{connectionParameters.UserName}:{connectionParameters.Password}@";

            var builder =
                new StringBuilder($"{connectionParameters.Scheme}://{userInfo}{connectionParameters.HostName}");

            if (connectionParameters.Port.HasValue)
                builder.Append($":{connectionParameters.Port}");

            if (connectionParameters.AdditionalQueryParameters is { Count: > 0 })
            {
                builder.Append('?');
                builder.Append(string.Join("&",
                    connectionParameters.AdditionalQueryParameters.Select(kvp => $"{kvp.Key}={kvp.Value}")));
            }

            return (builder.ToString(), connectionParameters.DatabasePath);
        }
    }
}