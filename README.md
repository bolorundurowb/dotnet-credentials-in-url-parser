# dotnet-credentials-in-url-parser

This package replaces both [PostgresConnString.NET](https://github.com/bolorundurowb/PostgresConnString.NET/tree/master) and [mongo-url-parser](https://github.com/bolorundurowb/mongo-url-parser) as they are very basic packages.

It aims to grant general .NET support for parsing and converting urls in the form "scheme://user:password@host:port/database?connectionparameters" also known as Credential-In-Url and converting it to formats easily used by the database service providers for .NET.


## Installation

You can install the package from nuget

```
Install-Package ciu-parser
```

or

```
dotnet add package ciu-parser
```

or for paket

```
paket add ciu-parser
```

## Usage

### Parsing Urls

To parse a url

```csharp
using CredentialsInUrlParser;
...
var details = CIU.Parse("postgres://someuser:somepassword@somehost:381/somedatabase");
```

The resulting details contains a subset of the following properties:

* `Scheme` - Database server scheme
* `HostName` - Database server hostname
* `Port` - port on which to connect
* `UserName` - User with which to authenticate to the server
* `Password` - Corresponding password
* `DatabasePath` - Database name within the server
* `AdditionalQueryParameters` - Additional database parameters provided as query options

### Exports

Currently, this library allows for generating an Npgsql compatible connection strings. With the following parameters

* `pooling`: type: boolean, default: true
* `trustServerCertificate`: type: boolean, default: true
* `sslMode`: type: enum, default: Prefer

```csharp
using CredentialsInUrlParser;
...
var details = CIU.Parse("postgres://someuser:somepassword@somehost:381/somedatabase");
var connString = details.ToNpgsqlSConnectionString(); //User ID=someuser;Password=somepassword;Server=somehost;Port=381;Database=somedatabase;Pooling=true;SSL Mode=Prefer;Trust Server Certificate=true
```

This library also allows for generating a MongoDB compatible connection string and database name.

```csharp
using CredentialsInUrlParser;
...
var details = CIU.Parse("mongodb://user:password@host:port/database-name?otheroptions");
var (dbUrl, dbName)  = details.ToMongoConnectionSplit(); 
// dbUrl: mongodb://user:password@host:port?otheroptions
// dbName: database-name
```

## Contributing

Feel free to make requests and to open pull requests with fixes and updates.