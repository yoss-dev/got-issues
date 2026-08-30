# Cheatsheet

One page of the things you look up mid-task. Authoritative sources: [WoW](../governance/WAY_OF_WORKING.md), [GIT.md](../standards/GIT.md), the [skills](../skills/README.md).

## Ticket states & who moves them

| Transition | Via skill | Persona |
| --- | --- | --- |
| *(idea)* → `backlog` | `capture-idea` → `create-ticket` | PO |
| `backlog` → `ready` | `refine-ticket` (DoR gate; checkbox ticked) | BA + 4 perspectives |
| `ready` → `committed` | `plan-sprint` | PO |
| `committed` → `in-progress` | `pick-up-ticket` (atomic claim) | ENG |
| `in-progress` ↔ `blocked` | blocker recorded in ticket + sprint | owner |
| `in-progress` → `in-acceptance` | `implement-ticket` handover (after reviewed merge) | ENG |
| `in-acceptance` → `in-progress` | acceptance FAIL, defects recorded | QA |
| `in-acceptance` → `done` | `acceptance-test` PASS + `complete-ticket` (DoD gate) | QA (≠ implementer) |
| any pre-sprint → `dropped` | deliberate, reason recorded | PO |

Owner is set **only** on `in-progress`/`blocked`. `implemented_by` set at handover; `accepted_by` at done; they must differ.

## Git in six lines

```bash
git pull && …edit claim… && git commit -am "os: T-0031 claimed by <id>" && git push   # claim (trunk; rejected push = lost race)
git worktree add ../<repo>--t-0031-<slug> t-0031-<slug>                               # code lives here; primary checkout stays on main
git commit -m "T-0031: <imperative summary>"                                          # source commits, on the branch
# handover: Work Log → PR → /review-code (other session) → squash-merge → os: status commit on trunk
git worktree remove ../<repo>--t-0031-<slug> && git branch -d t-0031-<slug>           # cleanup after merge
python3 tools/validate-project-os/validate.py                                          # before any process push
```

**Lane rule:** only-`project-os/` changes → straight to trunk (`os:` message). Source → ticket branch + reviewed PR. Exception up: governance/standards/templates/skills changes go via reviewed PR. Exception down: Work Log edits may ride the ticket branch.

## Message & identity formats

- Source: `T-0031: add date-range validation`
- Process: `os: T-0031 claimed by claude-eng-4f2a` · `os: plan SPRINT-004` · `os: ADR-0009 proposed` · `os: T-0031 stale claim released`
- Identity: humans use their handle; agents mint `<model>-<persona>-<suffix>` per session (`claude-eng-4f2a`) and keep it everywhere (owner, Work Log, commits, verdicts).

## The hard rules (blocking, not advisory)

1. No work without a claim; no claim without a push.
2. No unplanned work outside the sprint table (discovered work: ticket + sprint entry + table row).
3. Implementers never change acceptance criteria; QA never rewrites them to pass an implementation.
4. Nothing reaches `done` without independent acceptance (`accepted_by ≠ implemented_by`).
5. Failing tests = not done. A deviation needs a recorded human/PO decision.
6. Architecture decisions meeting [the bar](../architecture/adr/README.md) get an ADR *before* you build on them.
7. Governance never changes to rescue a failing ticket; changes go through `evolve-governance` + review.
8. Escalation answers count only when written into the repo.
9. Stale claim (24 h silent): release per WoW §7, don't claim over it.

## Escalation format

Issue → why the agent can't safely decide → options → tradeoffs → recommended default. Into the ticket Work Log + sprint Blockers. Not for routine engineering choices.
