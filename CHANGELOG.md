# Changelog

All notable changes to this project are documented in this file. The format is based on
[Keep a Changelog](https://keepachangelog.com/en/1.1.0/), and this project adheres to
[Semantic Versioning](https://semver.org/spec/v2.0.0.html).

## [3.1.0] - 2026-07-04

### Added

- **Multi-host / replica-set parsing**: `ConnectionParameters.Hosts`
  (`IReadOnlyList<HostEndpoint>?`) captures every `host:port` pair in URIs such as
  `mongodb://host1:27017,host2:27018/db`. `HostName` and `Port` continue to expose the
  primary (first) endpoint for backward compatibility.
- **`HostEndpoint` record** representing a single `host:port` pair.
- **`CredentialsParser.TryParse`**: non-throwing overload returning `bool` with an
  `out ConnectionParameters?`, ideal for user-facing input scenarios.
- **`ToRedisConnectionString()`** extension producing a StackExchange.Redis-compatible
  connection string (comma-separated `host:port` pairs followed by `,name=value` options).
- **CHANGELOG.md** for tracking version history (replaces release notes previously stored
  only in the `.csproj`).
- SourceLink / deterministic build configuration:
  - `<PublishRepositoryUrl>true</PublishRepositoryUrl>`
  - `<EmbedUntrackedSources>true</EmbedUntrackedSources>`
  - `<IncludeSymbols>true</IncludeSymbols>` / `<SymbolPackageFormat>snupkg</SymbolPackageFormat>`
  - `<Deterministic>true</Deterministic>` and `ContinuousIntegrationBuild` under GitHub Actions
  - `Microsoft.SourceLink.GitHub` package reference for source-link-enabled PDBs in NuGet.

### Changed

- `CredentialsParser.Parse` is now backed by a manual, dependency-light parser (no `System.Uri`
  construction of the full URL) so multi-host authorities that the framework previously rejected
  are handled consistently by the library itself.
- `ToNpgsqlConnectionString` now emits comma-separated `Server` and `Port` lists when the URI
  contains multiple hosts, matching Npgsql's multi-host failover/load-balancing syntax.
- `ToMongoConnectionSplit` now round-trips the full host list (including all ports) in the
  generated `DatabaseUrl` instead of only the primary host.
- README updated to document `TryParse`, multi-host URIs, Redis output, the `Hosts`/`HostEndpoint`
  API, and the SourceLink-enabled build.

### Notes

- URL-decoding of credentials via `Uri.UnescapeDataString` (already present for `user:pass`) is
  preserved; e.g. `user%40name` decodes to `user@name`.

## [3.0.0]

### Changed

- Renamed assembly and library to `UriCredentialParser` for a more intuitive identity.
- Updated target framework to align with .NET recommendations (`.NET Standard 2.0`).

## [2.0.0]

### Changed

- Rebranded namespace and class names to `UriCredentialParser`.
- Migrated `ConnectionParameters` to a `record` type and added query-parameter parsing.

## [1.x]

### Added

- Initial `CredentialsParser`, `ConnectionParameters`, and Npgsql/MySQL/MongoDB connection
  string extensions.