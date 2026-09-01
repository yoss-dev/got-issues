---
id: T-0025
title: Stop documentation from claiming that built things do not exist
type: technical
status: backlog
priority: normal
owner: none
implemented_by: none
accepted_by: none
depends_on: []
adrs: []
created: 2026-08-31
updated: 2026-08-31
---

# T-0025: Stop documentation from claiming that built things do not exist

## Problem / Context

Four consecutive tickets have shipped documentation asserting that their own deliverable does not
exist. It is the single most repeated defect in this repository's history, and it has been found by
a *different* mechanism each time — twice by acceptance, once by a reviewer, once by a sweep:

| When | Where | Claim |
|---|---|---|
| [T-0004](T-0004-create-and-list-projects.md) | `README.md`, `ARCHITECTURE.md` | projects endpoint "not built yet" |
| [T-0005](T-0005-create-and-read-issues.md) | same three lines | issues endpoint "not built yet" |
| [T-0006](T-0006-issue-lifecycle-fields.md) | same three lines, third time | lifecycle fields "not built yet" |
| T-0006 acceptance (G3) | `spec/README.md:9` | **the specification "does not exist yet"** — falsified by [T-0002](T-0002-contract-first-codegen-pipeline.md), months of commits ago |

The recurrence on the same three lines was **forecast in writing** in `eb1432a` before it happened
the third time. T-0006 applied the subtractive fix — deleting the enumerations in `README.md` and
`ARCHITECTURE.md`, pointing at [`BACKLOG.md`](../BACKLOG.md) instead — and acceptance then found
`spec/README.md` in a file nobody had thought to look at.

**That is the finding this ticket exists for.** The subtractive fix works on the files someone
remembers to edit. It does nothing about the file nobody remembers, which is precisely the failure
mode: a "not yet" sentence is written once, is true for a while, and is falsified silently by a
ticket whose author has no reason to open that file.

## Desired Outcome

No file in the repository claims that something which exists does not exist, and a mechanism —
not a habit — catches the next such claim when it is added or falsified.

## User / Business Value

Nothing user-visible. The value is in what the documentation is *for*: an agent or engineer
picking up work reads these files to decide what to build. Four times, that reader would have been
told to build something that already existed. The two acceptance failures this defect caused cost
more than the mechanism will.

## Scope

### In Scope

- **The sweep.** Every file in the repository — not just the ones previously caught — checked for
  claims that something is absent, planned, "not yet", "does not exist", "will be", "coming".
  `spec/README.md:9` is one known instance; the point of the ticket is that the *set* is unknown.
- **The mechanism**, which is retro candidate (c) from `eb1432a`: a rule in
  `tools/validate-project-os/validate.py` (or a sibling check, if that validator's scope should stay
  process-state-only — an open question below). It must fail on a **new** occurrence, not merely on
  today's known ones.
- Deciding what the mechanism actually keys on. The obvious candidates are not equivalent and the
  choice is the substance of this ticket — see Technical Notes.

### Out of Scope

- Rewriting documentation for style, structure, or completeness. This is about statements that are
  **false**, not statements that are thin.
- The `README.md` / `ARCHITECTURE.md` enumerations already removed by T-0006. They are done; this
  ticket must not re-litigate that fix, only ensure it was not mistaken for a complete one.
- Any change to `BACKLOG.md`'s role as the single place where "not built yet" is stated.

## Acceptance Criteria

