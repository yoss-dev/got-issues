# PROJECT.md — Project Facts, Constraints, and Configuration

This file is the single source of truth for *what this project is* and *how it is built*. It sits at precedence level 2, directly below the [Way of Working](governance/WAY_OF_WORKING.md). Agents load it for almost every activity.

Bootstrapped 2026-08-30 via [`bootstrap-project`](skills/bootstrap-project/SKILL.md).

## Fact status convention

Every statement in this file carries one of these tags. Agents MUST respect them and MUST NOT promote a tag without evidence or a human decision:

- `[confirmed]` — stated by a human stakeholder or verified against reality (code, infra, contract).
- `[default]` — a reasonable industry default adopted because nobody objected. Safe to rely on; cheap to change.
- `[assumption]` — believed but unverified. An agent relying on an assumption for a significant decision must first escalate or verify.
- `[open]` — an unresolved question. Work that depends on it is blocked or must be escalated.

## 1. Identity

- **Project name:** Got Issues `[confirmed]`
- **One-line description:** An API-first issue and task tracker for software delivery — Jira-like in shape, deliberately small in surface `[confirmed]`
- **Repository purpose:** product monorepo; delivery framework self-contained in `project-os/` per [ADR-0002](architecture/adr/ADR-0002-monorepo-with-self-contained-project-os.md) `[confirmed]`

## 2. Problem and users

- **Problem being solved:** the company wants to run its own development tooling in-house rather than on a third-party service. Issue tracking is the first piece: engineers need projects, issues, assignment, and discussion behind a clean, versioned HTTP contract that the company controls and can automate against. Got Issues is the **proof of concept** for that — both of the product and of the contract-first, self-hosted approach `[confirmed]`
- **Target users:** engineers inside the company, and the internal tools and automation they build against the API `[confirmed]` (detail in [product/USER_PERSONAS.md](product/USER_PERSONAS.md))
- **Nature of the effort:** a proof of concept, not a production rollout. Success is showing the approach works; production hardening is a later, separate decision `[confirmed]`
- **Wider context:** part of a company ambition to bring development tooling in-house — self-hosted git is the eventual prize. Got Issues de-risks that direction (self-hosting, contract-first delivery) without being the forge itself `[confirmed]`
- **Primary use cases:** `[confirmed]` (scope of the first slice, chosen by the maintainer)
  1. Create and organise projects, and the issues within them.
  2. Track an issue's type, status, priority, and assignee through its life.
  3. Discuss an issue via comments.
  4. Authenticate a user or a machine client and authorise their access to project data.

## 3. Goals, non-goals, success criteria

- **Product goals:** `[confirmed]`
  - A working, authenticated HTTP API covering projects, issues, comments, and users.
  - The OpenAPI specification is the source of truth: server contracts and client SDKs are generated from it, never hand-maintained alongside it.
  - The whole system — API, database, identity provider — starts from a clean clone with `docker compose up`.
- **Later goals (not committed):** configurable workflows and validated status transitions; boards; sprints; query/search `[confirmed]`
- **Non-goals (explicitly out of scope):**
  - A web or mobile UI — the API is the deliverable for now `[confirmed]`
  - Notifications, email, plugin/extension systems, marketplace `[default]`
  - Import/migration from Jira or other trackers `[default]`
  - **Git hosting.** The company intends to self-host its own git eventually; that is a *separate* effort. Got Issues stays an issue tracker and does not grow repositories, git transport, or issue↔commit linking `[confirmed]`
  - **Multi-tenancy — permanently out of scope.** One deployment serves one company; the data model is single-tenant by design and need not leave room for tenant isolation `[confirmed]`
- **Success criteria:** `[default]` — proposed by the agent, not yet confirmed (see Q4). As a PoC, these test the *approach* as much as the product:
  - A fresh clone reaches a running, authenticated API through documented commands only.
  - Every endpoint in the spec is exercised by an automated test against a real PostgreSQL instance.
  - Regenerating from the spec produces no diff against committed generated code (no drift).
  - The contract-first pipeline proves itself worth keeping — or is honestly reported as not worth it.

## 4. Constraints

Hard constraints that override ticket-level convenience:

- **Everything must run under Docker Compose** — API, PostgreSQL, and the identity provider; no component may require host-installed infrastructure to run `[confirmed]`
- **Contract-driven development is mandatory**: the OpenAPI specification is authored first; controllers and clients are generated from it by a source generator, automated as far as the toolchain allows. Hand-writing anything the generator owns is a defect `[confirmed]` — see [ADR-0004](architecture/adr/ADR-0004-contract-first-openapi-code-generation.md)
- **Solo delivery**: one maintainer, therefore one agent at a time on the repository (solo mode, §6) `[confirmed]`
- **Duende IdentityServer licensing**: the maintainer knows the terms and has decided **Got Issues runs Duende unlicensed for the duration of the proof of concept** — an informed, deliberate choice, not an oversight `[confirmed]`. Expect licence warnings at startup; they are expected behaviour, not defects. Licensing becomes a live question again only if the PoC turns into something the company actually runs, which is a separate decision
- No deadline, budget ceiling, or regulatory compliance regime stated `[assumption]` (absence of a stated constraint, not a confirmed absence)

## 5. Technical profile

