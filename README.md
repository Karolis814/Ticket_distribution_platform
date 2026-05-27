# Ticket Distribution Platform

Users can register, log in, browse and search events, filter by date / location / category,
buy tickets, get them by email and view QR codes.

Jira: https://markk-psk.atlassian.net/jira/software/projects/MARKK/summary

## Stack

- .NET 9
- ASP.NET Core Web API + EF Core
- PostgreSQL 16
- Blazor WebAssembly + Radzen
- Stripe (payments, sandbox mode)
- Azure Storage via Azurite (local emulator)
- Mailpit (local SMTP)
- xUnit (+ Testcontainers for integration tests)

## Project structure

```
Ticket_distribution_platform/
├── docker-compose.yml         postgres, pgadmin, mailpit, azurite, stripe-cli
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

You need .NET 9 SDK and Docker.

### 1. Store secrets

Store all keys with dotnet user-secrets so they never touch source control:

```
cd src/TicketPlatform.Api
dotnet user-secrets set "Stripe:SecretKey" "sk_test_..."
dotnet user-secrets set "GooglePlaces:ApiKey" "AIza..."
```

Get your Stripe test keys from the [Stripe Dashboard](https://dashboard.stripe.com/test/apikeys).
Get your Google Places API key from the [Google Cloud Console](https://console.cloud.google.com/) — enable the **Places API** on the key.

The Stripe CLI container also needs the secret key. Create a `.env` file in the repo root (already in `.gitignore`):

```
STRIPE_API_KEY=sk_test_...
```

### 2. Start dev services

```
docker compose up -d
```

### 3. Get the webhook signing secret

On the first run the Stripe CLI prints a `whsec_...` signing secret. Grab it from the logs:

```
docker compose logs stripe-cli
```

Store it the same way:

```
cd src/TicketPlatform.Api
dotnet user-secrets set "Stripe:WebhookSecret" "whsec_..."
```

### 4. Apply the database migration

```
dotnet ef database update \
  --project src/TicketPlatform.Infrastructure \
  --startup-project src/TicketPlatform.Api
```

If you don't have EF:
```
dotnet tool install -g dotnet-ef --version 9.0.*
```

If you come across any DB conflicts:
```
dotnet ef database drop \
  --project src/TicketPlatform.Infrastructure \
  --startup-project src/TicketPlatform.Api
```
Then apply the migration again.

### 5. Run the API

```
dotnet run --project src/TicketPlatform.Api --launch-profile https
```

### 6. Run the frontend (another terminal)

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
- pgAdmin: http://localhost:5050 (login: `admin@ticket.dev` / `admin`)
- Mailpit: http://localhost:8025
- Azurite: blob `localhost:10000`, queue `localhost:10001`, table `localhost:10002`

## Frontend conventions (short version)

- Use Radzen components and built-in `.rz` classes. Avoid custom CSS.
- Code-behind in `XxxBase.razor.cs` partials, not `@code{}` blocks.
- Repeated layout goes into `@layout` components, no copy-paste.
- Show errors with `RadzenNotification`, not `Console.WriteLine`.
- Pass data with `[Parameter]`, raise events with `EventCallback`.
- Navigate with `NavigationManager`, not `<a href>`.
- Files PascalCase, variables/methods camelCase.
