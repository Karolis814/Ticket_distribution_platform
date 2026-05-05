# Ticket Distribution Platform

Users can register, log in, browse and search events, filter by date / location / category,
buy tickets, get them by email and view QR codes.

Jira: https://markk-psk.atlassian.net/jira/software/projects/MARKK/summary

## Stack

- .NET 9
- ASP.NET Core Web API + EF Core
- PostgreSQL 16
- Blazor WebAssembly + Radzen
- xUnit (+ Testcontainers for integration tests)

## Project structure

```
Ticket_distribution_platform/
├── docker-compose.yml         postgres + pgadmin
├── global.json                pins SDK to .NET 9
├── TicketPlatform.sln
├── src/
│   ├── TicketPlatform.Core             business layer (entities, services)
│   ├── TicketPlatform.Infrastructure   data layer (EF, DbContext, migrations)
│   ├── TicketPlatform.Shared           DTOs shared between Api and Web
│   ├── TicketPlatform.Api              ASP.NET Core Web API
│   └── TicketPlatform.Web              Blazor WASM frontend
└── tests/
    ├── TicketPlatform.UnitTests
    └── TicketPlatform.IntegrationTests
```

3-layer split: presentation (`Api` + `Web`), business (`Core`), data (`Infrastructure`).

## Getting started

You need .NET 9.0.313 SDK and Docker

1. Start Postgres:
   ```
   docker compose up -d
   ```

2. Apply the database migration (first time only):
   ```
   dotnet ef database update \
     --project src/TicketPlatform.Infrastructure \
     --startup-project src/TicketPlatform.Api
   ```

   If you don't have the EF:
   ```
   dotnet tool install -g dotnet-ef --version 9.0.*
   ```

3. Run the API:
   ```
   dotnet run --project src/TicketPlatform.Api --launch-profile https
   ```

4. Run the frontend (another terminal):
   ```
   dotnet run --project src/TicketPlatform.Web --launch-profile https
   ```

   Open https://localhost:7174

If the browser blocks the dev cert, run `dotnet dev-certs https --trust` once.

## Tests

```
dotnet test
```

Integration tests need Docker running — they spin up a throwaway Postgres via Testcontainers.

## Useful URLs (dev)

- Frontend: https://localhost:7174
- API: https://localhost:7001
- pgAdmin: http://localhost:5050 (login: `admin@ticket.local` / `admin`)

## Frontend conventions (short version)

- Use Radzen components and built-in `.rz` classes. Avoid custom CSS.
- Code-behind in `XxxBase.razor.cs` partials, not `@code{}` blocks.
- Repeated layout goes into `@layout` components, no copy-paste.
- Show errors with `RadzenNotification`, not `Console.WriteLine`.
- Pass data with `[Parameter]`, raise events with `EventCallback`.
- Navigate with `NavigationManager`, not `<a href>`.
- Files PascalCase, variables/methods camelCase.
