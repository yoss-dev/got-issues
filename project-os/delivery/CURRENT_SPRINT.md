# SPRINT-003

## Goal

**Got Issues becomes a usable issue tracker: a person can create a project, file issues in it, and move them through their lifecycle — all through the API.**

This is the MVP the Product Owner asked for, and it is the first sprint whose outcome is
*product* rather than foundation. Everything shipped so far answered *can we run this?*;
this answers *is it worth running?* As the mid-sprint tiebreaker: work that gets a real
resource through the contract-first pipeline serves the goal, and work that hardens what
already exists does not — this sprint.

The word doing the work is **lifecycle**. Creating and reading issues would make this a
system that records work; changing their state is what makes it one that tracks it
([T-0006](../product/tickets/T-0006-issue-lifecycle-fields.md)'s own framing, and the reason
it is committed rather than held back).

## Dates

**Continuous flow — no fixed end date.** The sprint closes when the goal is met, as in
SPRINT-001 and SPRINT-002. Throughput history is now two data points — **4 tickets, then 3** —
and this commitment of 3 sits at the lower one deliberately, because of the shape change
described in Notes.

## Committed Work

**Sequenced, not parallel — the first sprint where that is true.** Each ticket depends on the
one above it, so the order in this table is the order of work, not a ranking.

| Ticket | Title | Status | Owner | Blocked by |
| --- | --- | --- | --- | --- |
| [T-0004](../product/tickets/T-0004-create-and-list-projects.md) | Create and list projects | done | none | — |
| [T-0005](../product/tickets/T-0005-create-and-read-issues.md) | Create and read issues within a project | in-progress | claude-sm-9d4e | — (T-0004 done) |
| [T-0006](../product/tickets/T-0006-issue-lifecycle-fields.md) | Track an issue's lifecycle — type, status, priority, assignee | committed | none | T-0005 (in this sprint) |

## Blockers & Escalations

*(none)*

## Discovered / Unplanned Work

*(none)*

## Notes

**Goal and scope confirmed by the human Product Owner (2026-08-31)**, who asked for an MVP as
soon as possible and chose this scope over a thinner two-ticket version and two larger ones.

**The shape of this sprint is different, and it is the main risk.** SPRINT-002's plan said of
its commitment: *"All three are independent: none blocks another, and all three can start
immediately."* The opposite is true here. T-0004 → T-0005 → T-0006 is a strict chain, and
solo mode runs one ticket at a time ([GIT.md](../standards/GIT.md)), so there is no
parallelism to spend and no way to absorb a stalled ticket by starting another. A review round
that bounces does not cost one ticket's slack; it delays everything behind it.

**T-0004 carries most of the risk, and not because it is the largest.** It is the first *real*
resource to travel the contract-first pipeline — [ADR-0004](../architecture/adr/ADR-0004-contract-first-openapi-code-generation.md)
has so far been exercised only by a deliberately disposable placeholder. Whatever T-0004
discovers about writing a genuine resource in `spec/openapi.yaml` and implementing the
generated interface, it discovers once and T-0005 and T-0006 inherit. That is also the reason
to expect the second and third tickets to be cheaper than the trailing average suggests:
SPRINT-002's cost was concentrated in three novel problems, while this chain repeats one
pattern three times.

**Two expectations recorded before the demo rather than after** (raised with the PO, 2026-08-31):

- **There is no UI, by confirmed decision** (`PROJECT.md` §3 — the API is the deliverable). The
  MVP is demonstrated with the generated client, `curl`, or the specification in a viewer.
- **`assignee` will read as a client id, not a person.** No token this system issues carries a
  `sub` ([ADR-0007](../architecture/adr/ADR-0007-test-only-extension-grant-for-user-tokens.md)),
  so T-0006's assignment works and points at `gotissues-member-client`. Fixing that is
  [T-0018](../product/tickets/T-0018-user-subject-tokens.md), deliberately **not** committed
  here: it would add a fourth ticket to a chain already at capacity, and the MVP is legible
  without it. It is the first thing to consider for SPRINT-004.

