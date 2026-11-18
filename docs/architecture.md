# Architecture guide

## Project relationships

`Web` is the application host. Its HTTP operations and internal queries use generated Immediate.Handlers dispatch;
Immediate.Apis generates HTTP mappings; Immediate.Validations supplies validation; Immediate.Injections generates service
and options registration. The public [README](../readme.md#architectural-choices) explains why these libraries were chosen.

`Scaffold` is an executable invoked by the `Web` build. It creates a temporary SQL Server database, applies the migration
scripts, and writes `Web/Database/Models/Scaffold.json`. `SourceGen`, referenced as an analyzer, uses that schema description
and Vogen declarations to generate database models and the partial `DbContext`. It also generates synchronization for
`[SyncEnum]` declarations. `Analyzers`, also referenced as an analyzer, enforces repository conventions.

`Web/ClientApp` is an npm-managed React/TypeScript app inside the `Web` project. Vite builds its production assets, and the
server build synchronizes them to generated `wwwroot` content. The publish output contains the compiled assets, not the
frontend source or npm dependencies. Design-time builds do not run npm. The frontend remains independently runnable with
Vite's development server and backend proxy.

The browser uses shadcn/ui primitives under `Web/ClientApp/src/components/ui` and Tailwind CSS v4 through the Vite
plugin. Workspace layout and theme tokens live in `src/styles.css`. The theme control applies the selected palette
to the document root so portaled select menus and dialogs share the page colors. Hash links continue to identify
workspace routes and sections.

`Web.Client` provides a foundation for typed Refit clients. `Web.Tests` hosts the application through ASP.NET Core's
application factory and uses a real SQL Server Testcontainer for HTTP integration tests. Tests may use typed clients or
`HttpClient` directly; choose the form that makes the behavior under test clear.

## Feature ownership and folder meaning

Keep business operations within their feature under `Web/Features`. Feature subfolders identify the kind of code they hold:

| Folder | Contents |
| --- | --- |
| `Endpoints` | HTTP-mapped operations and the request/response contracts directly belonging to those operations. |
| `Queries` | Internal read operations that are not themselves HTTP endpoints. |
| `Models` | Feature models, typed IDs, enums, and contracts shared within the feature. |
| `Services` | Reusable feature operations, cache declarations, and supporting coordination. |
| `Authorization` | Scoped request contracts, authorization behaviors, and related access rules. |
| `Validators` | Reusable validation rules when a feature needs a separate validation abstraction. |
| `Exceptions` | Feature-specific exceptions when they are separate from an operation. |

Do not put an ordinary helper in `Endpoints` because endpoints happen to call it. A folder's name is a contract with the
reader. Introduce another appropriately named folder when its members warrant it rather than misclassifying the members.

`Web/Infrastructure` owns application mechanics such as authentication, email delivery, logging, and exception handling.
`Web/Database` owns persistence setup, migrations, and schema input. `Features/Shared` holds feature code with actual shared
consumers. Avoid turning a shared folder into a mandatory layer through which unrelated operations must pass.

## Authorization and caches

Authorization behaviors consume cache-backed answers only. Database information needed for an authorization decision
belongs in the query behind the relevant cache, including user or resource activity, membership, and effective permission
calculation. Do not add a separate database read in a behavior or hide the same eligibility check in another behavior
dependency. Keeping eligibility in the cache query gives authorization a single definition.

Current-user identifiers come from authenticated claims and are read synchronously, without an `async` or `ValueTask`
wrapper. API-key membership-owner resolution and permission lookups remain asynchronous when they need database or cache
data. Methods returning `ValueTask` use `async` and await their work, including cache forwarding, to preserve the method in
exception stack traces with runtime-async. Never return a `ValueTask` directly or block it to make a caller synchronous.

The scoped request contracts expose **static virtual** permission demands. Their default `None` demand expresses base
membership access; operations requiring management rights override the demand. Do not make the demand abstract merely to
force every read operation to repeat the default.

An empty effective permission set (`[]`) means that the scope cannot be used by that identity. A nonempty set containing
`None` represents base membership. Inactive entities return no ordinary scoped permissions. For example, an inactive tenant
must return `[]` from `TenantUserPermissionCache`. Explicit lifecycle administration, such as restoration, uses a separate
cached authorization answer instead of weakening the ordinary permission cache to permit access to inactive resources.

Tenant membership and organization membership are independent. A tenant is a collection of organizations; membership in an
organization does not imply, or require, membership in that tenant. Ordinary organization authorization depends on the
organization's own eligibility and permissions, without a parent tenant membership or activity gate. Removing a user's tenant
membership does not remove their separately assigned organization memberships.

A query that already produces effective organization permissions is the source for that result. Its caller should not
reconstruct permission calculation by querying tenant or organization user services again. Keep permission derivation and
its cache invalidation coordinated where the underlying state is read and changed.

After a committed role, membership, permission, or activity change, invalidate all affected cached views, including views for
delegated API keys. Finish revocation even when the HTTP caller disconnects after the database commit. Do not use an API-key
identity as a human member or expose its credential through public member records. Transactional guards must preserve the
application's administrator invariants when members or administrative roles change.

## Persistence and schema changes

SQL migration scripts under `Web/Database/Scripts` are the source of database structure. Add a new, monotonically ordered
script for a schema change; do not rewrite a script that could have been applied to an existing database. The application
embeds the scripts and applies missing migrations according to its version history at startup.

Scaffolding runs the scripts against a temporary database to discover the resulting schema. Keep `Scaffold.json` consistent
with that process and allow source generation to derive the C# models. Do not maintain a parallel handwritten version of a
generated model. SQL Server-specific types and behavior must be tested against SQL Server, not assumed to work with a
substitute provider.

Use Vogen ID types in feature contracts to distinguish identifiers with the same primitive representation. Refer to generated
database entities with their explicit `Database.Models` qualification, keeping persistence entities visibly separate from
feature request and response models.

## Browser and API responsibilities

Validate request and command input with Immediate.Validations, including nested bodies and internal entry points. Reuse
custom validators for shared input formats. Keep normalization, such as trimming, canonical email casing, and permission
deduplication, explicit in the operation. Preserve whether length limits apply before or after normalization. Authorization,
database existence checks, and transactional invariants remain with the code that owns those decisions; request validation
must not replace their status codes, locking, or state updates. Checks on generated server output remain server invariants.

The server enforces authentication, authorization, input validation, and persistence invariants. Frontend permission controls
explain available actions, but never replace API enforcement. Production UI and API share an origin; cookie-authenticated
mutations use antiforgery protection, while API-key clients use their own authentication path. Browser authentication uses
[session-bound email codes](authentication.md); keep that flow separate from the public homepage's content and from the
roles that authorize an authenticated identity.

Keep the public homepage capable of serving an application's public content independently of its authenticated workspace.
Frontend source, configuration, and tests live under `Web/ClientApp`; do not introduce a separate JavaScript project merely
for Visual Studio source review. Preserve the npm lockfile and server build/publish integration when changing the frontend.
