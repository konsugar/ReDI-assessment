# ReDI Assessment API

Backend API for the ReDI Assessment project.

This API provides questions for the student assessment flow, accepts student submissions, calculates results, and stores assessment attempts in a SQL Server database.

## Requirements

- .NET SDK
- Visual Studio 2022 or another .NET-compatible IDE
- SQL Server LocalDB

The project has been developed and tested locally with a .NET / Visual Studio / SQL Server LocalDB setup.

## Local database

The project uses SQL Server LocalDB by default.

Default connection string:

```json
"Server=(localdb)\\MSSQLLocalDB;Database=ReDIAssessmentDb;Trusted_Connection=True;TrustServerCertificate=True"
```

Database name:

```text
ReDIAssessmentDb
```

LocalDB is intended for local development only. Do not use it as a production database.

## Run locally

Open the project folder in a terminal.

Restore packages:

```powershell
dotnet restore
```

Apply database migrations:

```powershell
dotnet ef database update
```

Run the API:

```powershell
dotnet run
```

Check the actual local URL in the terminal output after starting the project.

The local API URL is usually one of these:

```text
https://localhost:7101
http://localhost:5101
```

## Main API endpoints

### Student frontend

```text
GET /api/questions
POST /api/submissions
```

### Admin / submissions

```text
GET /api/submissions
GET /api/submissions/details
GET /api/submissions/{id}
```

## Frontend integration

For frontend integration, run the backend first and switch the frontend from mock mode to backend mode.

Example frontend config:

```javascript
useMock = false;
apiBase = "https://localhost:7101";
```

Use the actual backend URL shown in the terminal after `dotnet run`.

The frontend should call the backend API instead of relying on local mock data or localStorage for the student submission flow.

## Current integration focus

The current priority is the student flow:

```text
GET /api/questions
POST /api/submissions
```

The admin dashboard can use backend read endpoints where available, but some admin-side data may still be frontend-local during the transition.

## Notes for collaborators

- Run database migrations before starting the API for the first time.
- Make sure the backend is running before testing the frontend against it.
- Do not commit real passwords, tokens, or production connection strings.
- If frontend and backend are running on different origins, CORS must allow the frontend origin.
- For shared testing outside one local machine, the backend should be deployed to external hosting later.

## Git workflow

Recommended workflow for integration work:

```powershell
git pull
git switch -c feature/student-flow-integration
```

After changes:

```powershell
git status
git add .
git commit -m "Describe the change"
git push
```

Use pull requests for merging integration work into the main branch.
