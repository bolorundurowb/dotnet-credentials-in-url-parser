using OmniAssert;
using UriCredentialParser.Enums;

namespace UriCredentialParser.Tests;

[TestFixture]
public class ExtensionsTests
{
    [Test]
    public void ToNpgsqlConnectionString_WithDefaultParameters_ReturnsExpectedString()
    {
        var parameters = new ConnectionParameters("postgres", "db-server", "dbuser", "dbpass", "maindb", 5432, null);

        var result = parameters.ToNpgsqlConnectionString();

        result.Must().Be("User ID=dbuser;Password=dbpass;Server=db-server;Port=5432;Database=maindb;Pooling=true;SSL Mode=Prefer;Trust Server Certificate=true");
    }

    [Test]
    public void ToNpgsqlConnectionString_WithCustomParameters_ReturnsExpectedString()
    {
        var parameters = new ConnectionParameters("postgres", "127.0.0.1", "usr", "pwd", "db", 5433, null);

        var result = parameters.ToNpgsqlConnectionString(pooling: false, sslMode: PostgresSSLMode.Require, trustServerCertificate: false);

        result.Must().Be("User ID=usr;Password=pwd;Server=127.0.0.1;Port=5433;Database=db;Pooling=false;SSL Mode=Require;Trust Server Certificate=false");
    }

    [Test]
    public void ToMySqlConnectionString_WithFullParameters_ReturnsExpectedString()
    {
        var parameters = new ConnectionParameters("mysql", "mysql-server", "root", "p@ss", "inventory", 3306, null);

        var result = parameters.ToMySqlConnectionString();

        result.Must().Be("Server=mysql-server;Port=3306;Database=inventory;User ID=root;Password=p@ss");
    }

    [Test]
    public void ToNpgsqlConnectionString_WithNullFields_UsesEmptyValues()
    {
        var parameters = new ConnectionParameters("postgres", null, null, null, null, null, null);

        var result = parameters.ToNpgsqlConnectionString();

        result.Must().Be("User ID=;Password=;Server=;Port=;Database=;Pooling=true;SSL Mode=Prefer;Trust Server Certificate=true");
    }

    [Test]
    public void ToMySqlConnectionString_WithNullFields_UsesEmptyValues()
    {
        var parameters = new ConnectionParameters("mysql", null, null, null, null, null, null);

        var result = parameters.ToMySqlConnectionString();

        result.Must().Be("Server=;Port=;Database=;User ID=;Password=");
    }

    [Test]
    public void ToConnectionString_WithCustomTemplate_ReplacesAllSupportedTokens()
    {
        var parameters = new ConnectionParameters(
            "oracle",
            "db.example.net",
            "appuser",
            "secret",
            "sales",
            1521,
            new Dictionary<string, string> { { "charset", "utf8" }, { "timeout", "20" } });

        var template = "Host={HostName};Port={Port};Service={DatabasePath};User={UserName};Pwd={Password};Options={QueryParameters};Scheme={Scheme}";

        var result = parameters.ToConnectionString(template);

        result.Must().Be("Host=db.example.net;Port=1521;Service=sales;User=appuser;Pwd=secret;Options=charset=utf8&timeout=20;Scheme=oracle");
    }

    [Test]
    public void ToConnectionString_WithNullQueryParameters_ReturnsEmptyQueryParameters()
    {
        var parameters = new ConnectionParameters("custom", "localhost", "u", "p", "db", 1, null);

        var result = parameters.ToConnectionString("Host={HostName};Options={QueryParameters}");

        result.Must().Be("Host=localhost;Options=");
    }

    [Test]
    public void ToConnectionString_WithTemplateMissingPlaceholders_KeepsLiteralTokens()
    {
        var parameters = new ConnectionParameters("custom", "localhost", "u", "p", "db", 1, null);

        var result = parameters.ToConnectionString("Host={HostName}");

        result.Must().Be("Host=localhost");
    }

    [Test]
    public void ToConnectionString_WithAlternativeSinglePlaceholder_CoversRemainingReplaceBranches()
    {
        var parameters = new ConnectionParameters("custom", "localhost", "u", "p", "db", 1, null);

        var result = parameters.ToConnectionString("Port={Port}");

        result.Must().Be("Port=1");
    }

    [Test]
    public void ToConnectionString_WithNullFields_UsesEmptyValues()
    {
        var parameters = new ConnectionParameters(null, null, null, null, null, null, null);

        var result = parameters.ToConnectionString("{Scheme}:{HostName}:{UserName}:{Password}:{DatabasePath}:{Port}:{QueryParameters}");

        result.Must().Be("::::::");
    }

    [Test]
    public void ToConnectionString_WithNullTemplate_ThrowsArgumentNullException()
    {
        var parameters = new ConnectionParameters("custom", "localhost", "u", "p", "db", 1, null);

        Action act = () => parameters.ToConnectionString(null!);

        act.Throws<ArgumentNullException>();
    }

