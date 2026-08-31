# Got Issues

A self-hosted, API-first issue and task tracker for the company's own engineering work — Jira-like in shape, deliberately small in surface. There is no UI: the HTTP API is the product, and its [OpenAPI specification](spec/README.md) is written first, with server contracts and clients generated from it. Single-tenant, internal-only.

This is a **proof of concept** — a step toward running the company's development tooling in-house (self-hosted git being the eventual prize) and a test of whether contract-first delivery holds up in practice. Full project facts, constraints, and their confidence levels are in [`project-os/PROJECT.md`](project-os/PROJECT.md).

> **Status: the stack runs.** The API, PostgreSQL, and an explicit migration step come up under Docker Compose ([ADR-0003](project-os/architecture/adr/ADR-0003-initial-technology-stack.md)); the commands below work as written. There are no product endpoints yet — the contract-first pipeline ([ADR-0004](project-os/architecture/adr/ADR-0004-contract-first-openapi-code-generation.md)) and authentication arrive next. See *Not here yet* below for what does not exist.

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
| Docker (with Compose) | The whole system runs under Compose — API, PostgreSQL, and the migration step |
| .NET SDK 10 | Building and working on the code outside containers |

### Run the stack

```bash
cp .env.example .env      # then edit .env — it is git-ignored and holds local credentials
docker compose up --build
```

That brings up PostgreSQL, runs the **migration step** to completion, and then starts the API. The ordering is enforced by Compose health conditions, so a slow database delays startup rather than crashing it.

Check it is alive:

```bash
curl -s localhost:8080/health
# {"status":"Healthy","checks":{"database":{"status":"Healthy","description":"database reachable"}}}
```

`/health` is an **operational** endpoint: it is deliberately not part of the API contract and does not appear in the OpenAPI specification ([ADR-0005](project-os/architecture/adr/ADR-0005-operational-endpoints-outside-the-api-contract.md)). It is documented here because operators are a different audience from the clients that generate against the specification.

### Migrations

Schema changes are applied by an explicit step, never silently at API startup ([ADR-0003](project-os/architecture/adr/ADR-0003-initial-technology-stack.md)). `docker compose up` runs it; to run it alone:

```bash
docker compose run --rm migrator
```

To scaffold a new migration after changing the model:

```bash
dotnet restore     # required on a fresh clone: the EF tooling builds the project
dotnet dotnet-ef migrations add <Name> --project apps/GotIssues.Api --output-dir Data/Migrations
```

### Build and test

```bash
dotnet build          # warning-clean: the projects build with warnings as errors
dotnet test           # unit + integration; needs Docker running
```

The integration tier starts a real PostgreSQL container ([Testcontainers](https://dotnet.testcontainers.org)) and drives the API through its real HTTP pipeline — never an in-memory database, which enforces no constraints and translates no real SQL. One container per run, a fresh database per test. With Docker stopped, the integration tier fails fast and names the container runtime rather than timing out against a database.

### Not here yet

These are documented in the standards but their tooling arrives with the tickets that build it — do not expect them to run today:

- `./tools/generate.sh` and the OpenAPI specification — the contract-first pipeline is [T-0002](project-os/product/tickets/T-0002-contract-first-codegen-pipeline.md). Until it lands, `spec/` holds only its README.
- Authentication — the identity host is [T-0010](project-os/product/tickets/T-0010-duende-identity-host.md). Nothing is protected yet.

### The one rule that shapes everything

**Change the specification, then regenerate — never the other way round.** Generated code under `libs/` is not hand-edited; if the API needs to change, `spec/openapi.yaml` changes first. Operational endpoints are the single exemption ([ADR-0005](project-os/architecture/adr/ADR-0005-operational-endpoints-outside-the-api-contract.md)). See [`project-os/standards/ENGINEERING.md`](project-os/standards/ENGINEERING.md).

## How this project is run

Humans and AI agents deliver this project together using the framework in [`project-os/`](project-os/README.md) — start there. New to the framework? The tutorials and cheatsheet live in [`project-os/docs/`](project-os/docs/README.md). Agents: your entry point is [`CLAUDE.md`](CLAUDE.md) / `project-os/README.md`, and every delivery activity (refining, planning, implementing, accepting) has a skill in [`project-os/skills/`](project-os/skills/README.md).

This repository is in **solo mode**: no git remote, therefore **safe for one agent at a time** ([`project-os/standards/GIT.md`](project-os/standards/GIT.md)).
