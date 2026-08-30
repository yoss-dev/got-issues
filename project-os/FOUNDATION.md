# Foundation Lineage

This directory structure originates from the team's **foundation repository** (`prj-foundation`). The foundation is the template; each project carries its own copy and evolves it via [`evolve-governance`](skills/evolve-governance/SKILL.md).

## In a project copy — fill this in when adopting

| Field | Value |
| --- | --- |
| Copied from | `prj-foundation` (team foundation repository; no URL recorded at adoption — `[open]`) |
| Foundation version | 1.7.0 (adopted at 1.5.0; upgraded 2026-08-30) |
| Copied on | 2026-08-30 |
| Local divergences | **Solo mode.** `standards/GIT.md` records no remote, no PR platform, and no CODEOWNERS-based governance-path protection — the foundation assumes a shared remote. Lane rules, review independence, and the pre-merge gates are unchanged; only the platform enforcement is absent, replaced by locally-run gates. Revisit if a remote is added (`PROJECT.md` Q6). Bootstrap also populated the project-specific sections of all four standards and the glossary — expected instance state, not divergence. |

## Contributing back

When a retrospective or `evolve-governance` change fixes something **project-agnostic** — an ambiguous rule, a better template, an improved skill procedure — propose the same change to the foundation repository so future projects start better. Record proposed upstream contributions in the table below; project-specific changes (anything referencing this project's product, stack, or constraints) stay local and are listed under *Local divergences* instead.

| Date | Change | Upstreamed? |
| --- | --- | --- |

## Foundation changelog

*Maintained in the foundation repository only; project copies keep the version they copied and consult the foundation for updates. Adopting a newer foundation version into a running project is itself an `evolve-governance` change (diff, assess impact on in-flight work, apply deliberately — never blind-overwrite instance state like tickets, sprints, or ADRs).*

- **1.7.0** — 2026-08-30 — `run-sprint` orchestration skill (14th): drains the current sprint through the per-activity skills with independent sessions for review/acceptance, parks human-shaped obstacles as recorded escalations and continues, guards against failure loops, exits with a batched decision digest; never plans, never retros, adds no state of its own.
- **1.6.1** — 2026-08-30 — Bootstrap re-run semantics hardened: re-runs are additive (existing `[confirmed]`/`[default]` facts stand; only gaps and missing/new steps execute), scoped by explicit human confirmation; foundation upgrades adding steps are a named re-run trigger.
- **1.6.0** — 2026-08-30 — Bootstrap seeds the pipeline: prescribed-but-unperformed setup work becomes chore/technical tickets, investigable open questions become spikes, interview use cases land as ideas (promoted only on explicit human priority); bootstrap validation requires a green validator on the seeded state. *(Adopted into this copy 2026-08-30 — this project's bootstrap ran under 1.5.0, so the seeding step applies retroactively: see the upgrade note in the report/backlog.)*
- **1.5.0** — 2026-08-30 — Help documentation (`project-os/docs/`): mental-model index, three tutorials on a shared running example (bootstrap, idea→Ready, implement-feature), one-page cheatsheet, the human's-role guide, troubleshooting; wired into both READMEs.
- **1.4.0** — 2026-08-30 — Full gate closure: validator now enforces sprint membership (sprint-only statuses require an active sprint + table row), DoR/DoD checkbox evidence, acceptance-criteria presence, and implementer≠acceptor independence via new `implemented_by`/`accepted_by` ticket frontmatter; governance paths reclassified to lane 2 (reviewed PRs + CODEOWNERS prescription); discovered work must join the Committed Work table.
- **1.3.0** — 2026-08-30 — Enforcement & recovery: state-consistency validator (`tools/validate-project-os/`, wired into GIT.md and skill execution); stale-claim release protocol (WoW §7); remote/solo-mode rules (GIT.md); `review-code` skill (13th skill — independent pre-merge review); actor identity convention (GIT.md); escalation answers must be recorded in-repo (WoW §13); Work Log gains a Branch/PR line.
- **1.2.0** — 2026-08-30 — Worktree convention: one git worktree per ticket branch, primary checkout pinned to the trunk for process-lane commits (`standards/GIT.md` Working copies; wired into `pick-up-ticket`, `implement-ticket`, `CLAUDE.md`).
- **1.1.0** — 2026-08-30 — Monorepo model baked in (ADR-0002): framework self-contained under `project-os/`; root scaffold (`apps/`, `libs/`, `tools/`, `infra/`, root README, `.gitignore`, `CLAUDE.md`); `.claude/skills` symlink for Claude Code skill discovery; new `standards/GIT.md` (trunk + ticket branches, two commit lanes, `os:`/`T-NNNN:` message conventions).
- **1.0.0** — 2026-08-30 — Initial framework: governance (WoW, personas, DoR, DoD), hybrid backlog (index + ticket files), 8-state ticket model, sprint/retro artifacts, ADR system, 12 skills.
