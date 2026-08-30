# tools/

Developer tooling: scripts, generators, local-dev helpers, CI utilities. Anything here is versioned, reviewed, and documented like production code — a script run by the team is part of the product. Automation candidates identified in retrospectives ([WoW principle 6](../project-os/governance/WAY_OF_WORKING.md)) usually land here.

The foundation ships one tool: [`validate-project-os/validate.py`](validate-project-os/validate.py), a stdlib-only consistency checker for the framework's state (tickets ↔ backlog ↔ sprint ↔ ADR index, plus link integrity). Run it before process-state pushes and in CI: `python3 tools/validate-project-os/validate.py`.
