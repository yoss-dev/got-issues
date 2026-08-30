# Security Standards

Baseline secure-development rules, applied by every persona and owned by the Security Engineer persona. Security findings are never silently accepted: they are fixed, or ticketed with severity and a human-approved deferral.

## Universal rules

### Secrets

- No secrets, credentials, tokens, or private keys in the repository — ever, including in history, tests, fixtures, and documentation examples. Use the project's secret store / environment injection (defined below).
- A leaked secret is treated as compromised: rotate first, then clean up.

### Input & data

- All external input (users, APIs, files, queues) is validated at the boundary; injection-safe APIs (parameterized queries, safe templating) are mandatory.
- Personal data is identified in tickets during refinement (DoR conditional item), minimized, and never written to logs.
- Authentication and authorization changes always get Security-persona review during refinement and acceptance.

### Dependencies

- New dependencies are checked for maintenance health and known vulnerabilities before adoption; the check is noted in the ticket Work Log.
- Vulnerability reports from tooling are triaged, not muted: fix, upgrade, or ticket with severity.

### Design

- Least privilege for services, tokens, and CI jobs.
- Security-relevant events (authn/authz failures, privilege changes) are logged — without secrets or personal data.
- Threats are considered during refinement for any ticket touching auth, money, personal data, file handling, or external input; the DoR requires naming the concern on the ticket.

## Escalation

The Security persona escalates to a human more readily than any other persona: unclear privacy implications, potential compliance impact, or a vulnerability in production always stop autonomous work per [WoW §13](../governance/WAY_OF_WORKING.md).

## Project-specific rules

> ⚠ **Replace during `bootstrap-project`.**

- **Secret management mechanism:** *TBD* `[open]`
- **Compliance/regulatory requirements:** *TBD — GDPR, HIPAA, SOC 2, none, …* `[open]`
- **Dependency scanning tooling:** *TBD* `[open]`
- **Data classification & retention:** *TBD* `[open]`