    [Test]
    public void ToMongoConnectionSplit_WithFullCredentialsAndPort_ReturnsCorrectTuple()
    {
        var parameters = new ConnectionParameters("mongodb", "mongo-cluster", "admin", "pass123", "appdb", 27017, new Dictionary<string, string> { { "retryWrites", "true" } });

        var (url, dbName) = parameters.ToMongoConnectionSplit();

        url.Must().Be("mongodb://admin:pass123@mongo-cluster:27017?retryWrites=true");
        dbName.Must().Be("appdb");
    }

    [Test]
    public void ToMongoConnectionSplit_WithReservedCharacters_EncodesCredentialsAndQuery()
    {
        var parameters = new ConnectionParameters(
            "mongodb",
            "mongo-cluster",
            "ad:min",
            "p@ss/word?",
            "appdb",
            27017,
            new Dictionary<string, string> { { "retry writes", "true&w=majority" } });

        var (url, dbName) = parameters.ToMongoConnectionSplit();

        url.Must().Be("mongodb://ad%3Amin:p%40ss%2Fword%3F@mongo-cluster:27017?retry%20writes=true%26w%3Dmajority");
        dbName.Must().Be("appdb");
    }

    [Test]
    public void ToMongoConnectionSplit_WithoutCredentials_ReturnsCorrectTuple()
    {
        var parameters = new ConnectionParameters("mongodb", "localhost", null, null, "localdb", null, null);

        var (url, dbName) = parameters.ToMongoConnectionSplit();

        url.Must().Be("mongodb://localhost");
        dbName.Must().Be("localdb");
    }

    [Test]
    public void ToMongoConnectionSplit_WithUsernameButNullPassword_DoesNotThrow()
    {
        var parameters = new ConnectionParameters("mongodb", "host", "admin", null, "db", 27017, null);

        var (url, _) = parameters.ToMongoConnectionSplit();

        url.Must().Be("mongodb://admin:@host:27017");
    }

    [Test]
    public void ToMongoConnectionSplit_WithMultipleHosts_EmitsCommaSeparatedHostList()
    {
        var parameters = new ConnectionParameters(
            "mongodb",
            "host1",
            "admin",
            "pass",
            "appdb",
            27017,
            new Dictionary<string, string> { { "replicaSet", "myset" } })
        {
            Hosts = new List<HostEndpoint>
            {
                new("host1", 27017),
                new("host2", 27018)
            }
        };

        var (url, dbName) = parameters.ToMongoConnectionSplit();

        url.Must().Be("mongodb://admin:pass@host1:27017,host2:27018?replicaSet=myset");
        dbName.Must().Be("appdb");
    }

    [Test]
    public void ToMongoConnectionSplit_WithMultipleHostsWithoutPorts_OmitsPortSegments()
    {
        var parameters = new ConnectionParameters(
            "mongodb",
            "host1",
            null,
            null,
            "appdb",
            null,
            null)
        {
            Hosts = new List<HostEndpoint>
            {
                new("host1", null),
                new("host2", null)
            }
        };

        var (url, _) = parameters.ToMongoConnectionSplit();

        url.Must().Be("mongodb://host1,host2");
    }

    [Test]
    public void ToNpgsqlConnectionString_WithMultipleHosts_EmitsCommaSeparatedServersAndPorts()
    {
        var parameters = new ConnectionParameters(
            "postgres",
            "host1",
            "user",
            "pass",
            "db",
            5432,
            null)
        {
            Hosts = new List<HostEndpoint>
            {
                new("host1", 5432),
                new("host2", 5433)
            }
        };

        var result = parameters.ToNpgsqlConnectionString();

        result.Must().Be("User ID=user;Password=pass;Server=host1,host2;Port=5432,5433;Database=db;Pooling=true;SSL Mode=Prefer;Trust Server Certificate=true");
    }

    [Test]
    public void ToRedisConnectionString_WithHostNameAndPort_ReturnsStackExchangeFormat()
    {
        var parameters = new ConnectionParameters("redis", "localhost", null, "p@ss", null, 6379, null);

        var result = parameters.ToRedisConnectionString();

        result.Must().Be("localhost:6379,password=p@ss");
    }

    [Test]
    public void ToRedisConnectionString_WithoutPort_OmitsPortSegment()
    {
        var parameters = new ConnectionParameters("redis", "localhost", null, null, null, null,
            new Dictionary<string, string> { { "ssl", "true" } });

        var result = parameters.ToRedisConnectionString();

        result.Must().Be("localhost,ssl=true");
    }

