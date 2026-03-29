# WorkoutLog

## API

### Architecture (CQRS)

- Controllers only handle HTTP concerns.
- `Commands` mutate state, `Queries` read state.
- Handlers delegate data/business logic to feature services.
- CQRS is wired through MediatR (`ISender` + handlers) with shared primitives under `Api/Application/Cqrs`.
- MediatR pipeline behaviors:
  - command logging
  - command transaction
  - query logging
- Input validation happens at the API boundary through request contract attributes.
- Unhandled exceptions are returned as RFC7807 problem details with trace IDs.

### Persistence Layout

- Feature-based persistence config lives under:
  - `Infrastructure/Persistence/Features/Exercises/Configurations`
  - `Infrastructure/Persistence/Features/Muscles/Configurations`
- Shared cross-feature persistence code (if needed) lives under:
  - `Infrastructure/Persistence/Shared`
- Migrations remain under:
  - `Infrastructure/Persistence/Migrations`

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
docker compose --profile migrations run --rm --build migrator
```

```powershell
docker compose --profile migrations run --rm --build migrator-down
```

```powershell
docker compose --profile migrations run --rm --build migrator migrations add <MigrationName> --project Infrastructure/Infrastructure.csproj --startup-project Api/Api.csproj --output-dir Persistence/Migrations
```

### Reset DB schema safely (PowerShell)

```powershell
# roll back all EF migrations to 0
docker compose --profile migrations run --rm --build migrator-down

# apply current migration set from scratch
docker compose --profile migrations run --rm --build migrator
```

```powershell
# optional hard reset: remove postgres volume data too
docker compose down -v
docker compose up -d database
docker compose --profile migrations run --rm --build migrator
```

### Full local Docker flow (PowerShell)

```powershell
# first-time setup (or when env changes)
Copy-Item .env.example .env -Force

# start database
docker compose up -d database

# apply migrations
docker compose --profile migrations run --rm --build migrator

# start API
docker compose up -d api
```

```powershell
# stop everything
docker compose down
```

### Muscle API quick test (PowerShell)

```powershell
# Create muscles first (required before exercise creation)
$musclesBody = @'
[
  {
    "name": "Gastrocnemius",
    "muscleGroup": "calves",
    "function": "Plantar flexes the ankle and flexes the knee.",
    "wikiPageUrl": "https://en.wikipedia.org/wiki/Gastrocnemius_muscle"
  },
  {
    "name": "Rectus Femoris",
    "muscleGroup": "quadriceps",
    "function": "Flexes the hip and extends the knee.",
    "wikiPageUrl": "https://en.wikipedia.org/wiki/Rectus_femoris_muscle"
  }
]
'@

Invoke-RestMethod -Method Post `
  -Uri "https://localhost:8081/api/muscles/bulk" `
  -SkipCertificateCheck `
  -ContentType "application/json" `
  -Body $musclesBody
```

```powershell
# Get all muscles
Invoke-RestMethod -Method Get `
  -Uri "https://localhost:8081/api/muscles" `
  -SkipCertificateCheck
```

### Exercise API quick test (PowerShell)

```powershell
# requires existing muscles
$body = @{
  name = "Push-up"
  description = "Upper body exercise"
  howTo = "Keep your core tight and lower until elbows hit ~90 degrees."
  difficulty = 2
  exerciseMuscles = @(
    @{ muscleName = "Gastrocnemius"; isPrimary = $false }
  )
  howTos = @(
    @{ name = "Video"; url = "https://example.com/push-up-video" },
    @{ name = "Written guide"; url = "https://example.com/push-up-guide" }
  )
} | ConvertTo-Json -Depth 6

Invoke-RestMethod -Method Post `
  -Uri "https://localhost:8081/api/exercises" `
  -SkipCertificateCheck `
  -ContentType "application/json" `
  -Body $body
```

```powershell
# Search exercises (paged + filtered) via body
# body fields:
# - pageNumber (>=1)
# - pageSize (1..100)
# - search (name/description contains)
# - difficulty (0..5)
# - muscleName
# - muscleGroup
# - isPrimary (true/false)
$searchBody = @{
  pageNumber = 1
  pageSize = 20
  search = "squat"
  difficulty = 3
  muscleGroup = "quadriceps"
  isPrimary = $true
} | ConvertTo-Json -Depth 4

Invoke-RestMethod -Method Post `
  -Uri "https://localhost:8081/api/exercises/search" `
  -SkipCertificateCheck `
  -ContentType "application/json" `
  -Body $searchBody
```

```powershell
# Get one exercise by id
Invoke-RestMethod -Method Get `
  -Uri "https://localhost:8081/api/exercises/1" `
  -SkipCertificateCheck
```

```powershell
# Bulk create exercises (extra legacy fields are ignored if sent)
$bulkBody = @'
[
  {
    "name": "Rope Jumping",
    "description": "Cardio rope exercise.",
    "howTo": "Jump rope with small controlled hops.",
    "difficulty": 2,
    "trainingTypes": ["Cardiovascular"],
    "exerciseMuscles": [{ "muscleName": "Gastrocnemius", "isPrimary": true }],
    "howTos": [{ "name": "youtube", "url": "https://example.com/rope-jump" }]
  },
  {
    "name": "High Bar Squat",
    "description": "Squat with high bar placement.",
    "howTo": "Keep torso upright and drive through mid-foot.",
    "difficulty": 3,
    "exerciseMuscles": [{ "muscleName": "Rectus Femoris", "isPrimary": true }],
    "howTos": []
  }
]
'@

Invoke-RestMethod -Method Post `
  -Uri "https://localhost:8081/api/exercises/bulk" `
  -SkipCertificateCheck `
  -ContentType "application/json" `
  -Body $bulkBody
```
