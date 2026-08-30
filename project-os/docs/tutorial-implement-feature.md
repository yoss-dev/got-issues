# Tutorial: Implementing a Feature

Goal: take Ready work through sprint planning, implementation, review, and independent acceptance to Done — with every gate exercised. This is the delivery half of the lifecycle. Cast: `claude-eng-4f2a` (implementer session), `claude-eng-9c1b` (reviewer session), `claude-qa-b81d` (acceptance session), `pat` (human).

## 1. Plan the sprint

`/plan-sprint` (PO persona). One goal — *"an employee can create and use a go-link end to end"* — then Ready tickets admitted top-down: T-0001 (create + redirect) and T-0002 (list existing links, sequenced after). `CURRENT_SPRINT.md` becomes SPRINT-001 with the Committed Work table; each admitted ticket flips to `status: committed`. Work that isn't Ready doesn't get in, no matter who asks — it gets refined instead.

## 2. Claim the ticket — atomically

A fresh session runs `/pick-up-ticket`. The claim is the framework's concurrency lock, so the mechanics matter:

```bash
git pull                                  # never claim from a stale snapshot
# edit: ticket owner: claude-eng-4f2a, status: in-progress + sprint table row
git commit -am "os: T-0001 claimed by claude-eng-4f2a"
git push                                  # immediately — an unpushed claim locks nothing
```

**If the push is rejected, you lost the race** — someone claimed it first. Pull, pick another ticket. That's the system working, not an error.

Before any code: the agent reads the ticket, its ADRs, the standards, and writes an **implementation plan into the Work Log** — approach, files to touch, an acceptance-criterion→test mapping, risks. The test of a good plan: a different agent could implement from it.

## 3. Implement in a worktree, in two lanes

```bash
git worktree add ../golinks--t-0001-create-short-link t-0001-create-short-link
```

Source work happens in that worktree on the ticket branch (`T-0001: add redirect handler` commits); the primary checkout stays on `main` for anything process-shaped ([GIT.md](../standards/GIT.md)). During the work:

- **Scope discipline:** build In Scope, satisfy every criterion, touch nothing Out of Scope. A tempting extra ("let's also track click counts!") becomes a ticket, not a commit.
- **Ambiguity or architecture:** stop that thread — record the question, escalate or `/create-adr`. *Inventing requirements is forbidden.* When the agent hits the redirect-storage question (SQLite table vs. flat file survives the VM's disk snapshots differently), that meets the ADR bar → ADR-0003, Accepted, linked from the ticket.
- **Blocked?** Push the WIP branch, flip `status: blocked` on the trunk with the blocker recorded in ticket + sprint, pick other work. Never idle silently, never quietly cut scope.
- **Work Log stays resumable** — updated at decisions and session ends, riding the branch with the code it describes.

## 4. Verify, then hand over in the fixed sequence

Before handover the engineer self-checks every criterion against the *running* code, gets the suite green (command + output recorded), lints clean, self-reviews the diff. Then, in order:

1. Final Work Log entry (built, decided, test evidence, branch/PR reference) — on the branch.
2. Open the PR.
3. **Independent review** — a *different* session runs [`/review-code`](../skills/review-code/SKILL.md): scope fidelity first, then correctness, tests-that-can-actually-fail, standards/ADR compliance. Verdict in the Work Log: Approve, or Request-changes with blocking findings. Reviewers don't fix code — that would void their independence.
4. Merge (squash, `T-0001: employee-created go-links with redirect`), delete branch + worktree.
5. On the trunk: `status: in-acceptance`, `implemented_by: claude-eng-4f2a`, owner cleared — `os: T-0001 in-acceptance`.

## 5. Independent acceptance

A **fresh session** (`claude-qa-b81d` — the validator enforces `accepted_by ≠ implemented_by`) runs `/acceptance-test`:

- Reads the ticket's *requirements before the Work Log*, deriving its own checks so the implementer's narrative can't anchor it.
- Runs the suite itself, exercises the real service, probes adversarially: `go/../etc`, a 500-char name, claiming an existing name.
- Suppose the probe finds: claiming a reserved name returns 500, but AC3 says 409. **Verdict: FAIL** — defect recorded (repro, expected-with-criterion-quoted, observed), ticket back to `in-progress`. This is the framework succeeding. The engineer fixes on a fresh branch pass (with a regression test), re-hands-over; second acceptance passes with per-criterion evidence.
- What QA may never do: rewrite a criterion so the implementation passes. A genuinely wrong criterion is a PO decision, recorded, and rare.

## 6. Close it out

`/complete-ticket` (same QA session): walks the [DoD](../governance/DEFINITION_OF_DONE.md) literally — universal items, then applicable conditionals (ADR-0003 Accepted? docs updated? migration tested?). Then one finalize commit: `status: done`, `accepted_by: claude-qa-b81d`, DoD checkbox ticked, sprint row done, backlog row moved to Completed, dependents unblocked. Checkpoint:

```bash
python3 tools/validate-project-os/validate.py   # OK — every gate left a trace
git log --oneline | head                        # readable delivery journal:
# os: T-0001 done - accepted by claude-qa-b81d
# T-0001: employee-created go-links with redirect
# os: ADR-0003 accepted
# os: T-0001 claimed by claude-eng-4f2a
# os: plan SPRINT-001
```

When the sprint goal is met (or dates elapse): [`/retrospective`](../skills/retrospective/SKILL.md) — archive, mine the evidence, produce owned improvement actions. That loop is what makes the framework get better instead of just older.
