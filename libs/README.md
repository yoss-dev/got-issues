# libs/

Shared libraries and packages consumed by the apps — one directory per library, each with a clear owner boundary and documented public interface. A library exists because ≥2 consumers need it or an [ADR](../project-os/architecture/adr/README.md) says so — not speculatively (see [engineering standards](../project-os/standards/ENGINEERING.md)). Prune this directory during bootstrap if the project has no shared code yet.
