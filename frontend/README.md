# ReDI Assessment — Frontend

A static web app for ReDI School's pre-course knowledge evaluation. Students complete a self-assessment, a multiple-choice test, and open questions; teachers review submissions and manage the question bank via a separate dashboard.

## Pages

The tool consists of two standalone pages — there's no landing page. Share the URLs directly with the people who need them:

- `evaluation.html` — student-facing evaluation (login → self-evaluation → multiple choice → open questions → submit)
- `admin.html` — teacher dashboard:
  - **Submissions** tab: review each participant's answers, take per-question notes, export CSV (summary or detailed)
  - **Manage Questions** tab: add/edit/delete topics, multiple-choice questions, and open questions; import/export the question bank as JSON

## Running locally

No build step. From this `frontend/` directory, serve the folder with a static server:

```sh
python -m http.server 8000
# or
npx serve
```

## Current state

The frontend stores questions, topics, submissions, teacher notes and evaluations through the API in SQL Server. There is no browser-storage or mock-data fallback. Both pages use `https://localhost:7101/api`; start the backend with its `https` launch profile. Only the temporary admin authentication token is kept in sessionStorage.

Teachers select an assessment, create a draft or clone an existing assessment. Each editor Save, JSON import and Reset to Defaults persists the complete draft question bank atomically. A version check rejects stale edits from another session. Reload saved questions to resolve a conflict. Published assessments and assessments with attempts are locked against content changes; clone them to make a new version without changing historical answers.

Publish a ready draft and share its **Open student link**, which includes `?assessmentId=...`. The student page without this parameter continues to use the backend's configured current assessment. Teacher submissions and review operations are scoped to the selected assessment.

Use **Save review** for an open answer's note and evaluation. Saved values are loaded from the database in every session and included in detailed CSV exports. Errors remain visible; the interface does not report success when a request fails. Old browser-only data is not silently imported or deleted; question sets can be imported explicitly as JSON.

Keep the API base URL consistent in both JavaScript files. See [the backend contract](../backend/Docs/frontend-contract.md) for the integrated API details.

## Backend API contract

### `GET /api/questions`

Returns the active question set.

```json
{
  "topics": [
    { "id": 1, "name": "HTML", "description": "HyperText Markup Language" }
  ],
  "questions": [
    {
      "id": 1,
      "text": "What does HTML stand for?",
      "options": ["Hyper Text Markup Language", "High Tech Modern Language", "...", "..."],
      "correctIndex": 0
    }
  ],
  "openQuestions": [
    { "id": 101, "text": "Explain what a function is in JavaScript." }
  ]
}
```

See `questions-default.js` for a full example.

### `POST /api/submissions`

Accepts a participant's completed evaluation.

**Request:**
```json
{
  "participantCode": "ReDI-001",
  "selfEvaluation": [
    { "topicId": 1, "topicName": "HTML", "rating": 3 }
  ],
  "multipleChoiceAnswers": [
    { "questionId": 1, "selectedOption": 0 }
  ],
  "openAnswers": [
    { "questionId": 101, "questionText": "Explain...", "text": "Student's answer..." }
  ]
}
```

**Response:** the backend grades the MCQ answers and returns the scored result:
```json
{
  "submissionId": "abc123",
  "participantCode": "ReDI-001",
  "score": 5,
  "total": 8,
  "percentage": 62,
  "questionResults": [
    {
      "questionId": 1,
      "questionText": "What does HTML stand for?",
      "options": ["Hyper Text Markup Language", "...", "...", "..."],
      "selectedOption": 0,
      "correctIndex": 0,
      "correct": true
    }
  ],
  "selfEvaluation": [
    { "topicId": 1, "topicName": "HTML", "rating": 3 }
  ],
  "openAnswers": [
    { "questionId": 101, "questionText": "Explain...", "text": "Student's answer..." }
  ]
}
```

The response is supplied by the backend after the submission is stored.

## Persistence endpoints

- Authenticated `GET/PUT /api/assessments/{id}/question-bank` loads/saves a complete draft bank. PUT requires the version returned by GET. Validation and changes run in one transaction; failed imports leave the saved bank intact.
- Authenticated `/api/assessments/{id}/submissions/details` loads work for the selected assessment; existing evaluation and teacher-note PUT endpoints save review changes.
- Public `/api/assessments/{id}/frontend-questions` and submission POST serve the selected student's test.
- Topic descriptions shared by other assessments cannot be silently changed; use a different topic name to create independent content.
- Development credentials must be replaced before deployment. See [integration test instructions](../tests/README.md) for database and browser verification.

## File map

| File | Purpose |
| --- | --- |
| `evaluation.html`, `evaluation.css`, `evaluation.js` | Student evaluation |
| `admin.html`, `admin.css`, `admin.js` | Teacher dashboard |
| `questions-default.js` | Starter question set used by the teacher's explicit Reset to Defaults action |
| `logo/` | Logo assets |

## Privacy notes

- Participant codes (e.g. `ReDI-001`) are used in place of names; no PII is collected.
- The participant-facing results page intentionally shows only a confirmation — no score, no per-question feedback. Scores and answers live only in the teacher dashboard.