**Not committed, deliberately.** [T-0007](../product/tickets/T-0007-list-and-filter-issues.md)
(list and filter) is `ready` and is arguably what a demo *shows* — "here are the open issues".
It was left out because it makes the chain four deep with no slack. If the first two tickets
land faster than the trailing average suggests, it is the obvious candidate to pull in under
[WoW](../governance/WAY_OF_WORKING.md) §7 — and the honest reason to consider it is that
without it, the MVP can create and change issues but can only read them one at a time.

Nine other tickets are `ready` and none is committed: T-0008, T-0012, T-0013, T-0014, T-0016,
T-0017, T-0018, T-0019 and T-0007. That is a healthy buffer, and the reason none of them is
here is that they do not serve this goal.

### Remote configured — and what it deliberately does not change (2026-08-31)

**Approved by the maintainer**, who supplied the remote and chose "remote as backup; workflow
unchanged" over PR-based review and over full multi-agent mode. Applied via `evolve-governance`;
to be folded into RETRO-SPRINT-003.

`https://github.com/yoss-dev/got-issues.git` is configured and `main` tracks `origin/main`; 117
commits pushed. Eight `[confirmed]` statements across [GIT.md](../standards/GIT.md),
[PROJECT.md](../PROJECT.md) §5–6 and [FOUNDATION.md](../FOUNDATION.md) asserted that no remote
existed and are corrected.

**The distinction worth keeping**, because getting it backwards is the dangerous direction:
**a remote alone does not end solo mode.** Push-based collision detection is a *workflow* — the
claim commit is pushed and a rejected push means the claim was lost — and no skill here performs
it. So the repository is still safe for **one agent at a time**, and GIT.md now says that in the
general section rather than leaving it to be inferred from "no remote".

Pull requests are available and deliberately unused: review stays an independent session against
the branch diff, merges stay local. `PROJECT.md` Q6 is narrowed rather than answered — a remote
exists; whether to add a pipeline is still open.

*Noticed independently by `claude-qa-8f52` during T-0005's acceptance, which flagged the `origin`
URL against `PROJECT.md`'s claim and declined to edit the trunk mid-run — the right call.*

### Sequencing decision — T-0006 before the layering refactor (2026-08-31)

The maintainer adopted Clean Architecture layering mid-sprint
([ADR-0010](../architecture/adr/ADR-0010-clean-architecture-layering.md), superseding ADR-0009)
and created [T-0022](../product/tickets/T-0022-adopt-clean-architecture-layering.md) for it. Asked
whether that should precede [T-0006](../product/tickets/T-0006-issue-lifecycle-fields.md), the
MVP's last committed ticket, they chose **T-0006 first**.

The sprint's committed work is unchanged: T-0022 is not added to this sprint, and the goal still
closes when T-0006 is done. T-0006 is written in the current shape on purpose — a half-migrated
third shape would be worse for T-0022 than either endpoint. The cost accepted is that lifecycle
code is written once and moved once.

### Governance change — mutation narrowed, exploration strengthened (2026-08-31)

**Approved by the maintainer (human) on 2026-08-31**, raised by them mid-sprint: *"our current
mutation approach to testing is wasting a lot of time and resources"*. Applied via
[`evolve-governance`](../skills/evolve-governance/SKILL.md) in one commit. Owning personas: QA/Test
Engineer for [TESTING.md](../standards/TESTING.md), Scrum Master for the three skills. To be
folded into RETRO-SPRINT-003.

**The evidence, counted rather than argued.** ~80 recorded mutants across seven tickets (T-0009
alone: 33; T-0015: 19), **nine explicitly recorded as invalid**, and blocking review findings on
both T-0004 and T-0005 that were about *mutation records* rather than about defects. Meanwhile the
sprint's two most serious defects — an undeclared 500 with an empty body, and a migration that
would have made every existing project's first issue unreadable — were found by **driving real
infrastructure into a state the code was not built for**, and neither came from mutation.