- [ ] AC1: Given the whole repository, when it is swept, then every claim that an existing thing does not exist is corrected, and the list of files checked is recorded — so a later reader can tell the difference between "clean" and "not looked at".
- [ ] AC2: Given `spec/README.md`, when it is read, then it does not say the specification does not exist.
- [ ] AC3: Given a **newly added** sentence asserting the absence of something that is present, when the check runs, then it fails and names the file and line. Demonstrated by adding one, watching it fail, and removing it — a check verified only against the instances that motivated it has not been shown to catch anything.
- [ ] AC4: Given the check, when it runs against the repository in its corrected state, then it exits 0 — no findings that must be tolerated, since a check with standing exceptions is one people learn to ignore (the `20601` warning in [T-0006](T-0006-issue-lifecycle-fields.md) is this repository's own example).
- [ ] AC5: Given the check, when a legitimate forward-looking statement is written (`BACKLOG.md` saying what is not built; an ADR describing a rejected option; a ticket describing its own unbuilt outcome), then it does not fire. A check that fires on those will be disabled within two tickets.
- [ ] AC6: Given [GIT.md](../../standards/GIT.md)'s gate list, when the check is adopted, then it is added there and to the relevant skills, or an explicit decision is recorded that it runs only in CI — an unwired check is a check that does not run.

## Examples / Scenarios

- **Must catch:** `spec/README.md` — *"the specification does not exist yet"* — while `spec/openapi.yaml` is present and generating clients.
- **Must catch:** a new `README.md` line saying "comments are not implemented" added the same week `POST /issues/{key}/comments` ships ([T-0008](T-0008-comment-on-an-issue.md) is the live risk).
- **Must NOT catch:** `BACKLOG.md` listing T-0007 as not built. That is its job.
- **Must NOT catch:** [ADR-0010](../../architecture/adr/ADR-0010-clean-architecture-layering.md) saying CQRS is "not adopted". A rejected option is a decision, not a stale claim.

## Technical Notes

The mechanism is the hard part and deliberately not decided here. Three shapes, in increasing order
of ambition:

1. **Phrase blacklist outside sanctioned files** — grep for "not yet", "does not exist", "will be
   added" in every path except `BACKLOG.md`, tickets, and ADRs. Cheap, catches all four known
   instances, and its false-positive rate against AC5 is the open question.
2. **Claims must name a ticket.** Any absence claim carries a ticket reference, and the check fails
   when that ticket is `done`. Precise, and it makes the claim self-invalidating — but it requires
   editing every existing claim, and it only works for claims someone remembers to annotate.
3. **Assert against reality** — e.g. a claim that an endpoint does not exist is checked against
   `spec/openapi.yaml`. Strongest and narrowest; it would have caught `spec/README.md` directly.

(1) and (3) are not exclusive and (3) is a small addition once (1) exists. Sizing should assume the
sweep is fast and the check's *false-positive tuning* is the real work.

**A caution from this repository's own record:** T-0006 fixed three lines and acceptance found a
fourth file. Do not treat the sweep's output as the specification for the check — the check must
catch instances the sweep never saw, which is what AC3 is for.

## Dependencies

None. It can run at any time and does not touch application code.

## Risks / Unknowns

- **False positives are the failure mode**, not misses. A check that fires on `BACKLOG.md`'s
  legitimate "not built" rows gets suppressed, and a suppressed check is worse than none because it
  reads as coverage. AC5 exists for this and should be treated as the hard criterion.
- **Whether `validate.py` is the right home is genuinely open.** It validates process state under
  `project-os/`; this check needs to read `spec/`, `README.md`, and `apps/`. Extending its scope may
  be wrong — a separate `tools/check-stale-claims.sh` alongside `check-drift.sh` may fit the
  existing gate list better. Decide in refinement, and record it.
- **The sweep may find many instances**, in which case the ticket is larger than it looks. Report
  rather than absorb: a sweep that quietly grows into a documentation rewrite has lost the thread.

## Testing Notes

AC3 is the only criterion that proves anything about the mechanism, and it is a mutation in the
sense [TESTING.md](../../standards/TESTING.md) means: introduce the defect, watch the check fail on
it, remove it. Verifying the check only against the four known instances demonstrates that it
matches things already written — which is not the claim being made.

## Relevant ADRs & Documentation

- [`eb1432a`](../../delivery/CURRENT_SPRINT.md) — where candidates (a) and (c) were recorded, and where the third recurrence was forecast before it happened
- [T-0006](T-0006-issue-lifecycle-fields.md) — applied candidate (a); its acceptance found G3, which is why (c) is still live
- [DOCUMENTATION.md](../../standards/DOCUMENTATION.md) — what documentation is obliged to say
- [GIT.md](../../standards/GIT.md) — the merge gate list this check would join (AC6)

## Definition of Ready

- [ ] Meets [DoR](../../governance/DEFINITION_OF_READY.md) — not yet refined. The open mechanism question (Technical Notes) is what refinement must close.

## Definition of Done

- [ ] Meets [DoD](../../governance/DEFINITION_OF_DONE.md) — checked by `complete-ticket`.

---

## Work Log

### 2026-08-31 — Created from T-0006 acceptance finding G3 (claude-sm-9d4e)

- **Did:** Created to hold G3, raised by `claude-qa-2e64` during T-0006's re-acceptance. The acceptor deliberately did **not** attach it to T-0006 — a deferral pointed at a ticket whose scope cannot accept it is worse than no deferral — and left it as retro evidence instead. This gives it a home without widening T-0006.
- **Decided:** scoped as *sweep plus mechanism*, not sweep alone. Three prior fixes were sweeps, and the fourth instance was found in a file none of them touched; a fifth sweep would be the same bet a fourth time.
- **Remaining:** refinement, where the mechanism (Technical Notes 1–3) and its home (`validate.py` vs. a sibling tool) are the questions.
- **Open questions / blockers:** none blocking. Note for the retrospective: this ticket **is** candidate (c), so a retro that adopts (c) should point at this rather than create a second home for it.
- **Test state:** n/a — not started.
