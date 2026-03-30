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

### MCP Server

- MCP endpoint is hosted by the API at `https://localhost:8081/mcp` (or `http://localhost:8080/mcp`).
- Exposed read-only tools:
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

### MCP Setup For LM Studio

1. Start the API so the MCP endpoint is live:

```powershell
docker compose up -d api
```

2. In LM Studio, open `Program` -> `Install` -> `Edit mcp.json`.

3. Add this server entry (for local dev, use `http` to avoid cert trust issues):

```json
{
  "mcpServers": {
    "workoutlog": {
      "url": "http://localhost:8080/mcp"
    }
  }
}
```

4. Save `mcp.json`, then reload MCP servers (or restart LM Studio).

5. In the chat/tool UI, enable the `workoutlog` server and use tools like:
   - `search_exercises`
   - `get_exercise_by_id`
   - `get_muscles`
   - `search_muscles`
   - `get_muscles_by_group`
   - `get_equipments`
   - `search_equipments`
   - `get_equipment_by_name`
   - `search_workouts` (Development-only, requires `userId` input)
   - `get_workout_by_id` (Development-only, requires `userId` input)
   - `search_workout_blocks` (Development-only, requires `userId` input)
   - `get_workout_block_by_id` (Development-only, requires `userId` input)
   - `search_user_exercise_stats` (Development-only, requires `userId` input)

### Persistence Layout

- Feature-based persistence config lives under:
  - `Infrastructure/Persistence/Features/Auth/Configurations`
  - `Infrastructure/Persistence/Features/Equipments/Configurations`
  - `Infrastructure/Persistence/Features/Exercises/Configurations`
  - `Infrastructure/Persistence/Features/Muscles/Configurations`
  - `Infrastructure/Persistence/Features/WorkoutBlocks/Configurations`
  - `Infrastructure/Persistence/Features/Workouts/Configurations`
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

### Auth environment variables

- `AUTH_JWT_ISSUER`
- `AUTH_JWT_AUDIENCE`
- `AUTH_JWT_SECRET`
- `AUTH_JWT_ACCESS_TOKEN_MINUTES`
- `AUTH_REFRESH_COOKIE_NAME`
- `AUTH_REFRESH_DAYS`
- `AUTH_REFRESH_COOKIE_PATH`
- `AUTH_PASSWORD_RESET_INCLUDE_DEBUG_TOKEN`
- `AUTH_BOOTSTRAP_ADMIN_ENABLED`
- `AUTH_BOOTSTRAP_ADMIN_EMAIL`
- `AUTH_BOOTSTRAP_ADMIN_USERNAME`
- `AUTH_BOOTSTRAP_ADMIN_PASSWORD`

`AUTH_JWT_SECRET` must be replaced with a strong custom value. The API will fail startup in all environments (including Development) if the placeholder value is still used.

### Auth API quick test (PowerShell)

```powershell
$session = New-Object Microsoft.PowerShell.Commands.WebRequestSession

# Register
$registerBody = @'
{
  "username": "demo.user",
  "email": "demo.user@example.com",
  "password": "DemoPass123"
}
'@

$registerResponse = Invoke-RestMethod -Method Post `
  -Uri "https://localhost:8081/api/auth/register" `
  -SkipCertificateCheck `
  -WebSession $session `
  -ContentType "application/json" `
  -Body $registerBody
```

```powershell
# Login (identifier can be email or username)
$loginBody = @'
{
  "identifier": "demo.user@example.com",
  "password": "DemoPass123"
}
'@

$loginResponse = Invoke-RestMethod -Method Post `
  -Uri "https://localhost:8081/api/auth/login" `
  -SkipCertificateCheck `
  -WebSession $session `
  -ContentType "application/json" `
  -Body $loginBody
```

```powershell
# Me (requires bearer access token)
Invoke-RestMethod -Method Get `
  -Uri "https://localhost:8081/api/auth/me" `
  -SkipCertificateCheck `
  -Headers @{ Authorization = "Bearer $($loginResponse.accessToken)" }
```

```powershell
# Refresh access token (uses HttpOnly refresh cookie from $session)
$refreshResponse = Invoke-RestMethod -Method Post `
  -Uri "https://localhost:8081/api/auth/refresh" `
  -SkipCertificateCheck `
  -WebSession $session
```

```powershell
# Forgot password (debugResetToken is returned only when BOTH conditions are true:
# 1) ASPNETCORE_ENVIRONMENT=Development
# 2) AUTH_PASSWORD_RESET_INCLUDE_DEBUG_TOKEN=true)
$forgotBody = @'
{
  "identifier": "demo.user@example.com"
}
'@

$forgotResponse = Invoke-RestMethod -Method Post `
  -Uri "https://localhost:8081/api/auth/forgot-password" `
  -SkipCertificateCheck `
  -ContentType "application/json" `
  -Body $forgotBody
```

