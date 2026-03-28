# WorkoutLog

## API

### HTTPS development certificate (PowerShell)

Run these commands on each development machine:

```powershell
dotnet dev-certs https -ep "$env:USERPROFILE\.aspnet\https\workoutlog.pfx" -p "WorkoutLogDev!2026"
dotnet dev-certs https --trust
```

### Run EF migrations in Docker (PowerShell)

```powershell
Copy-Item .env.example .env
docker compose up -d database
docker compose --profile migrations run --rm migrator
```

```powershell
docker compose --profile migrations run --rm migrator migrations add <MigrationName> --project Infrastructure/Infrastructure.csproj --startup-project Api/Api.csproj --output-dir Persistence/Migrations
```

### Full local Docker flow (PowerShell)

```powershell
# first-time setup (or when env changes)
Copy-Item .env.example .env -Force

# start database
docker compose up -d database

# apply migrations
docker compose --profile migrations run --rm migrator

# start API
docker compose up -d api
```

```powershell
# stop everything
docker compose down
```