| Aspect | Choice | Status |
| --- | --- | --- |
| Programming language(s) | C# 14 | `[confirmed]` (language chosen by maintainer; version follows the SDK) |
| Runtime | .NET 10 (LTS) — SDK `10.0.300` verified on the maintainer's machine 2026-08-30 | `[confirmed]` |
| Backend framework | ASP.NET Core 10 (controller-based, to match generated server stubs) | `[confirmed]` |
| Frontend stack | None — API-only product (§3 non-goal) | `[confirmed]` |
| Database / storage | PostgreSQL, accessed via EF Core 10 with code-first migrations | `[confirmed]` |
| API style | REST over HTTP/JSON, specified in OpenAPI 3.1 | `[confirmed]` |
| API contract & codegen | Spec-first; [OpenAPI Generator](https://openapi-generator.tech) CLI (`aspnetcore` server stubs + `csharp` client) — [ADR-0004](architecture/adr/ADR-0004-contract-first-openapi-code-generation.md) | `[confirmed]` |
| Authentication | Duende IdentityServer (OpenID Connect / OAuth 2.1), self-hosted in compose; API validates JWT bearer tokens | `[confirmed]` |
| Authorization model | **Global roles `admin` and `member`**, carried as a claim in the Duende token — the API reads the claim per request and never stores roles. `admin` additionally performs administrative acts: creating and archiving projects, deleting issues and comments. Role *assignment* happens in Duende, not through this API. Scope-based access for machine clients | `[confirmed]` |
| Package / build tooling | .NET SDK (`dotnet build/test`), NuGet with a lock file; OpenAPI Generator CLI runs on the JDK (25 verified locally) | `[confirmed]` |
| Testing frameworks | xUnit; `WebApplicationFactory` for API-level tests; Testcontainers for PostgreSQL | `[default]` |
| CI/CD system | None yet — no remote (solo mode). The validator and test suite are run locally before every merge | `[open]` (Q6) |
| Hosting / cloud provider | None — local Docker Compose only for now | `[confirmed]` |
| Infrastructure approach | Docker Compose as the single orchestration surface; `compose.yaml` at the repository root, supporting files in `infra/` | `[confirmed]` (compose) / `[default]` (file layout) |
| Observability | Built-in `ILogger` structured logging plus OpenTelemetry traces/metrics exported to the console in local runs | `[default]` |
| Supported environments | Local development on macOS (Apple Silicon) via Docker Compose; Linux containers | `[confirmed]` (verified: Docker 29.2.1, Compose v5.1.0 on darwin 25.6.0) |
| External integrations | None | `[confirmed]` |

Significant technology choices made after bootstrap require an [ADR](architecture/adr/README.md); this table then links to it.

## 6. Engineering practices

| Practice | Decision | Status |
| --- | --- | --- |
| Source repository layout | Monorepo per [ADR-0002](architecture/adr/ADR-0002-monorepo-with-self-contained-project-os.md). All four scaffold directories retained: `apps/` (the API service and the identity host), `libs/` (generated contracts and client), `tools/` (validator, codegen scripts), `infra/` (compose support files, DB init) | `[confirmed]` |
| **Remote mode** | **Solo mode — no git remote configured.** Per [GIT.md](standards/GIT.md) *Remotes and solo mode*: both commit lanes and all conventions still apply, push-based collision detection is void, and the repository is safe for **one agent at a time**. A second concurrent agent requires setting up a remote first | `[confirmed]` |
| Branching strategy | Trunk-based (`main`), ticket branches in per-ticket worktrees, two commit lanes — see [standards/GIT.md](standards/GIT.md) | `[default]` |
| Code review | Every merge to `main` reviewed. With no PR platform, an independent session runs `review-code` against the branch diff and records the verdict in the Work Log before the local merge | `[confirmed]` (follows from solo mode) |
| Test expectations | See [standards/TESTING.md](standards/TESTING.md) | `[default]` |
| Deployment strategy | None — `docker compose up` is the only "deployment" until a hosting target exists | `[confirmed]` |
| Security requirements | See [standards/SECURITY.md](standards/SECURITY.md); no compliance regime stated | `[default]` |
| Documentation expectations | See [standards/DOCUMENTATION.md](standards/DOCUMENTATION.md); the OpenAPI spec is the user-facing API documentation | `[confirmed]` |
| Supported platforms | Linux containers, orchestrated by Docker Compose; developed on macOS/Apple Silicon | `[confirmed]` |
| Performance expectations | No numeric budget; avoid obvious waste (N+1 queries, unbounded result sets — pagination is mandatory on collection endpoints) | `[default]` |

## 7. Open questions

Questions that block or shape work. Remove entries only when answered (record the answer above with `[confirmed]`).

| # | Question | Blocking? | Raised by / date |
| --- | --- | --- | --- |
| Q4 | Are the proposed success criteria (§3) the right ones? | No | bootstrap / 2026-08-30 |
| Q6 | CI: stay local-only, or add a remote (e.g. GitHub) with a pipeline running the validator, build, tests, and a spec-drift check? | No | bootstrap / 2026-08-30 |
| Q8 | Employee personal data (names, email addresses from the identity provider) in an internal tool — does the company's data-protection regime (e.g. GDPR) apply, and does a PoC get an exemption? | No (Yes before real employee data is loaded) | bootstrap / 2026-08-30 |

**Answered 2026-08-30 (maintainer):** ~~Q1~~ Duende licensing is understood and accepted — running unlicensed for the PoC (§4). ~~Q2~~ internal company tool, a PoC — target users are the company's own engineers (§2). ~~Q3~~ single-tenant, multi-tenancy permanently out of scope (§3). ~~Q5~~/~~Q7~~ authorization uses **global roles `admin` and `member`**, carried as a Duende token claim; admin acts are project creation/archival and deleting issues/comments (§5, implemented by [T-0009](product/tickets/T-0009-role-authorisation-and-user-projection.md)).
