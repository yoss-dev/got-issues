# Build stage
FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src

# The lock file is copied with the project file so --locked-mode can fail the
# build if the container would resolve anything the dev build did not (PROJECT.md §5).
COPY apps/GotIssues.Api/GotIssues.Api.csproj apps/GotIssues.Api/packages.lock.json apps/GotIssues.Api/
RUN dotnet restore apps/GotIssues.Api/GotIssues.Api.csproj --locked-mode

COPY apps/GotIssues.Api/ apps/GotIssues.Api/
RUN dotnet publish apps/GotIssues.Api/GotIssues.Api.csproj -c Release -o /app/publish

# Runtime stage
FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS runtime
WORKDIR /app

# curl is here for the container health check only. Without it Compose cannot probe
# /health, and T-0001 AC1 ("every service reports healthy") is unverifiable.
RUN apt-get update \
    && apt-get install --no-install-recommends -y curl \
    && rm -rf /var/lib/apt/lists/*

COPY --from=build /app/publish .

# Least privilege (SECURITY.md): drop to the image's non-root user. This must come
# after apt-get, which needs root.
USER $APP_UID

EXPOSE 8080
ENTRYPOINT ["dotnet", "GotIssues.Api.dll"]
