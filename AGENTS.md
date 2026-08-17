# Bancada contributor guide

## Architecture

- `Bancada.Web` is a standalone Blazor WebAssembly client and only calls the HTTP API.
- `Bancada.Api` owns endpoints, authentication, authorization, and dependency composition.
- `Bancada.Domain` contains entities and domain rules without web or persistence dependencies.
- `Bancada.Application` contains use-case contracts and DTOs shared with the client.
- `Bancada.Infrastructure` contains EF Core, Identity, PostgreSQL, seeds, and file storage.

Keep feature code cohesive. Do not add repositories over `DbContext`, MediatR, CQRS, or one-use abstractions without a concrete need.

## C# conventions

- Target .NET 10 with nullable reference types and implicit usings enabled.
- Use English for code and API routes; use Brazilian Portuguese for visible UI copy.
- Prefer clear names, small methods, async I/O, `CancellationToken`, `DateTimeOffset`, and `Guid` IDs.
- API contracts are DTOs; never serialize EF Core or Identity entities directly.

## Commands

```powershell
dotnet restore Bancada.sln
dotnet build Bancada.sln --no-restore
dotnet test Bancada.sln --no-build
```

## Migrations

Create migrations in `Bancada.Infrastructure` with `Bancada.Api` as the startup project. Inspect generated SQL and model changes before committing. Never edit an applied production migration.

## UI

Use semantic, responsive, accessible HTML and the tokens in `wwwroot/css/app.css`. Prefer focused components over a component framework or an oversized design system. Every remote operation needs a useful loading, empty, or error state.

## Commits

Use short Conventional Commit messages. Inspect staged files and run the relevant build/tests before each commit. Never commit secrets, generated build output, or unrelated changes.
