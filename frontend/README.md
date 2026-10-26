# ReDI Assessment — Frontend

Static HTML, CSS and JavaScript frontend for the [ReDI Assessment prototype](../README.md). No build step is required.

## Pages and workflow

- `evaluation.html` — student flow: participant code (no student account) → self-evaluation → single-choice and open-text questions → submit. A participant code is an identifier, **not authentication**.
- `admin.html` — teacher dashboard: login, assessment selection, draft creation/cloning, question-bank editing and JSON import/export, publication, submission review, notes/evaluations and CSV exports.

Share the assessment-specific **Open student link** from the teacher dashboard (`evaluation.html?assessmentId=...`). Without that parameter, the student page uses the backend's configured default assessment, which may not exist in a new database.

The student sees a submission confirmation, not the teacher's scores or per-question feedback.

## Local operation

Use the [root README](../README.md) for prerequisites, database setup, test credentials and running both frontend and backend. The frontend is served as static files, typically on `http://localhost:8000`, and both pages call `https://localhost:7101/api` by default. The backend must be running and its development HTTPS certificate trusted.

The API base URL is defined in `evaluation.js` and `admin.js`, with optional `window.REDI_API_BASE` override.

## Editing and persistence

- All assessment content, submissions and teacher reviews are persisted through the backend API; there is no localStorage/mock-data fallback.
- Draft question-bank edits, JSON imports and Reset to Defaults are saved atomically. A version conflict requires reloading before editing again.
- Published assessments and assessments with submissions cannot have their structure edited; clone to make a new draft.
- Open-answer teacher notes and evaluations are persisted with **Save review** and are available in later sessions and detailed exports.
- The admin authentication token is held in `sessionStorage`; it is not a production authentication design.
- `questions-default.js` contains a starter question set used only by the explicit Reset to Defaults action.

## File map

| Path | Purpose |
| --- | --- |
| `evaluation.html`, `evaluation.css`, `evaluation.js` | Student interface |
| `admin.html`, `admin.css`, `admin.js` | Teacher interface |
| `questions-default.js` | Starter questions |
| `logo/` | Visual assets |

See the [API contract](../backend/Docs/frontend-contract.md) for request/response details and [tests](../tests/README.md) for integration checks. Participant codes should not be treated as verified identities; do not enter sensitive personal information in them.
