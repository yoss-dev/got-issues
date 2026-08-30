# Ideas

Raw product and engineering ideas, captured via [`capture-idea`](../skills/capture-idea/SKILL.md) before any commitment is made. An idea is **not** a ticket: it carries no priority, no acceptance criteria, and no promise. Ideas graduate to tickets via [`create-ticket`](../skills/create-ticket/SKILL.md) when the Product Owner persona judges them worth working on; otherwise they are rejected with a one-line reason (kept, for institutional memory).

**Conventions:** newest first; IDs `IDEA-NNN`, stable, never reused; format per [`templates/IDEA_TEMPLATE.md`](../templates/IDEA_TEMPLATE.md); when promoted or rejected, update the idea's status line in place and link the ticket if one was created.

---

## IDEA-004: Authenticate users and machine clients, and authorise their access

- **Status:** captured — deliberately not promoted while `PROJECT.md` Q7 (the global role set) is unanswered; a ticket for it could not reach Ready
- **Date / Source:** 2026-08-30 — maintainer, during `bootstrap-project` (primary use case 4, `PROJECT.md` §2)
- **Idea:** A user or a machine client can authenticate, and the API decides what they are allowed to do based on their global role.
- **Motivation:** An internal tool holding the company's work needs to know who is calling. Machine clients are first-class here, not an afterthought — internal automation is half the point of an API-first tracker.
- **Possible value:** Priya's automation can act against the API without a human in the loop; the company's own work stays visible only to the company.
- **Unresolved questions:** Which global roles exist and what may each do (`PROJECT.md` Q7)? How do users get into the system in the first place — self-service, seeded, or synced from an existing company directory? Do machine clients get roles, or only scopes?

## IDEA-003: Discuss an issue through comments

- **Status:** promoted → [T-0008](tickets/T-0008-comment-on-an-issue.md)
- **Date / Source:** 2026-08-30 — maintainer, during `bootstrap-project` (primary use case 3, `PROJECT.md` §2)
- **Idea:** A user can comment on an issue, forming its discussion thread.
- **Motivation:** The conversation around a piece of work is usually where the reasoning lives; losing it means losing why something was decided.
- **Possible value:** Sam keeps the decision history attached to the work itself rather than in chat. Automation (Priya) can post context — a failing CI run, a linked deployment — onto the issue it concerns.
- **Unresolved questions:** Editing and deleting — allowed, and by whom? Ordering and pagination for long threads? Any formatting (plain text, Markdown)? Mentions of other users, given there are no notifications (`PROJECT.md` §3 non-goal)?

## IDEA-002: Track an issue's lifecycle — type, status, priority, assignee

- **Status:** promoted → [T-0006](tickets/T-0006-issue-lifecycle-fields.md), [T-0007](tickets/T-0007-list-and-filter-issues.md)
- **Date / Source:** 2026-08-30 — maintainer, during `bootstrap-project` (primary use case 2, `PROJECT.md` §2)
- **Idea:** An issue carries a type, a status, a priority, and an assignee, and these change as work progresses.
- **Motivation:** This is what separates a tracker from a list: knowing what state work is in and who holds it.
- **Possible value:** Sam can see what is in flight and what is next; the team can answer "who has this?" without asking.
- **Unresolved questions:** Which statuses, types, and priorities — a fixed set for now? Configurable workflows and validated transitions are explicitly a *later* goal (`PROJECT.md` §3), so what does the first version do when an issue moves from any state to any other? Can an issue be unassigned? Is assignment history worth keeping?

## IDEA-001: Organise work as projects containing issues

- **Status:** promoted → [T-0004](tickets/T-0004-create-and-list-projects.md), [T-0005](tickets/T-0005-create-and-read-issues.md)
- **Date / Source:** 2026-08-30 — maintainer, during `bootstrap-project` (primary use case 1, `PROJECT.md` §2)
- **Idea:** Create and organise projects, and create issues within them.
- **Motivation:** The foundational structure of the product — everything else hangs off projects and issues. Without it there is nothing to track.
- **Possible value:** Sam gets structure that survives work spanning several projects, which a flat list does not provide. Everything in the tracker becomes addressable by an internal tool.
- **Unresolved questions:** Do projects have keys (Jira-style `PROJ-123` issue identifiers), and are issue numbers per-project or global? Who may create a project — everyone, or a specific global role (`PROJECT.md` Q7)? Can projects be archived or deleted, and what happens to their issues? Note: roles are global, so there is no project membership concept to lean on.
