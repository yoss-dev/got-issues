# Build stage
FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src

COPY apps/GotIssues.IdentityHost/GotIssues.IdentityHost.csproj apps/GotIssues.IdentityHost/packages.lock.json apps/GotIssues.IdentityHost/
RUN dotnet restore apps/GotIssues.IdentityHost/GotIssues.IdentityHost.csproj --locked-mode

# The root .editorconfig carries the generated-code exclusions for EF migrations.
# Without it the container build applies analyzers to generated files and fails
# where the host build succeeds — a difference that only shows up in Docker.
COPY .editorconfig ./
COPY apps/GotIssues.IdentityHost/ apps/GotIssues.IdentityHost/
RUN dotnet publish apps/GotIssues.IdentityHost/GotIssues.IdentityHost.csproj -c Release -o /app/publish

# Runtime stage
FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS runtime
WORKDIR /app

# curl is here for the container health check only.
RUN apt-get update \
    && apt-get install --no-install-recommends -y curl \
    && rm -rf /var/lib/apt/lists/*

COPY --from=build /app/publish .

# The signing key lives on a mounted volume so tokens survive a restart. Created
# writable for the non-root user the image runs as.
RUN mkdir -p /app/keys && chown -R $APP_UID:$APP_UID /app/keys

USER $APP_UID
EXPOSE 8080
ENTRYPOINT ["dotnet", "GotIssues.IdentityHost.dll"]