| Change | Artifact |
| --- | --- |
| Mutation required only where a test is the sole evidence for a load-bearing claim, or where a reviewer challenges one; exempt where a compiler, constraint or framework enforces it; one mutant per claim; no re-mutation of unchanged claims; cheapest tier | [TESTING.md](../standards/TESTING.md), [implement-ticket](../skills/implement-ticket/SKILL.md) |
| New: *Exercise the system in a state it was not built in* — unanticipated input, **a database that already holds rows**, a dependency removed underneath a live service | [TESTING.md](../standards/TESTING.md) |
| Acceptance's existing adversarial step sharpened with those three techniques and the evidence for them | [acceptance-test](../skills/acceptance-test/SKILL.md) |
| Reviewers may demand a mutant when they doubt a claim — and may not demand re-mutation of a claim whose code has not changed | [review-code](../skills/review-code/SKILL.md) |

**What is deliberately unchanged:** the principle that a test is not shown to guard a behaviour
until seen to fail, and the rule that a mutant only counts if it reaches the assertion. The
narrowing is about when a demonstration is *required*, not about what counts as one.

**Precondition checked:** T-0005 is the only in-flight ticket; its mutation evidence is complete
and valid, and nothing here lets it newly pass a gate.

**How we would notice this was wrong:** a coverage claim reaching acceptance unchallenged and
proving false. That is the SPRINT-001 failure the original mandate was built for, and it is the
signal that this narrowing went too far.

**Process changes now in force** from [RETRO-SPRINT-002](retrospectives/RETRO-SPRINT-002.md),
applying to every ticket in this sprint: a mutant only counts if the build accepts it; a
mutation record must state what its mutant proves; the testing standards bind the test
infrastructure too, including running gates in the working copy under test; and `review-code`
now asks what input actually reaches the code under test.

---

### Retro input, recorded 2026-08-31 by `claude-rev-5c14` (T-0005 review, F1)

**Two acceptance runs have now failed a ticket on the same three lines.** `README.md:7`,
`README.md:113` and `ARCHITECTURE.md:5` enumerate what exists and what does not.
[T-0004](../product/tickets/T-0004-create-and-list-projects.md)'s acceptance caught them stale and
they were fixed by rewriting the sentences to describe projects; [T-0005](../product/tickets/T-0005-create-and-read-issues.md)'s
acceptance caught the same lines stale again, with T-0005's own deliverable listed under
*Not here yet*. Recorded here rather than left to the retro because **T-0006 lands before the
retro and will falsify the same lines a third time.**

**The countermeasure already in place has already failed.** `ARCHITECTURE.md` line 7 reads
*"Updating this banner is part of any ticket that changes the state above. It has repeatedly been
left stale by the very ticket that falsified it."* That reminder was written **because** of the
first occurrence, and the second occurrence happened anyway. A stronger reminder is the same idea
louder, and the evidence says the idea does not work.

**The proposed durable fix does not survive inspection either.** T-0005's Work Log suggests the
banner should name *what is not yet built* by ticket rather than enumerating what is. That is
still a hand-maintained enumeration — when T-0006 ships, "not yet built: T-0006" becomes false in
exactly the same way. Whichever side is enumerated, a human has to remember.

**Three candidates that actually remove the failure mode**, for the retro to choose between:

| | Approach | Cost |
| --- | --- | --- |
| a | **Delete the enumerations**; point at [`BACKLOG.md`](../product/BACKLOG.md), which is already authoritative and already updated by `complete-ticket` at every handover | Lowest — removes machinery rather than adding it |
| b | **Generate** the lists from ticket frontmatter, which `validate.py` already parses | Moderate; keeps prose, adds a build step |
| c | **Make `validate.py` fail** when a ticket whose `status` is `done` is referenced under a *Not here yet* / *remains intended* heading | Small; encodes the exact defect, which has now occurred twice, and keeps the prose humans read |

(a) is the cheapest and is the only one that cannot go stale at all; (c) is the most in keeping
with this project, since the validator already enforces cross-file consistency. Not a
recommendation between them — that is the retro's call, and the point of recording it now is that
the next ticket should not discover this a third time by acceptance failure.
