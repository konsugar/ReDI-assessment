# ReDI School — Knowledge Evaluation

A static web app for ReDI School's pre-course knowledge evaluation. Students complete a self-assessment, a multiple-choice test, and open questions; teachers review submissions and manage the question bank via a separate dashboard.

## Pages

The tool consists of two standalone pages — there's no landing page. Share the URLs directly with the people who need them:

- `evaluation.html` — student-facing evaluation (login → self-evaluation → multiple choice → open questions → submit)
- `admin.html` — teacher dashboard:
  - **Submissions** tab: review each participant's answers, take per-question notes, export CSV (summary or detailed)
  - **Manage Questions** tab: add/edit/delete topics, multiple-choice questions, and open questions; import/export the question bank as JSON

## Running locally

No build step. Either open `evaluation.html` directly in a browser, or serve the folder with any static server:

```sh
python -m http.server 8000
# or
npx serve
```

## Current state

The frontend runs in **mock mode** today — `CONFIG.useMock = true` in `evaluation.js`. In that mode:

- Questions are loaded from `localStorage` (key `redi_questions`, written by the admin's Manage Questions tab) or fall back to `questions-default.js`.
- Submissions are stored in `localStorage` (key `redi_submissions`) and read back by the admin dashboard.
- Teacher notes are stored in `localStorage` (key `redi_teacher_notes`).

To switch to a real backend, set `CONFIG.useMock = false` in `evaluation.js`. The expected base URL is `http://localhost:5000/api` (also configurable in `CONFIG.apiBase`).

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

The shape mirrors the mock response in `postSubmission` in `evaluation.js`.

## Open items for the backend

These are not implemented in the frontend yet, but worth discussing:

- **Persisting teacher-edited questions** — currently the admin writes the question bank to `localStorage` only. A `PUT /api/questions` endpoint plus a small change in `admin.js` would let edits sync across machines.
- **Fetching submissions in the dashboard** — currently the admin reads `localStorage` for submissions. Once the backend stores them, the dashboard should `GET /api/submissions` instead.
- **Teacher notes persistence** — same pattern; currently localStorage-only.
- **Auth** — the admin page is unauthenticated by design (internal trust model). If the backend is exposed beyond localhost, this needs revisiting.

## File map

| File | Purpose |
| --- | --- |
| `evaluation.html`, `evaluation.css`, `evaluation.js` | Student evaluation |
| `admin.html`, `admin.css`, `admin.js` | Teacher dashboard |
| `questions-default.js` | Built-in starter question set (shared by both pages) |
| `logo/` | Logo assets |

## Privacy notes

- Participant codes (e.g. `ReDI-001`) are used in place of names; no PII is collected.
- The participant-facing results page intentionally shows only a confirmation — no score, no per-question feedback. Scores and answers live only in the teacher dashboard.
