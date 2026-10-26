# Database and browser integration test

`database-ui.cjs` runs real HTTP and browser checks against an isolated LocalDB database. It creates uniquely named test assessments and synthetic submissions and leaves them in that test database for inspection. Do not point the test API at an application database.

Prerequisites: .NET 10 SDK, dotnet-ef, SQL Server LocalDB, Node.js, Python 3 and Playwright. From the repository root, run each PowerShell command separately:

```powershell
npm install --prefix tests
```

```powershell
dotnet build .\backend\ReDI.Assessment.Api.csproj --configuration Release
```

```powershell
dotnet ef database update --project .\backend\ReDI.Assessment.Api.csproj --configuration Release --no-build --connection 'Server=(localdb)\MSSQLLocalDB;Database=ReDI_Consolidation_Test_20261026_7c91;Trusted_Connection=True;TrustServerCertificate=True'
```

Start the test API in one terminal (the explicit connection override is required):

```powershell
dotnet run --project .\backend\ReDI.Assessment.Api.csproj --configuration Release --no-build --launch-profile http -- --urls http://127.0.0.1:15069 --ConnectionStrings:DefaultConnection 'Server=(localdb)\MSSQLLocalDB;Database=ReDI_Consolidation_Test_20261026_7c91;Trusted_Connection=True;TrustServerCertificate=True'
```

Serve the frontend in a second terminal:

```powershell
python -m http.server 18000 --bind 127.0.0.1 --directory frontend
```

In a third terminal, acknowledge that the API uses the isolated test database:

```powershell
$env:REDI_TEST_DATABASE='ReDI_Consolidation_Test_20261026_7c91'
```

Use installed Edge with an isolated temporary profile:

```powershell
$env:REDI_TEST_BROWSER='msedge'
```

```powershell
node .\tests\database-ui.cjs
```

Alternatively leave `REDI_TEST_BROWSER` unset and install Playwright's Chromium. The test reads development admin credentials from the backend configuration without printing them. Both servers should be stopped with Ctrl+C after testing.

Coverage: atomic question-bank saves; stale-version, invalid-import, unauthenticated and locked-assessment rejection; real browser editing/add/delete/import/publication; failed-write feedback; student submission; teacher review; visibility from a fresh browser context; absence of localStorage application data.

For the complete three-student workflow, use the same test servers, dependencies and environment variables, then run:

```powershell
node .\tests\multi-student-workflow.cjs
```

This second scenario creates a draft through the teacher UI, imports and publishes a two-question test, submits answers from three independent browser contexts with expected scores 2/2, 1/2 and 0/2, saves written-answer reviews, checks both CSV exports and reopens each review in a fresh teacher session. The test leaves labelled synthetic data in the separate test database.

The test servers can also be launched with one command using [the local workflow tools](../docs/local-workflow.md#test-environment).
