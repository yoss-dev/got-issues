---
id: T-0008
title: Comment on an issue
type: feature
status: ready
priority: normal
owner: none
implemented_by: none
accepted_by: none
depends_on: [T-0005, T-0009, T-0018]
adrs: [ADR-0004]
created: 2026-08-30
updated: 2026-08-31
---

# T-0008: Comment on an issue

## Problem / Context

Promoted from [IDEA-003](../IDEAS.md). The conversation around a piece of work is usually where the reasoning lives; without it, an issue records what was decided but not why. For a company moving its tooling in-house, that history is much of what makes a tracker worth having.

## Desired Outcome

An authenticated caller can add a comment to an issue and retrieve the issue's comments in order, paginated.

## User / Business Value

Sam keeps decision history attached to the work rather than scattered in chat. Priya's automation can post context onto the issue it concerns — a failing build, a linked change — which is one of the clearest wins of an API-first tracker.

## Scope

### In Scope

- Specification of the comment resource: create on an issue, list an issue's comments, schemas, errors, scopes.
- Implementation behind generated contracts, plus the EF Core migration.
- Authorship recorded from the authenticated caller — never taken from the request body.
- Ordering and pagination of a comment thread.
- Unit and integration tests, including the unauthenticated case and commenting on a nonexistent issue.

### Out of Scope

- Editing and deleting comments (see *Risks* — deferred deliberately, not forgotten).
- Mentions of other users. Without notifications (a non-goal), a mention would be decoration.
- Rich formatting, attachments, reactions, threaded replies.
- Any UI rendering concern.

## Acceptance Criteria

- [ ] AC1: Given an existing issue and an authenticated caller, when they post a comment, then it is persisted against that issue with the caller recorded as its author.
- [ ] AC2: Given a comment payload that also contains an author field, when it is posted, then the authenticated caller's identity is used and the supplied value is ignored — authorship cannot be spoofed through the request body.
- [ ] AC3: Given an issue with several comments, when its comments are requested, then they are returned in the defined order, paginated.
- [ ] AC4: Given an issue identifier that does not exist, when a comment is posted to it, then the API returns 404 with an `application/problem+json` body and nothing is persisted.
- [ ] AC5: Given an empty or over-long comment body, when it is posted, then the API returns 400 with a problem document naming the constraint, as declared in the specification.
- [ ] AC6: Given an unauthenticated or invalid-token caller, when they attempt either operation, then the API returns 401 and nothing is persisted.
- [ ] AC7: Given the specification, when generation and the drift check run, then the diff is empty.
- [ ] AC8: Given a comment thread, when it is listed, then comments are ordered **oldest first** by creation time, tie-broken by the comment's own key, so a thread reads forward and paging is stable.
- [ ] AC9: Given a caller whose token carries a subject with no row in the `users` projection, when they post a comment, then the projection is created for them first and the comment is attributed to it — commenting must not fail because the caller is new ([T-0009](T-0009-role-authorisation-and-user-projection.md) AC5 already creates the row on any authenticated request; this criterion asserts the comment path depends on it rather than duplicating it).
- [ ] AC10: Given a listed comment, when it is read, then the author is carried as `subject` plus `displayName` — the same shape [T-0006](T-0006-issue-lifecycle-fields.md) uses for an assignee, so a client renders a person the same way everywhere.

## Examples / Scenarios

- Post a comment, list the thread: it appears with the correct author and timestamp.
- Post with a forged author in the body: stored against the authenticated caller instead (AC2).
- Post to a deleted or nonexistent issue: 404, nothing written.
- A thread longer than one page: paging returns each comment exactly once.
- Comment text containing personal data: stored, never logged ([SECURITY.md](../../standards/SECURITY.md)).
- A comment of exactly the maximum length: accepted. One character longer: 400 (AC5).
- A comment of only whitespace: 400 — "empty" means empty after trimming, not zero-length (AC5).
- First-ever comment from a caller with no projection row: the row is created, the comment is attributed (AC9).
- **Counter-example, explicitly not expected:** an `authorId` field a client may set. Authorship comes from the token, always (AC2).

## Technical Notes

AC2 is the security-relevant criterion here: taking authorship from the token rather than the payload is the difference between an audit trail and a fiction. It should be tested explicitly, not assumed from the implementation.

**Decisions taken in refinement, 2026-08-31.**

