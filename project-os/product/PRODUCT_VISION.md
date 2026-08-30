# Product Vision

Owned by the Product Owner persona; changed only with PO (human) approval. Populated during bootstrap on 2026-08-30 — facts carry the [`PROJECT.md`](../PROJECT.md) status tags.

This document is the "why" behind the backlog. Refinement and prioritization decisions should be traceable to it. If a ticket cannot be connected to this vision, question the ticket — or update the vision deliberately.

## Vision statement

For the company's own engineers and the internal tools they build, **Got Issues** is a self-hosted issue and task tracker whose HTTP contract is the product. Unlike the third-party trackers it would replace, the OpenAPI specification is written first and the implementation is generated from it — so every capability is, by construction, available to a program, and the whole thing runs on infrastructure the company controls. `[confirmed]`

**This is a proof of concept.** It exists to answer two questions before anyone commits further: *can we run our development tooling in-house?* and *does contract-first delivery hold up in practice?* Reporting honestly that the answer is no would also be a successful outcome. `[confirmed]`

## Problem

The company's development work is tracked on tooling it does not own or run. Bringing that in-house — eventually including git itself — means starting somewhere, and issue tracking is the piece with the clearest boundaries. `[confirmed]`

Tracking software work means recording projects, issues, who owns what, what state each item is in, and the conversation around it. Third-party tools do this well for humans clicking through a UI, but integrating with them means working against an API designed after the fact: inconsistent resources, undocumented edge cases, and drift between the published specification and actual behaviour. Automating internal process against someone else's afterthought API is where the friction lives. `[assumption]` — the framing is the agent's; the goal of self-hosting is the maintainer's.

## Target users

The company's own engineers, and the internal tools and automation they build against the API. One deployment, one company — single-tenant by design. Detailed personas in [USER_PERSONAS.md](USER_PERSONAS.md). `[confirmed]`

## What success looks like

- A fresh clone reaches a running, authenticated API by following the README literally — nothing installed on the host but Docker and the .NET SDK. `[default]`
- Every endpoint in the specification is exercised by an automated test against a real PostgreSQL instance. `[default]`
- Regenerating code from the specification produces no diff: spec and implementation cannot drift apart. `[default]`
- The core loop works end to end: authenticate → create a project → create, assign, and progress an issue → comment on it. `[confirmed]`

- The PoC answers its question either way: the approach is shown to be worth continuing, or it is honestly reported as not worth it. `[confirmed]`

*(The first three are the agent's proposal pending the maintainer's confirmation — `PROJECT.md` Q4.)*

## Non-goals

- **No UI.** Web or mobile clients are out of scope; the API is the deliverable. `[confirmed]`
- **No notifications, email, plugins, or marketplace.** `[default]`
- **No import or migration from Jira** or other trackers. `[default]`
- **Not multi-tenant.** One deployment serves one company; tenant isolation is permanently out of scope. `[confirmed]`
- **Not a git forge.** Self-hosted git is a separate future effort this PoC clears the way for — Got Issues does not grow repositories or issue↔commit linking. `[confirmed]`
- **Not feature parity with Jira.** Jira is a reference for *domain shape*, not a specification to reproduce. `[confirmed]`

## Guiding product principles

1. **The contract comes first.** If a capability isn't in the OpenAPI specification, it doesn't exist. Design happens in the spec, not in controllers. `[confirmed]`
2. **Generated over hand-written.** Where a generator can own code, it owns it — hand-editing generated output is a defect, not a shortcut. `[confirmed]`
3. **Simplicity over configurability.** Jira's cost is its configurability; prefer one good default to a settings screen. When two designs are close, ship the one with fewer knobs. `[default]`
4. **It runs anywhere Docker runs.** No capability may depend on host-installed infrastructure or a cloud service. `[confirmed]`
