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

> ⚠ **Replace during `bootstrap-project`** with the project's language/framework conventions.

- **Language & style guide:** *TBD — name the authoritative style guide and formatter; formatting disputes are settled by the formatter, not review comments.* `[open]`
- **Linting / static analysis:** *TBD — name the tools and the rule that CI must be clean.* `[open]`
- **Project structure:** *TBD — where code, tests, and assets live; how modules are organized.* `[open]`
- **Branching & review:** see [`GIT.md`](GIT.md); confirm its project-specific section during bootstrap.
- **Performance expectations:** *TBD — budgets or "no specific budget; avoid obvious waste".* `[open]`
