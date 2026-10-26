# ReDI Assessment

One assessment application, with one backend, one frontend and one database.

- `backend/`: ASP.NET Core API (.NET 10), Entity Framework Core migrations and backend documentation.
- `frontend/`: static student evaluation and teacher dashboard (HTML, CSS and JavaScript).

## Run locally

Install .NET 10 SDK, SQL Server LocalDB and Python 3. Run these PowerShell commands separately from the repository root.

```powershell
dotnet build .\backend\ReDI.Assessment.Api.csproj
```

Configure the local database and development admin credentials as described in [backend/README.md](backend/README.md). Apply migrations explicitly when you are ready to update your local database; consolidation does not apply them automatically.

```powershell
dotnet run --project .\backend\ReDI.Assessment.Api.csproj --launch-profile https
```

In a second terminal:

```powershell
python -m http.server 8000 --directory .\frontend
```

Open http://localhost:8000/evaluation.html or http://localhost:8000/admin.html. Both pages use `https://localhost:7101/api`; Swagger is at `https://localhost:7101/swagger`. Trust your local ASP.NET development certificate if your browser requires it.

The frontend stores application data through the API in SQL Server. Teachers can create or clone a draft, edit/import its question bank, publish it and share its student link. Notes and evaluations are saved with **Save review** and are available across sessions. See [frontend/README.md](frontend/README.md) for the workflow and [tests/README.md](tests/README.md) for real database/browser integration checks.

## Repository history

The consolidation branch preserves the original backend main history and the independent frontend `feature/student-flow-integration` history through a merge commit. The backend root was moved in a separate commit before importing the integrated frontend/backend snapshot. No existing history was rewritten.

See [docs/consolidation.md](docs/consolidation.md) for source revisions, preservation details and validation limits.