    [Test]
    public void ToRedisConnectionString_WithMultipleHosts_EmitsCommaSeparatedHosts()
    {
        var parameters = new ConnectionParameters(
            "redis",
            "redis1",
            null,
            "topsecret",
            null,
            6379,
            new Dictionary<string, string> { { "ssl", "true" }, { "name", "mycluster" } })
        {
            Hosts = new List<HostEndpoint>
            {
                new("redis1", 6379),
                new("redis2", 6380)
            }
        };

        var result = parameters.ToRedisConnectionString();

        result.Must().Be("redis1:6379,redis2:6380,password=topsecret,ssl=true,name=mycluster");
    }

    [Test]
    public void ToRedisConnectionString_SkipsPasswordQueryParameterToAvoidDuplication()
    {
        var parameters = new ConnectionParameters(
            "redis",
            "localhost",
            null,
            "explicit",
            null,
            6379,
            new Dictionary<string, string> { { "password", "from-query" } });

        var result = parameters.ToRedisConnectionString();

        result.Must().Be("localhost:6379,password=explicit");
    }

    [Test]
    public void ToRedisConnectionString_WithDebugQueryParameter_EmitsEmptyValue()
    {
        var parameters = CredentialsParser.Parse("redis://localhost?debug");

        var result = parameters.ToRedisConnectionString();

        result.Must().Be("localhost,debug=");
    }

    [Test]
    public void ToRedisConnectionString_WithDecodedQueryParameters_DoesNotDoubleEncode()
    {
        var parameters = CredentialsParser.Parse("redis://localhost?retry%20writes=true%26safe");

        var result = parameters.ToRedisConnectionString();

        result.Must().Be("localhost,retry writes=true&safe");
    }

    [Test]
    public void ToMongoConnectionSplit_WithNullHostNameAndNullPort_UsesEmptyHost()
    {
        var parameters = new ConnectionParameters("mongodb", null, "user", "pass", "db", null, null);

        var (url, dbName) = parameters.ToMongoConnectionSplit();

        url.Must().Be("mongodb://user:pass@");
        dbName.Must().Be("db");
    }

    [Test]
    public void ToMongoConnectionSplit_WithNullHostNameAndPort_UsesEmptyHostWithPort()
    {
        var parameters = new ConnectionParameters("mongodb", null, "user", "pass", "db", 27017, null);

        var (url, dbName) = parameters.ToMongoConnectionSplit();

        url.Must().Be("mongodb://user:pass@:27017");
        dbName.Must().Be("db");
    }

    [Test]
    public void ToMongoConnectionSplit_WithHostsListAndNullHostNameEndpoint_HandlesGracefully()
    {
        var parameters = new ConnectionParameters(
            "mongodb",
            "host1",
            "user",
            "pass",
            "db",
            27017,
            null)
        {
            Hosts = new List<HostEndpoint>
            {
                new(null, 27017),
                new("host2", null)
            }
        };

        var (url, dbName) = parameters.ToMongoConnectionSplit();

        url.Must().Be("mongodb://user:pass@:27017,host2");
        dbName.Must().Be("db");
    }

    [Test]
    public void ToRedisConnectionString_WithNullHostNameAndPort_UsesEmptyHostWithPort()
    {
        var parameters = new ConnectionParameters("redis", null, null, "pass", null, 6379, null);

        var result = parameters.ToRedisConnectionString();

        result.Must().Be(":6379,password=pass");
    }

    [Test]
    public void ToNpgsqlConnectionString_WithHostsContainingNullHostName_EmitsEmptyHost()
    {
        var parameters = new ConnectionParameters(
            "postgres",
            "host1",
            "user",
            "pass",
            "db",
            5432,
            null)
        {
            Hosts = new List<HostEndpoint>
            {
                new(null, 5432),
                new("host2", 5433)
            }
        };

        var result = parameters.ToNpgsqlConnectionString();

        result.Must().Contain("Server=,host2");
        result.Must().Contain("Port=5432,5433");
    }

    [Test]
    public void ToRedisConnectionString_WithEmptyAdditionalQueryParameters_NoTrailingComma()
    {
        var parameters = new ConnectionParameters("redis", "localhost", null, "pass", null, 6379,
            new Dictionary<string, string>());

        var result = parameters.ToRedisConnectionString();

        result.Must().Be("localhost:6379,password=pass");
    }

    [Test]
    public void ToRedisConnectionString_WithHostsContainingNullHostName_EmitsEmptyHost()
    {
        var parameters = new ConnectionParameters(
            "redis",
            "redis1",
            null,
            "secret",
            null,
            6379,
            new Dictionary<string, string> { { "ssl", "true" } })
        {
            Hosts = new List<HostEndpoint>
            {
                new(null, 6379),
                new("redis2", 6380)
            }
        };

        var result = parameters.ToRedisConnectionString();

        result.Must().Contain(":6379,redis2:6380");
    }
}