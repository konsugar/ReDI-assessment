# ReDI Assessment — Backend

ASP.NET Core Web API (.NET 10), Entity Framework Core and SQL Server backend for the [ReDI Assessment prototype](../README.md).

## Where to start

- [Root README](../README.md) — installation, migrations, launch commands, local test credentials and end-to-end walkthrough.
- [Technical overview](../docs/technical.md) — architecture, data model, persistence and important invariants.
- [Frontend/backend contract](Docs/frontend-contract.md) — endpoint groups, payloads, authentication and error handling.
- [Integration tests](../tests/README.md) — isolated database and browser tests.

## Backend-specific configuration

- Default connection: `ConnectionStrings:DefaultConnection` in `appsettings.json`, using SQL Server LocalDB `ReDIAssessmentDb`. Never use LocalDB as a production database.
- Development settings: `appsettings.Development.json`, including `AdminAuth` and `FrontendAssessment:AssessmentId`.
- `FrontendAssessment:AssessmentId` selects the default assessment for legacy/current-assessment routes such as `GET /api/questions` and `POST /api/submissions`. Assessment-specific routes use an explicit ID and are preferable for student links.
- Development HTTPS: `https://localhost:7101`; HTTP: `http://localhost:5069`, as defined in `Properties/launchSettings.json`.
- Swagger UI in development: `https://localhost:7101/swagger`.

Do not commit real production credentials or JWT signing secrets. The checked-in development values are only for local testing.

## Key implementation rules

- Admin operations require JWT Bearer authentication. Student submission routes are public and use a participant code, not student authentication.
- Assessments start as drafts. Publishing requires frontend readiness; only the publication endpoint changes publication state.
- Published assessments and assessments with submissions are protected against structural changes. Clone an assessment to create a new editable draft without copying attempts or reviews.
- Global topics can be linked to multiple assessments; edits to shared topics are restricted to preserve existing assessments.
- The integrated question-bank editor supports `SingleChoice` and `TextInput`. Its draft save uses a version check and a database transaction; unsupported question types must not be silently discarded.
- Choice answers are scored automatically; written-answer evaluations and teacher notes are saved separately.
- The backend database is the persistent source of truth. The integrated frontend has no mock-data fallback.

For exact behavior and response fields, use the [contract](Docs/frontend-contract.md) and code, rather than duplicating API listings here.
