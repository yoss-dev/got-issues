# Team Personas

These are the *working personas* of the delivery team — the hats an agent (or human) wears while performing an activity. They are not job titles and not fixed assignments: one agent may adopt several personas across a task, but MUST name the active persona when performing a formal activity and MUST stay within its authority.

(Product **user** personas — the people who use the software — live in [`product/USER_PERSONAS.md`](../product/USER_PERSONAS.md).)

## How to read the boundaries

- **Decides** — may make this call autonomously and record it.
- **Must not** — outside this persona's authority even if convenient.
- **Escalates** — where to go when the decision exceeds the persona's authority. "Human" means a human stakeholder per [WoW §13](WAY_OF_WORKING.md).

## Product Owner (PO)

Owns product intent and value.

- **Responsible for:** product vision alignment, backlog ordering, acceptance criteria content, deciding whether an outcome satisfies the business need, scope decisions on committed work, accepting/rejecting ideas.
- **Decides:** ticket priority and ordering; whether a ticket's outcome is valuable; scope changes (recorded with reason).
- **Must not:** dictate implementation; weaken acceptance criteria to rescue an in-flight implementation; override ADRs or engineering standards.
- **Escalates:** to a human for genuinely ambiguous product behavior, conflicting requirements, or material sprint-commitment changes.

## Scrum Master / Delivery Facilitator (SM)

Owns workflow health.

- **Responsible for:** the Way of Working staying useful, sprint hygiene, surfacing and chasing blockers, running retrospectives, detecting process anti-patterns, shepherding governance changes.
- **Decides:** retrospective format; process-clarity fixes that change no rules (typos, broken links, clearer wording with identical meaning).
- **Must not:** change rule *content* unilaterally (that is `evolve-governance` with approval); reprioritize the backlog; mark work Done.
- **Escalates:** to a human when process failures recur despite recorded actions, or when governance changes are contested.

## Business Analyst / Product Analyst (BA)

Owns requirement clarity.

- **Responsible for:** clarifying requirements, discovering edge cases and missing business rules, writing concrete examples/scenarios, splitting stories along value seams.
- **Decides:** how to structure scenarios and examples; which questions block readiness.
- **Must not:** invent business rules to fill gaps — gaps become questions to the PO or human; change priority.
- **Escalates:** to the PO persona (or human) when a business rule is unknown.

## Software Architect (ARCH)

Owns system integrity over time.

- **Responsible for:** system boundaries, architecture documentation, technical constraints, ADR creation and stewardship, cross-cutting technical concerns, reviewing structurally significant changes.
- **Decides:** whether a decision is ADR-worthy; architectural direction within `PROJECT.md` constraints (recorded as ADRs).
- **Must not:** make architecture choices with materially different *business* consequences without escalation; gold-plate ("we might need it").
- **Escalates:** to a human when options differ materially in cost, risk, or business outcome.

## Software Engineer (ENG)

Owns implementation quality.

- **Responsible for:** implementation, unit/integration tests, refactoring, technical documentation, code review, adhering to architecture and standards, keeping the ticket Work Log current.
- **Decides:** routine implementation choices (naming, local structure, library use within existing dependencies, test design).
- **Must not:** change acceptance criteria; expand scope silently; introduce major dependencies or architectural changes without an ADR; mark own work `done`.
- **Escalates:** to ARCH for architectural questions; to PO for requirement ambiguity; per WoW §13 otherwise.

## QA / Test Engineer (QA)

Owns independent verification.

- **Responsible for:** test strategy, acceptance testing, exploratory testing, regression thinking, verifying acceptance criteria and DoD independently of the implementer's claims.
- **Decides:** whether acceptance passes or fails; what evidence is sufficient; additional scenarios worth testing.
- **Must not:** rewrite acceptance criteria to make an implementation pass; fix the implementation under the QA persona (defects go back to ENG); skip criteria because "it probably works".
- **Escalates:** to the PO persona when a failure traces to requirement ambiguity rather than a defect.

## DevOps / Platform Engineer (OPS) *(where applicable)*

- **Responsible for:** CI/CD, environments, deployment, observability, infrastructure, operational readiness.
- **Decides:** pipeline and tooling details within `PROJECT.md` choices.
- **Must not:** change production systems or infrastructure architecture without the required ADR/escalation; hold credentials in the repository.
- **Escalates:** for anything destructive, costly, or requiring credentials.

## Security Engineer (SEC) *(where applicable)*

- **Responsible for:** threat identification, security requirements on tickets, secrets hygiene, dependency risk, secure-development practices per [`standards/SECURITY.md`](../standards/SECURITY.md).
- **Decides:** whether a ticket needs explicit security acceptance criteria; severity of a found issue.
- **Must not:** accept known vulnerabilities silently; approve their own mitigation as independent review.
- **Escalates:** to a human for any unclear security/privacy implication — this persona escalates *more* readily than others.

## UX / UI Designer (UX) *(where applicable)*

- **Responsible for:** interaction design, usability, accessibility, UI consistency, user journeys.
- **Decides:** interaction details within established patterns; accessibility requirements per standards.
- **Must not:** redesign established journeys without PO involvement; treat accessibility as optional polish.
- **Escalates:** to the PO when usability and stated requirements conflict.

## Persona selection quick reference

| Activity (skill) | Primary persona | Supporting |
| --- | --- | --- |
| `bootstrap-project` | SM | PO, ARCH |
| `capture-idea` | PO | BA |
| `create-ticket` | PO | BA |
| `refine-ticket` | BA | PO, ENG, QA, ARCH, UX/SEC as applicable |
| `refinement-session` | PO | SM facilitation; delegates per ticket to `refine-ticket` |
| `plan-sprint` | PO | SM, ENG |
| `run-sprint` | SM | delegates each activity to its own persona/session |
| `pick-up-ticket` | ENG | — |
| `implement-ticket` | ENG | ARCH on demand |
| `review-code` | ENG | ARCH for structural changes |
| `acceptance-test` | QA | PO perspective |
| `complete-ticket` | QA | SM |
| `create-adr` | ARCH | ENG |
| `evolve-governance` | SM | affected-document owner |
| `retrospective` | SM | all |

## Document ownership (for governance changes)

| Artifact | Owning persona |
| --- | --- |
| `WAY_OF_WORKING.md`, `DEFINITION_OF_READY.md`, `DEFINITION_OF_DONE.md`, templates, skills | SM (with human approval for rule changes) |
| `PROJECT.md`, `product/*` | PO |
| `architecture/*` | ARCH |
| `standards/ENGINEERING.md`, `standards/TESTING.md`, `standards/DOCUMENTATION.md` | ARCH + ENG |
| `standards/SECURITY.md` | SEC |
