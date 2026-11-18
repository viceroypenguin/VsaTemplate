# Contributor and agent guide

## Before changing code

Read the public [README](../readme.md) and the [architecture guide](architecture.md), then follow the existing feature that
owns the behavior being changed. Inspect the working tree and preserve unrelated work. Read the request contracts, handler
pipeline, supporting queries, and relevant integration tests before changing a permission or persistence rule.

Use the existing projects and package managers. Backend package versions are centralized in `Directory.Packages.props`;
frontend dependencies use npm and the committed `Web/ClientApp/package-lock.json`. Keep database scripts, schema input,
source generation, runtime initialization, and tests consistent when a schema changes.

## Code placement and style

Use feature subfolders for the kind of item they name. Put HTTP endpoints in `Endpoints`, reusable management or
validation helpers in services or validators, and standalone feature exceptions in exceptions. A cache and the handler
named by its `[CacheFor<T>]` attribute belong in the same file. Keep related code within the feature unless it has a real
cross-feature consumer.

Prefer request and response records with named properties over positional record constructors. Keep database entities
explicitly qualified as `Database.Models` types. Use typed IDs consistently rather than weakening a feature contract to a
primitive integer for convenience.

Use `async` and `await` in every `ValueTask`-returning implementation rather than returning a `ValueTask` directly. The
wrapper is intentional for error stack reporting with runtime-async; do not remove it as a simplification. Keep genuinely
synchronous methods synchronous and return their values without an asynchronous wrapper.

## Formatting C# with ReSharper

Follow `.editorconfig` for C# formatting and syntax style. Restore the pinned ReSharper tool and run the formatting
profile from the repository root.

```sh
dotnet tool restore
dotnet jb cleanupcode VsaTemplate.slnx --profile="Built-in: Reformat Code" --no-build --include="**/*.cs"
```

For the repository's syntax cleanup, use `--profile="Full Cleanup"` to select the shared profile in
`VsaTemplate.slnx.DotSettings`. Omitting `--profile` on a solution selects JetBrains' built-in Full Cleanup.
Cleanup writes files in place, so scope `--include` to the files you are editing and review the diff. To exclude personal
settings, add `--disable-settings-layers="GlobalAll;GlobalPerProduct;SolutionPersonal;ProjectPersonal"`.

## Working on the frontend

Run frontend commands in `Web/ClientApp` using npm. To add an official shadcn/ui component, use the configured
registry and commit the generated source, dependency changes, and npm lockfile together. This command adds the
separator component.

```sh
npx shadcn@latest add separator
```

`components.json` configures the registry style and aliases. Use `@/components/ui` for primitives and keep
workspace-specific behavior in the feature components. Set shared colors and radii in `src/styles.css`, where the
Tailwind tokens use the Immediate.Jobs dashboard palette for both themes. When editing controls, check focus and
keyboard behavior in both themes, including select menus and confirmation dialogs.

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
