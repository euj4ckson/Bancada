# Architecture decisions

## Separate static client and HTTP API

The UI is a standalone Blazor WebAssembly application so it can be deployed to static hosting. It communicates only through REST DTOs with the ASP.NET Core API. Development CORS is restricted to the configured client origin.

## Identity cookies for browser sessions

ASP.NET Core Identity owns account and password handling. The API issues an HTTP-only application cookie after login; the WebAssembly client includes browser credentials and reads `/api/auth/me` to build its UI authentication state. Authorization is always enforced again by the API.

## Domain IDs do not depend on Identity

Domain entities use `Guid` user IDs. `ApplicationUser` remains in Infrastructure because it extends the Identity persistence model. This keeps the domain independent of ASP.NET Core packages.

## EF Core is the persistence boundary

PostgreSQL is accessed through EF Core and Npgsql. There is no generic repository or additional unit of work. Read queries project directly to DTOs and use pagination and no-tracking where appropriate.

## Replaceable image storage

`IFileStorage` is a small application boundary with filesystem and Cloudflare R2 implementations. Development uses local files. R2 uses its S3-compatible API and a separately configured public base URL; the domain stores only the resulting URL.

## Tests use a relational substitute

API integration tests replace PostgreSQL with an isolated SQLite database. Production mappings and migrations remain PostgreSQL-specific, while tests exercise the real request pipeline, Identity, authorization, and EF relational behavior without external credentials.
