# Technical Overview — ReDI Assessment

This is a concise guide for developers taking over the [basic prototype](../README.md). Product boundaries are documented in [Product & Scope](product-scope.md); exact HTTP contracts belong in [frontend-contract.md](../backend/Docs/frontend-contract.md).

## Architecture

The repository contains one static frontend (`frontend/`) and one ASP.NET Core Web API (`backend/`) with a single SQL Server database. The frontend uses plain HTML/CSS/JavaScript and calls the API directly. The API uses .NET 10, Entity Framework Core and SQL Server LocalDB in development. There is no frontend build pipeline and no mock-data fallback in the integrated pages.

Backend folders: `Controllers/` (HTTP entry points), `DTOs/` (request/response shapes), `Models/` (entities), `Data/` (EF context and seed data), `Services/` (business rules), `Migrations/` (schema evolution), `Settings/` (configuration). Swagger/OpenAPI is available in Development.

## Core data model

- `Assessment` has questions, linked topics and attempts, plus title, description, optional `TimeLimitMinutes` and publication state.
- `Topic` is global; `AssessmentTopic` links it to an assessment and maintains ordering. Topic names and assessment-topic pairs have unique indexes.
- `Question` belongs to an assessment and has `QuestionType`, text, sort order and zero or more `AnswerOption` records.
- `AssessmentAttempt` belongs to an assessment, stores `ParticipantCode` and timestamps, and has `AttemptQuestion` and `AttemptSelfEvaluation` children.
- `AttemptAnswerOption` connects attempt answers to selected options. Teacher reviews and open-answer details are handled through the attempt/submission workflow.

The `QuestionType` enum defines `SingleChoice`, `MultipleChoice`, `NumericInput`, `Slider` and `TextInput`. **Only SingleChoice and TextInput are supported by the integrated student/editor flow.** The existence of an enum value is not proof of a delivered feature.

## Main flows

1. **Prepare:** an authenticated teacher creates or clones a draft, manages topics and questions, and saves the question bank.
2. **Publish:** backend readiness rules determine whether the assessment can be launched; publication is a separate action.
3. **Participate:** the student follows an assessment-specific link, enters an arbitrary participant code, completes self-evaluation/questions and submits.
4. **Review:** the API stores submissions and automatic choice scoring; teachers review written answers, save notes/evaluations and export results.

Student codes are identifiers, not authentication. Admin access uses a configured development login and JWT Bearer tokens. No full user/role system is implemented.

## Critical invariants

- New assessments are unpublished drafts.
- Published assessments and assessments with attempts cannot be structurally edited; clone to start a new draft without copying historical submissions.
- Question-bank saves are transactional and reject stale versions, invalid content and unsupported question types rather than partially applying changes.
- A launchable assessment must be published and frontend-ready, with at least one `SingleChoice` or `TextInput` question. Topics are optional for MVP readiness.
- The backend database owns persistent application data. The browser does not independently persist assessment content or reviews.
- Choice scoring and manual written-answer evaluation are separate mechanisms; do not assume a unified overall score.

## Development and verification

Follow the [root README](../README.md) for first-time setup and the [local workflow](local-workflow.md) for `redi.ps1` start/stop/backup/restore. Database migrations are applied explicitly. The database is not committed to Git.

Use [tests/README.md](../tests/README.md) for isolated database/browser integration checks. **Never run destructive integration checks against the normal application database.** See [product backlog](product-backlog.md) for proposals, not implemented features.

The detailed [frontend/backend contract](../backend/Docs/frontend-contract.md) is the reference for endpoints, payloads and authentication. The code and migrations are authoritative if documentation becomes outdated.
