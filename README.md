# Uri Credential Parser

[![Build, Test & Coverage](https://github.com/bolorundurowb/UriCredentialParser/actions/workflows/build-and-test.yml/badge.svg)](https://github.com/bolorundurowb/UriCredentialParser/actions/workflows/build-and-test.yml) [![codecov](https://codecov.io/gh/bolorundurowb/UriCredentialParser/graph/badge.svg?token=36BZ72JVU7)](https://codecov.io/gh/bolorundurowb/UriCredentialParser) [![NuGet](https://img.shields.io/nuget/v/ciu-parser.svg)](https://www.nuget.org/packages/ciu-parser/)

A lightweight .NET library for parsing **credential-in-URL** connection strings (for example: `scheme://user:password@host:port/database?options`) into a strongly-typed object and converting the result into common connection string formats.

## What this library does

- Parses absolute URIs into `ConnectionParameters`.
- Extracts credentials, host, scheme, database path, port, and query options.
- Decodes URL-encoded credentials when parsing.
- Supports explicit port validation (`0` to `65535`).
- Rebuilds URIs from parsed values (`ToString()`), including safe masked output (`ToMaskedUri()` / `ToSafeString()`).
- Exports to provider-specific or custom connection string formats:
  - PostgreSQL/Npgsql (`ToNpgsqlConnectionString`)
  - MySQL (`ToMySqlConnectionString`)
  - MongoDB split output (`ToMongoConnectionSplit`)
  - Template-based custom format (`ToConnectionString`)
- Lightweight package targeting `.NET Standard 2.0`.

## Installation

### NuGet

```bash
dotnet add package ciu-parser
```

## Quick Start

```csharp
using UriCredentialParser;

// Parse a connection URI
var parameters = CredentialsParser.Parse(
    "postgres://admin:secret@localhost:5432/testdb?timeout=30&sslmode=require");

Console.WriteLine(parameters.HostName); // localhost
Console.WriteLine(parameters.UserName); // admin
Console.WriteLine(parameters.AdditionalQueryParameters!["timeout"]); // 30
```

## Main API

### Parse a URI

```csharp
var parameters = CredentialsParser.Parse(
    "postgres://u%20ser:p%40ssword@localhost:5432/testdb?timeout=30&sslmode=require");

Console.WriteLine(parameters.Scheme);       // postgres
Console.WriteLine(parameters.HostName);     // localhost
Console.WriteLine(parameters.UserName);     // u ser
Console.WriteLine(parameters.Password);     // p@ssword
Console.WriteLine(parameters.DatabasePath); // testdb
Console.WriteLine(parameters.Port);         // 5432
```

### Reconstruct and safely log a URI

```csharp
var original = parameters.ToString();      // includes password
var masked = parameters.ToMaskedUri();     // password replaced with ***
var safe = parameters.ToSafeString();      // alias of ToMaskedUri()
```

### PostgreSQL (Npgsql)

```csharp
using UriCredentialParser;
using UriCredentialParser.Enums;

var connString = parameters.ToNpgsqlConnectionString(
    pooling: true,
    sslMode: PostgresSSLMode.Require,
    trustServerCertificate: false
);

// User ID=admin;Password=secret;Server=localhost;Port=5432;Database=testdb;Pooling=true;SSL Mode=Require;Trust Server Certificate=false
```

### MySQL

```csharp
var mySql = parameters.ToMySqlConnectionString();
// Server=localhost;Port=5432;Database=testdb;User ID=admin;Password=secret
```

### MongoDB

```csharp
using UriCredentialParser;

var mongo = CredentialsParser.Parse(
    "mongodb://admin:secret@localhost:27017/appdb?retryWrites=true");

var (databaseUrl, databaseName) = mongo.ToMongoConnectionSplit();
// databaseUrl: mongodb://admin:secret@localhost:27017?retryWrites=true
// databaseName: appdb
```

### Custom template-based connection string

Supported placeholders in `ToConnectionString(template)`:

- `{Scheme}`
- `{HostName}`
- `{UserName}`
- `{Password}`
- `{DatabasePath}`
- `{Port}`
- `{QueryParameters}`

```csharp
var template = "Host={HostName};Port={Port};Db={DatabasePath};User={UserName};Pwd={Password};Options={QueryParameters};Scheme={Scheme}";
var custom = parameters.ToConnectionString(template);
```

## Parsing Rules

`CredentialsParser.Parse` expects a valid absolute URI.

| Part of URL | Property | Description |
|-------------|----------|-------------|
| `scheme` | `Scheme` | e.g., `postgres`, `mongodb`, `redis` |
| `user:pass` | `UserName` / `Password` | Extracted from UserInfo |
| `host` | `HostName` | Server address |
| `port` | `Port` | Numeric port or `null` if omitted |
| `/path` | `DatabasePath` | The path segment (usually database name) |
| `?query` | `AdditionalQueryParameters` | A `Dictionary<string, string>` of options |

Additional behavior:

- Missing credentials are returned as empty strings.
- Query keys without values are included with an empty string value (for example `?debug`).
- `Port` is nullable; explicit `0` is supported.
- Invalid ports (negative or above `65535`) throw `UriFormatException`.

## ConnectionParameters Record

The `ConnectionParameters` record encapsulates the parsed data:

- **`Scheme`**: The URI scheme.
- **`HostName`**: The server host.
- **`UserName`** / **`Password`**: Credentials (defaults to empty strings if missing).
- **`DatabasePath`**: The database name or path.
- **`Port`**: The port number (`int?`).
- **`AdditionalQueryParameters`**: A dictionary of all query parameters.

Useful members:

- **`ComposeAdditionalQueryParameters()`**: Joins query values into `k=v&k2=v2`.
- **`ToString()`**: Reconstructs the URI.
- **`ToMaskedUri()` / `ToSafeString()`**: Reconstructs the URI while masking password values.

## Relationship to other packages

This library supersedes the narrower [PostgresConnString.NET](https://github.com/bolorundurowb/PostgresConnString.NET) and [mongo-url-parser](https://github.com/bolorundurowb/mongo-url-parser) packages with a single, shared parser and typed exports.

## Contributing

Contributions are welcome! Please feel free to submit a Pull Request.

### Development

To build and test the project:

```bash
cd src
dotnet build UriCredentialParser.slnx
dotnet test
```

## License

This project is licensed under the MIT License - see the [LICENSE](LICENSE) file for details.
