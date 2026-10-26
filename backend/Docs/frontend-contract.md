# ReDI Assessment Frontend Contract

This document describes the current backend contract for the student frontend and admin frontend.

Backend base URL in local development is usually:

```text
{{ReDI.Assessment.Api_HostAddress}}
```

or a concrete URL such as:

```text
https://localhost:7101
```

## 0. Authentication

### 0.1 Admin login

```http
POST /api/admin/auth/login
Content-Type: application/json

{
	"username": "admin",
	"password": "admin-dev-password"
}
```

Successful response:
```
{
	"accessToken": "...",
	"tokenType": "Bearer",
	"expiresUtc": "2026-01-01T12:00:00Z",
	"username": "admin"
}
```

Use the token for protected admin endpoints:

```
Authorization: Bearer <accessToken>
```

### 0.2 Public endpoints

These endpoints do not require an admin token:

```
POST /api/admin/auth/login
GET /api/questions
POST /api/submissions
GET /api/assessments/{assessmentId}/frontend-questions
POST /api/assessments/{assessmentId}/submissions
GET /api/frontend-assessments/launchable
```

### 0.3 Protected admin endpoints

Admin content, assessment status, submissions review, topics, questions, answer options, and attempt endpoints require:

```
Authorization: Bearer <accessToken>
```

## 1. Student frontend contract

### 1.1 Current frontend assessment

Use these endpoints when the frontend works with the assessment configured in backend settings:

```http
GET /api/questions
POST /api/submissions
```

`GET /api/questions` returns the current published and frontend-ready assessment.

Response shape:

```json
{
	"topics": [
		{
			"id": 1,
			"name": "HTML",
			"description": null
		}
	],
	"questions": [
		{
			"id": 10,
			"text": "Question text",
			"options": [
				"Option A",
				"Option B"
			]
		}
	],
	"openQuestions": [
		{
			"id": 20,
			"text": "Open question text"
		}
	]
}
```

Important:

- `questions[]` contains `SingleChoice` questions only.
- `openQuestions[]` contains `TextInput` questions.
- Correct answers are not returned to the frontend.
- Backend does the scoring.

### 1.2 Selected assessment flow

Use these endpoints when the frontend/admin allows selecting a specific assessment:

```http
GET /api/assessments/{assessmentId}/frontend-questions
POST /api/assessments/{assessmentId}/submissions
```

The response/request shapes are the same as for:

```http
GET /api/questions
POST /api/submissions
```

### 1.3 Launchable assessments

Use this endpoint if the student frontend needs to list assessments that can actually be started:

```http
GET /api/frontend-assessments/launchable
```

This endpoint is public and is the only assessment-list endpoint intended for the student frontend.
It returns only assessments where:

```text
canLaunchFrontend = true
```

That means:

```text
isPublished = true
isReadyForFrontend = true
```

### 1.4 Submit answers

Current frontend assessment:

```http
POST /api/submissions
```

Selected assessment:

```http
POST /api/assessments/{assessmentId}/submissions
```

Request body:

```json
{
	"participantCode": "ReDI-001",
	"selfEvaluation": [
		{
			"topicId": 1,
			"topicName": "Ignored by backend",
			"rating": 3
		}
	],
	"multipleChoiceAnswers": [
		{
			"questionId": 10,
			"selectedOption": 1
		}
	],
	"openAnswers": [
		{
			"questionId": 20,
			"questionText": "Ignored by backend",
			"text": "Student answer"
		}
	]
}
```

Rules:

- `participantCode` is required.
- `selectedOption` is a zero-based option index.
- `topicName` is ignored by backend.
- `questionText` is ignored by backend.
- If the assessment has no topics, send `selfEvaluation: []`.
- If the assessment has no `TextInput` questions, send `openAnswers: []`.
- If the assessment has no `SingleChoice` questions, send `multipleChoiceAnswers: []`.
- The assessment must contain at least one frontend-supported question: `SingleChoice` or `TextInput`.

