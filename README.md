# Got Issues

A self-hosted, API-first issue and task tracker for the company's own engineering work — Jira-like in shape, deliberately small in surface. There is no UI: the HTTP API is the product, and its [OpenAPI specification](spec/README.md) is written first, with server contracts and clients generated from it. Single-tenant, internal-only.

This is a **proof of concept** — a step toward running the company's development tooling in-house (self-hosted git being the eventual prize) and a test of whether contract-first delivery holds up in practice. Full project facts, constraints, and their confidence levels are in [`project-os/PROJECT.md`](project-os/PROJECT.md).

> **Status: bootstrapped, not yet built.** The stack and the way of working are decided ([ADR-0003](project-os/architecture/adr/ADR-0003-initial-technology-stack.md), [ADR-0004](project-os/architecture/adr/ADR-0004-contract-first-openapi-code-generation.md)); no application code exists yet. The commands below describe the intended shape and become real with the first implementation ticket.

## Repository layout

This is a **monorepo** ([ADR-0002](project-os/architecture/adr/ADR-0002-monorepo-with-self-contained-project-os.md)). Everything the product needs lives here, in one history:

| Directory | Contents |
| --- | --- |
| `spec/` | The hand-authored OpenAPI 3.1 specification — the contract everything else derives from |
| [`apps/`](apps/README.md) | The API service and the Duende IdentityServer host |
| [`libs/`](libs/README.md) | Generated server contracts and the generated C# client — **never hand-edited** |
| [`tools/`](tools/README.md) | The code-generation script and the framework state validator |
| [`infra/`](infra/README.md) | Compose support files, database initialisation |
| [`project-os/`](project-os/README.md) | The delivery operating system: governance, backlog, sprints, ADRs, standards, agent skills |

Two rules keep the boundary clean: **delivery-process artifacts live only in `project-os/`** (no tickets, boards, or process docs scattered through the source tree), and **source code never lives in `project-os/`**. Git conventions, including the trunk/branch commit lanes, are in [`project-os/standards/GIT.md`](project-os/standards/GIT.md).

## Getting started

### Prerequisites

| Tool | Why |
| --- | --- |
| Docker + Compose | The whole system runs under Compose — API, PostgreSQL, identity host |
| .NET SDK 10 | Build and test |
| A JDK (17+) | OpenAPI Generator is a Java tool ([ADR-0004](project-os/architecture/adr/ADR-0004-contract-first-openapi-code-generation.md)) |

### Run the stack

```bash
docker compose up          # API + PostgreSQL + identity host; migrations run as an explicit step
```

### Build, test, regenerate

```bash
dotnet build                                     # must be warning-clean
dotnet test                                      # unit + integration (needs Docker running)
./tools/generate.sh && git diff --exit-code      # codegen drift check — must produce no diff
```

The drift check is a **merge gate**, not a nicety: a non-empty diff means the committed code no longer matches the contract.

### The one rule that shapes everything

**Change the specification, then regenerate — never the other way round.** Generated code under `libs/` is not hand-edited; if the API needs to change, `spec/openapi.yaml` changes first. See [`project-os/standards/ENGINEERING.md`](project-os/standards/ENGINEERING.md).

## How this project is run

Humans and AI agents deliver this project together using the framework in [`project-os/`](project-os/README.md) — start there. New to the framework? The tutorials and cheatsheet live in [`project-os/docs/`](project-os/docs/README.md). Agents: your entry point is [`CLAUDE.md`](CLAUDE.md) / `project-os/README.md`, and every delivery activity (refining, planning, implementing, accepting) has a skill in [`project-os/skills/`](project-os/skills/README.md).

This repository is in **solo mode**: no git remote, therefore **safe for one agent at a time** ([`project-os/standards/GIT.md`](project-os/standards/GIT.md)).
