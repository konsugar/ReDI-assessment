# ReDI Assessment

> **Project status — basic prototype.** This repository contains a very basic, locally runnable prototype that meets the minimum requirements of the original project specification. Further development was started but has not been published in this repository. Continuing that work depends on the availability of additional resources and funding. The current prototype is not a production-ready service.

ReDI Assessment is a simple assessment application for educational programs. Students complete a self-evaluation, single-choice questions and open-text questions; teachers manage assessments, review submissions, record evaluations and export results.

## Credits

- **Frontend:** implemented by [alisher_kay](https://github.com/alisher-kay).
- **Backend and frontend–backend integration:** implemented by [konsugar](https://github.com/konsugar), with additional development and refinement assisted by ChatGPT.

## Repository structure

- `frontend/` — static HTML, CSS and JavaScript pages for students and teachers (no frontend build step).
- `backend/` — ASP.NET Core Web API (.NET 10), Entity Framework Core and SQL Server migrations.
- `tests/` — database/browser integration checks.
- `docs/` — technical notes on repository consolidation and dependency updates.

The backend database is the source of truth for assessments, questions, submissions and teacher reviews; the frontend communicates with it through HTTP APIs.

## One-command local operation

After the database setup below, you can start both servers from one PowerShell terminal:

```powershell
.\redi.ps1 start
```

Check or stop the managed processes:

```powershell
.\redi.ps1 status
```

```powershell
.\redi.ps1 stop
```

Create and verify a database backup:

```powershell
.\redi.ps1 backup
```

Backup and restore require SQL Server command-line tools (`sqlcmd`). Restore creates a separate database and never overwrites an existing one. See [local workflow instructions](docs/local-workflow.md) for restore, logs and test ports, and [candidate functional improvements](docs/product-backlog.md) for the proposed backlog. The manual two-terminal startup remains documented below.

## Run locally (Windows)

These instructions use **Windows PowerShell** because the included database configuration relies on **SQL Server LocalDB**. Open a separate terminal for each server.

### 1. Install prerequisites

- [Git](https://git-scm.com/downloads)
- [.NET 10 SDK](https://dotnet.microsoft.com/download/dotnet/10)
- [SQL Server Express LocalDB](https://learn.microsoft.com/en-us/sql/database-engine/configure-windows/sql-server-express-localdb)
- [Python 3](https://www.python.org/downloads/) for serving the static frontend

### 2. Clone the repository

```powershell
git clone https://github.com/konsugar/ReDI-assessment.git
cd ReDI-assessment
```

All subsequent commands run from this repository root.

### 3. Set up the backend database

The default development database is `ReDIAssessmentDb` on `(localdb)\\MSSQLLocalDB`, configured in `backend/appsettings.json`. The database is local and is **not included** in the repository.

Install the EF Core command-line tool if you do not have it:

```powershell
dotnet tool install --global dotnet-ef
```

If it is already installed, update it if required (`dotnet tool update --global dotnet-ef`). Ensure the tool is compatible with EF Core 10.

Restore and build the backend, then create/update the local database using the included migrations:

```powershell
dotnet restore .\backend\ReDI.Assessment.Api.csproj
dotnet build .\backend\ReDI.Assessment.Api.csproj
dotnet ef database update --project .\backend\ReDI.Assessment.Api.csproj
```

Migrations are not applied automatically at startup.

### 4. Start the backend

```powershell
dotnet run --project .\backend\ReDI.Assessment.Api.csproj --launch-profile https
```

The configured development API is `https://localhost:7101/api` and the Swagger UI is at [https://localhost:7101/swagger](https://localhost:7101/swagger). If the browser reports a development HTTPS certificate problem, run:

```powershell
dotnet dev-certs https --trust
```

Restart the backend/browser if necessary. Keep this terminal running.

### 5. Start the frontend

Open **another PowerShell terminal** in the repository root:

```powershell
python -m http.server 8000 --directory .\frontend
```

Then open:

- **Teacher dashboard:** [http://localhost:8000/admin.html](http://localhost:8000/admin.html)
- **Student assessment:** [http://localhost:8000/evaluation.html](http://localhost:8000/evaluation.html)

Both pages are configured to call `https://localhost:7101/api`. They will not function correctly without the backend. Open the backend Swagger URL once and accept/trust the development certificate if prompted.

### 6. Test the prototype

**Teacher / admin login (local development only)**

- Username: `admin`
- Password: `admin-dev-password`

These are intentionally published **test credentials** from `backend/appsettings.Development.json`. Never use them, or the development JWT signing secret, for a publicly accessible deployment.

Open [http://localhost:8000/admin.html](http://localhost:8000/admin.html), sign in, create or clone an assessment, edit its questions, and publish it when ready. Click **Open student link** to open that assessment (`evaluation.html?assessmentId=...`).

**Student flow — no account or password**

Open the student link and enter **any participant code you choose**, for example `TEST-001`. The code is an identifier attached to the submission, **not a login, password, or proof of identity**. No student account or registration is required. Use distinct codes when you want to distinguish test submissions. Complete the self-evaluation and questions, then submit; return to the teacher dashboard to review the saved submission.

The plain `evaluation.html` URL uses the configured `FrontendAssessment:AssessmentId` instead of a selected assessment. On a fresh database, this default ID may not exist: **the teacher-generated student link is the recommended route**.

For additional details, see [backend/README.md](backend/README.md) and [frontend/README.md](frontend/README.md). Database/browser integration tests are described in [tests/README.md](tests/README.md).

## Prototype limitations

- The application uses a minimal development admin login, not a complete multi-user/role system.
- Students identify themselves with a participant code; student accounts are not implemented.
- The student frontend supports single-choice and free-text questions; other question types present in backend code are not fully integrated.
- The default database and local development credentials are **not suitable for public deployment**. Replace credentials/secrets and review security, hosting and privacy requirements before deploying.

## Further development

This repository represents the published basic prototype, not the unpublished work started afterward. Its current implementation can serve as a starting point for future development if resources and funding become available.