Successful response:

```http
201 Created
```

Response body:

```json
{
	"submissionId": 123,
	"participantCode": "ReDI-001",
	"score": 5,
	"total": 8,
	"percentage": 62,
	"questionResults": [],
	"selfEvaluation": [],
	"openAnswers": []
}
```

## 2. Admin content management contract

All endpoints in this section require an admin JWT token unless explicitly stated otherwise.

Use this contract for the admin screen that creates and edits assessments, topics, questions, answer options, and publication status.

### 2.1 Main aggregate endpoint

Use this as the primary data source for the assessment content editor screen:

```http
GET /api/assessments/{assessmentId}/content
```

Response shape:

```json
{
	"assessment": {
		"id": 14,
		"title": "Assessment title",
		"description": null,
		"timeLimitMinutes": 30,
		"isPublished": true,
		"createdUtc": "2026-05-30T10:00:00Z"
	},
	"status": {
		"id": 14,
		"title": "Assessment title",
		"description": null,
		"questionCount": 12,
		"topicCount": 3,
		"submissionCount": 5,
		"isCurrent": true,
		"isPublished": true,
		"isReadyForFrontend": true,
		"canLaunchFrontend": true,
		"readinessIssues": [],
		"canEditStructure": false,
		"structureLockReason": "Assessment structure is locked because it already has 5 submission(s)."
	},
	"topics": [],
	"availableTopics": [],
	"questionTypes": [],
	"questions": []
}
```

### 2.2 UI lock rule

Use this field to enable or disable structural editing:

```text
status.canEditStructure
```

If `canEditStructure = false`, disable:

- add question
- edit question text/type/sortOrder
- delete question
- add/edit/delete answer options
- attach/remove/reorder assessment topics

Show this field to the admin:

```text
status.structureLockReason
```

### 2.3 Publication

Publication is controlled only through the dedicated publication endpoint.

```http
PUT /api/assessments/{assessmentId}/publication
Content-Type: application/json

{
	"isPublished": true
}
```

To unpublish:

```http
PUT /api/assessments/{assessmentId}/publication
Content-Type: application/json

{
	"isPublished": false
}
```

Do not publish via:

```http
PUT /api/assessments/{assessmentId}
```

Publishing can be rejected if the assessment is not ready for frontend.

### 2.4 Assessment metadata

```http
GET /api/assessments
GET /api/assessments/{assessmentId}
POST /api/assessments
PUT /api/assessments/{assessmentId}
DELETE /api/assessments/{assessmentId}
POST /api/assessments/{assessmentId}/clone
```

Create assessment:

```http
POST /api/assessments
Content-Type: application/json

{
	"title": "Assessment title",
	"description": null,
	"timeLimitMinutes": 30
}
```

New assessments are drafts:

```text
isPublished = false
```

Update assessment:

```http
PUT /api/assessments/{assessmentId}
Content-Type: application/json

{
	"title": "Updated title",
	"description": null,
	"timeLimitMinutes": 30
}
```

`title` and `description` can be updated as metadata.

`timeLimitMinutes` can be rejected if the assessment is published or already has submissions.

Delete assessment can be rejected if the assessment is published or already has submissions.

Clone creates a draft copy:

```text
isPublished = false
submissionCount = 0
```

### 2.5 Assessment status endpoints

Admin-only status endpoints:

```http
GET /api/frontend-assessments
GET /api/frontend-assessments/{assessmentId}
GET /api/frontend-assessments/current
```

These require:
```
Authorization: Bearer <accessToken>
```
Use these endpoints for selectors, badges, launch status, and refresh after admin operations.

Important fields:

```text
isPublished
isReadyForFrontend
canLaunchFrontend
readinessIssues
canEditStructure
structureLockReason
submissionCount
isCurrent
```

### 2.6 Attached topics

In the aggregate response, attached topics are in:

```text
topics[]
```

