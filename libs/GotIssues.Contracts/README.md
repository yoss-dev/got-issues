# GotIssues.Contracts - ASP.NET Core 8.0 Server

A self-hosted, API-first issue and task tracker for internal engineering work.

This document is the product's user-facing documentation as well as its
contract: every operation and schema is described for a reader who has never
seen the implementation.


## Upgrade NuGet Packages

NuGet packages get frequently updated.

To upgrade this solution to the latest version of all NuGet packages, use the dotnet-outdated tool.


Install dotnet-outdated tool:

```
dotnet tool install --global dotnet-outdated-tool
```

Upgrade only to new minor versions of packages

```
dotnet outdated --upgrade --version-lock Major
```

Upgrade to all new versions of packages (more likely to include breaking API changes)

```
dotnet outdated --upgrade
```


## Run

Linux/OS X:

```
sh build.sh
```

Windows:

```
build.bat
```
