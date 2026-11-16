# Contributor and agent guide

## Before changing code

Read the public [README](../readme.md) and the [architecture guide](architecture.md), then follow the existing feature that
owns the behavior being changed. Inspect the working tree and preserve unrelated work. Read the request contracts, handler
pipeline, supporting queries, and relevant integration tests before changing a permission or persistence rule.

Use the existing projects and package managers. Backend package versions are centralized in `Directory.Packages.props`;
frontend dependencies use npm and the committed `Web/ClientApp/package-lock.json`. Keep database scripts, schema input,
source generation, runtime initialization, and tests consistent when a schema changes.

## Code placement and style

A feature subfolder contains only the kind of item named by that folder. HTTP endpoints belong in `Endpoints`; reusable
management or validation helpers belong in services or validators; standalone feature exceptions belong in exceptions.
Keep related code within the feature unless it has a real cross-feature consumer.

Each LINQ operator starts on a separate line, including terminal operators. The deliberate exception is `Merge().Using()`,
which may remain together on one line. This makes the sequence of transformations easy to scan:

```csharp
var names = users
    .Where(user => user.IsActive)
    .OrderBy(user => user.Name)
    .Select(user => user.Name)
    .ToList();
```

```csharp
await context.Users
    .Merge().Using(source)
    .OnTargetKey()
    .InsertWhenNotMatched()
    .MergeAsync(token);
```

Put a blank line before and after a multiline statement, separating it from adjacent statements. For example:

```csharp
var userId = await currentUserService.GetLoggedInUserId();

var roles = await context.TenantRoles
    .Where(role => role.TenantId == request.TenantId)
    .ToListAsync(token);

return new Response { Roles = roles };
```

Apply the same spacing to multiline calls, declarations, and control-flow statements, rather than limiting the convention
to LINQ. Keep object properties and named parameters readable; avoid compressing a query or initializer to bypass the
spacing convention. Follow the repository's `.editorconfig` for indentation and the remaining C# style rules.

Prefer request and response records with named properties over positional record constructors. Keep database entities
explicitly qualified as `Database.Models` types. Use typed IDs consistently rather than weakening a feature contract to a
primitive integer for convenience.

## Validating a change

Choose checks that demonstrate the behavior affected by the change. Run frontend checks from `Web/ClientApp`:

```sh
npm run check
npm run build
```

Run the integration tests from the repository root with Docker available:

```sh
dotnet test --project Web.Tests/Web.Tests.csproj
```

For changes to hosting or build integration, also verify a production publish and the assets it contains:

```sh
dotnet publish Web/Web.csproj -c Release -o ./publish
```

Permission and membership changes should exercise both permitted and denied requests, independent tenant and organization
membership, activity state, and the visibility of changes on the next request. Persistence or transaction changes need
checks against the actual SQL Server schema. Prefer meaningful integration assertions to tests that merely restate the
implementation. A narrowly isolated rule can also warrant a focused unit test.

Do not weaken tests or architectural boundaries to make a check pass. Resolve a routine implementation choice within the
requested scope, and keep checks proportional to the change. Report checks that could not run and the concrete reason.

## Maintaining documentation

The public README explains how to build and run the application and why its architecture is arranged this way. Maintain its
teaching purpose when changing the implementation; project responsibilities and their interactions are part of the public
documentation.

`/docs` is for enduring repository and architectural guidance that helps future contributors and agents. Update a guide when
an architectural contract, convention, or long-term workflow changes. Do not record short-term reasoning, task progress,
specific query answers, one-off environment details, or a history of temporary approaches. A future agent should be able to
make an appropriate change without treating an old conversation as a permanent constraint.
