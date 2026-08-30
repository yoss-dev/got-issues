---
name: capture-idea
description: Record a raw product or engineering idea in product/IDEAS.md faithfully and cheaply, without refining it or committing to it.
---

# Skill: capture-idea

## Purpose

Preserve a raw idea — its motivation, possible value, source, and open questions — so it isn't lost, **without** prematurely turning it into work. Capturing is cheap; commitment happens later via `create-ticket`.

## When to Use

Whenever a human or agent surfaces a product/engineering thought that is not yet understood well enough to be a ticket: user feedback, a retro insight, an incident observation, a "what if".

## Active Persona(s)

Product Owner (with Business Analyst support for phrasing questions).

## Inputs

- The idea, in whatever form it arrived (message, note, observation).

## Preconditions

- None. This skill is deliberately the lowest-friction entry point in the framework.

## Context to Load

1. `product/IDEAS.md` (existing ideas — check for duplicates; and the next `IDEA-NNN` number)
2. `templates/IDEA_TEMPLATE.md`
3. `product/PRODUCT_VISION.md` (only to note obvious vision conflicts — not to filter)

## Procedure

1. Check `IDEAS.md` for an existing similar idea. If found, enrich that entry (add the new source/motivation) instead of duplicating.
2. Assign the next `IDEA-NNN` and fill the template: idea (in the originator's terms — don't editorialize), motivation, possible value (hypothesis language), source and date, unresolved questions.
3. **Do not** add acceptance criteria, sizing, technical design, or priority. If the originator supplied solution details, record them under the idea verbatim as *originator's suggestion*, not as the idea's definition.
4. If the idea plainly conflicts with `PRODUCT_VISION.md` non-goals, still capture it, and note the conflict as an unresolved question — rejection is a PO/human decision, not a capture-time filter.
5. Insert the entry at the top of the ideas list (newest first).

## Validation

- Entry follows the template; ID is unique; unresolved questions are real questions, not placeholders.
- Nothing about the entry implies commitment (no priority, no "we will").

## Outputs

One new (or enriched) idea entry in `product/IDEAS.md`.

## State Changes

May modify: `product/IDEAS.md` only.

## Failure / Escalation

- If the "idea" is actually an urgent defect report, capture it AND immediately flag that it should go through bug ticketing instead (WoW §7, DoR exceptions).

## Example

A user complains: "I can never find last month's invoices." Captured as `IDEA-007: Invoice search/filtering` — motivation: recurring support complaint (3 tickets in June); possible value: reduced support load, faster self-service; unresolved questions: search by date only, or amount/customer too? How many invoices does a heavy user have? Status: captured.
