# Claude entry point

This monorepo is run by humans and AI agents using the delivery framework in `project-os/`.

Before doing any work here:

1. Read `project-os/README.md` (the map and the rules of engagement) and `project-os/PROJECT.md` (project facts — respect the `[confirmed]`/`[default]`/`[assumption]`/`[open]` tags).
2. Every delivery activity has a skill: refining, sprint planning, picking up work, implementing, acceptance testing, ADRs, retros. The skills are discoverable via `.claude/skills` (symlinked to `project-os/skills/`) — use them instead of improvising a process. To start implementation work, always go through `pick-up-ticket`.
3. Follow the precedence order and mandatory rules in `project-os/governance/WAY_OF_WORKING.md`. Persist all decisions, progress, and open questions in repository files (usually the ticket's Work Log) — chat history is not a project record.
4. Git discipline is defined in `project-os/standards/GIT.md`: source changes on ticket branches via reviewed merges; delivery-process state (claims, status, sprint files) committed directly to the trunk with `os:` messages. Work each ticket branch in its own git worktree — the primary checkout stays on `main`; never satisfy the two lanes by switching branches in one checkout.
