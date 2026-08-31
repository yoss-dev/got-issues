#!/usr/bin/env bash
#
# Regenerates server contracts and clients from spec/openapi.yaml.
#
# The specification is the source of truth (ADR-0004). Everything this script
# writes under libs/ is generated output: never hand-edit it, change the spec and
# re-run this instead. The generated code is committed so that drift shows up as a
# reviewable diff.
#
# Runs the generator from a pinned container image rather than a host JDK, so the
# only prerequisites are Docker and the .NET SDK. The first run pulls ~1 GB and can
# take several minutes — that is a slow pull, not a hang.
set -euo pipefail

# Pinned deliberately. An unpinned generator turns an unrelated upstream release
# into a repository-wide diff (ADR-0004, Risks).
GENERATOR_VERSION="v7.18.0"
GENERATOR_IMAGE="openapitools/openapi-generator-cli:${GENERATOR_VERSION}"

REPO_ROOT="$(cd "$(dirname "${BASH_SOURCE[0]}")/.." && pwd)"
SPEC="spec/openapi.yaml"

# The whole option string is load-bearing and was established by T-0011.
#
#   operationIsAsync alone makes the method body async while leaving the return
#   type IActionResult; operationResultTask is what produces Task<IActionResult>.
#   Missing that pairing is what made the spike's first verdict wrong.
#
#   buildTarget=library is NOT cosmetic: without it the generator emits
#   `public abstract async Task<...>`, which is invalid C# (CS1994).
#
#   useNewtonsoft=false removes the MVC Newtonsoft package but NOT the transitive
#   Newtonsoft that JsonSubTypes drags in — that is pinned in
#   libs/Directory.Build.targets instead.
SERVER_PROPS="packageGuid={4E1B6D6F-9B3A-4F2E-A1C7-0D5E8F2A3B41},aspnetCoreVersion=8.0,buildTarget=library,classModifier=abstract,operationModifier=abstract,operationIsAsync=true,operationResultTask=true,nullableReferenceTypes=true,useSwashbuckle=false,useNewtonsoft=false,packageName=GotIssues.Contracts"

# generichost is the default library and the one that uses System.Text.Json:
# async methods with CancellationToken, and no Newtonsoft anywhere.
CLIENT_PROPS="packageGuid={7C2A9E14-5D83-4B67-9F0A-2E6C1B8D4A93},library=generichost,targetFramework=net8.0,packageName=GotIssues.Client"

run_generator() {
  docker run --rm \
    -u "$(id -u):$(id -g)" \
    -v "${REPO_ROOT}:/local" \
    "${GENERATOR_IMAGE}" \
    generate -i "/local/${SPEC}" "$@"
}

echo "Generating server contracts (aspnetcore) …"
rm -rf "${REPO_ROOT}/libs/GotIssues.Contracts"
run_generator -g aspnetcore -o /local/libs/GotIssues.Contracts --additional-properties="${SERVER_PROPS}"

echo "Generating client (csharp, generichost) …"
rm -rf "${REPO_ROOT}/libs/GotIssues.Client"
run_generator -g csharp -o /local/libs/GotIssues.Client --additional-properties="${CLIENT_PROPS}"

# --- Determinism ------------------------------------------------------------
# The generator emits fresh random identifiers on every run. Left alone, the drift
# check in tools/check-drift.sh would be permanently red and would be ignored or
# switched off within a day — which would silently remove the merge gate that makes
# the contract-first rule real. Everything below is deterministic post-processing
# performed by this script; none of it is hand-editing generated output.
#
# packageGuid is pinned above via --additional-properties. Two things it does not
# cover:

# 1. The target framework. The aspnetcore templates cap at ASP.NET Core 8.0 and
#    hard-code net8.0 into the generated .csproj. A Directory.Build.props loses to
#    the project's own PropertyGroup, and a Directory.Build.targets — which was the
#    first attempt — is imported too late to change the framework the build actually
#    uses: it makes `-getProperty:TargetFramework` report net10.0 while the compiler
#    still emits net8.0, which is a trap, not a fix. Rewriting the generated file
#    here is the only mechanism that works, and it belongs to the script that owns
#    the file.
find "${REPO_ROOT}/libs" -name "*.csproj" -exec \
  sed -i.bak 's|<TargetFramework>net8\.0</TargetFramework>|<TargetFramework>net10.0</TargetFramework>|' {} \;

# 2. UserSecretsId, a fresh GUID per run. The generated contract library has no use
#    for user secrets at all, so the line is removed rather than pinned.
find "${REPO_ROOT}/libs" -name "*.csproj" -exec sed -i.bak '/<UserSecretsId>/d' {} \;
find "${REPO_ROOT}/libs" -name "*.csproj.bak" -delete

# 3. Per-project .sln files. This repository has its own solution; these are unused,
#    and they carry generated GUIDs that change every run.
find "${REPO_ROOT}/libs" -maxdepth 2 -name "*.sln" -delete

# The csharp generator also emits a test project scaffold. It tests the generator's
# own output, which the project deliberately does not do (TESTING.md), so it is
# removed here rather than committed as dead weight.
rm -rf "${REPO_ROOT}/libs/GotIssues.Client/src/GotIssues.Client.Test"

echo "Done. Generated output is under libs/ — do not hand-edit it."
