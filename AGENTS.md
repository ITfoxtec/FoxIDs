# Repository Guidelines

## Project Structure & Module Organization
`FoxIDs.sln` builds the .NET 10 stack: `src/FoxIDs` is the identity runtime, `src/FoxIDs.Control` exposes the admin UI/API, shared contracts live in `src/FoxIDs.Shared*`, cross-cutting constants sit in `src/FoxIDs.SharedBase/Constants.cs`, and the generated client resides in `src/FoxIDs.ControlClient`. Tests mirror this layout in `test/FoxIDs.UnitTests` (logic) and `test/FoxIDs.IntegrationTests` (protocol/storage). Reference docs live in `docs/`, data seeds in `data/`, helper CLIs in `tools/`, and deploy assets sit under `Docker/` plus `Kubernetes/`.

## Build, Test, and Development Commands
- `dotnet restore FoxIDs.sln` – sync NuGet state before switching branches.
- `dotnet build FoxIDs.sln -c Release` – reproduce the CI build.
- `dotnet watch run --project src/FoxIDs/FoxIDs.csproj --launch-profile Development` – hot-reload the identity runtime.
- `dotnet run --project src/FoxIDs.Control/FoxIDs.Control.csproj --launch-profile Development` – host the control plane UI/API.
- `dotnet test FoxIDs.sln --filter FullyQualifiedName~Unit` – run the fast xUnit suite.
- `dotnet test test/FoxIDs.IntegrationTests/FoxIDs.IntegrationTests.csproj --logger "trx;LogFileName=Integration.trx"` – execute the embedded Postgres integration checks.
- `docker compose -f Docker/docker-compose.development-https.yaml up --build` – bring up FoxIDs + Control with TLS for end-to-end testing.

## Coding Style & Naming Conventions
Stick to 4-space indentation, file-scoped namespaces, and expression-bodied members when they clarify intent. Use PascalCase for public types/members, camelCase for locals/parameters, and `_camelCase` for private readonly fields. Keep services small, constructor-inject dependencies, and place shared DTOs/contracts in the nearest `*.Shared` project. Run `dotnet format` (or VS equivalent) on touched files.

## Testing Guidelines
xUnit + Moq power both suites, while `MysticMind.PostgresEmbed` lets integration tests exercise persistence and token-bridge flows. Name tests `Method_State_Result` to aid filtering. Every change in `src/FoxIDs*` should include at least one unit test; storage or protocol edits need a companion integration spec plus updated fixtures in `data/`.

## Security & Configuration Tips
`appsettings.json` is illustrative—real secrets should come from environment variables or Azure Key Vault via `Azure.Extensions.AspNetCore.Configuration.Secrets`. Use `appsettings.Development.json` (gitignored) for local tweaks. When using Docker, supply `Settings__FoxIDsEndpoint` and certificate paths via a `.env`; keep the same values synchronized with `Kubernetes/*.yaml` secrets and rotate certificates before committing new material under `data/`.
