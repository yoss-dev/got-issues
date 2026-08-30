# ADR-0003: Build Got Issues as a .NET 10 API on PostgreSQL with Duende IdentityServer, running entirely under Docker Compose

## Status

Accepted

## Date

2026-08-30

## Context

Got Issues is starting from an empty repository: the delivery framework exists, no application code does. Bootstrap had to fix the platform before any ticket can be refined, because the stack determines project structure, testing approach, how the system is run, and what "done" means for the first slice.

Forces at play:

- **One maintainer, no operations team.** Whatever is chosen must be runnable and debuggable by one person, with no cloud account and no managed services.
- **API-only for now** ([`PROJECT.md`](../../PROJECT.md) §3): no frontend to constrain the choice.
- **Contract-driven development is a hard requirement** (§4). The server framework must accept generated controller stubs comfortably — decided separately in [ADR-0004](ADR-0004-contract-first-openapi-code-generation.md), but it constrains the framework choice here.
- **"Everything must run under Docker Compose"** was stated as a constraint, not a preference: no component may require host-installed infrastructure.
- The maintainer stated the language, framework, database, and identity product directly. This ADR records *why* and what it costs, not a deliberation the agent conducted on their behalf.

## Decision

Got Issues is built as:

- **C# on .NET 10 (LTS), using ASP.NET Core 10** in its controller-based form — the shape generated server stubs target.
- **PostgreSQL** as the only data store, accessed through **EF Core 10** with **code-first migrations** as the schema source of truth. Migrations are applied by an explicit migration step in the Compose stack, not silently at API startup.
- **Duende IdentityServer**, self-hosted as its own service, issuing OIDC / OAuth 2.1 tokens. The API is a pure resource server: it validates JWT bearer tokens against the identity host's discovery document and never handles credentials.
- **Docker Compose** as the single orchestration surface. `docker compose up` from a clean clone brings up the API, the database, and the identity host. No component may depend on host-installed infrastructure beyond Docker and the .NET SDK.

Changing any of these requires a superseding ADR.

## Options Considered

1. **.NET 10 / ASP.NET Core + PostgreSQL + EF Core + Duende, all under Compose (chosen)** — the maintainer's stated stack. ASP.NET Core's controller model maps directly onto generated server stubs; EF Core gives migrations, LINQ, and Testcontainers-friendly integration testing out of the box; Duende is the mature, standards-complete OIDC option in the .NET ecosystem, and self-hosting it keeps the "runs anywhere Docker runs" constraint intact.
2. **Same stack, but Dapper with hand-written SQL migrations instead of EF Core** — more explicit SQL and no ORM surprises, and it keeps the database schema independent of the object model. Rejected: for a solo project it multiplies boilerplate per endpoint, and EF Core's migrations plus `Testcontainers` integration tests are the shortest path to the "every endpoint tested against real PostgreSQL" success criterion. Revisit if query complexity or ORM-generated SQL becomes the bottleneck.
3. **ASP.NET Core Identity, or a hosted identity provider (Auth0, Entra ID, Keycloak)** — ASP.NET Core Identity is simpler but is a user-store, not an OAuth authorization server, so machine clients and scopes would have to be hand-rolled. Hosted providers were rejected outright: they break the Compose constraint and introduce an account, a vendor, and network dependence into local development. Keycloak satisfies the Compose constraint but is a Java service with a heavier operational surface and no .NET-native integration story; Duende was the maintainer's choice.
4. **Minimal APIs instead of controllers** — idiomatic modern ASP.NET Core and less ceremony. Rejected: the chosen code generator emits abstract *controllers*; using minimal APIs would mean discarding the generated server surface and hand-wiring routes, defeating the contract-first constraint.

## Consequences

### Positive

- One `docker compose up` gives a complete, self-contained system — no cloud account, no host services, reproducible on any machine with Docker.
- ASP.NET Core controllers, EF Core migrations, and Testcontainers combine into a well-trodden path for integration-testing an API against a real database.
- Duende self-hosted means OAuth 2.1 and OIDC are correct from day one, including client-credentials flows for the integrator persona, without inventing an auth scheme.
- EF Core migrations make schema change reviewable in the same commit as the code that needs it.
- LTS runtime: .NET 10 is supported long enough that the project will not be forced to upgrade mid-flight.

### Negative

- **Duende is commercially licensed.** Free for development and under its Community Edition revenue threshold, but any deployment beyond that requires payment. The exact terms are unverified (`PROJECT.md` Q1) — this is a real cost attached to a decision made today.
- **The identity host is a whole second service** to build, configure, seed, and keep running, for a product whose actual value is issue tracking. It is the largest fixed cost in the first slice.
- **EF Core hides the SQL.** N+1 queries and unbounded result sets are easy to write and invisible until they hurt; pagination is therefore mandatory on collection endpoints and query behaviour needs deliberate review.
- **Sharing one PostgreSQL instance between the API and the identity host** couples their availability and backup story. Acceptable while local-only; wrong for a real deployment.
- Container startup ordering (database ready → migrations applied → API starts) is a real source of local-development friction that must be handled explicitly with health checks and a dedicated migration step.
- No deployment target exists, so nothing here is validated beyond a developer machine.

## Risks

- **Duende licensing turns out to be incompatible** with the eventual use. Noticed when Q1 is answered or when a deployment is planned; mitigated by the API being a plain JWT resource server — swapping the issuer for Keycloak or another OIDC provider changes configuration, not application code. Keeping that seam clean is a design obligation, not a nicety.
- **EF Core's model-first migrations become awkward** if the schema later needs to be owned independently (DBA review, a second consumer). Noticed as migration files that fight the model; mitigated by moving to SQL-file migrations, which is a contained change.
- **Compose-only fits local development but not deployment.** Noticed the first time hosting is discussed. Deliberately deferred, not overlooked.

## Follow-up Actions

- Stand up the Compose stack (PostgreSQL + identity host + API skeleton) with health checks and an explicit migration step — to be ticketed via `create-ticket` during the first refinement pass.
- Answer `PROJECT.md` Q1 (Duende licensing) before any non-local deployment.
- Answer `PROJECT.md` Q5 (authorization model) before the first endpoint that must authorise; and Q3 (multi-tenancy) before the projects/users schema is fixed.

## Related ADRs

- Depends on [ADR-0002](ADR-0002-monorepo-with-self-contained-project-os.md) — the monorepo this stack lives in.
- Paired with [ADR-0004](ADR-0004-contract-first-openapi-code-generation.md), which fixes the contract-first pipeline this stack must accommodate (and is the reason controllers were chosen over minimal APIs).

## Related Tickets

None yet — this decision predates the first ticket. Tickets implementing the stack link back here.
