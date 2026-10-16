# RaceDay

# RaceDay API

![CI](https://github.com/murendiwa/POE-Part2/actions/workflows/ci.yml/badge.svg)

RaceDay is an ASP.NET Core Web API for managing race events. Organisers create events, categories (e.g. 10km, Half Marathon) and routes. Participants enrol in categories. Organisers record results, and anyone can view the public leaderboard.

**Student:** [YOUR NAME] · **Student number:** [STUDENT NUMBER]
**Module:** [MODULE CODE] · Part 2 (Web API)
**Video walkthrough:** [PASTE VIDEO LINK HERE]

## Tech stack

- C# on .NET 10, ASP.NET Core Web API (controllers)
- Entity Framework Core 10, Code-First migrations, SQL Server
- Server-side sessions for authentication, BCrypt for password hashing
- Swagger UI (Swashbuckle) for documentation and testing
- xUnit and the EF Core in-memory provider for unit tests
- GitHub Actions for CI

## Project structure

```
RaceDay.sln
├── RaceDay/                  The Web API project
│   ├── Auth/                 Session helpers and the [RequireRole] filter
│   ├── Controllers/          Auth, Events, Categories, CategoryRoutes, Enrolments, Results
│   ├── Data/                 RaceDayDbContext
│   ├── DTOs/                 Request and response models
│   ├── Models/               EF Core entities and enums
│   ├── Migrations/           EF Core migrations (InitialCreate)
│   ├── Program.cs
│   └── appsettings.Development.json
├── RaceDay.Test/             xUnit unit tests
├── .github/workflows/ci.yml  CI pipeline
└── docs/                     Screenshots used in this README
```

The database matches the Part 1 ERD and SQL script: Users (one table with a Role column), Events, Categories, Routes, Enrolments (junction table) and Results, with the same UNIQUE and CHECK constraints.

## Setup

### Prerequisites

- [.NET 10 SDK](https://dotnet.microsoft.com/download/dotnet/10.0)
- SQL Server, SQL Server Express or LocalDB
- Visual Studio 2026 (optional) or any editor with the `dotnet` CLI
- The EF Core tool: `dotnet tool install --global dotnet-ef`

### Steps

1. **Clone the repository**
```
   git clone https://github.com/murendiwa/POE-Part2.git
   cd POE-Part2
```
2. **Set the connection string.** Open `RaceDay/appsettings.Development.json` and make sure `DefaultConnection` points at your SQL Server instance, for example:
```json
   "ConnectionStrings": {
     "DefaultConnection": "Server=.\\SQLEXPRESS;Database=RaceDayDB;Trusted_Connection=True;TrustServerCertificate=True;MultipleActiveResultSets=true"
   }
```
3. **Create the database** by applying the migration:
```
   dotnet ef database update --project RaceDay
```
   (In Visual Studio you can run `Update-Database` in the Package Manager Console instead.)
4. **Run the API**
```
   dotnet run --project RaceDay
```
   or press F5 in Visual Studio.
5. **Open Swagger** at the address shown in the console plus `/swagger`, for example `https://localhost:7123/swagger`.

### Running the tests

```
dotnet test
```

The tests use an in-memory database, so no SQL Server is needed.

## Authentication and roles

- **Register** creates an account as an `Organiser` or a `Participant`. Passwords are hashed with BCrypt and are never stored or returned in plain text.
- **Login** starts a server-side session that stores the user's ID and role. The browser only holds a session cookie. Sessions expire after 30 minutes of inactivity.
- Protected endpoints use a custom `[RequireRole]` filter: **401** if you are not logged in, **403** if you are logged in with the wrong role.
- Some endpoints also check **ownership**. An organiser can only change events, categories, routes and results that belong to their own events, and a participant can only see or cancel their own enrolments.

**Using Swagger:** call `POST /api/auth/login` first. Swagger sends the session cookie automatically on later requests. To switch users, call `POST /api/auth/logout` and then log in as someone else.

## Endpoints

| Method | Route | Access |
|---|---|---|
| POST | `/api/auth/register` | Public |
| POST | `/api/auth/login` | Public |
| POST | `/api/auth/logout` | Any logged-in user |
| GET | `/api/auth/me` | Any logged-in user |
| GET | `/api/events` (filters: `province`, `status`) | Public |
| GET | `/api/events/{id}` | Public |
| POST | `/api/events` | Organiser |
| PUT | `/api/events/{id}` | Organiser (owner) |
| DELETE | `/api/events/{id}` | Organiser (owner) |
| GET | `/api/events/{eventId}/categories` | Public |
| GET | `/api/categories/{id}` | Public |
| POST | `/api/events/{eventId}/categories` | Organiser (owner) |
| PUT | `/api/categories/{id}` | Organiser (owner) |
| DELETE | `/api/categories/{id}` | Organiser (owner) |
| GET | `/api/categories/{categoryId}/route` | Public |
| POST | `/api/categories/{categoryId}/route` | Organiser (owner) |
| PUT | `/api/categories/{categoryId}/route` | Organiser (owner) |
| DELETE | `/api/categories/{categoryId}/route` | Organiser (owner) |
| POST | `/api/categories/{categoryId}/enrolments` | Participant |
| GET | `/api/enrolments/mine` | Participant |
| GET | `/api/enrolments/{id}` | Owning participant or the event's organiser |
| PUT | `/api/enrolments/{id}/cancel` | Participant (owner) |
| GET | `/api/events/{eventId}/enrolments` | Organiser (owner) |
| PUT | `/api/enrolments/{id}` | Organiser (owner) |
| GET | `/api/categories/{categoryId}/results` | Public leaderboard |
| GET | `/api/enrolments/{enrolmentId}/result` | Owning participant or the event's organiser |
| POST | `/api/enrolments/{enrolmentId}/result` | Organiser (owner) |
| PUT | `/api/enrolments/{enrolmentId}/result` | Organiser (owner) |
| DELETE | `/api/enrolments/{enrolmentId}/result` | Organiser (owner) |

### Business rules

- Event dates must be in the future when an event is created.
- An event with enrolments cannot be deleted (409). Set its status to Cancelled instead.
- A category cannot be deleted while it has enrolments, and its capacity cannot drop below the current enrolment count.
- Enrolment is blocked when the event is cancelled or past, when the category is full, or when you are already enrolled (409). Cancelled enrolments do not count towards capacity, and re-enrolling reactivates the same row.
- Cancelling a paid enrolment marks it as Refunded. An enrolment with a result cannot be cancelled.
- A Finished result needs a finish time under 24 hours. DNF and DNS results cannot have a finish time or position.
- Each category has at most one route, and each enrolment at most one result.

## Continuous integration

Every push to `main` runs the GitHub Actions workflow in `.github/workflows/ci.yml`. It:

1. Validates the repository structure (required files and folders are present and no `bin/` or `obj/` output is committed)
2. Restores and builds the solution in Release mode
3. Runs all unit tests

![Green CI build](docs/ci-green-build.png)

## Unit tests

The `RaceDay.Test` project covers:

- **Authentication:** registration, password hashing, duplicate emails, login, logout and session contents
- **Role enforcement:** the `[RequireRole]` filter (401 and 403), plus a check that every protected endpoint carries the correct role requirement and every public endpoint carries none
- **Events, Enrolments and Results:** ownership checks, capacity, duplicates, cancel and refund rules, and result validation
