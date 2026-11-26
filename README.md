# Invelop Contacts

A full-stack personal contact manager built with .NET 10, EF Core SQLite, Angular 21, NgRx, and PrimeNG.

## Features

- Create, list, edit, and delete contacts.
- Persists all fields in SQLite.
- Required fields: first name, surname, date of birth, address, phone number, and IBAN.
- Domain and command validation with FluentValidation.
- Rich domain value objects for names, addresses, phone numbers, and IBANs.
- NgRx state, effects, entity adapter, and selectors.
- Playwright end-to-end coverage.

## Prerequisites

- .NET 10 SDK
- Node.js and npm
- Chromium for Playwright (`npx playwright install chromium` if needed)

## First-time setup

```bash
npm install
npm --prefix web install
dotnet restore Invelop.Contacts.slnx
dotnet tool restore
npm run db:update
```

`dotnet-ef` is pinned in the local tool manifest at `dotnet-tools.json`, so no global install is required.

If you prefer the explicit command:

```bash
dotnet tool run dotnet-ef database update --project src/Invelop.Contacts.Infrastructure --startup-project src/Invelop.Contacts.Api
```

## Run the application

Combined preview:

```bash
npm run preview
```

The application is available at `http://localhost:4200`; the API is at `http://localhost:5000`.

Separate processes:

```bash
npm run start:backend
npm run start:frontend
```

The alternative root alias `npm start` starts both processes.

## Tests

Backend tests:

```bash
dotnet test Invelop.Contacts.slnx
```

Angular build:

```bash
npm --prefix web run build
```

Angular tests:

```bash
npm --prefix web test -- --watch=false
```

Playwright end-to-end tests:

```bash
npm run test:e2e
```

Playwright starts isolated API and SPA servers on ports 5099 and 4300 and uses a throwaway SQLite database.

Full local quality gate:

```bash
npm run verify:all
```

Shipping gate (quality gate + release compile + publish check):

```bash
npm run verify:ship
```

`verify:ship` proves that:

- tests and e2e pass,
- frontend production build succeeds,
- backend release build succeeds,
- backend publish output can be produced to `artifacts/api`.

## Database

The development database is `src/Invelop.Contacts.Api/contacts.db`. EF Core migrations live in `src/Invelop.Contacts.Infrastructure/Migrations`.

Update the database manually with:

```bash
npm run db:update
```

## Design decisions

- Separate command handlers are used instead of MediatR for a small, explicit CQRS surface.
- Contact IBANs are not unique; shared accounts are supported.
- Date of birth is represented as `DateOnly` and serialized as `yyyy-MM-dd`.
- Get operations do not construct domain aggregates; create/update operations do.

## CI and release readiness

- GitHub Actions runs the shipping gate on every push and pull request.
- CI uploads release artifacts (`artifacts/api`, `web/dist/web`) on success.
- CI uploads Playwright diagnostics (`playwright-report`, `test-results`) on failure.

This keeps local and CI verification aligned and makes failures auditable.
