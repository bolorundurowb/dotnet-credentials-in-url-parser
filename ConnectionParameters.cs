namespace CredentialsInUrlParser
{
    public class ConnectionParameters
    {
        /// <summary>
        /// The database server host address
        /// Default: ""
        /// </summary>
        public string? HostName { get; set; }

        /// <summary>
        /// The port the database is on
        /// Default: 5432
        /// </summary>
        public int? Port { get; set; }

        /// <summary>
        /// The database name
        /// Default: ""
        /// </summary>
        public string? Database { get; set; }

        /// <summary>
        /// The login user name
        /// Default: ""
        /// </summary>
        public string? UserName { get; set; }

        /// <summary>
        /// The login password
        /// Default: ""
        /// </summary>
        public string? Password { get; set; }

        /// <summary>
        /// Initialize with default values
        /// </summary>
        public ConnectionParameters() { }

        /// <summary>
        /// Initialize with specified values
        /// </summary>
        /// <param name="hostName">The database server host address</param>
        /// <param name="userName">The user name</param>
        /// <param name="password">The password</param>
        /// <param name="databasePath">The database name</param>
        /// <param name="port">The port the database is on</param>
        public ConnectionParameters(string? hostName, string? userName, string? password, string? databasePath,
            int? port)
        {
            HostName = hostName;
            UserName = userName;
            Password = password;
            Database = databasePath;
            Port = port;
        }
    }
}
