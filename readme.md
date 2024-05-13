# A Template Project for Vertical Slice Architecture

> [!NOTE]
> This is a heavily opinionated project, based on how I implement web projects. Some opinions I hold are heavily 
> debated. This is an attempt to show **a** VSA project, not how every VSA project should be implemented.

## Choices Made

### Linq2Db

A majority of the current C# templates today use Entity Framework Core. I personally prefer the advanced capabilities of
Linq2Db and so my projects rely heavily on this ORM instead of EFC. That said, a majority of the concepts in this
template can be applied to EFC as well.

### Database Initialization

In choosing Linq2Db, the open question remains about ensuring reliability between the db schema and ORM representation of
the schema. I have developed a system that works for me to do so:

* Database migration scripts are stored as `.sql` files under `Services\Database\Scripts`; these scripts are designed to be
  strictly additive. Other than the two initial scripts (`00.VersionHistory.sql` and `01.Hangfire.v9.sql`), they should
  be named in a monotonically increasing format. This way a simple string sort of the filenames develops the order in
  which they should be executed against the database.
* The `Scaffold` project builds an executable that will, as a build step: generate a blank database, execute all scripts
  against the database, and scaffold the ORM model.
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

### Mapperly

Mapperly is used to map data between various DTOs and db entities. This simplifies a lot of the boilerplate code for
copying data between objects.

In the past, I have rejected mapping libraries such as AutoMapper, because there can be hidden runtime bugs when
refactoring or changing entities. It is easy to forget to update one side of the object vs the other, and having this
show up at compile-time is much safer than at run-time.

However, Mapperly does have one advantage that even manually writing mapping code does not have: it will provide a build
error when properties are missing from one side or the other. An `.editorconfig` is added to the `Services` project to
enforce RMG012 and RMG020, which identify missing properties in either direction. This requires that any refactoring
include the DTOs and entities such that they are correctly matching.

### Auth0

Auth0 is a third-party platform for identity management. I prefer not to manage usernames and password in my databases, so
using Auth0 is a way to offload this part of the application. Once a user has logged in via Auth0, access control is done 
generally via Roles. A more developed application may also have additional access control based on individual user access;
this is not implemented in this template.

### MS SQL

Microsoft SQL Server is the database platform that I have used for my career. The concepts in this template will work fine 
with SQLite or PostgreSQL, either with Linq2Db or EFC; it is simply not currently implemented for them. 

### Four Projects

> [!NOTE]
> Normally VSA recommends only one or two projects

#### 1. `Scaffold` Project 

This project creates an executable that will run a series of scripts on a blank database and scaffold a linq2db
`DbContext` from the resultant schema. This is useful for managing a database-first (or script-first) approach to
database development while efficiently keeping the database schema and ORM model in sync.

The output exe from this project is used as a build step for the `Services` project.

#### 2. `SourceGen` Project

This project adds source generation to the `Services` project. It looks for any `enum`s that are marked with
`[SyncEnum]` and creates an initialization script that will update the database table with the values in the `enum`.
This is useful for automatically keeping the database tables up to date with the values in the C# `enum`.

#### 3. `Services` Project

This project holds the core functionality of the application. There are two "important" folders: `Database` and `Support`.

The `Database` folder contains the `DbContext`: initialization code, model code, etc. The build step is set up to 
generate the scaffolded orm models to the `Database\Models` folder.

The `Support` folder contains various an interface and two attributes. These are used to automatically register:
recurring Hangfire jobs; and option classes used as configuration. The code consuming these are found in the `Web`
project ([HangfireInitializationService](HangfireInitializationService.cs),
[ConfigureAllOptionsExtensions](ConfigureAllOptionsExtensions.cs)).

All other folders are organized by `Feature`, with `Jobs`, `Models`, and `Services` underneath them. 

#### 4. `Web` Project

This project 

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
