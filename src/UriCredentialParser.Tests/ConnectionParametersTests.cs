using OmniAssert;

namespace UriCredentialParser.Tests;

[TestFixture]
public class ConnectionParametersTests
{
    [Test]
    public void ComposeAdditionalQueryParameters_WhenNull_ReturnsNull()
    {
        var parameters = new ConnectionParameters("scheme", "host", null, null, null, null, null);

        var result = parameters.ComposeAdditionalQueryParameters();

        result.Must().BeNull();
    }

    [Test]
    public void ComposeAdditionalQueryParameters_WhenEmpty_ReturnsNull()
    {
        var parameters = new ConnectionParameters("scheme", "host", null, null, null, null, new Dictionary<string, string>());

        var result = parameters.ComposeAdditionalQueryParameters();

        result.Must().BeNull();
    }

    [Test]
    public void ComposeAdditionalQueryParameters_WithSingleParameter_ReturnsFormattedString()
    {
        var queryParams = new Dictionary<string, string> { { "key", "value" } };
        var parameters = new ConnectionParameters("scheme", "host", null, null, null, null, queryParams);

        var result = parameters.ComposeAdditionalQueryParameters();

        result.Must().Be("key=value");
    }

    [Test]
    public void ComposeAdditionalQueryParameters_WithMultipleParameters_ReturnsJoinedString()
    {
        var queryParams = new Dictionary<string, string>
        {
            { "timeout", "30" },
            { "ssl", "true" }
        };
        var parameters = new ConnectionParameters("scheme", "host", null, null, null, null, queryParams);

        var result = parameters.ComposeAdditionalQueryParameters();

        result.Must().Be("timeout=30&ssl=true");
    }

    [Test]
    public void ComposeAdditionalQueryParameters_WithReservedCharacters_EncodesKeyAndValue()
    {
        var queryParams = new Dictionary<string, string>
        {
            { "na me", "a&b=c+#" }
        };
        var parameters = new ConnectionParameters("scheme", "host", null, null, null, null, queryParams);

        var result = parameters.ComposeAdditionalQueryParameters();

        result.Must().Be("na%20me=a%26b%3Dc%2B%23");
    }

    [Test]
    public void ToString_WithCompleteParameters_ReconstructsUri()
    {
        var queryParams = new Dictionary<string, string>
        {
            { "timeout", "30" },
            { "sslmode", "require" }
        };
        var parameters = new ConnectionParameters("postgres", "localhost", "admin", "s3cret", "appdb", 5432, queryParams);

        var result = parameters.ToString();

        result.Must().Be("postgres://admin:s3cret@localhost:5432/appdb?timeout=30&sslmode=require");
    }

    [Test]
    public void ToString_WithEscapedCredentials_EncodesCredentialsInUri()
    {
        var parameters = new ConnectionParameters("postgres", "localhost", "user name", "p@ss", "db", 5432, null);

        var result = parameters.ToString();

        result.Must().Be("postgres://user%20name:p%40ss@localhost:5432/db");
    }

    [Test]
    public void ToSafeString_WithPassword_ReturnsMaskedUri()
    {
        var parameters = new ConnectionParameters("mysql", "db-server", "service-user", "top-secret", "main", 3306, null);

        var result = parameters.ToSafeString();

        result.Must().Be("mysql://service-user:***@db-server:3306/main");
    }
}
