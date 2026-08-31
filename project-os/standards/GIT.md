# Git Standards

How this repository's history is written. These rules exist because Git is not just storage here — it is the **coordination mechanism between agents** (ticket claiming, handoffs, sprint state) and the audit trail the whole framework promises. Authoritative over the git-related defaults elsewhere; where [ADR-0002](../architecture/adr/ADR-0002-monorepo-with-self-contained-project-os.md) or `PROJECT.md` says otherwise, they win.

## Repository model

- **One monorepo per product** (ADR-0002): all applications, shared libraries, tooling, infrastructure code, and the delivery framework live in a single repository with a single history. Cross-cutting changes are atomic; there is exactly one place to look.
- **The framework is self-contained in `project-os/`.** Delivery-process artifacts (tickets, sprints, ADRs, governance, skills) MUST NOT leak into the source tree, and source code MUST NOT live under `project-os/`. The root layout (`apps/`, `libs/`, `tools/`, `infra/`) is defined in the root README; bootstrap tailors it.

## Branches

- **Trunk:** `main` `[default]`. The trunk is always releasable: green build, no known-broken state. Nobody force-pushes a shared branch, ever; mistakes are fixed by reverting forward.
- **Ticket branches:** all source-code work happens on a short-lived branch named after the ticket: `t-0031-invoice-date-filtering` (lowercase ticket file name, no extension) `[default]`. One ticket per branch, one branch per ticket, deleted after merge. Branches live days, not weeks — a branch outliving its ticket's sprint is a blocker signal.
- No long-lived develop/release branches unless `PROJECT.md` records a reason `[default]`.

## The two commit lanes

This is the load-bearing rule for multi-agent collaboration:

1. **Process-state commits — directly on the trunk.** Any commit that only touches `project-os/` (claims, status changes, sprint planning, refinement, retros, ADR status, backlog order) is committed on `main` and **pushed immediately**. Rationale: ticket claiming and handoffs only prevent duplicate work if every agent sees them the moment they happen; a claim sitting on an unpushed branch locks nothing.
2. **Source changes — via ticket branches and reviewed merges.** Any commit touching `apps/`, `libs/`, `tools/`, `infra/`, or other source paths goes on the ticket's branch and reaches `main` only through a reviewed merge (WoW §10).

Consequences of the lanes:

