namespace CredentialsInUrlParser
{
    public class ConnectionParameters
    {
        /// <summary>
        /// The database scheme
        /// </summary>
        public string? Scheme { get; set; }
        
        /// <summary>
        /// The database server host address
        /// </summary>
        public string? HostName { get; set; }

        /// <summary>
        /// The port the database is on
        /// </summary>
        public int? Port { get; set; }

        /// <summary>
        /// The database name
        /// </summary>
        public string? DatabasePath { get; set; }

        /// <summary>
        /// The login user name
        /// </summary>
        public string? UserName { get; set; }

        /// <summary>
        /// The login password
        /// </summary>
        public string? Password { get; set; }

        /// <summary>
        /// The 
        /// </summary>
        public string? AdditionalQueryParameters { get; set; }

        /// <summary>
        /// Initialize with default values
        /// </summary>
        public ConnectionParameters() { }

        /// <summary>
        /// Initialize with specified values
        /// </summary>
        /// <param name="scheme">The database scheme</param>
        /// <param name="hostName">The database server host address</param>
        /// <param name="userName">The user name</param>
        /// <param name="password">The password</param>
        /// <param name="databasePathPath">The database name</param>
        /// <param name="port">The port the database is on</param>
        /// <param name="additionalQueryParameters">Any query parameters</param>
        public ConnectionParameters(string? scheme, string? hostName, string? userName, string? password, string? databasePathPath,
            int? port, string? additionalQueryParameters)
        {
            Scheme = scheme;
            HostName = hostName;
            UserName = userName;
            Password = password;
            DatabasePath = databasePathPath;
            Port = port;
            AdditionalQueryParameters = additionalQueryParameters;
        }
    }
}
