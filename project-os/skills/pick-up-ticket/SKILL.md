---
name: pick-up-ticket
description: Safely claim an eligible ticket from the current sprint — verify state, dependencies, and DoR; read required context; mark ownership atomically; and record an implementation plan before any code is written.
---

# Skill: pick-up-ticket

## Purpose

Move a ticket from `committed` to `in-progress` without collisions or false starts: one owner, verified preconditions, loaded context, and a written plan another agent could execute.

## When to Use

Always, before beginning implementation work — never start coding from a verbal request alone. (Urgent unplanned work first goes through `create-ticket` + WoW §7 sprint recording, *then* through this skill.)

## Active Persona(s)

Software Engineer.

## Inputs

- Optional: a specific ticket ID. Otherwise the agent selects per Procedure step 2.

## Preconditions

- An active sprint exists in `delivery/CURRENT_SPRINT.md`.
- Working tree is clean and synced with the shared branch (`git pull`) so claim state is fresh. No remote configured = solo mode (`standards/GIT.md`): skip the sync, and be aware collision detection is inactive — only one agent may operate.

## Context to Load

1. `PROJECT.md`
2. `governance/WAY_OF_WORKING.md` (§7 execution rules) and `governance/DEFINITION_OF_READY.md` / `DEFINITION_OF_DONE.md`
3. `delivery/CURRENT_SPRINT.md`
4. The selected ticket file, then: ADRs it references, tickets in `depends_on`, docs in *Relevant ADRs & Documentation*
5. `standards/ENGINEERING.md`, `standards/TESTING.md`, and `standards/GIT.md` (commit lanes, branch naming)
6. `architecture/ARCHITECTURE.md` sections covering the affected components

## Procedure

1. **Refresh state:** pull latest; re-read `CURRENT_SPRINT.md` — never claim from a stale snapshot.
2. **Select:** the highest ticket in the sprint's Committed Work table that is `status: committed`, `owner: none`, with all `depends_on` tickets `done`. Prefer unblocking others (tickets that gate later work) over personal convenience. If a specific ticket was requested but ineligible, report why instead of forcing it. An `in-progress` ticket whose owner shows no related commits for 24+ hours is a stale-claim candidate — release it per WoW §7 first, never claim over it.
3. **Verify:** open the ticket; confirm frontmatter status/owner match the sprint table (mismatch → trust the ticket file, fix the table, re-select if needed). Sanity-check DoR still holds — if reality has drifted (dependency dropped, criteria now ambiguous), do NOT start: return it to `ready`/`backlog` with a Work Log entry and pick another.
4. **Claim atomically:** in ONE commit **directly on the trunk** (process lane, `standards/GIT.md`), set the ticket's `owner: <your id>`, `status: in-progress`, `updated`, AND the sprint table row; message `os: T-NNNN claimed by <id>`. Push immediately. This commit is made in the primary checkout, which stays on the trunk (`standards/GIT.md`, Working copies) — the ticket's worktree comes later, in `implement-ticket`. **If the push/commit conflicts, you lost the race: pull, discard the claim, re-select (step 2).** This is the collision-prevention mechanism — never skip the immediate push.
5. **Read the loaded context** against the ticket: note in the Work Log any contradiction between ticket, ADRs, and code *before* writing code (WoW §3 conflict handling).
6. **Write the implementation plan** as the first Work Log entry: intended approach; files/components expected to change; test plan mapping each acceptance criterion to intended tests; risks; any criterion whose verification approach is unclear (resolve or escalate *now*, not at acceptance time).

## Validation

- Ticket and sprint table agree: `in-progress`, single owner, pushed.
- Work Log contains a plan concrete enough that a different agent could implement from it.
- All `depends_on` verified `done`, not assumed.

## Outputs

A claimed ticket with a written implementation plan.

## State Changes

May modify: the ticket file (frontmatter + Work Log), `delivery/CURRENT_SPRINT.md` (owner/status cell), `product/BACKLOG.md` (status mirror). MUST NOT modify: code (yet), other tickets, governance.

## Failure / Escalation

- No eligible ticket (all claimed/blocked) → report the situation with the Blockers list; do not invent work or claim a blocked ticket.
- DoR failure discovered → return the ticket (step 3), never "fix it up" solo mid-claim: re-refinement is a separate, recorded activity.

## Example

Agent `dev-2` pulls, sees T-0031 committed/unowned with T-0033 `done`, claims it in one pushed commit, then writes: "Plan: extend `GET /invoices` with `from`/`to` query params validated as ISO dates in user TZ (per ADR-0007 date handling); changes in `api/invoices.py`, `repo/invoice_query.py`; tests: unit for range validation (AC2), integration for boundary scenario from Examples (AC1); risk: timezone edge at DST — extra test planned."
