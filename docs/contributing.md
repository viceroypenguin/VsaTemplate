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

Keep short predicates, simple `foreach` sources, and small nested projections compact when they fit naturally in their
surrounding statement:

```csharp
if (requestType.Interfaces.Any(type => type.IsIAuthorizedRequest()))
    return;

foreach (var path in sqlFiles.OrderBy(Path.GetFileName, StringComparer.Ordinal))
    await LoadScript(path);

var keys = string.Join(',', columns.Select(column => column.MemberName));
```

A single call in an assignment or query lookup can still use a readable multiline layout. Do not collapse every
one-operator expression just because it could fit on one line; preserve deliberate layouts that make the receiver and
operation clear:

```csharp
var requestParameter = node.ParameterList.Parameters
    .FirstOrDefault();

var handlerSymbol = context.SemanticModel
    .GetDeclaredSymbol(handlerDeclaration, token);
```

For a multistage LINQ pipeline, put each operator on a separate line, including terminal operations and custom operators.
Keep the source expression together rather than splitting its member accesses. This makes the transformations easy to scan:

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

`Merge().Using()` may remain together on one line. Ordinary method calls are not automatically LINQ pipelines:
`string.Join(...)` and `string.Concat(...)` should keep their receiver and method name together. Choose wrapping from the
expression's structure rather than from method names alone; a short nested `Select` does not require expanding the whole
containing call.

Indent a nested pipeline relative to its containing expression. When a lambda returns a multiline pipeline, put the
containing call's closing parenthesis on its own line rather than attaching it to the last operator:

```csharp
var names = groups
    .Select(group => group
        .Distinct(StringComparer.Ordinal)
        .OrderBy(name => name, StringComparer.Ordinal)
        .ToEquatableReadOnlyList()
    );
```

Use blank lines to separate adjacent statements and logical groups, including before and after a multiline statement when
there are neighboring statements. Do not add padding immediately after an opening brace or before a closing brace. A first
or last statement in a block does not need a blank line between itself and the brace:

```csharp
private async Task<Response> LoadRoles(TenantId tenantId, CancellationToken token)
{
    var userId = currentUserService.GetLoggedInUserId();

    var roles = await context.TenantRoles
        .Where(role => role.TenantId == tenantId)
        .ToListAsync(token);

    return new Response { Roles = roles };
}
```

Apply this separation to multiline calls, declarations, and control flow without adding empty lines around every nested
block. Keep related short statements together when they form one readable group. Preserve meaningful grouping around
comments and preprocessor directives rather than treating them as reasons to pad blocks. Keep object properties and named
parameters readable; do not compress a query or initializer to bypass the spacing convention. Follow `.editorconfig` for
indentation and the remaining C# style rules.

Prefer request and response records with named properties over positional record constructors. Keep database entities
explicitly qualified as `Database.Models` types. Use typed IDs consistently rather than weakening a feature contract to a
primitive integer for convenience.

Use `async` and `await` in every `ValueTask`-returning implementation rather than returning a `ValueTask` directly. The
wrapper is intentional for error stack reporting with runtime-async; do not remove it as a simplification. Keep genuinely
synchronous methods synchronous and return their values without an asynchronous wrapper.

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
