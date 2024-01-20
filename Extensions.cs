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
            $"User ID={connectionParameters.UserName};Password={connectionParameters.Password};Server={connectionParameters.HostName};Port={connectionParameters.Port};Database={connectionParameters.Database};Pooling={pooling.ToString().ToLowerInvariant()};SSL Mode={sslMode.ToString()};Trust Server Certificate={trustServerCertificate.ToString().ToLowerInvariant()}";
    }
}