```powershell
# Reset password (use token from forgotResponse.debugResetToken)
$resetBody = @{
  identifier = "demo.user@example.com"
  token = $forgotResponse.debugResetToken
  newPassword = "DemoPass456"
} | ConvertTo-Json

Invoke-RestMethod -Method Post `
  -Uri "https://localhost:8081/api/auth/reset-password" `
  -SkipCertificateCheck `
  -ContentType "application/json" `
  -Body $resetBody
```

```powershell
# Logout (revokes refresh cookie token)
Invoke-RestMethod -Method Post `
  -Uri "https://localhost:8081/api/auth/logout" `
  -SkipCertificateCheck `
  -WebSession $session
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

### Equipment API quick test (PowerShell)

```powershell
# Create one equipment
$equipmentBody = @'
{
  "name": "barbell",
  "description": "Standard olympic barbell.",
  "howTo": "Use collars before loading plates.",
  "weightKg": 20
}
'@

Invoke-RestMethod -Method Post `
  -Uri "https://localhost:8081/api/equipments" `
  -SkipCertificateCheck `
  -ContentType "application/json" `
  -Body $equipmentBody
```

```powershell
# Bulk create equipments
$equipmentBulkBody = @'
[
  {
    "name": "dumbbell",
    "description": "Single-hand free weight.",
    "weightKg": 12.5
  },
  {
    "name": "kettlebell",
    "description": "Ballistic training weight.",
    "weightKg": 16
  }
]
'@

Invoke-RestMethod -Method Post `
  -Uri "https://localhost:8081/api/equipments/bulk" `
  -SkipCertificateCheck `
  -ContentType "application/json" `
  -Body $equipmentBulkBody
```

```powershell
# Get all equipments
Invoke-RestMethod -Method Get `
  -Uri "https://localhost:8081/api/equipments" `
  -SkipCertificateCheck
```

```powershell
# Search equipments by term
Invoke-RestMethod -Method Get `
  -Uri "https://localhost:8081/api/equipments/search/bell" `
  -SkipCertificateCheck
```

```powershell
# Get one equipment by unique name
Invoke-RestMethod -Method Get `
  -Uri "https://localhost:8081/api/equipments/barbell" `
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

### Workout API quick test (PowerShell)

```powershell
# requires a bearer token from /api/auth/login
$token = $loginResponse.accessToken
```

```powershell
# Create one workout
$createWorkoutBody = @'
{
  "feeling": "great",
  "durationInMinutes": 75,
  "mood": 8,
  "notes": "push day",
  "performedAtUtc": "2026-03-30T18:45:00Z",
  "entries": [
    {
      "exerciseId": 1,
      "orderNumber": 1,
      "repetitions": 12,
      "mood": 8,
      "weightUsedKg": 60,
      "rateOfPerceivedExertion": 7,
      "kcalBurned": 55
    }
  ]
}
'@

$createdWorkout = Invoke-RestMethod -Method Post `
  -Uri "https://localhost:8081/api/workouts" `
  -SkipCertificateCheck `
  -Headers @{ Authorization = "Bearer $token" } `
  -ContentType "application/json" `
  -Body $createWorkoutBody
```

```powershell
# Search workouts (paged)
$searchWorkoutsBody = @'
{
  "pageNumber": 1,
  "pageSize": 20,
  "search": "push",
  "minMood": 6,
  "maxMood": 10
}
'@

Invoke-RestMethod -Method Post `
  -Uri "https://localhost:8081/api/workouts/search" `
  -SkipCertificateCheck `
  -Headers @{ Authorization = "Bearer $token" } `
  -ContentType "application/json" `
  -Body $searchWorkoutsBody
```

```powershell
# Get recent workouts (last 48 hours)
Invoke-RestMethod -Method Get `
  -Uri "https://localhost:8081/api/workouts/recent?hours=48" `
  -SkipCertificateCheck `
  -Headers @{ Authorization = "Bearer $token" }
```

```powershell
# Bulk create workouts
$bulkWorkoutsBody = @'
[
  {
    "feeling": "good",
    "durationInMinutes": 45,
    "mood": 7,
    "entries": [
      {
        "exerciseId": 1,
        "orderNumber": 1,
        "repetitions": 10,
        "mood": 7,
        "weightUsedKg": 50,
        "rateOfPerceivedExertion": 6,
        "kcalBurned": 40
      }
    ]
  },
  {
    "feeling": "cardio",
    "durationInMinutes": 30,
    "mood": 6,
    "entries": [
      {
        "exerciseId": 2,
        "orderNumber": 1,
        "timerInSeconds": 1200,
        "mood": 6,
        "weightUsedKg": 0,
        "rateOfPerceivedExertion": 5,
        "kcalBurned": 120
      }
    ]
  }
]
'@

Invoke-RestMethod -Method Post `
  -Uri "https://localhost:8081/api/workouts/bulk" `
  -SkipCertificateCheck `
  -Headers @{ Authorization = "Bearer $token" } `
  -ContentType "application/json" `
  -Body $bulkWorkoutsBody
