# PowerShell Examples

These are lightweight local smoke-test snippets for common API flows.
For exhaustive endpoint coverage, use the Bruno or Postman collections in this directory.

## Prerequisites

Use the Docker HTTPS endpoint unless you intentionally run the API from Rider instead:

```powershell
$baseUrl = "https://localhost:8081"
$session = New-Object Microsoft.PowerShell.Commands.WebRequestSession
```

## Auth

```powershell
$registerBody = @'
{
  "username": "demo.user",
  "email": "demo.user@example.com",
  "password": "DemoPass123"
}
'@

$registerResponse = Invoke-RestMethod -Method Post `
  -Uri "$baseUrl/api/auth/register" `
  -SkipCertificateCheck `
  -WebSession $session `
  -ContentType "application/json" `
  -Body $registerBody
```

```powershell
$loginBody = @'
{
  "identifier": "demo.user@example.com",
  "password": "DemoPass123"
}
'@

$loginResponse = Invoke-RestMethod -Method Post `
  -Uri "$baseUrl/api/auth/login" `
  -SkipCertificateCheck `
  -WebSession $session `
  -ContentType "application/json" `
  -Body $loginBody

$token = $loginResponse.accessToken
$headers = @{ Authorization = "Bearer $token" }
```

```powershell
Invoke-RestMethod -Method Get `
  -Uri "$baseUrl/api/auth/me" `
  -SkipCertificateCheck `
  -Headers $headers
```

```powershell
$refreshResponse = Invoke-RestMethod -Method Post `
  -Uri "$baseUrl/api/auth/refresh" `
  -SkipCertificateCheck `
  -WebSession $session
```

```powershell
$forgotBody = @'
{
  "identifier": "demo.user@example.com"
}
'@

$forgotResponse = Invoke-RestMethod -Method Post `
  -Uri "$baseUrl/api/auth/forgot-password" `
  -SkipCertificateCheck `
  -ContentType "application/json" `
  -Body $forgotBody
```

```powershell
$resetBody = @{
  identifier = "demo.user@example.com"
  token = $forgotResponse.debugResetToken
  newPassword = "DemoPass456"
} | ConvertTo-Json

Invoke-RestMethod -Method Post `
  -Uri "$baseUrl/api/auth/reset-password" `
  -SkipCertificateCheck `
  -ContentType "application/json" `
  -Body $resetBody
```

```powershell
Invoke-RestMethod -Method Post `
  -Uri "$baseUrl/api/auth/logout" `
  -SkipCertificateCheck `
  -WebSession $session
```

## Catalog

```powershell
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
  -Uri "$baseUrl/api/muscles/bulk" `
  -SkipCertificateCheck `
  -ContentType "application/json" `
  -Body $musclesBody
```

```powershell
Invoke-RestMethod -Method Get `
  -Uri "$baseUrl/api/trainingtypes" `
  -SkipCertificateCheck
```

```powershell
$equipmentBody = @'
{
  "name": "barbell",
  "description": "Standard olympic barbell.",
  "howTo": "Use collars before loading plates.",
  "weightKg": 20
}
'@

Invoke-RestMethod -Method Post `
  -Uri "$baseUrl/api/equipments" `
  -SkipCertificateCheck `
  -ContentType "application/json" `
  -Body $equipmentBody
```

```powershell
$exerciseBody = @{
  name = "Push-up"
  description = "Upper body exercise"
  howTo = "Keep your core tight and lower until elbows hit about 90 degrees."
  difficulty = 2
  trainingTypes = @("strength", "hypertrophy")
  exerciseMuscles = @(
    @{ muscleName = "Gastrocnemius"; isPrimary = $false }
  )
  howTos = @(
    @{ name = "Video"; url = "https://example.com/push-up-video" }
  )
} | ConvertTo-Json -Depth 6

Invoke-RestMethod -Method Post `
  -Uri "$baseUrl/api/exercises" `
  -SkipCertificateCheck `
  -ContentType "application/json" `
  -Body $exerciseBody
```

```powershell
$searchExercisesBody = @{
  pageNumber = 1
  pageSize = 20
  search = "squat"
  difficulty = 3
  trainingTypeName = "strength"
  muscleGroup = "quadriceps"
  isPrimary = $true
} | ConvertTo-Json -Depth 4

Invoke-RestMethod -Method Post `
  -Uri "$baseUrl/api/exercises/search" `
  -SkipCertificateCheck `
  -ContentType "application/json" `
  -Body $searchExercisesBody
```

