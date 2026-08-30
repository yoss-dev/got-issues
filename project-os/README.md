# Project Foundation — Agent-Driven Delivery Framework

This directory structure is the **shared operating environment for a software delivery team of humans and AI agents**. It contains the team's governance, product knowledge, work items, sprint state, architecture decisions, and executable skills — everything an engineer or agent needs to continue the work tomorrow without access to anyone's chat history.

> Process exists to improve software delivery, not to create ceremony.

## Foundation, and copies of it

This structure lives twice:

- **The foundation repository** (`prj-foundation`) — the team's master template. No product work happens there; it only evolves the framework itself.
- **A copy inside each project** — where the real backlog, sprints, ADRs, and tickets live. Once copied, the project's copy is authoritative for that project and evolves independently via [`evolve-governance`](skills/evolve-governance/SKILL.md); generic improvements are contributed back per [`FOUNDATION.md`](FOUNDATION.md).

Every product built with this framework is a **monorepo** ([ADR-0002](architecture/adr/ADR-0002-monorepo-with-self-contained-project-os.md)): the repository root holds the source scaffold (`apps/`, `libs/`, `tools/`, `infra/`) plus this framework, self-contained under `project-os/`. Delivery-process artifacts never leave `project-os/`; source code never enters it.

**Adopting for a new project:**

1. Create the new project repository from the entire foundation repo (template-repo feature, clone-and-reinit, or full copy). The root scaffold and `project-os/` arrive together; all framework links are relative, so nothing needs rewiring. For an *existing* codebase, copy `project-os/`, the root `CLAUDE.md`, and the `.claude/skills` symlink in, and fold the root README's layout conventions into the existing structure deliberately.
2. Fill in the adoption table in [`FOUNDATION.md`](FOUNDATION.md) (source, version, date).
3. Run the [`bootstrap-project`](skills/bootstrap-project/SKILL.md) skill to populate `PROJECT.md`, the product docs, and the standards — it also tailors the monorepo skeleton (pruning `libs/` or `infra/` if unneeded) and the project-specific section of [`standards/GIT.md`](standards/GIT.md).
4. Delete nothing else — empty artifacts (backlog, ideas, sprint) are the correct starting state, and the seed ADRs ([ADR-0001](architecture/adr/ADR-0001-record-architecture-decisions.md), [ADR-0002](architecture/adr/ADR-0002-monorepo-with-self-contained-project-os.md)) apply to every project.

## How to use this repository

**If you are a human starting a new project:** run (or ask an agent to run) the [`bootstrap-project`](skills/bootstrap-project/SKILL.md) skill. It interviews you about the product, technology, and engineering practices, and populates [`PROJECT.md`](PROJECT.md) and the standards.

**If you are an AI agent:** do not read the whole repository. Instead:

1. Read this file and [`PROJECT.md`](PROJECT.md).
2. Identify the activity you were asked to perform and open the matching skill in [`skills/`](skills/README.md).
3. Load only the context the skill lists, adopt the persona it names, and follow its procedure.
4. Persist every decision, result, and open question in repository files before you stop. Chat history is not a project record.

## Map

| Area | Path | What lives there |
| --- | --- | --- |
| Project facts & constraints | [`PROJECT.md`](PROJECT.md) | The single source of truth for what this project is and how it is built |
| Framework lineage | [`FOUNDATION.md`](FOUNDATION.md) | Where this copy came from, local divergences, contributing improvements upstream |
| Help & tutorials | [`docs/`](docs/README.md) | Mental model, task tutorials, cheatsheet, the human's guide, troubleshooting |
| Governance | [`governance/`](governance/WAY_OF_WORKING.md) | Way of Working, team personas, Definition of Ready/Done, glossary |
| Product | [`product/`](product/BACKLOG.md) | Vision, user personas, ideas, backlog index, ticket files |
| Delivery state | [`delivery/`](delivery/CURRENT_SPRINT.md) | Current sprint, archived sprints, retrospectives |
| Architecture | [`architecture/`](architecture/ARCHITECTURE.md) | System overview and Architecture Decision Records |
| Standards | [`standards/`](standards/ENGINEERING.md) | Engineering, testing, security, documentation standards |
| Templates | [`templates/`](templates/TICKET_TEMPLATE.md) | Canonical formats for tickets, ADRs, sprints, ideas, retrospectives |
| Skills | [`skills/`](skills/README.md) | Step-by-step executable procedures for every delivery activity |

## Precedence of instructions

When documents conflict, higher wins. Never resolve a conflict by silently following the more convenient rule — see [`governance/WAY_OF_WORKING.md`](governance/WAY_OF_WORKING.md) for the full rule and conflict-handling procedure.

1. `governance/WAY_OF_WORKING.md`
2. `PROJECT.md` (constraints and configuration)
3. Accepted ADRs in `architecture/adr/`
4. `governance/DEFINITION_OF_READY.md` / `governance/DEFINITION_OF_DONE.md`
5. Sprint goal and commitments in `delivery/CURRENT_SPRINT.md`
6. The ticket's requirements and acceptance criteria
7. `standards/`
8. Skill instructions in `skills/`
9. Agent judgment

## The work lifecycle at a glance

```text
Idea (product/IDEAS.md)
  → capture-idea → create-ticket → backlog
  → refine-ticket → ready
  → plan-sprint → committed (delivery/CURRENT_SPRINT.md)
  → pick-up-ticket → in-progress
  → implement-ticket → in-acceptance
  → acceptance-test → complete-ticket → done
  → retrospective → process improvements
```

The full state machine and all alternate flows (blocked work, urgent bugs, spikes, discovered work) are defined in the Way of Working.
