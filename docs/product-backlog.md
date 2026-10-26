# Candidate functional improvements

The current accepted workflow supports creating a draft, importing/editing questions, publishing and sharing a test, student submission, teacher review and CSV export. The items below are proposals from inspecting that workflow, not additional implemented features. Their product decisions should be agreed before development.

| Priority | Proposal | Reason / decision needed |
| --- | --- | --- |
| Next | Warn before leaving unsaved teacher edits/reviews | A user can navigate away from a form before pressing Save. Decide which navigation should prompt and whether autosave is wanted. |
| Next | Label automatic and manual scoring separately | The main score is the choice-question score; written-answer evaluations are separate. Decide whether written answers contribute to an overall result. |
| Next | Archive completed tests and identify diagnostic data | Repeated tests and acceptance runs remain in the assessment list. Agree on archive/retention rules before implementing cleanup. |
| Later | Define repeat-attempt and participant-code rules | Student codes are not authenticated accounts. Decide whether the same code can submit several attempts and which attempt is considered final. |
| Later | Resume an unfinished student attempt | Current pages keep in-progress entries in memory until submission. Resuming safely requires a server-backed attempt flow and a way to identify that attempt. |
| When needed | Individual teacher accounts and roles | Local development currently uses one configured admin login. Multiple real teachers need account lifecycle and access decisions. |

There is no external-hosting work planned while the application is used locally. Imported historical browser data remains in a separate draft and in its local backup; there is no need to publish or delete it automatically.
