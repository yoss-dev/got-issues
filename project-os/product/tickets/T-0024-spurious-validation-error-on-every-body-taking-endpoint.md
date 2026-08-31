---
id: T-0024
title: A spurious "field is required" accompanies every body validation failure
type: bug
status: backlog
priority: normal
owner: none
implemented_by: none
accepted_by: none
depends_on: []
adrs: [ADR-0004]
created: 2026-08-31
updated: 2026-08-31
---

# T-0024: A spurious "field is required" accompanies every body validation failure

## Problem / Context

Found by `claude-rev-7a03` during [T-0006](T-0006-issue-lifecycle-fields.md)'s review and confirmed
against endpoints that ticket does not touch, so it is neither new nor local to it.

**Reproduction.** Send a body whose value fails validation, to any endpoint that takes one:

| Request | Response carries |
| --- | --- |
| `POST /projects` with `{"key":123}` | the real error **and** `"The createProjectRequest field is required."` |
| `POST /projects/{key}/issues` with `{"title":123}` | the real error **and** `"The createIssueRequest field is required."` |
| `PATCH /issues/{key}` with an undeclared enum value | the real error **and** `"The updateIssueRequest field is required."` |

**Expected:** the problem document names the field that is actually wrong.
**Observed:** it names that field *and* claims the entire request body is missing, which it is not
— the body was sent, parsed, and rejected on its contents.

**Cause:** when the body fails to bind, ASP.NET Core's model binder records the parameter itself as
missing in addition to the underlying error. The parameter is named after the generated request
type, so the message leaks a generated C# identifier into a public API response as well.

**Severity: low.** Every affected response is already a 400 with the correct field named; nothing is
wrong except that a second, false statement travels beside the true one. It is filed because this
project's recurring defect is exactly *a response saying something other than what is true*, and
because a client reading `errors` cannot tell which entry to show a user.

## Desired Outcome

A body validation failure names what is wrong with the body, and nothing else.

## User / Business Value

A client displaying validation errors currently has to know which of them to ignore. The API also
stops naming its own generated types in responses, which are an implementation detail no consumer
of the contract should see.

## Scope

### In Scope

- Suppress the parameter-level "is required" error when the body was present and failed validation
  on its contents.
- The same behaviour on every endpoint that takes a body, present and future — this is one place,
  not one per controller.
- A test that would fail if the spurious entry returned, on at least one endpoint per HTTP verb
  that takes a body.

### Out of Scope

- Changing what the genuine validation errors say.
- The problem-document shape generally (`type`, `traceId`, `errors`) — [T-0020](T-0020-correlate-a-500-with-its-cause.md)
  and [T-0017](T-0017-automated-contract-conformance-tier.md) hold those questions.
- A genuinely absent body, which *should* report that the body is required.

## Acceptance Criteria

- [ ] AC1: Given a request whose body is present but fails validation, when the problem document is inspected, then it contains only errors about the body's contents — no entry claiming the body itself is missing.
- [ ] AC2: Given a request with **no body at all** on an endpoint that requires one, when it is rejected, then the response does still say the body is required — the fix must not suppress the true case along with the false one.
- [ ] AC3: Given the responses from AC1, when they are inspected, then no generated C# type name (`createProjectRequest`, `updateIssueRequest`, …) appears in any message.
- [ ] AC4: Given the fix, when a body validation failure occurs on `POST /projects`, `POST /projects/{key}/issues` and `PATCH /issues/{key}`, then all three behave identically — this is shared behaviour and a per-controller fix would be the wrong shape.

## Examples / Scenarios

- `POST /projects` with `{"key":"lowercase","name":"x"}`: one error, about `Key`.
- `POST /projects` with an empty body: an error saying the body is required (AC2).
- `PATCH /issues/GOTI-1` with `{"status":"nonsense"}`: one error, about `Status`.

## Dependencies

None. The fix is in the API's model-binding configuration.

## Risks / Unknowns

- **AC2 is the trap.** The obvious fix — filtering out any error mentioning the parameter name —
  also suppresses the legitimate "you sent no body" case. The two must be distinguished by whether
  the body was present, not by the text of the message.
- The mechanism is a framework behaviour, so a future ASP.NET Core upgrade could change it. AC1's
  test is what would notice.

## Testing Notes

Integration tier. The assertion should be on the **count and content** of `errors`, not merely that
a 400 was returned — the current responses are already 400 with the right field named, so a test
that stops at the status code would pass today and prove nothing.

## Relevant ADRs & Documentation

- [ADR-0004](../../architecture/adr/ADR-0004-contract-first-openapi-code-generation.md) — the generated request types whose names leak
- [T-0006](T-0006-issue-lifecycle-fields.md) — where this was found

## Definition of Ready

- [ ] Meets [DoR](../../governance/DEFINITION_OF_READY.md) — not yet refined.

## Definition of Done

- [ ] Meets [DoD](../../governance/DEFINITION_OF_DONE.md) — checked by `complete-ticket`; deviations require recorded PO/human approval.

---

## Work Log

### 2026-08-31 — Product Owner (claude-sm-9d4e)

- **Did:** Created from T-0006's review note N2, after the reviewer confirmed it on endpoints that ticket does not touch rather than inferring it from the one it does.
- **Decided:** filed rather than fixed in T-0006 — it is shared behaviour on every body-taking endpoint, and changing that under a ticket about lifecycle fields would be a change to shared behaviour arriving sideways. Its sibling note N4 went the other way, because that one was measured to a single call site in T-0006's own code.
- **Remaining:** refinement.
- **Open questions / blockers:** none.
- **Test state:** n/a — not started.
