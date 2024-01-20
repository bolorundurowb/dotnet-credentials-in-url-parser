using System.Text;
using CredentialsInUrlParser.Enums;

namespace CredentialsInUrlParser
{
    public static class Extensions
    {
        /// <summary>
        /// Generates a formatted, valid Npgsql connection with the connection details
        /// </summary>
        /// <returns>A formatted <see cref="string"/></returns>
        public static string ToNpgsqlConnectionString(this ConnectionParameters connectionParameters,
            bool pooling = true, PostgresSSLMode sslMode = PostgresSSLMode.Prefer,
            bool trustServerCertificate = true) =>
            $"User ID={connectionParameters.UserName};Password={connectionParameters.Password};Server={connectionParameters.HostName};Port={connectionParameters.Port};Database={connectionParameters.DatabasePath};Pooling={pooling.ToString().ToLowerInvariant()};SSL Mode={sslMode.ToString()};Trust Server Certificate={trustServerCertificate.ToString().ToLowerInvariant()}";

        public static (string DatabaseUrl, string? DatabaseName) ToMongoConnectionSplit(
            this ConnectionParameters connectionParameters)
        {
            string userInfo;

            if (string.IsNullOrWhiteSpace(connectionParameters.UserName) &&
                string.IsNullOrWhiteSpace(connectionParameters.UserName))
                userInfo = string.Empty;
            else
                userInfo = $"{connectionParameters.UserName}:{connectionParameters.Password}@";

            var builder =
                new StringBuilder($"{connectionParameters.Scheme}://{userInfo}{connectionParameters.HostName}");

            if (connectionParameters.Port.HasValue)
                builder.Append($":{connectionParameters.Port}");

            builder.Append(connectionParameters.AdditionalQueryParameters);

            return (builder.ToString(), connectionParameters.DatabasePath);
        }
    }
}
