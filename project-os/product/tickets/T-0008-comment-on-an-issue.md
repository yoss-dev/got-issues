---
id: T-0008
title: Comment on an issue
type: feature
status: backlog
priority: normal
owner: none
implemented_by: none
accepted_by: none
depends_on: [T-0005, T-0009]
adrs: [ADR-0004]
created: 2026-08-30
updated: 2026-08-30
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

## Examples / Scenarios

- Post a comment, list the thread: it appears with the correct author and timestamp.
- Post with a forged author in the body: stored against the authenticated caller instead (AC2).
- Post to a deleted or nonexistent issue: 404, nothing written.
- A thread longer than one page: paging returns each comment exactly once.
- Comment text containing personal data: stored, never logged ([SECURITY.md](../../standards/SECURITY.md)).

## Technical Notes

AC2 is the security-relevant criterion here: taking authorship from the token rather than the payload is the difference between an audit trail and a fiction. It should be tested explicitly, not assumed from the implementation.

## Dependencies

- **T-0005** — comments attach to issues.
- **T-0009** — comment authorship references the user projection built from token claims.

## Risks / Unknowns

- **Editing and deleting are deferred, and that is a product decision, not an oversight.** A comment thread that cannot be corrected is annoying; one that can be silently rewritten undermines the history the feature exists to preserve. Refinement should record which behaviour is wanted before someone implements the easy version.
- Comment bodies are free text typed by employees and must be treated as potentially containing personal data — never logged, minimised ([SECURITY.md](../../standards/SECURITY.md)). The DoR's security conditional applies.
- Authorship depends on T-0009's user projection; if it slips, there is nothing to attribute a comment to.
- Maximum comment length is unspecified; unbounded text columns invite abuse and awkward responses.
- Ordering is stated as "defined" but not defined — creation time is the obvious choice, and refinement should say so rather than leave it to the implementer.

## Testing Notes

Integration tests against real PostgreSQL. AC2 needs a test that deliberately forges an author in the payload and asserts it is ignored — the negative case is the point.

## Relevant ADRs & Documentation

- [ADR-0004](../../architecture/adr/ADR-0004-contract-first-openapi-code-generation.md), [ENGINEERING.md](../../standards/ENGINEERING.md), [TESTING.md](../../standards/TESTING.md), [SECURITY.md](../../standards/SECURITY.md)
- [IDEA-003](../IDEAS.md) — the originating idea

## Definition of Ready

- [ ] Meets [DoR](../../governance/DEFINITION_OF_READY.md) — checked during refinement; note applied exceptions here.

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
