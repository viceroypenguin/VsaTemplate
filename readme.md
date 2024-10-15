# A Template Project for Vertical Slice Architecture

> [!NOTE]
> This is a heavily opinionated project, based on how I implement web projects. Some opinions I hold are heavily 
> debated. This is an attempt to show **a** VSA project, not how every VSA project should be implemented.

## Choices Made

### Immediate.Handlers

[Immediate.Handlers](https://github.com/ImmediatePlatform/Immediate.Handlers) is used to implement the command-dispatch
pattern, colloquially known as the "mediator" pattern, from a common library named MediatR. Immediate.Handlers uses
compile-time binding of commands, requests, responses, and pipeline behaviors to implement a high-performance link
between callers and callees that also allows cross-cutting concerns to be injected project-wide.

Already implemented in VsaTemplate are:
* `LoggingBehavior<,>`: used to log additional information about handler performance, as well as add log scope with
  request information which will be applied to any log entries made inside of the handler.
* `AuthorizationBehavior<,>`: used to authorize all handlers according to the created aspnetcore authorization policies.
* `TodoAuthorizationBehavior<,>`: used to authorize handlers responding to specific todo item entries. This technique
  allows custom authentication to be applied by feature.

### Immediate.Apis

[Immediate.Apis](https://github.com/immediateplatform/immediate.apis) is used to bind Immediate.Handlers handlers with
asp.net core Minimal APIs registrations at compile time. Adding `[MapGet(<route>)]` will generate code to register the
handler with Minimal APIs.

### Immediate.Validations

[Immediate.Validations](https://github.com/immediateplatform/immediate.validations) is used to generate validation
routines for Immediate.Handler handler request types. Applying the existing `ValidationBehavior<,>` behavior to the IH
pipeline means that requests are automatically validated via the pipeline.


### Linq2Db

A majority of the current C# templates today use Entity Framework Core. I personally prefer the advanced capabilities of
Linq2Db and so my projects rely heavily on this ORM instead of EFC. That said, a majority of the concepts in this
template can be applied to EFC as well.

#### Database Initialization

In choosing Linq2Db, the open question remains about ensuring reliability between the db schema and ORM representation of
the schema. I have developed a system that works for me to do so:

* Database migration scripts are stored as `.sql` files under `Services\Database\Scripts`; these scripts are designed to be
  strictly additive. Other than the two initial scripts (`00.VersionHistory.sql` and `01.Hangfire.v9.sql`), they should
  be named in a monotonically increasing format. This way a simple string sort of the filenames develops the order in
  which they should be executed against the database.
* The `Scaffold` project builds an executable that will, as a build step: generate a blank database, execute all scripts
  against the database, and scaffold the DB Schema into `Scaffold.json`.
* The migration scripts are baked into the `Services` dll as embedded resources. This way, at application
  initialization, the current list of executed scripts in `VersionHistory` can be compared against the known list of
  scripts and any missing scripts will be executed.

### Hangfire

Hangfire is a useful tool for executing background tasks for execution in an asp.net core project. There are two types
of jobs that can be loaded into Hangfire: one-time jobs and recurring jobs. One-time jobs are not a point of concern, 
as it is a simple process to create a job for Hangfire to execute.

To simplify implementation of background jobs, I have added the `IRecurringJob` interface and the `RecurringJob`
attribute. At application initialization, there is an initialization step that looks for any classes with the
`RecurringJob` attribute and registers the job as a background job with Hangfire. 

### Vogen

Vogen is used to fight against primitive obsession, where every id in the system is an `int`. Instead, all business layer
DTOs use a Vogen generated id type instead. 

### Auth0

Auth0 is a third-party platform for identity management. I prefer not to manage usernames and password in my databases, so
using Auth0 is a way to offload this part of the application. Once a user has logged in via Auth0, access control is done 
generally via Roles. A more developed application may also have additional access control based on individual user access;
this is not implemented in this template.

### MS SQL

Microsoft SQL Server is the database platform that I have used for my career. The concepts in this template will work fine 
with SQLite or PostgreSQL, either with Linq2Db or EFC; it is simply not currently implemented for them. 

### Six Projects

#### 1. `Scaffold` Project 

This project creates an executable that will run a series of scripts on a blank database and scaffold a linq2db
`DbContext` from the resultant schema. This is useful for managing a database-first (or script-first) approach to
database development while efficiently keeping the database schema and ORM model in sync.

The output exe from this project is used as a build step for the `Services` project.

#### 2. `SourceGen` Project

This project adds source generation to the `Services` project. There are two primary goals of this generation:

* It looks for any `enum`s that are marked with `[SyncEnum]` and creates an initialization script that will update the
  database table with the values in the `enum`. This is useful for automatically keeping the database tables up to date
  with the values in the C# `enum`.

* It loads the `Scaffold.json` file generated by the Scaffold project along with Vogen Value Types present in the
  assembly, in order to provide Vogen Value-Types for ORM representation of database columns.

#### 3. `Analyzer` Project

This project adds analyzers to ensure correct code in `Api` and `Web`.

* (Web only) VSA0001: Api Handler must support authorization by implementing `IAuthorizedRequest`.
  * This ensures that all api endpoints are properly protectedby an authorization check.
* VSA0002: Records with primary constructors are difficult to maintain; remove the primary constructor.
  * Positional notation for records are a) not compatible with database queries, and b) create bugs when refactoring
* VSA0003: Database entities should only be referenced via Database.Models.Entity.
  * Prevent accidentally referencing database entities casually. All entities should be explicitly referenced instead.

#### 4. `Api`/`Web` Project

These projects contain the core of the application; there is one template for API-only projects, and another template for
Blazor+API projects. The core architecture of each is very similar and follows the following pattern:

* The `Database` folder contains the `DbContext`: scripts, initialization code, model code, etc. 

* The `Infrastructure` folder contains code to support the infrastructure of the application. Things like `Authentication`, 
  `Authorization`, `Caching`, etc. can be found here. In general, no business logic of any kind should be found here.

* The `Features` folder contains the business logic of the application. It is organized by general concept/slice/feature,
  such as the `Todos` feature containing code to read, create, and update todo items; and the `Users` feature containing
  code to manage users and api keys.

  * The `Shared` folder will contain business logic code that is intended to be consumed broadly, like extension
    methods, etc.

#### 5. `Api.Client`/`Web.Client` Project

These projects contain `Refit` clients which build on `HttpClient` to provide strong-typed api definitions for C#. These
are strictly used to provide foundations for the unit tests. 

#### 6. `Api.Tests`/`Web.Tests` Project

These projects perform integration testing on the `Api`/`Web` project. The `ApplicationFactoryFixture` class sets up a
testing environment consisting of the following:

* An MSSQL Test Container containing a single target database, which is then initialized using the database migration scripts.

* A `WebApplicationFactory` based on the `Program.cs`, configured to access the MSSQL Test Container.

* Exposes `HttpClient` instances coded for various permission levels. These `HttpClient` instances can then be provided
  to the `Refit` clients exposed by the `Api.Client`/`Web.Client` projects to make API calls testing the full
  integration of the api calls.

  * Integration tests are preferred over unit tests. Unit tests rely on an accurate mocking of the real service, which is
    frequently difficult to accomplish, or allowing the consuming code to operate in the face of differences between the two.
    Only implementing integration tests is easier; especially when test containers are 

## Installation

This template is designed primarily as a reading template and guide for implementing a theoretical VSA project. While
this project can be downloaded and used as a base project, it does not include advanced error handling or unit testing.
To run this project, do the following:

1. Clone the project
2. Add a `Secrets.targets` file to the `Services` project. This file will contain the following (note that `;` are replaced with `%3B`
   to work around msbuild encoding issues:

```xml
<?xml version="1.0" encoding="UTF-8"?>
<Project>

	<PropertyGroup>
		<ScaffoldConnectionString>Server=<server>%3BUser=<user>%3BPassword=<password>%3Bapp=VsaTemplate.scaffold</ScaffoldConnectionString>
	</PropertyGroup>

</Project>
```

3. Create a blank database on a MS SQL Server. At first run, this project will automatically execute migrations necessary at startup.
4. Add a `secrets.json` file to the `Web` project. This file will contain the following:

```json
{
	"DbContextOptions": {
		"ConnectionString": "<blank db>"
	},

	"Auth0": {
		"Domain": "<from-auth0>",
		"ClientId": "<from-auth0>"
	},

	"ProcessFeatureJob": {
		"Enabled": false
	},

	"EmailServiceOptions": {
		"Host": "smtp.sendgrid.net",
		"Port": 587,
		"Username": "apikey",
		"Password": "<from-auth0>",
		"FromEmailAddress": "VSA Template <no-reply@VsaTemplate.com>",
		"AdminEmailAddresses": [
			"Admin User <admin@example.com>"
		]
	}
}
```

With these two files, the project should build and run as expected.
