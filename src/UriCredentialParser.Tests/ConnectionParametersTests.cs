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
    public void ComposeAdditionalQueryParameters_WhenEmpty_ReturnsEmptyString()
    {
        var parameters = new ConnectionParameters("scheme", "host", null, null, null, null, new Dictionary<string, string>());

        var result = parameters.ComposeAdditionalQueryParameters();

        result.Must().BeEmpty();
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
}
