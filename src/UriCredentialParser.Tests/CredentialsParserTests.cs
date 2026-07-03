using OmniAssert;

namespace UriCredentialParser.Tests;

[TestFixture]
public class CredentialsParserTests
{
    [Test]
    public void Parse_ValidAbsoluteUri_ReturnsPopulatedConnectionParameters()
    {
        var url = "postgres://admin:secret@localhost:5432/testdb?timeout=30";

        var result = CredentialsParser.Parse(url);

        result.Must().NotBeNull();
        result.Scheme.Must().Be("postgres");
        result.UserName.Must().Be("admin");
        result.Password.Must().Be("secret");
        result.HostName.Must().Be("localhost");
        result.Port.Must().Be(5432);
        result.DatabasePath.Must().Be("testdb");
        result.AdditionalQueryParameters.Must().NotBeNull();
        result.AdditionalQueryParameters!["timeout"].Must().Be("30");
    }

    [Test]
    public void Parse_MultipleQueryParameters_ReturnsCorrectDictionary()
    {
        var url = "mysql://user:pass@localhost/db?timeout=30&ssl=true&mode=readonly";

        var result = CredentialsParser.Parse(url);

        result.AdditionalQueryParameters.Must().NotBeNull();
        result.AdditionalQueryParameters!.Count.Must().Be(3);
        result.AdditionalQueryParameters!["timeout"].Must().Be("30");
        result.AdditionalQueryParameters!["ssl"].Must().Be("true");
        result.AdditionalQueryParameters!["mode"].Must().Be("readonly");
    }

    [Test]
    public void Parse_QueryParameterWithoutValue_ReturnsEmptyStringValue()
    {
        var url = "redis://localhost?debug";

        var result = CredentialsParser.Parse(url);

        result.AdditionalQueryParameters.Must().NotBeNull();
        result.AdditionalQueryParameters.Must().ContainKey("debug");
        result.AdditionalQueryParameters!["debug"].Must().BeEmpty();
    }

    [Test]
    public void Parse_UriWithoutCredentials_ReturnsEmptyCredentials()
    {
        var url = "mongodb://localhost/mydb";

        var result = CredentialsParser.Parse(url);

        result.UserName.Must().BeEmpty();
        result.Password.Must().BeEmpty();
        result.HostName.Must().Be("localhost");
        result.DatabasePath.Must().Be("mydb");
    }

    [Test]
    public void Parse_NullUrl_ThrowsArgumentNullException()
    {
        Action act = () => CredentialsParser.Parse(null!);

        act.Throws<ArgumentNullException>()
            .WithMessageContaining("url");
    }

    [TestCase("")]
    [TestCase("   ")]
    public void Parse_EmptyOrWhitespaceUrl_ThrowsArgumentException(string invalidUrl)
    {
        Action act = () => CredentialsParser.Parse(invalidUrl);

        act.Throws<ArgumentException>()
            .WithMessageContaining("url");
    }

    [Test]
    public void Parse_InvalidUriFormat_ThrowsUriFormatException()
    {
        Action act = () => CredentialsParser.Parse("not-a-valid-url");

        act.Throws<UriFormatException>();
    }

    [Test]
    public void Parse_UrlEncodedCredentials_DecodesCorrectly()
    {
        var url = "postgres://u%20ser:p%40ssword@localhost:5432/testdb";

        var result = CredentialsParser.Parse(url);

        result.Must().NotBeNull();
        result.UserName.Must().Be("u ser");
        result.Password.Must().Be("p@ssword");
    }

    [Test]
    public void Parse_ExplicitPortZero_ReturnsPortZero()
    {
        var url = "postgres://host:0/db";

        var result = CredentialsParser.Parse(url);

        result.Must().NotBeNull();
        result.Port.Must().Be(0);
    }

    [Test]
    public void Parse_InvalidNegativePort_ThrowsUriFormatException()
    {
        Action act = () => CredentialsParser.Parse("postgres://host:-1/db");

        act.Throws<UriFormatException>();
    }

    [Test]
    public void Parse_InvalidPortOutOfRange_ThrowsUriFormatException()
    {
        Action act = () => CredentialsParser.Parse("postgres://host:65536/db");

        act.Throws<UriFormatException>();
    }

    [Test]
    public void Parse_CredentialsWithoutPassword_ReturnsEmptyPassword()
    {
        var url = "postgres://user@localhost/db";

        var result = CredentialsParser.Parse(url);

        result.UserName.Must().Be("user");
        result.Password.Must().BeEmpty();
        result.HostName.Must().Be("localhost");
        result.DatabasePath.Must().Be("db");
    }

    [Test]
    public void Parse_UriWithoutExplicitPortAndNoDefaultScheme_ReturnsNullPort()
    {
        var url = "myscheme://host/db";

        var result = CredentialsParser.Parse(url);

        result.Scheme.Must().Be("myscheme");
        result.HostName.Must().Be("host");
        result.Port.Must().BeNull();
        result.DatabasePath.Must().Be("db");
    }

    [Test]
    public void Parse_AuthorityOnlyUri_ReturnsPortAndEmptyDatabasePath()
    {
        var url = "postgres://host:1234";

        var result = CredentialsParser.Parse(url);

        result.Scheme.Must().Be("postgres");
        result.HostName.Must().Be("host");
        result.Port.Must().Be(1234);
        result.DatabasePath.Must().BeEmpty();
    }

    [Test]
    public void Parse_Ipv6AddressWithExplicitPort_ReturnsPort()
    {
        var url = "postgres://[::1]:5432/db";

        var result = CredentialsParser.Parse(url);

        result.Scheme.Must().Be("postgres");
        result.HostName.Must().Be("[::1]");
        result.Port.Must().Be(5432);
        result.DatabasePath.Must().Be("db");
    }

    [Test]
    public void Parse_Ipv6AddressWithoutPortAndNoDefaultScheme_ReturnsNullPort()
    {
        var url = "myscheme://[::1]/db";

        var result = CredentialsParser.Parse(url);

        result.Scheme.Must().Be("myscheme");
        result.HostName.Must().Be("[::1]");
        result.Port.Must().BeNull();
        result.DatabasePath.Must().Be("db");
    }

    [Test]
    public void Parse_RelativeNotationUriWithoutSchemeSeparator_ParsesAsFileScheme()
    {
        var url = "//host/db";

        var result = CredentialsParser.Parse(url);

        result.Scheme.Must().Be("file");
        result.HostName.Must().Be("host");
        result.Port.Must().BeNull();
        result.DatabasePath.Must().Be("db");
    }

    [Test]
    public void Parse_UriWithDefaultSchemePortAndNoExplicitPort_ReturnsDefaultPort()
    {
        var url = "http://host/db";

        var result = CredentialsParser.Parse(url);

        result.Scheme.Must().Be("http");
        result.HostName.Must().Be("host");
        result.Port.Must().Be(80);
        result.DatabasePath.Must().Be("db");
    }
}