Each item contains:

```text
assessmentTopicId
topicId
name
description
sortOrder
```

Attach topic:

```http
POST /api/assessments/{assessmentId}/topics
Content-Type: application/json

{
	"topicId": 3,
	"sortOrder": 0
}
```

Reorder attached topic:

```http
PUT /api/assessments/{assessmentId}/topics/{assessmentTopicId}
Content-Type: application/json

{
	"sortOrder": 10
}
```

Remove topic from assessment:

```http
DELETE /api/assessments/{assessmentId}/topics/{assessmentTopicId}
```

This removes only the link between assessment and topic. It does not delete the global topic.

### 2.7 Available global topics

In the aggregate response, global topics not yet attached to this assessment are in:

```text
availableTopics[]
```

Each item contains:

```text
id
name
description
```

Use this list for an “Add topic” dropdown.

### 2.8 Global topics

```http
GET /api/topics
GET /api/topics/{topicId}
POST /api/topics
PUT /api/topics/{topicId}
DELETE /api/topics/{topicId}
```

Create topic:

```http
POST /api/topics
Content-Type: application/json

{
	"name": "Backend",
	"description": null
}
```

Update topic:

```http
PUT /api/topics/{topicId}
Content-Type: application/json

{
	"name": "Backend updated",
	"description": null
}
```

Delete topic:

```http
DELETE /api/topics/{topicId}
```

Global topic update/delete can be rejected if the topic is used by published/submitted assessments.

Deleting a global topic is allowed only if it is not used by any assessment.

### 2.9 Questions

In the aggregate response, questions are in:

```text
questions[]
```

Each item contains:

```text
id
text
questionType
questionTypeName
isSupportedForFrontend
sortOrder
answerOptions[]
```

Create question:

```http
POST /api/assessments/{assessmentId}/questions
Content-Type: application/json

{
	"text": "Question text",
	"questionType": 1,
	"sortOrder": 0
}
```

Update question:

```http
PUT /api/assessments/{assessmentId}/questions/{questionId}
Content-Type: application/json

{
	"text": "Updated question text",
	"questionType": 1,
	"sortOrder": 0
}
```

Delete question:

```http
DELETE /api/assessments/{assessmentId}/questions/{questionId}
```

Deleting a question in an editable draft also deletes its answer options.

### 2.10 Answer options

In the aggregate response, answer options are in:

```text
questions[].answerOptions[]
```

Each item contains:

```text
id
questionId
text
isCorrect
sortOrder
```

Create answer option:

```http
POST /api/questions/{questionId}/answer-options
Content-Type: application/json

{
	"text": "Option text",
	"isCorrect": false,
	"sortOrder": 0
}
```

Update answer option:

```http
PUT /api/questions/{questionId}/answer-options/{answerOptionId}
Content-Type: application/json

{
	"text": "Updated option text",
	"isCorrect": true,
	"sortOrder": 0
}
```

Delete answer option:

```http
DELETE /api/questions/{questionId}/answer-options/{answerOptionId}
```

### 2.11 Question types

Use this aggregate response field to populate the editor dropdown:

```text
questionTypes[]
```

Each item contains:

```text
value
name
isSupportedForFrontend
usesAnswerOptions
```

For current student frontend, only question types with this field should be used for launchable assessments:

```text
isSupportedForFrontend = true
```

## 3. Admin submissions/review contract

All review endpoints in this section require an admin JWT token, except student submission creation endpoints described in the student frontend contract.

Use this contract for the teacher/admin review dashboard.

Backend is the source of truth for submissions, open answer evaluation, and teacher notes.

Do not use `localStorage` as permanent storage for review data.

### 3.1 Load submissions for current frontend assessment

```http
GET /api/submissions/details
```

This returns an array of submissions for the current configured frontend assessment.

Each submission contains:

```text
attemptId
assessmentId
submissionId
participantCode
timestamp
submittedUtc
score
total
percentage
questionResults[]
selfEvaluation[]
openAnswers[]
```

