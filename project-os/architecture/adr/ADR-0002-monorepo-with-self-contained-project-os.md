# ADR-0002: Build the product as a monorepo with the framework self-contained under project-os/

## Status

Accepted

## Date

2026-08-30

## Context

The delivery framework must coexist with the product's source code without polluting it, and the team builds every product from the same foundation template. Multiple agents coordinate through repository state (ticket claims, sprint commitments), which only works if that state and the code it governs share one history. The team has decided that software built with this framework is developed as a monorepo, and the framework needs a fixed, copyable home inside it.

## Decision

Every product built from this foundation is a **single monorepo** containing all applications, shared libraries, tooling, and infrastructure code, laid out as `apps/`, `libs/`, `tools/`, `infra/` (tailored during bootstrap). The delivery framework lives **entirely inside one top-level directory, `project-os/`** — governance, product knowledge, backlog, sprints, ADRs, standards, templates, and skills. Delivery-process artifacts never appear outside `project-os/`; source code never appears inside it. All framework-internal links are relative, keeping the directory copyable and upgradable as a unit. Git conventions supporting this model are defined in [`standards/GIT.md`](../../standards/GIT.md).

## Options Considered

1. **Monorepo with self-contained `project-os/` (chosen)** — one history for code and process state (atomic claims, traceable commits), clean separation, trivial to copy from the foundation and to diff against it later.
2. **Polyrepo (one repo per service/app)** — rejected: cross-cutting changes lose atomicity, each repo would need its own framework copy with multiplied divergence, and agents would need cross-repo coordination the claiming mechanism cannot provide.
3. **Framework in a separate repo beside the code repo** — rejected: ticket claims could not gate code changes atomically, every skill would juggle two checkouts, and the "one place to look" property dies.
4. **Framework files interleaved at the repo root** (the foundation's original layout) — rejected: collides with the product's own README and docs, scatters process files through the source tree, and makes copying/upgrading the framework error-prone.

## Consequences

### Positive

- Source tree stays clean; the framework is one directory to copy, ignore in code tooling, or diff against the foundation.
- Process state and code share one history: a `git log` interleaves delivery events with the changes they governed.
- Monorepo enables atomic cross-cutting changes and a single CI entry point.

### Negative

- Monorepos concentrate scale problems (CI times, clone size) — acceptable at team scale, revisit via a superseding ADR if it hurts.
- Tooling that assumes repo-root config (linters, IDEs) may need explicit include/exclude rules for `project-os/`.

## Risks

If agents write process artifacts outside `project-os/` (or code inside it), the separation erodes — guarded by `standards/GIT.md` and skill State Changes lists. Very large teams may outgrow a single trunk; that would be a superseding ADR, not an ad-hoc split.

## Follow-up Actions

None — the root scaffold, `standards/GIT.md`, and skill updates land together with this ADR.

## Related ADRs

[ADR-0001](ADR-0001-record-architecture-decisions.md) (the decision-recording practice this ADR uses).

## Related Tickets

None (framework-level decision, foundation version 1.1.0).
