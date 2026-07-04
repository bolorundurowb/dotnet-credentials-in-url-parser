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
    public void Parse_Ipv6AddressWithEmptyPort_ThrowsUriFormatException()
    {
        Action act = () => CredentialsParser.Parse("postgres://[::1]:/db");

        act.Throws<UriFormatException>();
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

    [Test]
    public void Parse_MultiHostMongodbUri_ReturnsAllHostsAndPrimaryFallbacks()
    {
        var url = "mongodb://host1:27017,host2:27018/mydb?replicaSet=myset";

        var result = CredentialsParser.Parse(url);

        result.Scheme.Must().Be("mongodb");
        result.HostName.Must().Be("host1");
        result.Port.Must().Be(27017);
        result.DatabasePath.Must().Be("mydb");
        result.Hosts.Must().NotBeNull();
        result.Hosts!.Count.Must().Be(2);
        result.Hosts[0].HostName.Must().Be("host1");
        result.Hosts[0].Port.Must().Be(27017);
        result.Hosts[1].HostName.Must().Be("host2");
        result.Hosts[1].Port.Must().Be(27018);
        result.AdditionalQueryParameters!["replicaSet"].Must().Be("myset");
    }

    [Test]
    public void Parse_MultiHostPostgresUriWithCredentials_PreservesCredentialsAcrossHosts()
    {
        var url = "postgres://user:pass@host1:5432,host2:5432/db";

        var result = CredentialsParser.Parse(url);

        result.UserName.Must().Be("user");
        result.Password.Must().Be("pass");
        result.HostName.Must().Be("host1");
        result.Port.Must().Be(5432);
        result.Hosts.Must().NotBeNull();
        result.Hosts!.Count.Must().Be(2);
        result.Hosts[1].HostName.Must().Be("host2");
        result.Hosts[1].Port.Must().Be(5432);
        result.DatabasePath.Must().Be("db");
    }

    [Test]
    public void Parse_MultiHostWithMixedPorts_ParsesEachPortIndependently()
    {
        var url = "mongodb://host1:27017,host2:27018,host3:27019/db";

        var result = CredentialsParser.Parse(url);

        result.Hosts.Must().NotBeNull();
        result.Hosts!.Count.Must().Be(3);
        result.Hosts[2].HostName.Must().Be("host3");
        result.Hosts[2].Port.Must().Be(27019);
    }

    [Test]
    public void Parse_MultiHostWithIpv6Addresses_ParsesAllEndpoints()
    {
        var url = "mongodb://[::1]:27017,[::2]:27018/db";

        var result = CredentialsParser.Parse(url);

        result.Hosts.Must().NotBeNull();
        result.Hosts!.Count.Must().Be(2);
        result.Hosts[0].HostName.Must().Be("[::1]");
        result.Hosts[0].Port.Must().Be(27017);
        result.Hosts[1].HostName.Must().Be("[::2]");
        result.Hosts[1].Port.Must().Be(27018);
    }

    [Test]
    public void Parse_MultiHostWithoutPorts_ReturnsNullPortsForEachEndpoint()
    {
        var url = "mongodb://host1,host2/db";

        var result = CredentialsParser.Parse(url);

        result.Hosts.Must().NotBeNull();
        result.Hosts!.Count.Must().Be(2);
        result.Hosts[0].Port.Must().BeNull();
        result.Hosts[1].Port.Must().BeNull();
        result.Port.Must().BeNull();
    }

    [Test]
    public void Parse_MultiHostWithInvalidPort_ThrowsUriFormatException()
    {
        Action act = () => CredentialsParser.Parse("mongodb://host1:27017,host2:99999/db");

        act.Throws<UriFormatException>();
    }

    [Test]
    public void Parse_SingleHostUri_PopulatesHostsListWithSingleEndpoint()
    {
        var url = "postgres://localhost:5432/db";

        var result = CredentialsParser.Parse(url);

        result.Hosts.Must().NotBeNull();
        result.Hosts!.Count.Must().Be(1);
        result.Hosts[0].HostName.Must().Be("localhost");
        result.Hosts[0].Port.Must().Be(5432);
    }

    [Test]
    public void TryParse_ValidUrl_ReturnsTrueAndPopulatesParameters()
    {
        var ok = CredentialsParser.TryParse("postgres://admin:secret@localhost:5432/db", out var parameters);

        ok.Must().BeTrue();
        parameters.Must().NotBeNull();
        parameters!.UserName.Must().Be("admin");
        parameters.HostName.Must().Be("localhost");
    }

    [TestCase(null)]
    [TestCase("")]
    [TestCase("   ")]
    public void TryParse_NullOrWhitespaceUrl_ReturnsFalseAndNullParameters(string? invalidUrl)
    {
        var ok = CredentialsParser.TryParse(invalidUrl!, out var parameters);

        ok.Must().BeFalse();
        parameters.Must().BeNull();
    }

    [Test]
    public void TryParse_MalformedUrl_ReturnsFalseAndNullParameters()
    {
        var ok = CredentialsParser.TryParse("not-a-valid-url", out var parameters);

        ok.Must().BeFalse();
        parameters.Must().BeNull();
    }

    [Test]
    public void TryParse_UrlWithInvalidPort_ReturnsFalseAndNullParameters()
    {
        var ok = CredentialsParser.TryParse("postgres://host:99999/db", out var parameters);

        ok.Must().BeFalse();
        parameters.Must().BeNull();
    }

    [Test]
    public void TryParse_MultiHostUrl_ReturnsTrueAndAllHosts()
    {
        var ok = CredentialsParser.TryParse("mongodb://host1:27017,host2:27018/db", out var parameters);

        ok.Must().BeTrue();
        parameters.Must().NotBeNull();
        parameters!.Hosts.Must().NotBeNull();
        parameters.Hosts!.Count.Must().Be(2);
    }

    [Test]
    public void Parse_UrlEncodedAtSignInUsername_DecodesToAtSign()
    {
        var url = "postgres://user%40name:pass@localhost:5432/db";

        var result = CredentialsParser.Parse(url);

        result.UserName.Must().Be("user@name");
        result.Password.Must().Be("pass");
    }

    [Test]
    public void Parse_QueryParametersWithEncodedCharacters_DecodesKeyAndValue()
    {
        var result = CredentialsParser.Parse("redis://localhost?retry%20writes=true%26safe");

        result.AdditionalQueryParameters.Must().NotBeNull();
        result.AdditionalQueryParameters!.Must().ContainKey("retry writes");
        result.AdditionalQueryParameters!["retry writes"].Must().Be("true&safe");
    }

    [Test]
    public void Parse_MultiHostUrl_ToString_RoundTripsOriginalUri()
    {
        const string url = "mongodb://user:pass@host1:27017,host2:27018/appdb?replicaSet=myset";

        var result = CredentialsParser.Parse(url);

        result.ToString().Must().Be(url);
    }
}