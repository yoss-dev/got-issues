---
name: evolve-governance
description: Change the rules safely - propose and apply modifications to the Way of Working, DoR/DoD, standards, templates, or skills with explicit justification, the right approval, and a durable record.
---

# Skill: evolve-governance

## Purpose

Let the process improve deliberately: rule changes that are explicit, justified, approved by the right authority, and recorded — never opportunistic edits.

## When to Use

- A retrospective action calls for a rule/template/skill change.
- A rule proves ambiguous, contradictory, or ceremonial in practice.
- A recurring failure needs a new guard (e.g., a new DoD item), or a new repeatable activity needs a skill.

**Never** to make a specific failing or incomplete ticket pass — that is the canonical violation (WoW §15). If the trigger is "this ticket can't meet the DoD", the ticket is the problem.

## Active Persona(s)

Scrum Master (shepherd), with the owning persona of each affected document (see [PERSONAS.md — Document ownership](../../governance/PERSONAS.md)).

## Inputs

- The observed problem (with evidence: tickets, retro entries, recurring Work Log friction) and a proposed change.

## Preconditions

- The change is NOT motivated by a currently in-flight ticket's convenience. If any in-flight ticket would newly pass a gate because of this change, that is a red flag requiring explicit human approval regardless of the change's size.

## Context to Load

1. The document(s) to change, in full
2. `governance/WAY_OF_WORKING.md` (§15 and the precedence order — a change must not create contradictions upward)
3. `governance/PERSONAS.md` (ownership/approval)
4. Latest retro (is this already a recorded action?)
5. Skills that reference the changed rule (grep — skills mirror governance and must not drift)

## Procedure

1. **Write the proposal** (in the retro's Improvement actions, or the sprint Notes for mid-sprint proposals): the problem + evidence; the change (concrete wording, not "improve X"); expected improvement and how we'd notice it; affected artifacts (including every skill that references the rule); compatibility/migration (do existing tickets/sprints need touching? is a transition note needed?).
2. **Classify and route approval:**
   - *Clarity-only* (typos, broken links, wording with identical meaning): SM persona applies directly, noting "clarity-only" in the commit.
   - *Rule content change* (anything altering what is mandatory, a gate, a template's required fields, a skill's procedure): requires the owning persona AND a human's approval per WoW §15 — record who approved and when in the change log entry.
3. **Apply atomically:** the document change plus every dependent update (skills, templates, cross-references) in one coherent commit, so the rulebook is never self-contradictory between commits. Governance paths travel lane 2 (`standards/GIT.md`): a branch merged via reviewed PR where path protection exists; in solo mode, a direct commit with the approval recorded in the change log entry.
4. **Record durably:** an entry in the current retro (or, mid-sprint, the sprint Notes, folded into the next retro): date, change, reason, approver. Material precedence-affecting changes (rare) also get an ADR — cross-cutting engineering conventions meet the ADR bar.
5. **Version-stamp** significantly changed documents with a one-line changelog at the bottom where one exists, or rely on git history plus the retro record otherwise.
6. **Classify for the foundation** (project copies only): if the change is project-agnostic (would improve any project using this framework), record it in `FOUNDATION.md`'s contribution table and propose it upstream to the foundation repository; if it is project-specific, list it under *Local divergences* there instead. Skip this step when working in the foundation repository itself — there, add a `FOUNDATION.md` changelog entry and bump the version instead.

## Validation

- Post-change grep: no skill/template still describes the old rule; precedence order remains coherent; approval recorded for rule-content changes; the motivating evidence is linked, not asserted.

## Outputs

Updated governance artifact(s) + synchronized dependents + a durable, attributed record of the change.

## State Changes

May modify: `governance/*`, `standards/*`, `templates/*`, `skills/*`, and the retro/sprint record. MUST NOT modify: tickets' acceptance criteria, ADR bodies, `PROJECT.md` facts (those change via bootstrap/PO, not via process evolution).

## Failure / Escalation

- Approval unreachable → the proposal stays recorded as pending; the old rule remains in force. Rules never change by timeout.
- Two personas disagree on a change → human decides (WoW §13); both positions recorded.

## Example

Retro evidence: 3 of 5 acceptance failures this sprint were missing regression tests for adjacent behavior. Proposal: extend DoD conditional "Regression tests" to also require one adjacent-behavior regression check for changes to shared modules; affected artifacts: `DEFINITION_OF_DONE.md`, `acceptance-test/SKILL.md` step 4. Rule-content change → approved by human + QA persona owner; applied in one commit; recorded in RETRO-SPRINT-006 actions with approver and date.