- **A commit never mixes the two lanes**, with one exception: the ticket's **Work Log** may be updated on the ticket branch alongside the code it describes (it is part of the reviewable change). Frontmatter `status`/`owner` changes are **always** trunk-only — they happen at claim (before the branch exists) and at handover (after the merge), so the same lines never diverge.
- **Governance is not process-state.** Changes to `project-os/governance/`, `project-os/standards/`, `project-os/templates/`, or `project-os/skills/` travel lane 2: a branch and a reviewed PR carrying the approval `evolve-governance` requires. Protect these paths with CODEOWNERS (or the platform's equivalent) requiring human review, so the "no silent governance rewrites" rule is enforced by the platform, not just prose `[default]`. Delivery state (tickets, backlog, sprint, retros, ideas, ADRs) remains lane 1.
- Going `blocked` mid-implementation: push the WIP branch first, then commit the status change on the trunk. A later Work Log merge conflict on that ticket file is possible and acceptable — resolve by keeping both entries in chronological order.
- Keep ticket branches current by rebasing on `main` (or merging `main` in, if the branch is shared); resolve conflicts on the branch, never on the trunk.

## Working copies: use worktrees

The two lanes assume the ability to commit to two branches at once — never satisfy that by switching branches back and forth in one checkout. **Git worktrees are the desired mechanism:**

- The **primary checkout stays on `main`** at all times; every process-lane commit (claims, status changes, sprint files, new tickets, ADRs) is made there.
- **Each ticket branch gets its own worktree**, created at the start of implementation and named after the branch in a sibling directory `[default]`:

  ```bash
  git worktree add ../<repo>--t-0031-invoice-date-filtering t-0031-invoice-date-filtering
  ```

  All source work for the ticket happens inside that worktree; going `blocked` or recording discovered work needs no stashing — the trunk is already checked out next door.
- After the merge, clean up both: `git worktree remove ../<repo>--t-NNNN-…` and delete the branch.
- This also makes **parallel agents on one machine safe** (each ticket has its own working directory) and gives QA a clean trunk checkout for acceptance, untouched by anyone's branch state.
- Agent harnesses with native worktree isolation (e.g., Claude Code's worktree mode) satisfy this convention as-is — the requirement is one working directory per concurrent branch, not the exact commands.

## Remotes and solo mode

- **Normal mode requires a shared remote**, configured during bootstrap: process-lane commits are pushed immediately, and push conflicts are the collision-detection mechanism for claims.
- **No remote configured** (a fresh copy, offline work) = **solo mode**: the two lanes and all conventions still apply, but push-based collision detection is void — the repository is safe for **one agent at a time**. Running a second concurrent agent requires setting up a remote first. Solo mode is recorded in `PROJECT.md` §6 so it is a visible, deliberate state, and skills that say "pull/push" simply skip the network step.
- **A remote alone does not end solo mode.** Collision detection is a *workflow* that uses the remote — the claim commit is pushed, and a rejected push means the claim was lost. A repository with a remote nobody pushes claims to is still one-agent-at-a-time, and saying otherwise would be the more dangerous error of the two. This project is in that state deliberately: see the project-specific section below.
- Without a PR platform, "open the PR" degrades to: push (or keep) the ticket branch, have an independent session run `review-code` against the branch diff, then merge locally — the review verdict in the Work Log is the record.

## Commit messages

- **Source commits:** `T-NNNN: <imperative summary>` — the ticket ID is mandatory; it is how history stays traceable to requirements. Body text explains *why* when the summary can't.
- **Process commits:** `os: <activity>` — e.g., `os: T-0031 claimed by dev-2`, `os: T-0031 in-acceptance`, `os: plan SPRINT-004`, `os: RETRO-SPRINT-004`, `os: ADR-0009 proposed`. Greppable, uniform, and they make the delivery history readable from `git log` alone.
- Never describe process mechanics ("fix review comments", "wip", "changes") in a message that will reach the trunk.
- **Identity convention `[default]`:** every actor uses one stable identifier everywhere it acts — ticket `owner:`, Work Log entry headers, `os:` commit messages, review/acceptance verdicts. Humans use their usual handle (e.g., `yoss`). Agent sessions mint one id at session start and keep it for the whole session: `<model>-<persona>-<short unique suffix>` (e.g., `claude-eng-4f2a`, `claude-qa-b81d`); never reuse another session's id. Implementer/reviewer/acceptor independence (WoW §2, §9, §10) is judged by comparing these identifiers.

## Merging and review

- Merges to `main` require review per WoW §10. Where no independent reviewer (human or second session) is available, the deviation is noted in the ticket's Work Log — silently self-merging is the anti-pattern, not the emergency itself.
- **Squash-merge by default** `[default]`: the trunk gets one commit per reviewed change, titled `T-NNNN: <summary>`; the branch's step-by-step history is disposable. Projects preferring merge commits record it in the project-specific section below.
- The handover sequence at the end of implementation is fixed: final Work Log entry on the branch → PR → review → merge → *then* the `os:` status commit on the trunk (see `implement-ticket`).

## Hygiene

- Nothing generated gets committed: build outputs, dependency directories, caches — extend the root `.gitignore` per stack during bootstrap.
- No secrets in the repository or its history — see [SECURITY.md](SECURITY.md); a leaked secret means rotate first.
- Large binary assets are avoided; where unavoidable, use Git LFS `[default]`.
- Release tags: `vX.Y.Z` for single-deliverable projects; `<app>/vX.Y.Z` per app in multi-app monorepos `[default]`.
- **State validation:** run `python3 tools/validate-project-os/validate.py` before pushing a process-lane commit; CI runs it on every merge to the trunk `[default]`. It checks ticket/backlog/sprint/ADR consistency and link integrity — a red validator is a defect in the process state, fixed before proceeding.

## Project-specific rules

Set at bootstrap 2026-08-30.

- **Remote: `https://github.com/yoss-dev/got-issues.git`, configured 2026-08-31** `[confirmed]`. **PR tooling: not used** — the maintainer's decision on the same day was that the remote serves as a backup and publication point, and the workflow is otherwise unchanged. Commits are pushed to `origin/main`; review and merges stay local (below). **The one-agent-at-a-time constraint still holds**, because nothing in the workflow yet uses push conflicts to detect a claim collision — that would require the claim commit to be pushed and its rejection acted on, which no skill does. Enabling it is a decision, not a consequence of the remote existing.
- **Review without pull requests** `[confirmed]` — a remote now exists and PRs are deliberately not used: keep the ticket branch, have an **independent session** run [`review-code`](../skills/review-code/SKILL.md) against the branch diff, record the verdict in the ticket's Work Log, then merge locally. The Work Log verdict *is* the review record. Self-merging without that recorded verdict is the anti-pattern — where genuinely unavoidable, the deviation is stated in the Work Log.
- **Trunk protection rules:** none configured on the remote `[confirmed]` — a remote exists, but no branch protection is set on it, so nothing external enforces the gates. The trunk is protected by discipline: `main` stays releasable, no force-push ever, and the gates below are run locally before every merge.
- **Governance path protection:** no CODEOWNERS configured `[confirmed]`, though the remote could now carry one. Changes to `project-os/{governance,standards,templates,skills}` still travel **lane 2** (branch + reviewed merge) and still require the approval [`evolve-governance`](../skills/evolve-governance/SKILL.md) specifies — human approval, recorded in the change. Revisit when a remote is added (`PROJECT.md` Q6).
- **Merge strategy:** squash-merge `[default]` — one trunk commit per reviewed change, titled `T-NNNN: <summary>`.
- **Release & tagging scheme:** `vX.Y.Z` on the trunk `[default]`. Nothing is released until a deployment target exists, so no tag is expected during the first slice.
- **Gates before every merge to `main`** (run locally; there is no CI to catch a miss) `[default]`:

  ```bash
  python3 tools/validate-project-os/validate.py   # framework state consistency
  dotnet build                                     # warning-clean
  dotnet test                                      # unit + integration (Docker running)
  ./tools/generate.sh && git diff --exit-code       # OpenAPI codegen drift check
  ```

  The drift check is a merge gate, not a nicety: it is what makes the contract-first rule real ([ADR-0004](../architecture/adr/ADR-0004-contract-first-openapi-code-generation.md)).
- **Ignored paths:** build outputs (`bin/`, `obj/`), NuGet caches, coverage reports, and `.env` files are never committed; **generated OpenAPI output is committed on purpose** so drift shows up in review (ADR-0004) `[confirmed]`.
- **Actor identity:** the maintainer commits as `yoss`; agent sessions mint `<model>-<persona>-<suffix>` ids per the identity convention above `[default]`.
