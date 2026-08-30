# <Project Name>

> ⚠ **Template root README** — [`bootstrap-project`](project-os/skills/bootstrap-project/SKILL.md) replaces the placeholders with the product's name, description, and setup instructions. This file belongs to the **product**; the delivery framework documents itself in [`project-os/`](project-os/README.md).

*One-paragraph product description — what this is, for whom. See [`project-os/PROJECT.md`](project-os/PROJECT.md) for the full project facts.*

## Repository layout

This is a **monorepo** ([ADR-0002](project-os/architecture/adr/ADR-0002-monorepo-with-self-contained-project-os.md)). Everything the product needs lives here, in one history:

| Directory | Contents |
| --- | --- |
| [`apps/`](apps/README.md) | Deployable applications and services |
| [`libs/`](libs/README.md) | Shared libraries and packages consumed by apps |
| [`tools/`](tools/README.md) | Developer tooling, scripts, generators |
| [`infra/`](infra/README.md) | Infrastructure as code, deployment configuration |
| [`project-os/`](project-os/README.md) | The delivery operating system: governance, backlog, sprints, ADRs, standards, agent skills |

Two rules keep the boundary clean: **delivery-process artifacts live only in `project-os/`** (no tickets, boards, or process docs scattered through the source tree), and **source code never lives in `project-os/`**. Git conventions, including the trunk/branch commit lanes, are in [`project-os/standards/GIT.md`](project-os/standards/GIT.md).

## Getting started

*Filled during bootstrap: prerequisites, how to build, run, and test — literally followable from a fresh clone (see [documentation standards](project-os/standards/DOCUMENTATION.md)).*

## How this project is run

Humans and AI agents deliver this project together using the framework in [`project-os/`](project-os/README.md) — start there. New to the framework? The tutorials and cheatsheet live in [`project-os/docs/`](project-os/docs/README.md). Agents: your entry point is [`CLAUDE.md`](CLAUDE.md) / `project-os/README.md`, and every delivery activity (refining, planning, implementing, accepting) has a skill in [`project-os/skills/`](project-os/skills/README.md).
