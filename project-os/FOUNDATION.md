# Foundation Lineage

This directory structure originates from the team's **foundation repository** (`prj-foundation`). The foundation is the template; each project carries its own copy and evolves it via [`evolve-governance`](skills/evolve-governance/SKILL.md).

## In a project copy — fill this in when adopting

| Field | Value |
| --- | --- |
| Copied from | *link/path to the foundation repository* |
| Foundation version | *e.g., 1.0.0* |
| Copied on | *YYYY-MM-DD* |
| Local divergences | *none yet — list governance changes that deliberately differ from the foundation* |

## Contributing back

When a retrospective or `evolve-governance` change fixes something **project-agnostic** — an ambiguous rule, a better template, an improved skill procedure — propose the same change to the foundation repository so future projects start better. Record proposed upstream contributions in the table below; project-specific changes (anything referencing this project's product, stack, or constraints) stay local and are listed under *Local divergences* instead.

| Date | Change | Upstreamed? |
| --- | --- | --- |

## Foundation changelog

*Maintained in the foundation repository only; project copies keep the version they copied and consult the foundation for updates. Adopting a newer foundation version into a running project is itself an `evolve-governance` change (diff, assess impact on in-flight work, apply deliberately — never blind-overwrite instance state like tickets, sprints, or ADRs).*

- **1.5.0** — 2026-08-30 — Help documentation (`project-os/docs/`): mental-model index, three tutorials on a shared running example (bootstrap, idea→Ready, implement-feature), one-page cheatsheet, the human's-role guide, troubleshooting; wired into both READMEs.
- **1.4.0** — 2026-08-30 — Full gate closure: validator now enforces sprint membership (sprint-only statuses require an active sprint + table row), DoR/DoD checkbox evidence, acceptance-criteria presence, and implementer≠acceptor independence via new `implemented_by`/`accepted_by` ticket frontmatter; governance paths reclassified to lane 2 (reviewed PRs + CODEOWNERS prescription); discovered work must join the Committed Work table.
- **1.3.0** — 2026-08-30 — Enforcement & recovery: state-consistency validator (`tools/validate-project-os/`, wired into GIT.md and skill execution); stale-claim release protocol (WoW §7); remote/solo-mode rules (GIT.md); `review-code` skill (13th skill — independent pre-merge review); actor identity convention (GIT.md); escalation answers must be recorded in-repo (WoW §13); Work Log gains a Branch/PR line.
- **1.2.0** — 2026-08-30 — Worktree convention: one git worktree per ticket branch, primary checkout pinned to the trunk for process-lane commits (`standards/GIT.md` Working copies; wired into `pick-up-ticket`, `implement-ticket`, `CLAUDE.md`).
- **1.1.0** — 2026-08-30 — Monorepo model baked in (ADR-0002): framework self-contained under `project-os/`; root scaffold (`apps/`, `libs/`, `tools/`, `infra/`, root README, `.gitignore`, `CLAUDE.md`); `.claude/skills` symlink for Claude Code skill discovery; new `standards/GIT.md` (trunk + ticket branches, two commit lanes, `os:`/`T-NNNN:` message conventions).
- **1.0.0** — 2026-08-30 — Initial framework: governance (WoW, personas, DoR, DoD), hybrid backlog (index + ticket files), 8-state ticket model, sprint/retro artifacts, ADR system, 12 skills.