- **Maximum comment length: 10 000 characters**, declared in the specification so the bound is generated into clients and enforced at the contract boundary. The number is arbitrary but not silent: it is long enough for a considered technical explanation and short enough that a `text` column cannot be used as a file store. "Empty" means empty after trimming (see Examples), because a whitespace-only comment is the same defect with a disguise.
- **Ordering is oldest first** (AC8) — the opposite of the issue list's newest-first, deliberately. A thread is read forward; a work queue is read from the top. Both need the same tiebreaker for the same reason: a timestamp alone is not a total order and offset paging over ties duplicates and skips rows.
- **Author is carried as `subject` + `displayName`** (AC10), matching [T-0006](T-0006-issue-lifecycle-fields.md)'s assignee shape. Two different shapes for "a person" in a five-endpoint API is the kind of inconsistency that is free to avoid now and expensive to unpick later.
- **Comments stay append-only.** No edit, and deletion arrives later as an **admin act** (maintainer, 2026-08-30). Recorded here so nobody implements the easy version: a thread that can be silently rewritten undermines the history this feature exists to preserve, and that is a product decision rather than a missing feature.

**Why [T-0018](T-0018-user-subject-tokens.md) is now a dependency — the gap this refinement
found.** AC1 requires the *authenticated caller* to be recorded as author. Every token this
system can currently issue is a client-credentials token carrying **no `sub`** (decoded and
recorded in [T-0015](T-0015-compose-stack-smoke-test.md)), so there is no caller identity to
attribute a comment to. [T-0006](T-0006-issue-lifecycle-fields.md)'s assignment can be tested
against seeded projection rows because the subject arrives in the request body; authorship
cannot, because it arrives in the token. Building this before T-0018 would produce a comment
resource whose author is structurally null — the feature's whole point, absent.

## Dependencies

- **T-0005** — comments attach to issues.
- **T-0009** — comment authorship references the user projection built from token claims.

## Risks / Unknowns

- **Editing and deleting are deferred, and that is a product decision, not an oversight.** A comment thread that cannot be corrected is annoying; one that can be silently rewritten undermines the history the feature exists to preserve. Refinement should record which behaviour is wanted before someone implements the easy version.
- Comment bodies are free text typed by employees and must be treated as potentially containing personal data — never logged, minimised ([SECURITY.md](../../standards/SECURITY.md)). The DoR's security conditional applies.
- Authorship depends on T-0009's user projection; if it slips, there is nothing to attribute a comment to.
- ~~Maximum comment length is unspecified~~ — **10 000 characters**, declared in the contract. See Technical Notes.
- ~~Ordering is stated as "defined" but not defined~~ — **oldest first with a key tiebreaker**, now AC8.
- **Remaining risk: AC2 is the criterion most likely to pass while the system is unsafe.** A test that posts a forged author and reads back the correct one proves nothing if the endpoint never reads the body's author field at all — which is the desired implementation. Mutate it: make the handler prefer the body's author and confirm the test goes red. If it does not, the test is asserting the absence of a feature nobody built.
- **Remaining risk: comment bodies are the first free-text personal data this system stores.** `PROJECT.md` Q8 is still open, and this is the ticket that makes it concrete: an issue title is work data, a comment is a person writing in their own words.

## Testing Notes

Integration tests against real PostgreSQL. AC2 needs a test that deliberately forges an author in the payload and asserts it is ignored — the negative case is the point.

**Mutate first** ([TESTING.md](../../standards/TESTING.md)): AC2 as described in Risks, then AC5's boundary — move the declared maximum by one and confirm the length tests fail. A bound tested only well inside its limit is not tested. Note that AC5's cases must exercise *exactly* the maximum and exactly one over; a test using 5 000 and 50 000 characters would pass under any bound between them.

## Relevant ADRs & Documentation

- [ADR-0004](../../architecture/adr/ADR-0004-contract-first-openapi-code-generation.md), [ENGINEERING.md](../../standards/ENGINEERING.md), [TESTING.md](../../standards/TESTING.md), [SECURITY.md](../../standards/SECURITY.md)
- [IDEA-003](../IDEAS.md) — the originating idea

## Definition of Ready