Use this endpoint for the current MVP admin dashboard if the UI does not yet support selecting `assessmentId`.

### 3.2 Load submissions for selected assessment

```http
GET /api/assessments/{assessmentId}/submissions/details
```

Use this version when the admin UI supports selecting a specific assessment.

### 3.3 Load one submission

Current frontend assessment:

```http
GET /api/submissions/{submissionId}
```

Selected assessment:

```http
GET /api/assessments/{assessmentId}/submissions/{submissionId}
```

Use this if the dashboard first loads summaries and then opens a detail view.

### 3.4 Choice results

`questionResults[]` contains automatically scored choice answers.

Each item contains:

```text
questionId
questionText
options[]
selectedOption
correctIndex
correct
```

The frontend should display these as read-only review data.

### 3.5 Self-evaluation

`selfEvaluation[]` contains:

```text
topicId
topicName
rating
```

This is submitted by the student and is read-only in the current admin UI.

If an assessment has no topics, `selfEvaluation` can be an empty array.

### 3.6 Open answers

`openAnswers[]` contains:

```text
attemptQuestionId
questionId
questionText
text
isCorrect
evaluationStatus
teacherNote
```

Important:

- Use `attemptQuestionId` for review updates.
- Do not use `questionId` as the submitted answer identifier.

`questionId` identifies the original question.

`attemptQuestionId` identifies this specific submitted answer inside this submission.

### 3.7 Update teacher evaluation

Current frontend assessment:

```http
PUT /api/submissions/{submissionId}/open-answers/{attemptQuestionId}/evaluation
Content-Type: application/json

{
	"isCorrect": true
}
```

Selected assessment:

```http
PUT /api/assessments/{assessmentId}/submissions/{submissionId}/open-answers/{attemptQuestionId}/evaluation
Content-Type: application/json

{
	"isCorrect": true
}
```

Allowed values:

```json
{
	"isCorrect": true
}
```

```json
{
	"isCorrect": false
}
```

```json
{
	"isCorrect": null
}
```

Expected success response:

```http
204 No Content
```

UI mapping:

```text
true  -> isCorrect = true,  evaluationStatus = "Correct"
false -> isCorrect = false, evaluationStatus = "Incorrect"
null  -> isCorrect = null,  evaluationStatus = "NotEvaluated"
```

After success, either refetch the submission/submissions endpoint or update local UI state with this mapping.

### 3.8 Update teacher note

Current frontend assessment:

```http
PUT /api/submissions/{submissionId}/open-answers/{attemptQuestionId}/teacher-note
Content-Type: application/json

{
	"teacherNote": "Teacher comment here."
}
```

Selected assessment:

```http
PUT /api/assessments/{assessmentId}/submissions/{submissionId}/open-answers/{attemptQuestionId}/teacher-note
Content-Type: application/json

{
	"teacherNote": "Teacher comment here."
}
```

Expected success response:

```http
204 No Content
```

An empty string clears the note:

```json
{
	"teacherNote": ""
}
```

After success, either refetch the submission/submissions endpoint or update local UI state:

```text
teacherNote = submitted value
```

## 4. Error handling

For `400 Bad Request` responses, show the backend message to the admin/student where appropriate.

Some errors return plain text, for example:

```text
Assessment is not published.
```

Some readiness errors return structured JSON:

```json
{
	"message": "Assessment is not ready for frontend.",
	"readinessIssues": [
		"Question 23 must have exactly one correct answer option."
	]
}
```

Frontend should support both plain text and JSON error responses.

## 5. Storage rules

Backend is now the source of truth for:

- assessments
- publication status
- topics
- assessment-topic links
- questions
- answer options
- submissions
- automatic choice scoring
- open answer manual evaluation
- teacher notes
- self-evaluation

`localStorage` can be used only as temporary UI cache during migration. It must not remain a competing permanent source of truth.