```

```powershell
# Update a workout (replace payload)
$updateWorkoutBody = @'
{
  "feeling": "solid",
  "durationInMinutes": 80,
  "mood": 9,
  "notes": "updated",
  "entries": [
    {
      "exerciseId": 1,
      "orderNumber": 1,
      "repetitions": 15,
      "mood": 9,
      "weightUsedKg": 62.5,
      "rateOfPerceivedExertion": 8,
      "kcalBurned": 60
    }
  ]
}
'@

Invoke-RestMethod -Method Put `
  -Uri "https://localhost:8081/api/workouts/$($createdWorkout.id)" `
  -SkipCertificateCheck `
  -Headers @{ Authorization = "Bearer $token" } `
  -ContentType "application/json" `
  -Body $updateWorkoutBody
```

```powershell
# Delete workout
Invoke-RestMethod -Method Delete `
  -Uri "https://localhost:8081/api/workouts/$($createdWorkout.id)" `
  -SkipCertificateCheck `
  -Headers @{ Authorization = "Bearer $token" }
```

### Workout Blocks API quick test (PowerShell)

```powershell
# Create workout block
$createBlockBody = @'
{
  "name": "upper strength block",
  "sets": 4,
  "restInSeconds": 90,
  "orderNumber": 1,
  "instructions": "controlled tempo",
  "blockExercises": [
    {
      "exerciseId": 1,
      "orderNumber": 1,
      "repetitions": 8
    },
    {
      "exerciseId": 2,
      "orderNumber": 2,
      "repetitions": 10
    }
  ]
}
'@

$createdBlock = Invoke-RestMethod -Method Post `
  -Uri "https://localhost:8081/api/workoutblocks" `
  -SkipCertificateCheck `
  -Headers @{ Authorization = "Bearer $token" } `
  -ContentType "application/json" `
  -Body $createBlockBody
```

```powershell
# Search workout blocks
$searchBlocksBody = @'
{
  "pageNumber": 1,
  "pageSize": 20,
  "search": "upper"
}
'@

Invoke-RestMethod -Method Post `
  -Uri "https://localhost:8081/api/workoutblocks/search" `
  -SkipCertificateCheck `
  -Headers @{ Authorization = "Bearer $token" } `
  -ContentType "application/json" `
  -Body $searchBlocksBody
```

```powershell
# Bulk create workout blocks
$bulkBlocksBody = @'
[
  {
    "name": "lower block",
    "sets": 3,
    "restInSeconds": 120,
    "orderNumber": 1,
    "blockExercises": [
      { "exerciseId": 1, "orderNumber": 1, "repetitions": 12 }
    ]
  },
  {
    "name": "conditioning block",
    "sets": 5,
    "restInSeconds": 60,
    "orderNumber": 2,
    "blockExercises": [
      { "exerciseId": 2, "orderNumber": 1, "timerInSeconds": 300 }
    ]
  }
]
'@

Invoke-RestMethod -Method Post `
  -Uri "https://localhost:8081/api/workoutblocks/bulk" `
  -SkipCertificateCheck `
  -Headers @{ Authorization = "Bearer $token" } `
  -ContentType "application/json" `
  -Body $bulkBlocksBody
```

```powershell
# Delete workout block
Invoke-RestMethod -Method Delete `
  -Uri "https://localhost:8081/api/workoutblocks/$($createdBlock.id)" `
  -SkipCertificateCheck `
  -Headers @{ Authorization = "Bearer $token" }
```

### User Exercise Stats API quick test (PowerShell)

```powershell
# Search user exercise stats
$searchStatsBody = @'
{
  "pageNumber": 1,
  "pageSize": 20,
  "search": "squat"
}
'@

Invoke-RestMethod -Method Post `
  -Uri "https://localhost:8081/api/userexercisestats/search" `
  -SkipCertificateCheck `
  -Headers @{ Authorization = "Bearer $token" } `
  -ContentType "application/json" `
  -Body $searchStatsBody
```

```powershell
# Get stat by exercise id
Invoke-RestMethod -Method Get `
  -Uri "https://localhost:8081/api/userexercisestats/1" `
  -SkipCertificateCheck `
  -Headers @{ Authorization = "Bearer $token" }
```