- [x] Meets [DoR](../../governance/DEFINITION_OF_READY.md) — evaluated 2026-08-31 during `refinement-session`. All nine universal items hold. Item 5: now depends on **T-0018** as well as T-0005 and T-0009 — the gap this refinement found; sequencing behind it is what keeps item 9 (no blocker) honest rather than pretending authorship works today. Conditional items: security/privacy named (free-text personal data, authorship from the token, `PROJECT.md` Q8) and reflected in AC2 and AC6; data-shape impact identified (the comment table and its migration); no UX; no ADR-bar decision. No exceptions applied.

## Definition of Done

- [ ] Meets [DoD](../../governance/DEFINITION_OF_DONE.md) — checked by `complete-ticket`; deviations require recorded PO/human approval.

---

## Work Log

### 2026-08-30 — Product Owner (claude-sm-9d4e)

- **Did:** Created by promoting IDEA-003 during backlog seeding.
- **Decided:** Kept edit/delete out of scope but recorded *why* in Risks, so the deferral is a decision refinement can revisit rather than a gap someone fills by accident.
- **Remaining:** Refinement to Ready; edit/delete behaviour and comment length are the decisions to settle.
- **Open questions / blockers:** none blocking creation. The user-concept gap flagged at creation is now resolved by T-0009 (added as a dependency, 2026-08-30).
- **Branch / PR:** n/a
- **Test state:** n/a — not started.

### 2026-08-30 — Product Owner (claude-sm-9d4e)

- **Did:** Added T-0009 as a dependency — comment authorship resolves against its user projection rather than a bare token subject.
- **Decided:** none beyond the dependency.
- **Remaining:** Refinement to Ready; edit/delete behaviour is still the open decision. Note that deleting comments is an `admin` act when it arrives (maintainer, 2026-08-30).
- **Open questions / blockers:** none.
- **Branch / PR:** n/a
- **Test state:** n/a — not started.

### 2026-08-31 — Refinement (claude-sm-9d4e) — PO · BA · ENG · ARCH · QA · SEC

**The finding: this ticket had an unrecorded dependency on [T-0018](T-0018-user-subject-tokens.md).**
AC1 attributes a comment to the authenticated caller, and no token this system issues carries a
`sub`. T-0009's projection has been listed as the dependency since creation, but the projection
is only half of it — it stores whoever the token names, and the token names nobody. Recorded as
a dependency rather than a risk, because building this first yields a comment resource whose
author is structurally null, which is the feature's entire point.

Worth noting how it was found: by asking, for each criterion, *what has to exist for this to be
verifiable* — the same question that exposed T-0009's claim-mapping bug. The dependency was
invisible from the ticket alone and obvious from the token.

**Product (PO).** Valuable and vision-aligned; append-only confirmed as a decision, with
deletion recorded as a future admin act rather than an omission.

**Analysis (BA).** Closed both open decisions (length, ordering) and added three criteria: AC8
(ordering, with the tiebreaker that makes paging stable), AC9 (a first-time commenter must not
fail), AC10 (author shape consistent with T-0006's assignee). Added a counter-example forbidding
a client-settable author field, and an Examples entry for whitespace-only bodies, which is the
"empty" case an implementer would otherwise define as zero-length.

**Engineering (ENG).** Straightforward on the current stack: one table, one migration, two
operations. The author foreign key references `users(Subject)` and must not cascade, matching
T-0006's decision — deleting a user must not delete their words.

**Architecture (ARCH).** No ADR-bar decision.

**QA.** All criteria independently verifiable. AC2 is named as the one most likely to pass while
the system is unsafe, with the mutation that settles it.

**Security.** This is the first free-text personal data the system stores — an issue title is
work data, a comment is a person writing in their own words. `PROJECT.md` Q8 is open and this
ticket makes it concrete. Named in Risks; AC2 and AC6 carry the enforceable parts.

**Sizing.** Within the guideline.

- **Did:** Applied all six perspectives; added T-0018 as a dependency; settled length, ordering,
  author shape and append-only; added AC8–AC10 and the mutations for AC2 and AC5.
- **Decided:** 10 000 characters; oldest-first with a key tiebreaker; author as
  `subject` + `displayName`; append-only with deletion as a later admin act.
- **Remaining:** implementation, after T-0018.
- **Open questions / blockers:** none of its own. `PROJECT.md` Q8 applies but gates real
  employee data, not implementation against test identities — the same reading T-0009 used.
- **DoR verdict:** **ready.**
- **Branch / PR:** n/a
- **Test state:** n/a — not started.
