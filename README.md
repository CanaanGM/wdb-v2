# WorkoutLog

WorkoutLog is a .NET API for tracking workouts, workout blocks, plans, measurements, auth, and supporting exercise catalog data.

## Contents

- [Quick Start](#quick-start)
- [Local URLs](#local-urls)
- [HTTPS Development Certificate](#https-development-certificate)
- [Common Docker Tasks](#common-docker-tasks)
- [Configuration Notes](#configuration-notes)
- [REST And API Docs](#rest-and-api-docs)
- [MCP And LM Studio](#mcp-and-lm-studio)
- [Architecture](#architecture)
- [Persistence Layout](#persistence-layout)

## Quick Start

1. Create your local env file.

```bash
cp .env.example .env
```

PowerShell alternative:

```powershell
Copy-Item .env.example .env
```

2. Replace `AUTH_JWT_SECRET` in `.env` with your own long random secret.
3. Create and trust the local HTTPS development certificate.
4. Start PostgreSQL.

```bash
docker compose up -d database
```

5. Apply migrations.

```bash
docker compose --profile migrations run --rm --build migrator
```

6. Start the API.

```bash
docker compose up -d api
```

7. Use the API at `https://localhost:8081` or `http://localhost:8080`.

## Local URLs

- Docker HTTP: `http://localhost:8080`
- Docker HTTPS: `https://localhost:8081`
- Rider / launch profile HTTP: `http://localhost:6000`
- Rider / launch profile HTTPS: `https://localhost:6001`
- MCP endpoint: `/mcp` on either host
- OpenAPI JSON: `/openapi/v1.json` when the API is running in `Development` such as the Rider launch profiles

## HTTPS Development Certificate

The Docker API container reads the development certificate from `/https/workoutlog.pfx`.
`docker compose` resolves the host-side certificate directory in this order:

- `HTTPS_CERT_DIRECTORY`
- `HOME`
- `USERPROFILE`

If your certificate lives somewhere else, set `HTTPS_CERT_DIRECTORY` before starting the API.

### Windows (PowerShell)

```powershell
dotnet dev-certs https -ep "$env:USERPROFILE\.aspnet\https\workoutlog.pfx" -p "WorkoutLogDev!2026"
dotnet dev-certs https --trust
dotnet dev-certs https --check --trust
```

### Linux / CachyOS (zsh/bash)

Install the trust helpers once:

```bash
sudo pacman -S --needed ca-certificates openssl nss
```

Create and trust the certificate:

```bash
mkdir -p "$HOME/.aspnet/https"
dotnet dev-certs https --clean
dotnet dev-certs https -ep "$HOME/.aspnet/https/workoutlog.pfx" -p "WorkoutLogDev!2026" --trust
chmod 644 "$HOME/.aspnet/https/workoutlog.pfx"
dotnet dev-certs https --check --trust
```

If `curl` or your browser still does not trust the cert, add this to `~/.zshrc` or `~/.bashrc` and open a new shell:

```bash
export SSL_CERT_DIR="$HOME/.aspnet/dev-certs/trust:/etc/ssl/certs"
```

### Certificate Troubleshooting

Permission denied on `/https/workoutlog.pfx`:

```bash
chmod 644 "$HOME/.aspnet/https/workoutlog.pfx"
```

Password mismatch for `workoutlog.pfx`:

```bash
dotnet dev-certs https -ep "$HOME/.aspnet/https/workoutlog.pfx" -p "WorkoutLogDev!2026"
chmod 644 "$HOME/.aspnet/https/workoutlog.pfx"
```

## Common Docker Tasks

Apply current migrations:

```bash
docker compose --profile migrations run --rm --build migrator
```

Roll back all migrations to `0`:

```bash
docker compose --profile migrations run --rm --build migrator-down
```

Add a new migration:

```bash
docker compose --profile migrations run --rm --build migrator migrations add <MigrationName> --project Infrastructure/Infrastructure.csproj --startup-project Api/Api.csproj --output-dir Persistence/Migrations
```

Reset the local database from scratch:

```bash
docker compose --profile migrations run --rm --build migrator-down
docker compose down -v
docker compose up -d database
docker compose --profile migrations run --rm --build migrator
```

Stop the stack:

```bash
docker compose down
```

## Configuration Notes

- `.env.example` is the canonical list of local environment variables.
- `AUTH_JWT_SECRET` must be replaced before startup. The placeholder value fails fast in every environment, including `Development`.
- `AUTH_PASSWORD_RESET_INCLUDE_DEBUG_TOKEN=true` only affects forgot-password responses when the API is running in `Development`.
- `USER_EXERCISE_STATS_MAINTENANCE_RECOMPUTE_ALL_ON_STARTUP=true` triggers a one-time full user exercise stats rebuild on startup. Set it back to `false` after the rebuild completes.

## REST And API Docs

- REST collections overview: [Docs/Rest/README.md](Docs/Rest/README.md)
- PowerShell smoke-test examples: [Docs/Rest/PowerShell-Examples.md](Docs/Rest/PowerShell-Examples.md)
- Bruno collection: `Docs/Rest/bruno`
- Postman collection: `Docs/Rest/workoutlog.postman_collection.json`

The root README intentionally stays focused on setup and navigation now. Use the REST docs for request examples and endpoint smoke tests.

## MCP And LM Studio

The API hosts an MCP endpoint at `https://localhost:8081/mcp` or `http://localhost:8080/mcp`.

Exposed read-only tools:

- `search_exercises`
- `get_exercise_by_id`
- `get_muscles`
- `search_muscles`
- `get_muscles_by_group`
- `get_equipments`
- `search_equipments`
- `get_equipment_by_name`
- `search_workouts`
- `get_workout_by_id`
- `search_workout_blocks`
- `get_workout_block_by_id`
- `search_user_exercise_stats`

LM Studio setup:

1. Start the API.
2. Open `Program -> Install -> Edit mcp.json`.
3. Add this server entry for local dev:

```json
{
  "mcpServers": {
    "workoutlog": {
      "url": "http://localhost:8080/mcp"
    }
  }
}
```

4. Reload MCP servers or restart LM Studio.
5. Enable the `workoutlog` server in the tool UI.

## Architecture

- Controllers only handle HTTP concerns.
- The `Api` project currently contains transport and application orchestration concerns.
- Commands mutate state and queries read state.
- Handlers delegate business and persistence work to feature services.
- CQRS is wired through MediatR with shared primitives under `Api/Application/Cqrs`.
- Pipeline behaviors currently cover command logging, command transactions, and query logging.
- Input validation happens at the API boundary through request contract attributes.
- Unhandled exceptions are returned as RFC7807 problem details with trace IDs.

## Persistence Layout

- Feature-based EF configuration lives under `Infrastructure/Persistence/Features/*/Configurations`.
- Shared persistence code belongs under `Infrastructure/Persistence/Shared`.
- EF migrations live under `Infrastructure/Persistence/Migrations`.
