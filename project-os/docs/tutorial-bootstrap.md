# Tutorial: Bootstrapping a Project

Goal: go from the foundation template to a configured, validated project repository ready for its first idea. Time: ~30 minutes, mostly answering questions. Cast: `pat` (human), one agent session.

## 1. Create the repository

```bash
cd ~/work
git clone <foundation-repo> golinks && cd golinks
rm -rf .git
git init -b main
```

Verify the framework arrived intact — the skill-discovery symlink is the piece a non-git copy loses:

```bash
ls .claude/skills/          # must list the skill directories
python3 tools/validate-project-os/validate.py   # must print OK
```

## 2. Record where this copy came from

Edit `project-os/FOUNDATION.md`, fill the adoption table: copied from, foundation version (see its changelog), today's date. Then:

```bash
git add -A && git commit -m "Initial commit: adopt foundation X.Y.Z"
```

## 3. Run the bootstrap interview

Open your agent in the repo (for Claude Code: `claude`, then `/bootstrap-project`). The agent follows [`bootstrap-project/SKILL.md`](../skills/bootstrap-project/SKILL.md): it verifies the adoption (step 1–2 above), then interviews you in three batches — product, technical, engineering.

**How to answer well:**

- Answer what you know plainly — those become `[confirmed]` facts.
- Say "no preference, pick something sensible" freely — the agent records its choice as `[default]`, which is safe and cheap to change.
- Say "I don't know yet" for real unknowns — those become `[open]` questions in `PROJECT.md` §7, which is the *correct* outcome, not a failure.
- For go-links, pat confirms: internal tool, users are the whole company, TypeScript/Node, SQLite to start, hosted on the internal VM. Pat has no CI preference (`[default]`: the platform's built-in), and doesn't know yet whether links need per-team namespaces (`[open]` Q2).

## 4. What the agent should produce

- `project-os/PROJECT.md` — every fact tagged; banner removed only if no blocking `[open]` remains.
- `project-os/product/PRODUCT_VISION.md` and `USER_PERSONAS.md` — filled from your answers.
- `project-os/architecture/ARCHITECTURE.md` — what's known; possibly an `ADR-0003` *only if* a stack was genuinely decided (not merely inherited).
- Project-specific sections of `project-os/standards/*.md` — including `GIT.md`: platform, trunk protection, **governance path protection (CODEOWNERS)**, merge strategy.
- Tailored skeleton — for go-links, `libs/` is pruned (single app), recorded in `PROJECT.md`.
- Root `README.md` — placeholders replaced with the product's name and setup.
- **A seeded backlog** — bootstrap's own follow-up work as tickets, not prose: for go-links, `T-0001` (chore: remote + trunk/governance protection), `T-0002` (chore: CI running the validator and test suite), `T-0003` (technical: scaffold the Node service and test harness). Investigable `[open]` questions become spikes — pat's namespace question becomes `T-0004` (spike, time-boxed) since it needs a look at how teams actually share links, not just a yes/no.
- **Seeded ideas** — the interview's primary use cases land in `IDEAS.md` (create-a-link, redirect, list-my-links); only what pat explicitly wants first is promoted to a feature ticket. The PO value-gate applies even on day one.

## 5. Wire the platform gates

These are tracked as the setup tickets from step 4; a human with admin rights performs the platform side (agents must not hold admin credentials), closing the tickets through the normal flow:

- shared remote configured (`git remote add origin …`) — without one you are in [solo mode](../standards/GIT.md): one agent at a time;
- trunk protection with the validator and tests as required checks;
- CODEOWNERS (or equivalent) requiring human review on `project-os/{governance,standards,templates,skills}`.

## 6. Verify and finish

```bash
python3 tools/validate-project-os/validate.py   # OK
git log --oneline                               # bootstrap commits present
git push -u origin main
```

**Red flags to check before moving on:** any answer you never gave recorded as `[confirmed]`; template text left looking authoritative instead of tagged `[open]`; the foundation's ⚠ banners removed from files that are still placeholders.

Next: [from idea to Ready](tutorial-idea-to-ready.md).
