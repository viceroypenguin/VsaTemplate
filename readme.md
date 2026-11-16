# A Template Project for Vertical Slice Architecture

This template combines an ASP.NET Core API with a React and TypeScript application for managing tenants,
organizations, their membership, and role assignments. Features live together as vertical slices, with
Immediate.Handlers, Immediate.Apis, Immediate.Validations, Immediate.Cache, and Immediate.Injections providing
handler dispatch, HTTP endpoints, validation, caching, and service registration.

## Projects

- `Web`: ASP.NET Core host, authenticated JSON endpoints, database migrations, and the production SPA host.
- `Web/ClientApp`: React/TypeScript UI, managed with npm and built with Vite within the `Web` project.
- `Web.Client`: foundation for typed Refit API clients.
- `Web.Tests`: integration tests using ASP.NET Core's application factory and SQL Server Testcontainers.
- `Scaffold`: creates a temporary database from migration scripts and writes the database schema to `Scaffold.json`.
- `SourceGen`: generates LinqToDB models, Vogen ID mappings, and enum synchronization from that schema.
- `Analyzers`: checks conventions for endpoints, request models, and database model access.

The backend stores infrastructure in `Web/Infrastructure` and domain features in `Web/Features`. Each feature
contains its endpoints, models, and authorization. SQL migrations under `Web/Database/Scripts` are additive;
the server applies outstanding migrations at startup. Business IDs use Vogen types rather than interchangeable integers.

## Prerequisites

- .NET 11 SDK.
- Node.js 22.12 or later and npm.
- SQL Server 2025. The migrations use its native `json` type.
- An Auth0 regular web application for browser login.
- Docker for the backend integration tests and container builds. A local SQL Server 2025 instance can run the
  application and schema scaffolder without Docker.

## Configuration

Create `Web/Secrets.targets` with the scaffolder's SQL Server connection string:

```xml
<Project>
  <PropertyGroup>
    <ScaffoldConnectionString>Server=localhost%3BUser ID=sa%3BPassword=your-password%3BTrustServerCertificate=true</ScaffoldConnectionString>
  </PropertyGroup>
</Project>
```

Use `%3B` for semicolons in this MSBuild property. The scaffolder needs permission to create and drop a temporary
database. Create a separate application database, then create `Web/secrets.json`:

```json
{
  "DbContextOptions": {
    "ConnectionString": "Server=localhost;Database=VsaTemplate;User ID=sa;Password=your-password;TrustServerCertificate=true"
  },
  "Auth0": {
    "Domain": "your-tenant.auth0.com",
    "ClientId": "your-client-id"
  },
  "EmailServiceOptions": {
    "ServerType": "Disabled",
    "FromEmailAddress": "VsaTemplate <no-reply@example.com>",
    "AdminEmailAddresses": ["admin@example.com"]
  },
  "OVERRIDEROOTUSER": "admin@example.com"
}
```

`OVERRIDEROOTUSER` optionally gives the named email address global administrative access. Any authenticated
user can create a tenant and becomes its first administrator. For production, configure the root account deliberately and
supply secrets through the host's configuration provider. `DbContextOptions:ConnectionStringInit` can supply
a separate database migration identity. Never commit either secrets file.

## Development

Build and run the backend:

```sh
dotnet run --project Web
```

The backend build restores frontend packages with `npm ci`, typechecks the React app, builds it, and copies its
assets to the generated `Web/wwwroot` directory. The application is available at `http://localhost:5106` or
`https://localhost:7013` with the development launch profile.

For React hot reload, run the backend and start Vite in a second terminal:

```sh
cd Web/ClientApp
npm ci
npm run dev
```

Open `http://localhost:5173`. Vite proxies API and authentication requests to `http://localhost:5106` by default.
Set `WEB_API_URL` when the backend uses another origin. Configure the Auth0 callback and logout URLs to match
the origin used for authentication, including the `/callback` path for the callback URL.

The UI lists tenants accessible to the signed-in user and provides forms for tenant and organization settings,
user membership, and roles. Available actions follow the user's permissions; the backend enforces those permissions
for every request. Existing API-key clients remain supported.

## Visual Studio

Open `VsaTemplate.slnx` and expand `Web/ClientApp` in the `Web` project to review React components, TypeScript,
styles, and npm configuration. The app is part of the existing C# project. Set `Web` as the startup project to
serve the compiled UI, or run `npm run dev` from `Web/ClientApp` alongside the backend for Vite hot reload.
The backend build runs the npm pipeline; package installation and generated output are excluded from project
content and publishing.

## Validation

Run frontend checks from `Web/ClientApp`:

```sh
npm run check
npm run build
```

Run backend integration tests from the repository root:

```sh
dotnet test --project Web.Tests/Web.Tests.csproj
```

The integration tests create disposable SQL Server containers and exercise authenticated HTTP endpoints,
including permission boundaries, membership changes, and persistence behavior.

## Production build

```sh
dotnet publish Web/Web.csproj -c Release -o ./publish
```

Publish builds the React application and includes its generated assets in `publish/wwwroot`. The ASP.NET Core
host serves the UI and JSON endpoints from the same origin. Client routes support direct navigation and refresh.
Frontend source changes trigger an incremental rebuild; removed or renamed generated assets are removed from
`Web/wwwroot` before publishing. Neither npm dependencies nor generated output is committed.

To build the container, provide the scaffolder's connection string as a Docker BuildKit secret:

```sh
docker build --secret id=ScaffoldConnectionString,src=/path/to/scaffold-connection-string \
  -f Web/dockerfile -t vsatemplate .
```

The Node stage runs `npm ci` and builds the React app. The .NET 11 stage publishes the server with those assets,
and the final ASP.NET Core image runs as the `app` user on port 8080. The scaffolding database must be reachable
from the build container. Supply runtime configuration when starting the image.

When frontend output was built separately, `-p:BuildFrontend=false` skips npm and uses `Web/ClientApp/dist`. The build
fails if that output is missing. Normal local builds and publishes do not need this property.