## Workouts

```powershell
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
  -Uri "$baseUrl/api/workouts" `
  -SkipCertificateCheck `
  -Headers $headers `
  -ContentType "application/json" `
  -Body $createWorkoutBody
```

```powershell
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
  -Uri "$baseUrl/api/workouts/search" `
  -SkipCertificateCheck `
  -Headers $headers `
  -ContentType "application/json" `
  -Body $searchWorkoutsBody
```

```powershell
Invoke-RestMethod -Method Get `
  -Uri "$baseUrl/api/workouts/recent?hours=48" `
  -SkipCertificateCheck `
  -Headers $headers
```

```powershell
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
  -Uri "$baseUrl/api/workouts/$($createdWorkout.id)" `
  -SkipCertificateCheck `
  -Headers $headers `
  -ContentType "application/json" `
  -Body $updateWorkoutBody
```

```powershell
Invoke-RestMethod -Method Delete `
  -Uri "$baseUrl/api/workouts/$($createdWorkout.id)" `
  -SkipCertificateCheck `
  -Headers $headers
```

## Workout Blocks

```powershell
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
  -Uri "$baseUrl/api/workoutblocks" `
  -SkipCertificateCheck `
  -Headers $headers `
  -ContentType "application/json" `
  -Body $createBlockBody
```

```powershell
$searchBlocksBody = @'
{
  "pageNumber": 1,
  "pageSize": 20,
  "search": "upper"
}
'@

Invoke-RestMethod -Method Post `
  -Uri "$baseUrl/api/workoutblocks/search" `
  -SkipCertificateCheck `
  -Headers $headers `
  -ContentType "application/json" `
  -Body $searchBlocksBody
```

```powershell
Invoke-RestMethod -Method Delete `
  -Uri "$baseUrl/api/workoutblocks/$($createdBlock.id)" `
  -SkipCertificateCheck `
  -Headers $headers
```

## Plans

```powershell
$searchPlansBody = @'
{
  "status": "published",
  "search": "savage",
  "pageNumber": 1,
  "pageSize": 20
}
'@

Invoke-RestMethod -Method Post `
  -Uri "$baseUrl/api/plans/search" `
  -SkipCertificateCheck `
  -Headers $headers `
  -ContentType "application/json" `
  -Body $searchPlansBody
```

```powershell
$enrollBody = @'
{
  "startedAtUtc": "2026-04-01T06:00:00Z",
  "timeZoneId": "Asia/Amman"
}
'@

Invoke-RestMethod -Method Post `
  -Uri "$baseUrl/api/plans/1/enroll" `
  -SkipCertificateCheck `
  -Headers $headers `
  -ContentType "application/json" `
  -Body $enrollBody
```

```powershell
$searchAgendaBody = @'
{
  "fromLocalDate": "2026-04-01",
  "toLocalDate": "2026-04-14"
}
'@

Invoke-RestMethod -Method Post `
  -Uri "$baseUrl/api/myplans/agenda/search" `
  -SkipCertificateCheck `
  -Headers $headers `
  -ContentType "application/json" `
  -Body $searchAgendaBody
```

## User Exercise Stats

```powershell
Invoke-RestMethod -Method Get `
  -Uri "$baseUrl/api/userexercisestats?pageNumber=1&pageSize=20&search=squat&exerciseId=1" `
  -SkipCertificateCheck `
  -Headers $headers
```

```powershell
$searchStatsBody = @'
{
  "pageNumber": 1,
  "pageSize": 20,
  "search": "squat"
}
'@

Invoke-RestMethod -Method Post `
  -Uri "$baseUrl/api/userexercisestats/search" `
  -SkipCertificateCheck `
  -Headers $headers `
  -ContentType "application/json" `
  -Body $searchStatsBody
```

## Measurements

```powershell
$measurementBody = @'
{
  "hip": 95,
  "chest": 102,
  "waistOnBelly": 88,
  "waistUnderBelly": 82,
  "minerals": 3.5,
  "protein": 12.8,
  "totalBodyWater": 40.2,
  "bodyFatMass": 18.7
}
'@

Invoke-RestMethod -Method Post `
  -Uri "$baseUrl/api/measurements" `
  -SkipCertificateCheck `
  -Headers $headers `
  -ContentType "application/json" `
  -Body $measurementBody
```

```powershell
Invoke-RestMethod -Method Get `
  -Uri "$baseUrl/api/measurements" `
  -SkipCertificateCheck `
  -Headers $headers
```
