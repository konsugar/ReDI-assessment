# Product & Scope — ReDI Assessment

## Purpose and status

ReDI Assessment is a basic prototype for evaluating prospective students' knowledge before an educational course. It provides a student-facing assessment flow and a teacher-facing dashboard for preparing assessments and reviewing submissions.

The published repository implements a limited, locally runnable workflow meeting the minimum project requirements. It is **not production-ready**. Additional development was started but has not been published; continuation depends on available resources and funding.

This document describes the product and its boundaries. For installation, test credentials and the student entry flow, see the [root README](../README.md). For detailed API behavior, see the [frontend/backend contract](../backend/Docs/frontend-contract.md).

## Users and workflows

**Student / participant.** A participant opens a link to a published assessment, supplies a self-chosen participant code instead of registering or logging in, rates their own knowledge, answers single-choice and open-text questions, and submits the assessment. The code identifies a submission but does **not** authenticate the participant. The student receives a submission confirmation rather than teacher-only scoring details.

**Teacher / administrator.** A teacher signs in to the prototype's development admin interface, creates an assessment draft or clones an existing assessment, manages topics and questions, imports or exports a question bank, publishes a ready assessment, shares its student link, reviews submissions, adds notes and evaluations to open answers, and exports results.

## Core product concepts

- **Assessment:** a configured evaluation with metadata, questions, publication state and a student launch link.
- **Topic:** a reusable subject area used for organizing questions and student self-evaluation.
- **Question:** an assessment item with its text, type and, where applicable, answer options and a correct answer.
- **Submission / attempt:** a participant's submitted answers and associated self-evaluation, stored in the database.
- **Teacher review:** teacher notes and manual evaluation of open-text responses, retained across sessions.

The earlier product concept allowed assessments to contain sections, per-question timing, generated questions and multiple scoring methods. These remain broader design ideas, **not claims about the published student interface**.

## Implemented prototype scope

The published workflow includes:

- Draft assessment creation and cloning, question-bank editing and JSON import/export.
- Publication/readiness checks and assessment-specific student links.
- Student self-evaluation, single-choice questions and free-text questions.
- Persistence of assessments, submissions and reviews in one backend database.
- Automatic scoring of single-choice responses; separate manual review and notes for open answers.
- Teacher submission review and CSV exports.
- Protection against structural changes to published assessments or assessments with submissions.
- A basic configured admin login and a participant-code-based student flow.

The frontend is a static HTML/CSS/JavaScript application; the backend is an ASP.NET Core API using Entity Framework Core and SQL Server LocalDB for local development.

## Boundaries and limitations

- This is a prototype, not a deployed multi-user service.
- There are no student accounts, verified participant identities or full teacher/user role management.
- The participant code is not an access-control mechanism.
- The integrated student interface focuses on **single-choice and free-text** questions. Other question types represented in backend/domain code should not be described as available to students.
- Automatic choice scoring and teacher evaluation of written answers are distinct; a combined grading policy has not been established here.
- The application requires a local backend and database; production hosting, credentials, privacy and operational security require additional work.

## Broader product direction (not implemented scope)

The original specification also envisaged multi-section assessments, additional formats (multiple choice with several correct options, sliders, elimination and ordering), question banks and dynamically generated questions, configurable assessment/question time limits, weighted or penalty-based scoring, time-based and confidence-based scoring, and richer attempt histories and results.

These are **potential extensions**, not commitments or accepted requirements for this prototype. Their exact behavior and priorities need product decisions before implementation. The [candidate backlog](product-backlog.md) lists nearer-term improvements and unresolved decisions without treating them as delivered features.

## Scope and source-of-truth rules

- The backend database is the persistent source of truth for assessment content, publication, submissions and teacher reviews.
- Published or already-attempted assessment structures must not be silently modified; cloning creates a new editable draft.
- A launchable student assessment must satisfy publication and readiness rules.
- Any future feature must be documented as *implemented* only after it exists in the integrated application and has been verified.

This document intentionally omits setup commands, credentials, endpoint listings, database schemas and detailed change history; those belong to the linked technical documentation.
