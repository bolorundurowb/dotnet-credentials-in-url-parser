# Uri Credential Parser

[![Build, Test & Coverage](https://github.com/bolorundurowb/UriCredentialParser/actions/workflows/build-and-test.yml/badge.svg)](https://github.com/bolorundurowb/UriCredentialParser/actions/workflows/build-and-test.yml) [![codecov](https://codecov.io/gh/bolorundurowb/UriCredentialParser/graph/badge.svg?token=36BZ72JVU7)](https://codecov.io/gh/bolorundurowb/UriCredentialParser) [![NuGet](https://img.shields.io/nuget/v/ciu-parser.svg)](https://www.nuget.org/packages/ciu-parser/)

A lightweight .NET library for parsing **Credential-in-URL** connection strings (e.g., `scheme://user:password@host:port/database?options`) into structured objects, and converting them into **Npgsql** (PostgreSQL) or **MongoDB** connection strings.

## Features

- Parse complex database URIs into structured `ConnectionParameters`.
- Support for multiple schemes (PostgreSQL, MongoDB, etc.).
- Convert parsed parameters to standard connection strings for Npgsql and MongoDB.
- Handles query parameters as a dictionary.
- Lightweight and targets `.NET Standard 2.0`.
- Utilises C# 14 Extension Types for a clean and intuitive API.

## Installation

### NuGet

```bash
dotnet add package ciu-parser
```

## Quick Start

```csharp
using UriCredentialParser;

// Parse a connection URI
var details = CredentialsParser.Parse(
    "postgres://admin:secret@localhost:5432/testdb?timeout=30&sslmode=require");

Console.WriteLine(details.HostName); // localhost
Console.WriteLine(details.UserName); // admin
Console.WriteLine(details.AdditionalQueryParameters["timeout"]); // 30
```

### PostgreSQL (Npgsql)

```csharp
using UriCredentialParser;
using UriCredentialParser.Enums;

var connString = details.ToNpgsqlConnectionString(
    pooling: true,
    sslMode: PostgresSSLMode.Prefer
);
```

### MongoDB

```csharp
using UriCredentialParser;

var (databaseUrl, databaseName) = details.ToMongoConnectionSplit();
// databaseUrl: mongodb://admin:secret@localhost:5432?timeout=30&sslmode=require
// databaseName: testdb
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

## ConnectionParameters Record

The `ConnectionParameters` record encapsulates the parsed data:

- **`Scheme`**: The URI scheme.
- **`HostName`**: The server host.
- **`UserName`** / **`Password`**: Credentials (defaults to empty strings if missing).
- **`DatabasePath`**: The database name or path.
- **`Port`**: The port number (`int?`).
- **`AdditionalQueryParameters`**: A dictionary of all query parameters.

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
