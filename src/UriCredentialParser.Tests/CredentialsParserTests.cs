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
}