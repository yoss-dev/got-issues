# Engineering Standards

Default engineering rules for implementation work, applied by the Software Engineer persona and checked in review and acceptance. Technology-agnostic by design; **bootstrap replaces the marked sections with project-specific rules.** Where this document conflicts with an Accepted ADR or `PROJECT.md`, those win.

## Universal rules

### Code health

- Leave code better than you found it, within the ticket's scope. Larger cleanups become `technical` tickets.
- No speculative abstractions: build for the current acceptance criteria, not imagined futures. Three concrete usages justify an abstraction; one hypothetical does not.
- Dead code, commented-out code, and debug scaffolding do not merge.
- Every `TODO` carries a ticket reference (`TODO(T-0042): …`) or does not merge.

### Change discipline

- Branching, commit messages, merging, and the trunk/branch commit lanes are defined in [`GIT.md`](GIT.md) — one ticket per branch, `T-NNNN:` messages, reviewed squash-merges, process state on the trunk.
- Small, coherent changes: unrelated fixes go to their own tickets, never along for the ride.
- Never commit secrets, credentials, or personal data. See [SECURITY.md](SECURITY.md).
- Migrations, config changes, and feature flags ship with the change that needs them and are documented in the ticket.

### Dependencies

- Adding or replacing a *major* dependency (framework, platform, paid service, anything with lock-in) requires an [ADR](../architecture/adr/README.md). Minor libraries need a short justification in the ticket Work Log.
- Prefer the standard library and existing project dependencies over new ones.
- Pin or lock dependency versions per ecosystem convention.

### Error handling & logging

- Fail loudly and specifically; never swallow errors silently. Catch only what you can handle meaningfully.
- Log at boundaries and failures with enough context to debug without a debugger; never log secrets or personal data.

## Project-specific rules

Set at bootstrap 2026-08-30 for a C# / .NET 10 API ([`PROJECT.md`](../PROJECT.md) §5).

### The contract-first rule (overrides convenience)

This project is contract-driven ([ADR-0004](../architecture/adr/ADR-0004-contract-first-openapi-code-generation.md)) `[confirmed]`:

- **The OpenAPI specification at `spec/openapi.yaml` is the only place the API surface is designed.** No endpoint, field, status code, or error shape exists unless the spec says so.
- **Generated code is never hand-edited.** It lives in its own directory under `libs/` and is reproduced by the generation script in `tools/`. An edit to generated output is a defect, not a shortcut — change the spec and regenerate.
- **A spec change and its regenerated output belong in the same commit.** Regenerating must produce no diff on a clean tree; a diff means drift and fails review.
- Controllers **implement** generated interfaces. Hand-written routing attributes on a controller are a review rejection.

### Language & style

- **C# 14 on .NET 10** `[confirmed]`. Formatting is settled by `dotnet format` against a committed root `.editorconfig` — formatting is never a review comment `[default]`.
- Nullable reference types and implicit usings **on**; treat warnings as errors in project files `[default]`.
- `async`/`await` all the way down for I/O; no `.Result`/`.Wait()`. Pass `CancellationToken` through to EF Core and HTTP calls `[default]`.
- Public API DTOs are generated — do not write hand-rolled request/response models beside them `[confirmed]`.

### Linting / static analysis

- .NET analyzers enabled at `AnalysisLevel: latest-recommended`; the build must be warning-clean `[default]`.
- The validator (`python3 tools/validate-project-os/validate.py`) must pass before any process-lane commit ([GIT.md](GIT.md)) `[default]`.
- Generated directories are excluded from analyzers and formatting checks — they are not ours to fix `[default]`.

### Project structure

Monorepo layout per [ADR-0002](../architecture/adr/ADR-0002-monorepo-with-self-contained-project-os.md); all four scaffold directories are in use `[confirmed]`:

| Path | Contents |
| --- | --- |
| `spec/` | The hand-authored OpenAPI 3.1 specification — the contract |
| `apps/` | The API service and the Duende identity host |
| `libs/` | Generated server contracts and the generated C# client — **never hand-edited** |
| `tools/` | The framework validator and the code-generation script |
| `infra/` | Compose support files, database initialisation |
| `compose.yaml` (root) | The single supported way to run the system |

Tests live in their own projects alongside what they test — see [TESTING.md](TESTING.md).

### Data access

- EF Core 10 with code-first migrations; migrations are applied by an explicit migration step in Compose, never silently at API startup `[confirmed]`.
- **Pagination is mandatory on every collection endpoint.** Unbounded `ToListAsync()` over a user-controlled set fails review `[default]`.
- Watch for N+1 queries: EF Core makes them easy to write and invisible until they hurt `[default]`.

### Performance expectations

No numeric budget `[default]`. Avoid obvious waste: no unbounded result sets, no N+1 queries, no synchronous I/O on request paths. A measured problem gets a ticket, not a micro-optimisation in passing.
