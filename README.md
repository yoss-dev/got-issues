# Got Issues

A self-hosted, API-first issue and task tracker for the company's own engineering work — Jira-like in shape, deliberately small in surface. There is no UI: the HTTP API is the product, and its [OpenAPI specification](spec/README.md) is written first, with server contracts and clients generated from it. Single-tenant, internal-only.

This is a **proof of concept** — a step toward running the company's development tooling in-house (self-hosted git being the eventual prize) and a test of whether contract-first delivery holds up in practice. Full project facts, constraints, and their confidence levels are in [`project-os/PROJECT.md`](project-os/PROJECT.md).

> **Status: the pipeline is real.** The API, PostgreSQL, an explicit migration step and a self-hosted identity host come up under Docker Compose ([ADR-0003](project-os/architecture/adr/ADR-0003-initial-technology-stack.md)); the commands below work as written. The API's endpoints are generated from [`spec/openapi.yaml`](spec/openapi.yaml) ([ADR-0004](project-os/architecture/adr/ADR-0004-contract-first-openapi-code-generation.md)). What exists so far is a deliberately disposable placeholder resource proving that pipeline end to end — the real product resources come next. See *Not here yet* for what does not exist.

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

`dotnet test` runs the fast tiers only, and is meant to stay that way.

### The stack check

```bash
./tools/smoke.sh              # drives the real compose.yaml; minutes, not seconds
./tools/smoke.sh --build-only # compile it without starting anything
```

Some things are only true of the **stack**, not of the application: that a cold start on an empty volume reaches healthy, that restarting against an existing volume keeps your data and applies no migrations, that the API waits for a database instead of crashing, that a token from the real identity host is accepted and expired, wrong-audience or unknown-key tokens are refused. `WebApplicationFactory` starts the API in-process and can drive neither `docker compose` nor a live identity host, so none of that is reachable from `dotnet test` ([T-0015](project-os/product/tickets/T-0015-compose-stack-smoke-test.md)).

It builds images and starts containers, so it takes minutes — which is why it is a separate command and why `apps/GotIssues.SmokeTests` is deliberately absent from `GotIssues.slnx`. **Nothing else compiles that project**, so `--build-only` exists to catch it rotting without paying for a full run.

The check reads the service list from `compose.yaml` itself, and requires every service to be either running and healthy or exited 0 — so **a long-running service must declare a healthcheck** or the check fails. That is deliberate: a service whose health nobody declared cannot be asserted healthy.

Every stack it starts uses its own Compose project name and **ephemeral host ports**, so it cannot collide with a stack you already have running — and cannot be answered by one either, which is the point: a `curl` to `localhost:8080` proves nothing about a container that failed to start.

### The contract, and changing it

[`spec/openapi.yaml`](spec/openapi.yaml) is the API. It is written by hand, first; server contracts and clients are generated from it. **The workflow is one-directional:**

```bash
# 1. edit spec/openapi.yaml
./tools/generate.sh      # 2. regenerate into libs/
                         # 3. implement the generated interface in apps/
./tools/check-drift.sh    # 4. prove the two agree
```

Generated code under `libs/` is **never hand-edited** — a change there is lost on the next run. If the API needs to change, the specification changes first ([ADR-0004](project-os/architecture/adr/ADR-0004-contract-first-openapi-code-generation.md)).

`check-drift.sh` is a **merge gate**: it regenerates and fails if the result differs from what is committed. It is what makes the rule above real rather than aspirational.

The generator runs from a pinned container image, so **no JDK is needed** — only Docker. The first run pulls about a gigabyte and can take several minutes; that is a slow pull, not a hang.

*Operational endpoints (`/health` and friends) are deliberately absent from the specification — they serve operators, not clients generating against it ([ADR-0005](project-os/architecture/adr/ADR-0005-operational-endpoints-outside-the-api-contract.md)).*

### Not here yet

- Product resources — projects, issues, comments. What exists is a disposable placeholder proving the pipeline; [T-0004](project-os/product/tickets/T-0004-create-and-list-projects.md) brings the first real one.
- **User** tokens. The identity host issues machine-client tokens, which carry a role but no subject — so no endpoint is yet guarded by a *person's* identity, and the user projection stays empty in practice.

*(Everything else the standards mention now exists.)*

### Getting a token

The stack includes a self-hosted [Duende IdentityServer](https://duendesoftware.com). Two development identities are seeded from `.env` — one `admin`, one `member` — as OAuth clients using the client-credentials flow:

```bash
curl -s -X POST localhost:8081/connect/token \
  -d "grant_type=client_credentials&client_id=$ADMIN_CLIENT_ID&client_secret=$ADMIN_CLIENT_SECRET&scope=gotissues.api"
```

The token carries a `role` claim (`admin` or `member`) which the API reads per request and never stores.

Two authorisation policies act on that claim — `admin` is restricted to that role, `member` is a floor an admin also satisfies, and an absent or unrecognised role is refused rather than treated as a member. **No shipped endpoint uses them yet**: the only protected endpoint below requires authentication, not a role, so an `admin` and a `member` token both reach it. [T-0004](project-os/product/tickets/T-0004-create-and-list-projects.md) is the first endpoint to be role-guarded. To prove the round trip:

```bash
curl -i -H "Authorization: Bearer <token>" localhost:8080/health/authenticated
```

Seeded identities are inserted **only if absent**, so changing `ADMIN_CLIENT_SECRET` in `.env` after the first run has no effect — the stored secret wins. To rotate one, change it in the database or start from a clean volume (`docker compose down -v`).

Duende runs **unlicensed** for this proof of concept, a deliberate decision recorded in [`PROJECT.md`](project-os/PROJECT.md) §4 — licence warnings at startup are expected, not defects.

### The one rule that shapes everything

**Change the specification, then regenerate — never the other way round.** Generated code under `libs/` is not hand-edited; if the API needs to change, `spec/openapi.yaml` changes first. Operational endpoints are the single exemption ([ADR-0005](project-os/architecture/adr/ADR-0005-operational-endpoints-outside-the-api-contract.md)). See [`project-os/standards/ENGINEERING.md`](project-os/standards/ENGINEERING.md).

## How this project is run

Humans and AI agents deliver this project together using the framework in [`project-os/`](project-os/README.md) — start there. New to the framework? The tutorials and cheatsheet live in [`project-os/docs/`](project-os/docs/README.md). Agents: your entry point is [`CLAUDE.md`](CLAUDE.md) / `project-os/README.md`, and every delivery activity (refining, planning, implementing, accepting) has a skill in [`project-os/skills/`](project-os/skills/README.md).

This repository is in **solo mode**: no git remote, therefore **safe for one agent at a time** ([`project-os/standards/GIT.md`](project-os/standards/GIT.md)).
