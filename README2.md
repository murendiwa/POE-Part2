## Part 3: RaceDay MVC web application

The RaceDay front end is an ASP.NET Core MVC app (`RaceDay.Web`) that consumes the Part 2 Web API. It has no database access and no duplicated business logic: every operation goes through the API. Images are stored in Azure Blob Storage, but only the API talks to Azure; the MVC app sends files to the API.

**Demo video:** [THERE'S NO VIDEO]

### How the three parts fit together

| Part | What it is |
|---|---|
| Part 1 | Design: ERD, endpoint plan, SQL Server schema |
| Part 2 | `RaceDay`: ASP.NET Core Web API (EF Core, cookie session auth, Swagger) |
| Part 3 | `RaceDay.Web`: ASP.NET Core MVC front end, Azure Blob Storage in the API, Docker |

### Features

- **Everyone:** register, log in, log out (session-based, the role is kept across pages, protected pages redirect to login)
- **Organisers:** dashboard (events, enrolments per event, upcoming dates), create/edit/delete events with a banner image, manage categories, view and update enrolments, enter and publish results
- **Participants:** browse and filter upcoming events, view event details and categories, enrol in a category, see enrolments with colour-coded status, see race results (e.g. "47th out of 312 finishers"), update profile and picture

### Architecture

```
Browser -> RaceDay.Web (MVC, session) -> RaceDay (Web API) -> SQL Server
                                                    |
                                                    +-> Azure Blob Storage (images)
```

The API login cookie is kept per user inside the MVC session. The MVC server adds it to each API request.

### Prerequisites

- .NET 10 SDK
- SQL Server (database created from the Part 1/2 scripts and migrations)
- An Azure Storage account with a container named `raceday-images` (anonymous read access for blobs)
- Docker Desktop (to run the MVC app in a container)

### Configuration

**API** (`RaceDay`): the Azure connection string is a secret, so it is never committed.

```bash
dotnet user-secrets init --project RaceDay
dotnet user-secrets set "Azure:ConnectionString" "<your Azure storage connection string>" --project RaceDay
```

`appsettings.json` holds the non-secret container name: `"Azure": { "Container": "raceday-images" }`.

**MVC** (`RaceDay.Web`): the API address is set with `Api:BaseUrl` (or the environment variable `Api__BaseUrl`).

### Run locally

1. Start the API on HTTP:

   ```powershell
   $env:ASPNETCORE_ENVIRONMENT = "Development"
   dotnet run --project RaceDay --no-launch-profile --urls "http://0.0.0.0:5000"
   ```

2. In a second terminal, start the MVC app and open the address it prints:

   ```bash
   dotnet run --project RaceDay.Web
   ```

   By default it expects the API at `http://localhost:5000/` (see `appsettings.json`).

### Run with Docker

Start the API as above, then from the repository root:

```bash
docker build -f RaceDay.Web/Dockerfile -t raceday-web .
docker run --rm -p 8080:8080 -e Api__BaseUrl=http://host.docker.internal:5000/ raceday-web
```

Or, in a single command:

```bash
docker compose up --build
```

Open http://localhost:8080. The container behaves the same as the local app; only the address differs.

If you see "The RaceDay service is unavailable", the container cannot reach the API: make sure the API is running on `0.0.0.0:5000` and that your firewall allows it.

### Project structure

```
RaceDay/            Web API (Part 2 + Blob Storage and new endpoints for Part 3)
RaceDay.Web/        MVC app (controllers, views, API client, role filter)
RaceDay.Web/Dockerfile
docker-compose.yml
.github/workflows/  CI: builds API and MVC, runs tests
```

### Changes to the API in Part 3

- Azure Blob Storage service and upload endpoints (event banner, profile picture)
- `EventType` and `DistanceKm` on events, `BannerUrl` on events, `ProfilePictureUrl` on users (migration + updated SQL script)
- `GET /api/events/mine`, `GET /api/results/mine`, `GET` and `PUT /api/users/me`

### CI

GitHub Actions builds the API and the MVC project and runs the tests on every push to `main` and `part3-mvc`.
