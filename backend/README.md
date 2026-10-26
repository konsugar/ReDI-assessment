# ReDI Assessment API

Backend API for the ReDI Assessment project.

This API supports the student assessment flow, admin content management, assessment publication/readiness checks, student submissions, automatic scoring, manual review of open answers, and teacher notes.

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

If you work in Visual Studio Package Manager Console, use:

```powershell
Update-Database
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

Swagger UI is available in development mode:

```text
https://localhost:7101/swagger
```

## Configuration

The backend uses `FrontendAssessment:AssessmentId` to define the current assessment for compatibility endpoints.

Example configuration:

```json
"FrontendAssessment": {
	"AssessmentId": 14
}
```

This value is used by endpoints such as:

```text
GET /api/questions
POST /api/submissions
GET /api/submissions/details
GET /api/frontend-assessments/current
```

Assessment-specific endpoints are available when the frontend or admin UI works with an explicit `assessmentId`.

### Admin authentication

Development admin credentials are configured in `appsettings.Development.json`:

```json
"AdminAuth": {
	"Username": "admin",
	"Password": "admin-dev-password",
	"JwtSecret": "local-development-only-admin-auth-secret-key-change-before-production",
	"Issuer": "ReDI.Assessment.Api",
	"Audience": "ReDI.Assessment.Admin",
	"TokenLifetimeMinutes": 120
}
```

These values are for local development only. For external hosting, admin credentials and JWT secret must be provided through environment variables or hosting secrets.


## Main API areas

### Student frontend

Current configured assessment:

```text
GET /api/questions
POST /api/submissions
```

Selected assessment:

```text
GET /api/assessments/{assessmentId}/frontend-questions
POST /api/assessments/{assessmentId}/submissions
```

Launchable assessments:

```text
GET /api/frontend-assessments/launchable
```

### Admin authentication

```text
POST /api/admin/auth/login
```

Successful login returns a JWT access token.

Admin requests must include:
```
Authorization: Bearer <accessToken>
```

### Admin assessment status

```text
GET /api/frontend-assessments
GET /api/frontend-assessments/{assessmentId}
GET /api/frontend-assessments/current
GET /api/frontend-assessments/launchable
```

These endpoints expose publication, readiness, launchability, submission count, and structure-lock status.

### Admin content management

Main aggregate endpoint for the content editor:

```text
GET /api/assessments/{assessmentId}/content
```

Assessment metadata and publication:

```text
GET /api/assessments
GET /api/assessments/{assessmentId}
POST /api/assessments
PUT /api/assessments/{assessmentId}
DELETE /api/assessments/{assessmentId}
POST /api/assessments/{assessmentId}/clone
PUT /api/assessments/{assessmentId}/publication
```

Global topics:

```text
GET /api/topics
GET /api/topics/{topicId}
POST /api/topics
PUT /api/topics/{topicId}
DELETE /api/topics/{topicId}
```

Assessment-topic links:

```text
GET /api/assessments/{assessmentId}/topics
GET /api/assessments/{assessmentId}/topics/{assessmentTopicId}
POST /api/assessments/{assessmentId}/topics
PUT /api/assessments/{assessmentId}/topics/{assessmentTopicId}
DELETE /api/assessments/{assessmentId}/topics/{assessmentTopicId}
```

Questions:

```text
GET /api/assessments/{assessmentId}/questions
GET /api/assessments/{assessmentId}/questions/{questionId}
POST /api/assessments/{assessmentId}/questions
PUT /api/assessments/{assessmentId}/questions/{questionId}
DELETE /api/assessments/{assessmentId}/questions/{questionId}
```

Answer options:

```text
GET /api/questions/{questionId}/answer-options
GET /api/questions/{questionId}/answer-options/{answerOptionId}
POST /api/questions/{questionId}/answer-options
PUT /api/questions/{questionId}/answer-options/{answerOptionId}
DELETE /api/questions/{questionId}/answer-options/{answerOptionId}
```

### Admin submissions and review

Current configured assessment:

```text
GET /api/submissions
GET /api/submissions/details
GET /api/submissions/{submissionId}
PUT /api/submissions/{submissionId}/open-answers/{attemptQuestionId}/evaluation
PUT /api/submissions/{submissionId}/open-answers/{attemptQuestionId}/teacher-note
```

Selected assessment:

```text
GET /api/assessments/{assessmentId}/submissions
GET /api/assessments/{assessmentId}/submissions/details
GET /api/assessments/{assessmentId}/submissions/{submissionId}
PUT /api/assessments/{assessmentId}/submissions/{submissionId}/open-answers/{attemptQuestionId}/evaluation
PUT /api/assessments/{assessmentId}/submissions/{submissionId}/open-answers/{attemptQuestionId}/teacher-note
```

## Frontend contract

The detailed frontend/backend contract is documented separately:

```text
Docs/frontend-contract.md
```

It contains:

- student frontend contract
- admin content management contract
- admin submissions/review contract
- request/response examples
- error-handling notes
- localStorage migration rules


## Authentication rules

Public endpoints:

```text
POST /api/admin/auth/login
GET /api/questions
POST /api/submissions
GET /api/assessments/{assessmentId}/frontend-questions
POST /api/assessments/{assessmentId}/submissions
GET /api/frontend-assessments/launchable
```

Protected admin endpoints require:
```
Authorization: Bearer <accessToken>
```

Protected areas include:

```
/api/assessments...
/api/topics...
/api/questions/{questionId}/answer-options...
/api/submissions...
/api/assessments/{assessmentId}/submissions...
/api/attempts...
/api/frontend-assessments
/api/frontend-assessments/{assessmentId}
/api/frontend-assessments/current
```

## Important business rules

### Publication and launchability

New assessments are drafts by default:

```text
isPublished = false
```

An assessment can be launched in the student frontend only when:

```text
isPublished = true
isReadyForFrontend = true
canLaunchFrontend = true
```

Publication is changed only through:

```text
PUT /api/assessments/{assessmentId}/publication
```

Do not publish assessments through:

```text
PUT /api/assessments/{assessmentId}
```

### Structure locking

Published assessments cannot be structurally edited.

Assessments with submissions cannot be structurally edited.

Structural editing includes:

- questions
- answer options
- assessment-topic links

If an assessment is locked, the frontend should use:

```text
canEditStructure
structureLockReason
```

from the assessment status/content response to disable editing controls and show the reason.

### Metadata editing

Assessment title and description can be edited as metadata.

`timeLimitMinutes` cannot be changed if the assessment is published or already has submissions.

### Topics

Topics are global entities.

Assessment-topic links attach global topics to a specific assessment.

Global topics cannot be edited if they are used by published/submitted assessments.

Global topics cannot be deleted if they are used by any assessment.

### Clone behavior

Cloning an assessment creates a draft copy.

The clone does not copy submissions, attempts, manual evaluation, teacher notes, or submitted answers.

Expected clone status:

```text
isPublished = false
submissionCount = 0
canLaunchFrontend = false
```

## Current MVP limitations

- Minimal admin authentication is implemented with JWT Bearer tokens.
- Admin credentials are currently configured through application settings.
- There is no full user/role system yet.
- Student accounts are not implemented yet; student flow currently uses `participantCode`.
- Error responses are not fully standardized yet: some errors are plain text, some are JSON.
- Topics are optional for MVP launch readiness.
- `TextInput` questions are optional.
- `SingleChoice` questions are optional.
- An assessment must contain at least one frontend-supported question: `SingleChoice` or `TextInput`.
- `MultipleChoice`, `NumericInput`, and `Slider` may exist in backend/domain code, but the current student frontend flow is built around `SingleChoice` and `TextInput`.

## Source of truth

The backend is now the source of truth for:

- assessments
- publication state
- topics
- assessment-topic links
- questions
- answer options
- submissions
- automatic choice scoring
- manual evaluation of open answers
- teacher notes
- self-evaluation

`localStorage` may be used only as temporary UI cache during migration. It must not remain a competing permanent source of truth.

## Frontend integration

For frontend integration, run the backend first and switch the frontend from mock mode to backend mode.

Example frontend config:

```javascript
useMock = false;
apiBase = "https://localhost:7101/api";
```

Use the actual backend URL shown in the terminal after `dotnet run`.

The frontend should call the backend API instead of relying on local mock data or `localStorage`.

## Manual smoke checklist

Before handing a backend build to frontend integration, test these flows.

### Published and ready assessment

```text
GET /api/questions
GET /api/assessments/{assessmentId}/frontend-questions
POST /api/submissions
POST /api/assessments/{assessmentId}/submissions
GET /api/submissions/details
GET /api/assessments/{assessmentId}/submissions/details
PUT /api/submissions/{submissionId}/open-answers/{attemptQuestionId}/evaluation
PUT /api/submissions/{submissionId}/open-answers/{attemptQuestionId}/teacher-note
```

Expected result:

- student questions are returned
- submission returns `201 Created`
- submissions are visible in admin endpoints
- manual evaluation returns `204 No Content`
- teacher note update returns `204 No Content`

### Draft assessment

Expected result:

- structural editing is allowed if there are no submissions
- student launch is blocked while unpublished
- publication is allowed only when the assessment is ready for frontend

### Published assessment

Expected result:

- structural editing is blocked
- deletion is blocked
- unpublish is allowed
- metadata editing is allowed, except blocked `timeLimitMinutes` changes

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
