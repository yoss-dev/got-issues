---
id: T-0012
title: Pin container base images to immutable digests
type: technical
status: backlog
priority: normal
owner: none
implemented_by: none
accepted_by: none
depends_on: [T-0001]
adrs: [ADR-0003]
created: 2026-08-30
updated: 2026-08-30
---

# T-0012: Pin container base images to immutable digests

## Problem / Context

Raised as a non-blocking finding during T-0001's independent review (`claude-rev-2c8d`, 2026-08-30) and deliberately deferred: pinning to digests the implementer could not verify would have been worse than leaving it.

`compose.yaml` and the API Dockerfile reference **floating tags** — `mcr.microsoft.com/dotnet/sdk:10.0`, `mcr.microsoft.com/dotnet/aspnet:10.0`, `postgres:18-alpine`. Those tags move. Two builds of the same commit can therefore produce different images, which is the same class of problem the NuGet lock file solves for packages ([PROJECT.md](../../PROJECT.md) §5) and which [ENGINEERING.md](../../standards/ENGINEERING.md) addresses directly: *"Pin or lock dependency versions per ecosystem convention."* Base images are dependencies.

## Desired Outcome

Every container base image is referenced by an immutable digest, so a given commit builds the same images tomorrow as today.

## User / Business Value

Reproducibility. Without it, "it worked yesterday" has no answer, and a silent base-image change is indistinguishable from a code defect — expensive to diagnose precisely when something is already wrong.

## Scope

### In Scope

- Replace floating tags with `image@sha256:…` digests in `compose.yaml` and the Dockerfile, keeping the human-readable tag in a comment so the version is still legible.
- Document how to refresh a digest deliberately.
- Verify the stack still builds and runs (T-0001's criteria remain true).

### Out of Scope

- Automated update tooling (Renovate/Dependabot) — that needs a remote and CI (`PROJECT.md` Q6).
- Pinning NuGet packages; already handled by the lock file.

## Acceptance Criteria

- [ ] AC1: Given the repository, when `compose.yaml` and the Dockerfile are inspected, then every base image is referenced by digest and no floating tag remains.
- [ ] AC2: Given a pinned digest, when the stack is built and started, then all services reach a healthy state as in T-0001.
- [ ] AC3: Given the README or a comment beside each pin, when a reader asks how to move to a newer base image, then the documented procedure answers it.

## Examples / Scenarios

- Rebuild after upstream publishes a new `postgres:18-alpine`: the build uses the pinned digest, unchanged.
- Deliberate refresh: follow the documented procedure, digest changes in one reviewable commit.

## Dependencies

**T-0001** — the images and compose file must exist.

## Risks / Unknowns

- Digests are architecture-specific unless the manifest-list digest is used. Pinning an `arm64` image digest would break any future `amd64` build. **Use the multi-arch manifest digest**, and the ticket should verify that explicitly rather than assume it.
- Pinned images go stale silently, including on security patches. Without CI there is nothing to notice; the documented refresh procedure is the only mitigation, and that is a real limitation to state rather than paper over.

## Testing Notes

Verified by rebuilding from scratch and re-running T-0001's stack criteria. If [T-0003](T-0003-automated-test-harness.md) has landed, its suite covers the behavioural half.

## Relevant ADRs & Documentation

- [ADR-0003](../../architecture/adr/ADR-0003-initial-technology-stack.md) — the stack and the Compose constraint
- [ENGINEERING.md](../../standards/ENGINEERING.md) — dependency pinning

## Definition of Ready

- [ ] Meets [DoR](../../governance/DEFINITION_OF_READY.md) — not yet refined.

## Definition of Done

- [ ] Meets [DoD](../../governance/DEFINITION_OF_DONE.md) — checked by `complete-ticket`.

---

## Work Log

### 2026-08-30 — Software Engineer (claude-sm-9d4e)

- **Did:** Created to capture a T-0001 review deferral, so DoD item 4 is satisfied by a linked ticket rather than Work Log prose.
- **Decided:** none.
- **Remaining:** Refinement, then implementation.
- **Open questions / blockers:** none.
- **Branch / PR:** n/a
- **Test state:** n/a — not started.
