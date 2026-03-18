# DevSecOpsProject

## Overview

This repository is a small end‑to‑end **DevSecOps demo** built around a .NET 9 **minimal API**. It shows how to:

- Build and run a minimal API locally and in Docker
- Apply code quality, security, and dependency checks with GitHub Actions
- Build, scan, and push container images
- Orchestrate everything via a DevSecOps pipeline workflow

## Solution Structure

- `**DevSecOpsProject.sln`** – Root solution file.
- `**MinimalApi/**` – ASP.NET Core minimal API project.
  - `Program.cs` – All endpoints and middleware (top‑level statements).
    - `GET /weatherforecast` – Sample weather endpoint.
    - `GET /todos`, `GET /todos/{id}`, `POST /todos`, `PUT /todos/{id}`, `DELETE /todos/{id}` – In‑memory Todo CRUD API.
    - Swagger/OpenAPI is enabled in development.
  - `MinimalApi.csproj` – .NET 9 web SDK project with:
    - `Microsoft.AspNetCore.OpenApi`
    - `Swashbuckle.AspNetCore` for Swagger UI
  - `Dockerfile` – Single‑stage Dockerfile that restores, publishes, and runs the app.
  - `docker-compose.yml` – Optional local orchestration (if you add services later).
  - `appsettings*.json` – Environment‑specific configuration.
- `**.github/workflows/**` – Reusable GitHub Actions workflows composing the DevSecOps pipeline.

## Application Architecture

The API uses **ASP.NET Core minimal APIs** with:

- **Top‑level program** in `Program.cs` (no separate `Startup` class).
- **Swagger/OpenAPI**:
  - `AddEndpointsApiExplorer()` + `AddSwaggerGen(...)` during service registration.
  - `UseSwagger()` + `UseSwaggerUI(...)` in the HTTP pipeline (development environment).
- **Endpoints**:
  - Weather endpoint returns a fixed 5‑day forecast using an internal `WeatherForecast` record.
  - Todo endpoints use an in‑memory `List<TodoItem>` as a simple data store.
- **Records / Models**:
  - `TodoItem` – `Id`, `Title`, `IsDone`.
  - `WeatherForecast` – `Date`, `TemperatureC`, `Summary`, calculated `TemperatureF`.

There is intentionally **no database** in this demo; it focuses on CI/CD and security practices rather than persistence.

## Container Architecture

The `MinimalApi/Dockerfile` is a single‑stage build that uses the .NET 9 SDK image:

- `FROM mcr.microsoft.com/dotnet/sdk:9.0`
- `WORKDIR /app` + `COPY . .` (the build context is the repo root when used from CI).
- `RUN dotnet restore "MinimalApi/MinimalApi.csproj" && dotnet publish ... -o /app/publish`
- `EXPOSE 8080` and `ASPNETCORE_URLS=http://0.0.0.0:8080` so the app is reachable from outside the container.
- `ENTRYPOINT ["dotnet", "/app/publish/MinimalApi.dll"]`

### Local Docker usage

From the repository root:

```bash
docker build -t minimalapi -f MinimalApi/Dockerfile .
docker run -p 8080:8080 minimalapi
```

Browse to `http://localhost:8080` for Swagger UI and to exercise the endpoints.

## GitHub Actions & DevSecOps Pipeline

All workflows are defined as **reusable workflows** (triggered by `workflow_call`) under `.github/workflows/`. A higher‑level pipeline (e.g. `devsecops-pipeline.yml`) can call these building blocks.

### Code Quality (`code-quality.yml`)

Runs static checks against the .NET solution:

- Setup .NET 9 SDK.
- `dotnet restore` – restore dependencies.
- `dotnet build -c Release --no-restore` – build with analyzers.
- `dotnet format --verify-no-changes --no-restore` – verify code style/formatting.
- `dotnet list package --vulnerable --include-transitive` – basic dependency vulnerability check.

### Tests (`tests.yml`)

- Setup .NET 9 SDK.
- `dotnet restore`.
- `dotnet test` – run all test projects in the solution.

### Dependency Scan (`dependency-scan.yml`)

- Setup .NET 9 SDK.
- `dotnet restore`.
- `dotnet list package --vulnerable --include-transitive` – focused dependency vulnerability report.

### Docker Lint (`docker-lint.yml`)

- Uses `hadolint/hadolint-action` to lint `MinimalApi/Dockerfile` for best practices (layer usage, pinned tags, etc.).

### Secrets Scan (`secrets-scan.yml`)

- Uses `gitleaks/gitleaks-action` to scan the repository for hard‑coded secrets using `GITHUB_TOKEN` and (optionally) a `GITLEAKS_LICENSE` secret.

### Docker Build & Push (`docker-build-push.yml`)

- Logs in to Docker Hub using `DOCKERHUB_USER` (repo variable) and `DOCKERHUB_TOKEN` (secret).
- Builds the `MinimalApi/Dockerfile` from the repo root context.
- Pushes the image as `${DOCKERHUB_USER}/minimal-api:latest`.

### Image Scan (`image-scan.yml`)

- Uses `aquasecurity/trivy-action@v0.35.0` to scan the pushed Docker image.
- Scans both **OS** and **library** vulnerabilities with severities `CRITICAL,HIGH`.
- Fails the workflow on findings (`exit-code: '1'`).

### Deployment (`deploy-to-server.yml`)

The deployment workflow (high‑level outline, exact steps depend on your server setup):

- Checks out code and pulls the published container image.
- Connects to the target server (for example via SSH) using configured secrets.
- Deploys or updates the running container (e.g. `docker pull` + `docker run` / `docker compose up -d`).

## DevSecOps Pipeline Orchestration

The `devsecops-pipeline.yml` workflow (or an equivalent orchestrator) can combine these reusable workflows into a single pipeline for each push/PR, typically in this order:

1. **Code quality & tests** – `code-quality.yml` + `tests.yml`.
2. **Security checks** – `dependency-scan.yml` + `secrets-scan.yml`.
3. **Container checks** – `docker-lint.yml` + `docker-build-push.yml` + `image-scan.yml`.
4. **Deploy** – `deploy-to-server.yml` (often only on `main` branch or tagged releases).

This structure keeps each concern isolated but composable, making it easy to reuse and evolve the pipeline over time.

## Local Development

- **Run the API locally**:
  ```bash
  cd MinimalApi
  dotnet restore
  dotnet run
  ```
  The app will print the listening URL (typically `https://localhost:5073` for dev).
- **Run tests locally** (if you add test projects):
  ```bash
  dotnet test
  ```

## Future Extensions

- Replace the in‑memory Todo store with a real database (SQL, PostgreSQL, etc.).
- Add authentication/authorization to the API.
- Introduce integration tests and performance tests into the pipeline.
- Harden Docker images further (non‑root user, slimmer runtime image, pinned tags).

This README should give you and collaborators a clear high‑level understanding of how the API, containers, and DevSecOps workflows fit together in this project.