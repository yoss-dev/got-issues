# Glossary

Shared vocabulary. When a term here conflicts with casual usage, this definition wins. Add project-specific domain terms below the framework terms during bootstrap.

## Framework terms

- **ADR** — Architecture Decision Record; see [`architecture/adr/README.md`](../architecture/adr/README.md).
- **Acceptance criteria (AC)** — verifiable statements on a ticket that define pass/fail for the outcome. Owned by the Product Owner persona.
- **Acceptance testing** — independent verification of AC and DoD by the QA persona, distinct from the implementer's own verification.
- **Backlog** — the ordered set of tickets not yet committed to a sprint; indexed in [`product/BACKLOG.md`](../product/BACKLOG.md).
- **Blocker** — anything preventing a ticket from progressing; must be recorded, never silently worked around.
- **Committed** — selected into the current sprint; listed in `CURRENT_SPRINT.md`.
- **Discovered work** — necessary work identified mid-sprint that was not committed; must be ticketed and recorded, never silently absorbed.
- **DoR / DoD** — Definition of Ready / Definition of Done; the entry and exit quality gates for sprint work.
- **Idea** — a raw, unrefined product or engineering thought in `product/IDEAS.md`; not yet a commitment of any kind.
- **Owner** — the single agent or human currently responsible for an in-progress ticket.
- **Persona** — a working role with defined authority (see [PERSONAS.md](PERSONAS.md)); adopted per activity, not permanently assigned.
- **Refinement** — the activity of improving a ticket until it verifiably meets the DoR.
- **Skill** — an executable, step-by-step procedure for a delivery activity, stored in `skills/`.
- **Spike** — a time-boxed investigation whose deliverable is knowledge, not shippable code.
- **Sprint goal** — the single outcome the sprint exists to achieve; the tiebreaker for mid-sprint decisions.
- **Ticket** — a structured work item file in `product/tickets/`, ID `T-NNNN`.
- **Work Log** — the append-only section of a ticket where owners record plans, decisions, progress, and results so any agent can resume the work.

## Project domain terms

Seeded at bootstrap 2026-08-30 and extended during refinement. Add any term two people could interpret differently.

> **Note:** Got Issues borrows Jira's domain *shape* as a reference, not its definitions. Where a term below differs from Jira's usage, this glossary wins.

- **Project** — a named container for issues, with its own key and membership. The top-level unit of organisation in the product. Not to be confused with "the project" in the delivery sense (this repository); when ambiguity is possible, say *product project* or *delivery project*.
- **Issue** — a single unit of tracked work in a project: the product's central entity. Carries a type, status, priority, assignee, and a discussion. Deliberately the same word Jira uses.
- **Issue type** — what kind of work an issue represents (e.g. task, bug, story). Fixed set for now; per-project configurable types are a later goal.
- **Status** — where an issue currently sits in its life. In the first slice this is a flat field on the issue; validated transitions between statuses are a later goal.
- **Workflow** — a per-project definition of the legal statuses and the transitions between them. **Not built** — a later goal ([`PROJECT.md`](../PROJECT.md) §3). Do not write tickets assuming it exists.
- **Comment** — a text entry attached to an issue by a user, forming its discussion thread.
- **Assignee** — the single user currently responsible for an issue. Distinct from the framework's **Owner**, which is the agent or human responsible for a *delivery ticket*.
- **User** — a person or machine client authenticated by the identity host. The API stores a local projection (subject, display name) and never credentials.
- **Global role** — a user's authorisation level across the whole deployment. Roles are **company-wide, never per project** — there is no project membership or per-project permission concept. Saying "role" without qualification means this.
- **Machine client** — a non-human API consumer authenticating via OAuth client credentials; the *Priya the integrator* persona's programs.
- **The spec / the contract** — `spec/openapi.yaml`, the hand-authored OpenAPI 3.1 document. The single place the API surface is designed ([ADR-0004](../architecture/adr/ADR-0004-contract-first-openapi-code-generation.md)).
- **Generated code** — server contracts and clients produced from the spec by OpenAPI Generator, committed under `libs/` and **never hand-edited**.
- **Drift** — any divergence between the spec and the committed generated code. Detected by regenerating and finding a non-empty diff; a merge gate, and a defect when found.
- **Contract-first** — the working rule that the spec is written before implementation and generation flows one way, spec → code, never the reverse.

### Terms deliberately NOT in scope

Present in Jira, absent here — using them in a ticket signals scope creep, so name the gap explicitly instead: **board**, **sprint** (in the *product* sense — the delivery framework's own sprints are unrelated and defined above), **epic**, **JQL / search**, **workflow scheme**, **plugin**.

Also absent by decision, not by omission: **tenant / organisation** (the system is single-tenant), **project member / project role** (roles are global), and **repository / commit / branch** (Got Issues is not a git forge — that is a separate future effort).
